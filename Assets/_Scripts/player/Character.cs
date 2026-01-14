using System;
using Animancer;
using core.Managers;
using core.player;
using core.Vehicles;
using ExileSurvival.Networking.Core;
using ExileSurvival.Networking.Data;
using ExileSurvival.Networking.Entities;
using KinematicCharacterController;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using LiteNetLib;

public class Character : PredictedEntity, ICharacterController {
    #region Character Enums, Properties, and Fields

    public enum CharacterState : byte {
        Idle = 0,
        Strafe = 1,
        Crouched = 2,
        Running = 3,
        Jumping = 4,
        InVehicleDriver = 5,
        InVehiclePassenger = 6,
        Falling = 7,
        HandledByStateMachine = 8
    }

    public                        CharacterCam            orbitCamera;
    public                        Transform               cameraTarget;
    public                        KinematicCharacterMotor motor;
    public                        NavMeshAgent            navMeshAgent;
    [Required("Required")] public PlayerStatus            PlayerStatus;

    private bool useInputForRotation = true;

    [FoldoutGroup("Animation Parameters")] public PlayerAnimator pa;

    #region Movement

    [FoldoutGroup("Movement")] [SerializeField]                       private float MaxStableMoveSpeed      = 6;
    [FoldoutGroup("Movement")]                                        public  float orientationSharpness    = 10;
    [FoldoutGroup("Movement")]                                        public  float StableMovementSharpness = 15;
    [FormerlySerializedAs("WALKING_SPEED"), FoldoutGroup("Movement")] public  float STRAFE_SPEED            = 3;
    [FoldoutGroup("Movement")] [SerializeField]                       private float RUNNING_SPEED           = 5;
    [FoldoutGroup("Movement")] [SerializeField]                       private float CROUCH_SPEED            = 2;

    [FoldoutGroup("Movement/Air")] public float airOrientationSharpness = 5f;
    [FoldoutGroup("Movement/Air")] public float drag                    = 0.1f;

    [FoldoutGroup("Movement/Jumping")]                  public  float RunningjumpForce = 10f;
    [FoldoutGroup("Movement/Jumping")]                  public  float IdlejumpForce    = 10f;
    [FoldoutGroup("Movement/Jumping")] [SerializeField] private bool  isJumping;
    [FoldoutGroup("Movement/Jumping")]                  public  float coyoteTime     = 0.15f;
    [FoldoutGroup("Movement/Jumping")]                  public  float jumpBufferTime = 0.1f;
    [FoldoutGroup("Movement/Jumping")]                  public  float maxJumpTime    = 5f;

    [FoldoutGroup("Vehicle")] public CarController playerCar;
    [FoldoutGroup("Vehicle")] public bool          invehicle;

    [FoldoutGroup("Gravity")]                  public  float  initialGravity     = -9.81f;
    [FoldoutGroup("Gravity")]                  public  float  maxGravity         = -30f;
    [FoldoutGroup("Gravity")]                  public  float  gravityBuildUpTime = 0.5f;
    [FoldoutGroup("Gravity")] [SerializeField] private float  FallTime;
    [FoldoutGroup("Gravity")] [SerializeField] private double fallThreshold = 1.1f;

    #endregion

    [BoxGroup("Current Character state")] [SerializeField] private CharacterState _currentCharacterState = CharacterState.Idle;

    #endregion

    #region Prediction Data Structures

    [FoldoutGroup("Reconciliation")] public float positionSnapThreshold = 3f; // Snap if error > this
    [FoldoutGroup("Reconciliation")] public float ReconsileSmoothTime   = 25.5f;
    
    // Smoothing state
    private Vector3    _reconcilePositionVelocity;
    public  Quaternion _replicatedCameraRotation = Quaternion.identity;

    #endregion

    public CharacterState CurrentCharacterState {
        get => _currentCharacterState;
        set {
            if (_currentCharacterState == value) return;
            if (value == CharacterState.Falling) is_Falling = true;
            _currentCharacterState = value;
            UpdateAnimation();
        }
    }

    #region Events

    public event Action<CharacterState> OnCharacterStateChanged;
    public event Action<Collider>       onCharacterDetectCollider;

    #endregion

    #region Movement Input

    private Vector3 _internalVelocityAdd = Vector3.zero;
    public  float   Steeringsmoothness   = 5f;
    //private Vector3 _lookInputVector;
    private Vector3 _moveInputVector;

    [FoldoutGroup("Movement/Transitions")] public float movementTransitionSpeed = 8f;
    private float targetMoveSpeed;
    private float speedChangeVelocity;

    #endregion

    #region Gravity and Falling

    private float currentGravity;
    private float fallStartTime;
    private float gravityVelocity;
    private bool  is_Falling;

    #endregion

    #region Jumping

    private float jumpTime;
    private bool  jumpInput;
    private float lastGroundedTime;
    private float lastJumpPressedTime;

    #endregion

    #region Vehicle

    public CarSeat? playerCarSeat;

    #endregion

    #region Misc

    public bool                 isFrozen;
    public AnimationLockManager animationLockManager;

    #region Prediction Fields

// Store the last input we gathered
    private float      _lastHorizontal;
    private float      _lastVertical;
    private bool       _lastJumpInput;
    private bool       _crouchTogglePressed; // Track if crouch was pressed this frame

    #endregion

    private void InitializeAnimationLockManager() {
        animationLockManager = new AnimationLockManager(this);
    }

    #endregion

    private void Awake() {
        navMeshAgent.transform.SetParent(transform, false);
        motor.CharacterController = this;
        InitializeAnimationLockManager();
    }

    protected override void Start() {
        base.Start();
        currentGravity = initialGravity;
        if (ServerManager.Instance != null)
        {
             // OnStartClient logic moved here or called explicitly
             SetupPlayerCharacter();
        }
    }

    // --- Networking Implementation ---

    public override void OnServerInput(PlayerInputPacket input)
    {
        // Server authority: Apply input to movement
        // TODO: Validate input here (anti-cheat)
        ProcessInput(input);
        
        // Broadcast State
        var state = new PlayerStatePacket
        {
            PlayerId = OwnerId,
            Tick = input.Tick,
            Position = motor.TransientPosition,
            Rotation = motor.TransientRotation,
            Velocity = motor.Velocity,
            StateId = (byte)_currentCharacterState
        };
        
        // Send state back to client (and others)
        if (ServerManager.Instance != null)
        {
            ServerManager.Instance.BroadcastToAll(state, DeliveryMethod.Unreliable);
        }
    }

    public override void OnClientState(PlayerStatePacket state)
    {
        if (IsLocalPlayer)
        {
            // Reconciliation Logic
            // 1. Find the input corresponding to this state's tick
            // 2. Compare local state at that tick with server state
            // 3. If error > threshold, snap and replay inputs from Tick+1 to CurrentTick
            
             float positionError = Vector3.Distance(state.Position, motor.TransientPosition); // Simplified check, needs history buffer lookup
             if (positionError > positionSnapThreshold)
             {
                 motor.SetPosition(state.Position, true);
                 motor.SetRotation(state.Rotation);
                 // Replay inputs...
             }
        }
        else
        {
            // Proxy Interpolation
            // Simply smooth move to the new state
            motor.SetPosition(state.Position, true);
            motor.SetRotation(state.Rotation);
            CurrentCharacterState = (CharacterState)state.StateId;
        }
    }

    private void FixedUpdate()
    {
        if (IsOwnedByServer)
        {
            // Server logic handled by input packets usually, or AI
        }
        else if (IsLocalPlayer)
        {
            // Client Prediction Loop
            var input = GatherInputForPrediction();
            ProcessInput(input); // Apply locally immediately
            
            // Send to server
            if (ClientManager.Instance != null)
            {
                ClientManager.Instance.SendPacket(input, DeliveryMethod.ReliableOrdered);
            }
        }
    }

    // --- Input & Movement Logic ---

    private PlayerInputPacket GatherInputForPrediction() {
        var input = new PlayerInputPacket();
        if (ClientManager.Instance != null)
        {
            input.Tick = ClientManager.Instance.CurrentTick;
        }
        
        if (animationLockManager != null && animationLockManager.ShouldBlockInput()) {
            return input;
        }

        // Handle vehicle input separately (not predicted for now)
        if (CurrentCharacterState == CharacterState.InVehicleDriver) {
             // GatherVehicleInput(); // TODO: Add vehicle input packet
             return input;
        }

        // Get joystick input
        input.Horizontal = UiManager.Instance.ultimateJoystick != null
                              ? UiManager.Instance.ultimateJoystick.HorizontalAxis
                              : 0f;

        input.Vertical = UiManager.Instance.ultimateJoystick != null
                            ? UiManager.Instance.ultimateJoystick.VerticalAxis
                            : 0f;

#if UNITY_EDITOR || !UNITY_ANDROID
        // Keyboard override for testing
        if (Input.GetKey(KeyCode.W)) input.Vertical   = 1f;
        if (Input.GetKey(KeyCode.S)) input.Vertical   = -1f;
        if (Input.GetKey(KeyCode.A)) input.Horizontal = -1f;
        if (Input.GetKey(KeyCode.D)) input.Horizontal = 1f;

        if (Input.GetKeyDown(KeyCode.Space)) input.Jump = true;
        if (Input.GetKeyDown(KeyCode.C)) input.Crouch = true;
#endif

        if (_crouchTogglePressed)
        {
            input.Crouch = true;
            _crouchTogglePressed = false;
        }

        // Camera Yaw for rotation
        if (orbitCamera != null)
             input.CameraYaw = orbitCamera.transform.eulerAngles.y;

        return input;
    }

    private void ProcessInput(PlayerInputPacket input) {
        // Handle jump input
        if (input.Jump) {
            SetJumpInput(true);
        }

        // Handle crouch toggle
        if (input.Crouch) {
            ToggleCrouch();
        }

        // Reconstruct rotation from Yaw
        _replicatedCameraRotation = Quaternion.Euler(0, input.CameraYaw, 0);

        // Calculate the camera's forward and right vectors
        var cameraForward = Vector3.ProjectOnPlane(_replicatedCameraRotation * Vector3.forward, motor.CharacterUp)
                                   .normalized;
        var cameraRight = Vector3.Cross(Vector3.up, cameraForward)
                                 .normalized;

        // Calculate movement vector relative to camera
        _moveInputVector = (cameraForward * input.Vertical + cameraRight * input.Horizontal).normalized;
        _moveInputVector = Vector3.ClampMagnitude(_moveInputVector, 1f);

        // Set look direction to camera forward (for rotation)
        //_lookInputVector = cameraForward;

        // Update movement state based on input magnitude
        if (!animationLockManager.ShouldBlockInput()) {
            UpdateMovementState(new Vector2(input.Horizontal, input.Vertical));
        }
        
        // Simulate Physics Step (Manually calling KCC update/simulate could go here if separating from FixedUpdate)
    }

    #region Input Handling

    /// <summary>
    /// Gathers vehicle control input
    /// </summary>
    //private void GatherVehicleInput() {
    //    if (playerCar == null) return;
    //
    //    playerCar.controllerV4.fuelInput  = UiManager.Instance.GetInput(UiManager.Instance.gasButton);
    //    playerCar.controllerV4.brakeInput = UiManager.Instance.GetInput(UiManager.Instance.brakeButton);
    //    playerCar.controllerV4.steerInput = -UiManager.Instance.GetInput(UiManager.Instance.leftButton) + UiManager.Instance.GetInput(UiManager.Instance.rightButton);
    //
    //#if UNITY_EDITOR || !UNITY_ANDROID
    //    // Keyboard override for vehicle
    //    if (Input.GetKey(KeyCode.Space)) playerCar.controllerV4.handbrakeInput = 1f;
    //    else playerCar.controllerV4.handbrakeInput                             = 0f;
    //    if (Input.GetKey(KeyCode.Z)) playerCar.controllerV4.fuelInput  = 1f;
    //    if (Input.GetKey(KeyCode.S)) playerCar.controllerV4.brakeInput = 1;
    //    if (Input.GetKey(KeyCode.D)) playerCar.controllerV4.steerInput = 1;
    //    if (Input.GetKey(KeyCode.Q)) playerCar.controllerV4.steerInput = -1;
    //#endif
    //
    //    UpdateVehicleAnimation();
    //}

    #endregion

    #region Character Controller Interface Implementation

    public void BeforeCharacterUpdate(float deltaTime) { }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime) {
        // Early exit if no camera rotation data
        if (_replicatedCameraRotation == Quaternion.identity) {
            currentRotation = Quaternion.LookRotation(motor.CharacterForward, motor.CharacterUp);
            return;
        }

        // Calculate camera forward direction from replicated data
        Vector3 cameraForward = _replicatedCameraRotation * Vector3.forward;
        cameraForward.y = 0f;
        cameraForward   = cameraForward.normalized;

        // Determine rotation based on state and input
        Vector3 targetDirection;

        if (useInputForRotation && _moveInputVector.sqrMagnitude > 0.01f) {
            // Rotate towards movement direction (running/moving)
            targetDirection   = _moveInputVector;
            targetDirection.y = 0f;
            targetDirection   = targetDirection.normalized;
        }
        else {
            // Rotate towards camera forward (idle/strafing)
            targetDirection = cameraForward;
        }

        // Apply appropriate smoothing based on grounding
        float sharpness = motor.GroundingStatus.IsStableOnGround
                              ? orientationSharpness
                              : airOrientationSharpness;

        Vector3 smoothedDirection = Vector3.Slerp(motor.CharacterForward, targetDirection, 1 - Mathf.Exp(-sharpness * deltaTime))
                                           .normalized;

        currentRotation = Quaternion.LookRotation(smoothedDirection, motor.CharacterUp);
    }

    public void AddVelocity(Vector3 velocity) {
        _internalVelocityAdd += velocity;
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime) {
        if (motor.GroundingStatus.IsStableOnGround) {
            HandleGroundedMovement(ref currentVelocity, deltaTime);
        }
        else {
            HandleAirborneMovement(ref currentVelocity, deltaTime);
        }

        if (isFrozen) currentVelocity = Vector3.zero;
        // Take into account additive velocity
        if (_internalVelocityAdd.sqrMagnitude > 0f) {
            currentVelocity      += _internalVelocityAdd;
            _internalVelocityAdd =  Vector3.zero;
        }
    }

    public void AfterCharacterUpdate(float deltaTime) {
        HandleLanding();

        // Check if vehicle is still in range
        if (playerCar != null) {
            if (Vector3.Distance(playerCar.transform.position, motor.transform.position) > 5f) {
                playerCar = null;
                UiManager.Instance.ShowVehicleEnterExitButtons(false, false, false);
            }
        }
    }

    public bool IsColliderValidForCollisions(Collider coll) => true;

    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }

    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) {
        if (hitCollider.CompareTag("Vehicle")) {
            var nearbyVehicle = hitCollider.GetComponentInParent<CarController>();
            if (nearbyVehicle is null) return;

            UiManager.Instance.ShowVehicleEnterExitButtons(true,
                                                           nearbyVehicle.driverSeat.used,
                                                           nearbyVehicle.GetFreeCarSeats()
                                                                        .Count >
                                                           0);

            if (playerCar is null || playerCar != nearbyVehicle) {
                playerCar = nearbyVehicle;
            }
        }

        //check for interactable objects

        int miningLayer = LayerMask.NameToLayer("Interactable");

        if (hitCollider.gameObject.layer == miningLayer) {
            onCharacterDetectCollider?.Invoke(hitCollider);
        }
    }

    public void PostGroundingUpdate(float deltaTime) {
        if (motor.GroundingStatus.IsStableOnGround) {
            ResetJump();
        }
    }

    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) { }

    public void OnDiscreteCollisionDetected(Collider hitCollider) { }

    #endregion

    #region Movement State Management

    private void UpdateMovementState(Vector2 directions) {
        // Handle falling (highest priority)
        if (!motor.GroundingStatus.IsStableOnGround && !isJumping) {
            if (CurrentCharacterState != CharacterState.Falling) {
                CurrentCharacterState = CharacterState.Falling;
            }

            return;
        }

        // Don't change state while jumping
        if (isJumping) return;

        // Don't change state while in vehicle
        if (CurrentCharacterState is CharacterState.InVehicleDriver or CharacterState.InVehiclePassenger)
            return;

        // Handle crouched state separately
        if (CurrentCharacterState == CharacterState.Crouched) {
            if (directions.magnitude > 0.2f) {
                MaxStableMoveSpeed = CROUCH_SPEED;
                pa.UpdateCrouchAnimation(new Vector2(directions.x, directions.y));
                useInputForRotation = directions.y > 0.4f;
            }
            else {
                MaxStableMoveSpeed  = 0f;
                useInputForRotation = false;
                pa.resetCrouchAnimation();
                pa.PlayAnimation(pa.crouch_idle, 0.3f);
            }

            return;
        }

        // Movement state determination
        float inputMagnitude = directions.magnitude;

        // IDLE: No significant input
        if (inputMagnitude < 0.1f) {
            if (CurrentCharacterState != CharacterState.Idle) {
                SetState(CharacterState.Idle);
            }

            return;
        }

        // RUNNING: Strong forward input
        if (directions.y > 0.5f && inputMagnitude > 0.2f) {
            if (CurrentCharacterState != CharacterState.Running) {
                SetState(CharacterState.Running);
            }

            return;
        }

        // STRAFE: Any other movement (walking, side movement, backward)
        if (inputMagnitude > 0.1f) {
            if (CurrentCharacterState != CharacterState.Strafe) {
                SetState(CharacterState.Strafe);
            }

            // Update strafe animation every frame when in strafe state
            if (!isJumping) {
                pa.UpdateStrafeAnimation(new Vector2(directions.x, directions.y));
            }

            // Only use input for rotation when moving forward significantly
            useInputForRotation = directions.y > 0.4f;
        }
    }

    private void SetState(CharacterState newState) {
        if (CurrentCharacterState == newState) {
            Debug.LogError("calling setstate on same state");
            return;
        }

        HandleOldStateExitActions(CurrentCharacterState);

        switch (newState) {
            case CharacterState.Idle:
                MaxStableMoveSpeed = 0f;
                break;
            case CharacterState.Strafe:
                MaxStableMoveSpeed = STRAFE_SPEED;
                break;
            case CharacterState.Running:
                MaxStableMoveSpeed  = RUNNING_SPEED;
                useInputForRotation = true;
                break;
            case CharacterState.Crouched:
                MaxStableMoveSpeed = CROUCH_SPEED;
                break;
            case CharacterState.InVehicleDriver:
            case CharacterState.InVehiclePassenger:
                invehicle = true;
                break;
        }

        CurrentCharacterState = newState;
        OnCharacterStateChanged?.Invoke(newState);
    }

    private void HandleOldStateExitActions(CharacterState oldState) {
        switch (oldState) {
            case CharacterState.Strafe:
                pa.ResetStrafeAnimation();
                break;
            case CharacterState.Running:
                useInputForRotation = false;
                break;
        }
    }

    #endregion

    #region Movement Logic

    private void HandleGroundedMovement(ref Vector3 currentVelocity, float deltaTime) {
        if (jumpInput && CanJump()) {
            InitiateJump(ref currentVelocity);
        }
        else {
            lastGroundedTime = Time.time;
            currentVelocity  = ReorientVelocityOnSlope(currentVelocity);
            Vector3 targetMovementVelocity = CalculateTargetGroundVelocity();
            currentVelocity = SmoothVelocity(currentVelocity, targetMovementVelocity, StableMovementSharpness, deltaTime);
        }
    }

    private void HandleAirborneMovement(ref Vector3 currentVelocity, float deltaTime) {
        if (isJumping && jumpTime < 0.2f) {
            currentVelocity.y = Mathf.Max(currentVelocity.y,
                                          0.8f *
                                          (CurrentCharacterState == CharacterState.Idle
                                               ? IdlejumpForce
                                               : RunningjumpForce));
        }
        else {
            ApplySmoothGravity(ref currentVelocity, deltaTime);
        }

        if (isJumping) {
            UpdateJumpState(deltaTime);
        }
        else {
            CheckForFalling();
        }

        currentVelocity *= 1f / (1f + drag * deltaTime);
    }

    private Vector3 ReorientVelocityOnSlope(Vector3 velocity) {
        return motor.GetDirectionTangentToSurface(velocity, motor.GroundingStatus.GroundNormal) * velocity.magnitude;
    }

    private Vector3 CalculateTargetGroundVelocity() {
        Vector3 inputRight = Vector3.Cross(_moveInputVector, motor.CharacterUp);
        Vector3 reorientedInput = Vector3.Cross(motor.GroundingStatus.GroundNormal, inputRight)
                                         .normalized *
                                  _moveInputVector.magnitude;
        return reorientedInput * MaxStableMoveSpeed;
    }

    private Vector3 SmoothVelocity(Vector3 currentVelocity, Vector3 targetVelocity, float smoothing, float deltaTime) {
        return Vector3.Lerp(currentVelocity, targetVelocity, 1 - Mathf.Exp(-smoothing * deltaTime));
    }

    private void ApplySmoothGravity(ref Vector3 currentVelocity, float deltaTime) {
        currentGravity    =  Mathf.SmoothDamp(currentGravity, maxGravity, ref gravityVelocity, gravityBuildUpTime);
        currentVelocity.y += currentGravity * deltaTime;
    }

    #endregion

    #region Jumping

    public void SetJumpInput(bool newJumpState) {
        if (newJumpState) lastJumpPressedTime = Time.time;
        jumpInput = newJumpState;
    }

    private bool CanJump() {
        var timeSinceGrounded    = Time.time - lastGroundedTime;
        var timeSinceJumpPressed = Time.time - lastJumpPressedTime;
        return (timeSinceGrounded < coyoteTime || motor.GroundingStatus.IsStableOnGround) && timeSinceJumpPressed < jumpBufferTime && !isJumping;
    }

    private void InitiateJump(ref Vector3 currentVelocity) {
        if (CurrentCharacterState == CharacterState.Crouched) {
            ToggleCrouch();
            jumpInput = false;
            return;
        }

        AnimancerState jumpingAnimationState = null;
        if (_currentCharacterState == CharacterState.Jumping) return;

        if (_currentCharacterState == CharacterState.Idle) {
            pa.PlayAnimation(pa.anim_jump_inplace, 0.2f, FadeMode.FromStart);
        }
        else {
            jumpingAnimationState = pa.PlayAnimation(pa.anim_jump_start, 0.2f, FadeMode.FromStart);
        }

        if (jumpingAnimationState != null) {
            jumpingAnimationState.Events(this)
                                 .OnEnd = () => {
                isJumping = false;
                CheckForFalling();
            };
        }

        isJumping             = true;
        jumpTime              = 0f;
        CurrentCharacterState = CharacterState.Jumping;
        ApplyJumpForce(ref currentVelocity);
    }

    private void ApplyJumpForce(ref Vector3 currentVelocity) {
        float jumpForce = (CurrentCharacterState == CharacterState.Idle)
                              ? IdlejumpForce
                              : RunningjumpForce;
        currentVelocity.y = Mathf.Max(currentVelocity.y + jumpForce, jumpForce);
        motor.ForceUnground();
    }

    private void UpdateJumpState(float deltaTime) {
        jumpTime += deltaTime;

        if (jumpTime >= maxJumpTime) {
            if (_currentCharacterState != CharacterState.Falling) {
                isJumping             = false;
                is_Falling            = true;
                fallStartTime         = Time.time;
                FallTime              = 0f;
                CurrentCharacterState = CharacterState.Falling;
            }
        }
        else {
            if (_currentCharacterState != CharacterState.Jumping) {
                CurrentCharacterState = CharacterState.Jumping;
            }
        }
    }

    private void ResetJump() {
        if (isJumping) {
            isJumping = false;
        }
    }

    #endregion

    #region Falling

    private void CheckForFalling() {
        if (CurrentCharacterState == CharacterState.Falling) {
            FallTime += Time.deltaTime;
            if (FallTime >= 0.5f && FallTime < 1f && !pa.isPlayingAnimation(pa.anim_falling_second)) {
                pa.PlayAnimation(pa.anim_falling_second, 0.3f);
            }

            if (FallTime > 1f) {
                pa.PlayAnimation(pa.anim_falling_loop, 0.5f);
            }

            return;
        }

        if (isJumping) return;

        if (!is_Falling) {
            fallStartTime = Time.time;
            is_Falling    = true;
        }
        else if (Time.time - fallStartTime >= fallThreshold) {
            CurrentCharacterState = CharacterState.Falling;
        }
    }

    private void HandleLanding() {
        if (motor.GroundingStatus.IsStableOnGround) {
            if (is_Falling || CurrentCharacterState == CharacterState.Falling) {
                AnimancerState landingState = null;

                switch (FallTime) {
                    case > 0.8f:
                        if (!pa.isPlayingAnimation(pa.anim_land_med)) {
                            landingState = pa.PlayAnimation(pa.anim_land_med, 0.2f);
                            animationLockManager.LockForAnimation(landingState,
                                                                  AnimationLockManager.AnimationLockType.HardLanding,
                                                                  onUnlock: () => {
                                                                      CurrentCharacterState = CharacterState.Idle;
                                                                      pa.PlayAnimation(pa.anim_idle);
                                                                  });
                        }

                        break;

                    case > 0.2f:
                        if (!pa.isPlayingAnimation(pa.anim_land_low)) {
                            landingState = pa.PlayAnimation(pa.anim_land_low, 0.2f);
                            animationLockManager.LockForAnimation(landingState,
                                                                  AnimationLockManager.AnimationLockType.HardLanding,
                                                                  onUnlock: () => {
                                                                      CurrentCharacterState = CharacterState.Idle;
                                                                      pa.PlayAnimation(pa.anim_idle);
                                                                  });
                        }

                        break;
                }

                is_Falling = false;
            }

            FallTime = 0f;
        }
    }

    #endregion

    #region Vehicle

    private void OnCardoorDriverClicked() {
        if (playerCar != null) {
            playerCarSeat = playerCar.SetPlayerInVehicle(this, pa, true);
            if (playerCarSeat != null) {
                CurrentCharacterState = playerCarSeat.Value.isDriver
                                            ? CharacterState.InVehicleDriver
                                            : CharacterState.InVehiclePassenger;
            }
        }
    }

    private void OnCardoorPassangerClicked() {
        if (playerCar != null) {
            playerCarSeat = playerCar.SetPlayerInVehicle(this, pa);
            pa.PlayAnimation(pa.anim_Vehicle_Passenger_sit);
        }
    }

    private void OnCardoorExitClicked() {
        if (playerCar != null) {
            playerCar.RemovePlayerFromVehicle(this);
        }
    }

    public void TogglePlayerCollisionDetection(bool enable) {
        if (!enable) {
            motor.SetCapsuleCollisionsActivation(false);
            motor.SetGroundSolvingActivation(false);
            motor.Capsule.enabled = false;
        }
        else {
            motor.SetCapsuleCollisionsActivation(true);
            motor.SetGroundSolvingActivation(true);
            motor.Capsule.enabled = true;
        }
    }

    //private void ExitVehicle() {
    //    if (playerCar is null || playerCarSeat is null) return;
    //
    //    motor.transform.parent     = null;
    //    motor.transform.localScale = Vector3.one;
    //    motor.SetPositionAndRotation(playerCarSeat.Value.exitpos.position, Quaternion.identity);
    //    TogglePlayerCollisionDetection(true);
    //    motor.enabled = true;
    //
    //    if (!playerCar.controllerV4.engineRunning)
    //        StopCoroutine(playerCar.controllerV4.StartEngineDelayed());
    //
    //    if (playerCarSeat.Value.isDriver) {
    //        playerCar.controllerV4.KillEngine();
    //        playerCar.driverSeat.used = false;
    //    }
    //
    //    playerCar.controllerV4.handbrakeInput = 1f;
    //    playerCarSeat.Value.FreetheSeat();
    //    playerCarSeat = null;
    //
    //    navMeshAgent.enabled = false;
    //    playerCar.fx_CloseDoorSound();
    //
    //    if (orbitCamera.IgnoredColliders.Contains(playerCar.VehicleCollider))
    //        orbitCamera.IgnoredColliders.Remove(playerCar.VehicleCollider);
    //
    //    UiManager.Instance.ShowControllerPage();
    //    pa.FadeOutActionLayer();
    //
    //    invehicle             = false;
    //    CurrentCharacterState = CharacterState.Idle;
    //}

    private void UpdateVehicleAnimation() {
        if (playerCar != null && playerCar.controllerV4 != null) {
            bool isReversing = playerCar.controllerV4.direction == -1 && playerCar.controllerV4.speed > 5f;

            var newsteer = Mathf.Clamp(playerCar.controllerV4.FrontLeftWheelCollider.RotationValue, -1, 1);
            pa.UpdateVehicleSteering_Mixer(newsteer, Steeringsmoothness);

            if (isReversing) {
                if (!pa.isActionPlaying(pa.ACTION_DRIVER_REVERSE))
                    pa.PlayActionAnimation(pa.ACTION_DRIVER_REVERSE);
            }
        }
    }

    private void HandleVehicleAnimation(bool isDriver) {
        if (playerCar == null) return;
        if (isDriver)
            pa.UpdateVehicleSteering_Mixer(0, Steeringsmoothness);
        else
            pa.PlayAnimation(pa.anim_Vehicle_Passenger_sit);
    }

    #endregion

    #region Animation

    private void UpdateAnimation() {
        switch (CurrentCharacterState) {
            case CharacterState.Idle:
                pa.PlayAnimation(pa.anim_idle, 0.4f);
                break;
            case CharacterState.Strafe:
                pa.UpdateStrafeAnimation(new Vector2(_lastHorizontal, _lastVertical));
                break;
            case CharacterState.Running:
                pa.PlayAnimation(pa.anim_run, 0.4f);
                break;
            case CharacterState.Jumping:
                // Jump animations handled in InitiateJump
                break;
            case CharacterState.Falling:
                pa.PlayAnimation(pa.anim_falling_frist, 0.6f);
                break;
            case CharacterState.InVehicleDriver:
                HandleVehicleAnimation(true);
                break;
            case CharacterState.InVehiclePassenger:
                HandleVehicleAnimation(false);
                break;
        }
    }

    private void ToggleCrouch() {
        if (_currentCharacterState == CharacterState.Crouched) {
            pa.resetCrouchAnimation();
            SetState(CharacterState.Idle);
        }
        else {
            SetState(CharacterState.Crouched);
        }
    }

    #endregion

    #region Network Setup

    private void SetupPlayerCharacter() {
        if (!IsLocalPlayer) {
            if (orbitCamera != null) {
                Destroy(orbitCamera.gameObject);
            }
        }

        else {
            GameManager.Instance.character = this;
            gameObject.name                = "LOCAL_PLAYER";

            if (orbitCamera != null) {
                orbitCamera.gameObject.SetActive(true);
                orbitCamera.transform.parent = null;
                orbitCamera.gameObject.name  = "Local Player Cam";
            }

            // Subscribe to UI events
            UiManager.Instance.OnCardoorDriverButtonClicked    += OnCardoorDriverClicked;
            UiManager.Instance.OnCardoorPassangerButtonClicked += OnCardoorPassangerClicked;
            UiManager.Instance.OnCardoorExitButtonClicked      += OnCardoorExitClicked;
            UiManager.Instance.OnJumpPressed                   += OnJumpButtonPressed;
            UiManager.Instance.OnCrouchPressed                 += OnCrouchPressed;
            UiManager.Instance.SetupEventListeners();

            GameManager.Instance.DisableMainCamera();
            GameManager.Instance.PlayerSpawnedEventDispatcher();
        }
    }

    private void OnJumpButtonPressed() {
         // Logic for jump button
    }

    private void OnCrouchPressed() {
        _crouchTogglePressed = true;
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        if (IsLocalPlayer && UiManager.Instance != null) {
            UiManager.Instance.OnCardoorDriverButtonClicked    -= OnCardoorDriverClicked;
            UiManager.Instance.OnCardoorPassangerButtonClicked -= OnCardoorPassangerClicked;
            UiManager.Instance.OnCardoorExitButtonClicked      -= OnCardoorExitClicked;
            UiManager.Instance.OnJumpPressed                   -= OnJumpButtonPressed;
        }

        if (orbitCamera != null) {
            Destroy(orbitCamera.gameObject);
        }
    }

    #endregion

    #region Editor Gizmos

#if UNITY_EDITOR
    private void OnDrawGizmos() {
        if (navMeshAgent == null || !navMeshAgent.hasPath)
            return;

        Gizmos.color = Color.green;
        Vector3[] corners = navMeshAgent.path.corners;

        for (int i = 0; i < corners.Length - 1; i++) {
            Gizmos.DrawLine(corners[i], corners[i + 1]);
        }

        foreach (Vector3 corner in corners) {
            Gizmos.DrawSphere(corner, 0.1f);
        }

        if (navMeshAgent.hasPath) {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, navMeshAgent.destination);
            Gizmos.DrawSphere(navMeshAgent.destination, 0.2f);
        }
    }
#endif

    #endregion
}