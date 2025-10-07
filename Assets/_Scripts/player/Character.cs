using System;
using Animancer;
using core.Managers;
using core.player;
using core.Vehicles;
using FishNet.Object;
using KinematicCharacterController;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class Character : NetworkBehaviour, ICharacterController {
    #region Character Enums, Properties, and Fields

    public enum CharacterState {
        Idle,
        Strafe,
        Crouched,
        Running,
        Jumping,
        InVehicleDriver,
        InVehiclePassenger,
        Falling,
        HandledByStateMachine
    }

    public CharacterCam orbitCamera;
    public Transform cameraTarget;
    public KinematicCharacterMotor motor;
    public NavMeshAgent navMeshAgent;
    [Required("Required")] public PlayerStatus PlayerStatus;

    private bool useInputForRotation = true;

    [FoldoutGroup("Animation Parameters")] public PlayerAnimator pa;

    #region Movement

    [FoldoutGroup("Movement")] [SerializeField] private float MaxStableMoveSpeed = 6;
    [FoldoutGroup("Movement")] public float orientationSharpness = 10;
    [FoldoutGroup("Movement")] public float StableMovementSharpness = 15;
    [FormerlySerializedAs("WALKING_SPEED"), FoldoutGroup("Movement")] public float STRAFE_SPEED = 3;
    [FoldoutGroup("Movement")] [SerializeField] private float RUNNING_SPEED = 5;
    [FoldoutGroup("Movement")] [SerializeField] private float CROUCH_SPEED = 2;

    [FoldoutGroup("Movement/Air")] public float airOrientationSharpness = 5f;
    [FoldoutGroup("Movement/Air")] public float drag = 0.1f;

    [FoldoutGroup("Movement/Jumping")] public float RunningjumpForce = 10f;
    [FoldoutGroup("Movement/Jumping")] public float IdlejumpForce = 10f;
    [FoldoutGroup("Movement/Jumping")] [SerializeField] private bool isJumping;
    [FoldoutGroup("Movement/Jumping")] public float coyoteTime = 0.15f;
    [FoldoutGroup("Movement/Jumping")] public float jumpBufferTime = 0.1f;
    [FoldoutGroup("Movement/Jumping")] public float maxJumpTime = 5f;

    [FoldoutGroup("Vehicle")] public CarController playerCar;
    [FoldoutGroup("Vehicle")] public bool invehicle;

    [FoldoutGroup("Gravity")] public float initialGravity = -9.81f;
    [FoldoutGroup("Gravity")] public float maxGravity = -30f;
    [FoldoutGroup("Gravity")] public float gravityBuildUpTime = 0.5f;
    [FoldoutGroup("Gravity")] [SerializeField] private float FallTime;
    [FoldoutGroup("Gravity")] [SerializeField] private double fallThreshold = 1.1f;

    #endregion

    [BoxGroup("Current Character state")] [SerializeField] private CharacterState _currentCharacterState = CharacterState.Idle;

    #endregion

    public CharacterState CurrentCharacterState {
        get => _currentCharacterState;
        set {
            if (_currentCharacterState == value) return;
            if (value == CharacterState.Falling) is_Falling = true;
            _currentCharacterState = value;
            OnCharacterStateChanged?.Invoke(value);
            UpdateAnimation();
        }
    }

    #region Events

    public event Action<CharacterState> OnCharacterStateChanged;

    #endregion

    #region Movement Input

    public float Steeringsmoothness = 5f;
    private Vector3 _lookInputVector;
    private Vector3 _moveInputVector;

    [FoldoutGroup("Movement/Transitions")] public float movementTransitionSpeed = 8f;
    private float currentMoveSpeed;
    private float targetMoveSpeed;
    private float speedChangeVelocity;

    #endregion

    #region Gravity and Falling

    private float currentGravity;
    private float fallStartTime;
    private float gravityVelocity;
    private bool is_Falling;

    #endregion

    #region Jumping

    private float jumpTime;
    private bool jumpInput;
    private float lastGroundedTime;
    private float lastJumpPressedTime;

    #endregion

    #region Vehicle

    public CarSeat? playerCarSeat;

    #endregion

    #region Misc

    public bool isFrozen;
    public AnimationLockManager animationLockManager;

    private void InitializeAnimationLockManager() {
        animationLockManager = new AnimationLockManager(this);
    }

    #endregion

    private void Awake() {
        navMeshAgent.transform.SetParent(transform, false);
        motor.CharacterController = this;
        InitializeAnimationLockManager();
    }

    private void Start() {
        currentGravity = initialGravity;
        currentMoveSpeed = 0f;
    }

    private void Update() {
        if (IsOwner)
            GatherInput();
    }

    #region Input Handling

    /// <summary>
    /// Main input gathering method - called every frame for the owner
    /// </summary>
    private void GatherInput() {
        // Skip input if animation is locked
        if (animationLockManager != null && animationLockManager.ShouldBlockInput()) {
            return;
        }

        // Handle different states
        switch (CurrentCharacterState) {
            case CharacterState.InVehicleDriver:
                GatherVehicleInput();
                break;
            case CharacterState.InVehiclePassenger:
                // Passenger has no input
                break;
            default:
                GatherMovementInput();
                break;
        }
    }

    /// <summary>
    /// Gathers movement input from joystick and keyboard
    /// </summary>
    private void GatherMovementInput() {
        // Get joystick input
        float horizontal = UiManager.Instance.ultimateJoystick != null
            ? UiManager.Instance.ultimateJoystick.HorizontalAxis
            : 0f;

        float vertical = UiManager.Instance.ultimateJoystick != null
            ? UiManager.Instance.ultimateJoystick.VerticalAxis
            : 0f;

#if UNITY_EDITOR || !UNITY_ANDROID
        // Keyboard override for testing
        if (Input.GetKey(KeyCode.W)) vertical = 1f;
        if (Input.GetKey(KeyCode.S)) vertical = -1f;
        if (Input.GetKey(KeyCode.A)) horizontal = -1f;
        if (Input.GetKey(KeyCode.D)) horizontal = 1f;

        // Jump input
        if (Input.GetKeyDown(KeyCode.Space)) SetJumpInput(true);
        
        // Crouch toggle
        if (Input.GetKeyDown(KeyCode.C)) ToggleCrouch();
#endif

        // Convert input to movement vectors
        ProcessMovementInput(horizontal, vertical);
    }

    /// <summary>
    /// Converts raw input values into movement vectors based on camera orientation
    /// </summary>
    private void ProcessMovementInput(float horizontal, float vertical) {
        // Get camera orientation
        Quaternion cameraRotation = orbitCamera != null
            ? orbitCamera.transform.rotation
            : Quaternion.identity;

        var cameraForward = Vector3.ProjectOnPlane(cameraRotation * Vector3.forward, Vector3.up).normalized;
        var cameraRight = Vector3.Cross(Vector3.up, cameraForward).normalized;

        // Calculate move input based on camera orientation
        _moveInputVector = (cameraForward * vertical + cameraRight * horizontal).normalized;
        _moveInputVector = Vector3.ClampMagnitude(_moveInputVector, 1f);

        // Set look direction
        _lookInputVector = _moveInputVector.sqrMagnitude > 0.01f
            ? _moveInputVector
            : cameraForward;

        // Update movement state based on input magnitude
        UpdateMovementState(new Vector2(horizontal, vertical));
    }

    /// <summary>
    /// Gathers vehicle control input
    /// </summary>
    private void GatherVehicleInput() {
        if (playerCar == null) return;

        playerCar.controllerV4.fuelInput = UiManager.Instance.GetInput(UiManager.Instance.gasButton);
        playerCar.controllerV4.brakeInput = UiManager.Instance.GetInput(UiManager.Instance.brakeButton);
        playerCar.controllerV4.steerInput = -UiManager.Instance.GetInput(UiManager.Instance.leftButton) + 
                                             UiManager.Instance.GetInput(UiManager.Instance.rightButton);

#if UNITY_EDITOR || !UNITY_ANDROID
        // Keyboard override for vehicle
        if (Input.GetKey(KeyCode.Space)) playerCar.controllerV4.handbrakeInput = 1f;
        else playerCar.controllerV4.handbrakeInput = 0f;
        if (Input.GetKey(KeyCode.Z)) playerCar.controllerV4.fuelInput = 1f;
        if (Input.GetKey(KeyCode.S)) playerCar.controllerV4.brakeInput = 1;
        if (Input.GetKey(KeyCode.D)) playerCar.controllerV4.steerInput = 1;
        if (Input.GetKey(KeyCode.Q)) playerCar.controllerV4.steerInput = -1;
#endif

        UpdateVehicleAnimation();
    }

    #endregion

    #region Character Controller Interface Implementation

    public void BeforeCharacterUpdate(float deltaTime) { }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime) {
        if (orbitCamera != null) {
            if (useInputForRotation && _moveInputVector.sqrMagnitude > 0.01f) {
                // Rotate towards joystick direction when running
                Vector3 moveDir = _moveInputVector;
                moveDir.y = 0f;
                moveDir.Normalize();
                Vector3 smoothedLookDirection = Vector3.Slerp(motor.CharacterForward, moveDir, 
                    1 - Mathf.Exp(-orientationSharpness * deltaTime)).normalized;
                currentRotation = Quaternion.LookRotation(smoothedLookDirection, motor.CharacterUp);
            }
            else {
                // Default: rotate towards camera forward
                Vector3 cameraForward = orbitCamera.transform.forward;
                cameraForward.y = 0f;
                cameraForward.Normalize();
                Vector3 smoothedLookDirection = Vector3.Slerp(motor.CharacterForward, cameraForward, 
                    1 - Mathf.Exp(-orientationSharpness * deltaTime)).normalized;
                currentRotation = Quaternion.LookRotation(smoothedLookDirection, motor.CharacterUp);
            }
        }
        else if (motor.GroundingStatus.IsStableOnGround) {
            Vector3 smoothedLookInputDirection = Vector3.Slerp(motor.CharacterForward, _lookInputVector, 
                1 - Mathf.Exp(-orientationSharpness * deltaTime)).normalized;
            currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, motor.CharacterUp);
        }
        else {
            // Air control
            if (_moveInputVector.sqrMagnitude > 0.01f) {
                Vector3 smoothedLookInputDirection = Vector3.Slerp(motor.CharacterForward, _lookInputVector, 
                    1 - Mathf.Exp(-airOrientationSharpness * deltaTime)).normalized;
                currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, motor.CharacterUp);
            }
        }
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime) {
        if (motor.GroundingStatus.IsStableOnGround) {
            HandleGroundedMovement(ref currentVelocity, deltaTime);
        }
        else {
            HandleAirborneMovement(ref currentVelocity, deltaTime);
        }

        if (isFrozen) currentVelocity = Vector3.zero;
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

    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, 
        ref HitStabilityReport hitStabilityReport) { }

    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, 
        ref HitStabilityReport hitStabilityReport) {
        if (hitCollider.CompareTag("Vehicle")) {
            var nearbyVehicle = hitCollider.GetComponentInParent<CarController>();
            if (nearbyVehicle is null) return;

            UiManager.Instance.ShowVehicleEnterExitButtons(true,
                nearbyVehicle.driverSeat.used,
                nearbyVehicle.GetFreeCarSeats().Count > 0);

            if (playerCar is null || playerCar != nearbyVehicle) {
                playerCar = nearbyVehicle;
            }
        }
    }

    public void PostGroundingUpdate(float deltaTime) {
        if (motor.GroundingStatus.IsStableOnGround) {
            ResetJump();
        }
    }

    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, 
        Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) { }

    public void OnDiscreteCollisionDetected(Collider hitCollider) { }

    #endregion

    #region Movement State Management

    private void UpdateMovementState(Vector2 directions) {
        if (!motor.GroundingStatus.IsStableOnGround && !isJumping) {
            if (CurrentCharacterState != CharacterState.Falling) {
                CurrentCharacterState = CharacterState.Falling;
            }
            return;
        }

        if (isJumping) return;
        if (CurrentCharacterState is CharacterState.InVehicleDriver or CharacterState.InVehiclePassenger) return;

        if (CurrentCharacterState == CharacterState.Crouched) {
            if (directions.magnitude > 0.2f) {
                pa.UpdateCrouchAnimation();
                useInputForRotation = directions.y > 0.4f;
            }
            else {
                useInputForRotation = false;
                pa.resetCrouchAnimation();
                pa.PlayAnimation(pa.crouch_idle, 0.3f);
            }
            return;
        }

        // Determine new state from input
        if (directions.magnitude < 0.1f) {
            SetState(CharacterState.Idle);
            return;
        }

        if (directions.magnitude > 0.2f && directions.magnitude <= 0.8f || directions.y < 0.8f) {
            if (!isJumping) pa.UpdateStrafeAnimation();
            if (useInputForRotation) useInputForRotation = false;
            SetState(CharacterState.Strafe);
        }

        if (directions.y > 0.4f) {
            SetState(CharacterState.Running);
        }
    }

    private void SetState(CharacterState newState) {
        if (CurrentCharacterState == newState) return;

        HandleOldStateExitActions(CurrentCharacterState);

        switch (newState) {
            case CharacterState.Idle:
                MaxStableMoveSpeed = 0f;
                break;
            case CharacterState.Strafe:
                MaxStableMoveSpeed = STRAFE_SPEED;
                break;
            case CharacterState.Running:
                MaxStableMoveSpeed = RUNNING_SPEED;
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
            currentVelocity = ReorientVelocityOnSlope(currentVelocity);
            Vector3 targetMovementVelocity = CalculateTargetGroundVelocity();
            currentVelocity = SmoothVelocity(currentVelocity, targetMovementVelocity, 
                StableMovementSharpness, deltaTime);
        }
    }

    private void HandleAirborneMovement(ref Vector3 currentVelocity, float deltaTime) {
        if (isJumping && jumpTime < 0.2f) {
            currentVelocity.y = Mathf.Max(currentVelocity.y,
                0.8f * (CurrentCharacterState == CharacterState.Idle ? IdlejumpForce : RunningjumpForce));
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
        Vector3 reorientedInput = Vector3.Cross(motor.GroundingStatus.GroundNormal, inputRight).normalized * 
                                  _moveInputVector.magnitude;
        return reorientedInput * MaxStableMoveSpeed;
    }

    private Vector3 SmoothVelocity(Vector3 currentVelocity, Vector3 targetVelocity, float smoothing, float deltaTime) {
        return Vector3.Lerp(currentVelocity, targetVelocity, 1 - Mathf.Exp(-smoothing * deltaTime));
    }

    private void ApplySmoothGravity(ref Vector3 currentVelocity, float deltaTime) {
        currentGravity = Mathf.SmoothDamp(currentGravity, maxGravity, ref gravityVelocity, gravityBuildUpTime);
        currentVelocity.y += currentGravity * deltaTime;
    }

    #endregion

    #region Jumping

    public void SetJumpInput(bool newJumpState) {
        if (newJumpState) lastJumpPressedTime = Time.time;
        jumpInput = newJumpState;
    }

    private bool CanJump() {
        var timeSinceGrounded = Time.time - lastGroundedTime;
        var timeSinceJumpPressed = Time.time - lastJumpPressedTime;
        return (timeSinceGrounded < coyoteTime || motor.GroundingStatus.IsStableOnGround) && 
               timeSinceJumpPressed < jumpBufferTime && !isJumping;
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
            jumpingAnimationState.Events(this).OnEnd = () => {
                isJumping = false;
                CheckForFalling();
            };
        }

        isJumping = true;
        jumpTime = 0f;
        CurrentCharacterState = CharacterState.Jumping;
        ApplyJumpForce(ref currentVelocity);
    }

    private void ApplyJumpForce(ref Vector3 currentVelocity) {
        float jumpForce = (CurrentCharacterState == CharacterState.Idle) ? IdlejumpForce : RunningjumpForce;
        currentVelocity.y = Mathf.Max(currentVelocity.y + jumpForce, jumpForce);
        motor.ForceUnground();
    }

    private void UpdateJumpState(float deltaTime) {
        jumpTime += deltaTime;

        if (jumpTime >= maxJumpTime) {
            if (_currentCharacterState != CharacterState.Falling) {
                isJumping = false;
                is_Falling = true;
                fallStartTime = Time.time;
                FallTime = 0f;
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
            is_Falling = true;
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

    private void ExitVehicle() {
        if (playerCar is null || playerCarSeat is null) return;

        motor.transform.parent = null;
        motor.transform.localScale = Vector3.one;
        motor.SetPositionAndRotation(playerCarSeat.Value.exitpos.position, Quaternion.identity);
        TogglePlayerCollisionDetection(true);
        motor.enabled = true;

        if (!playerCar.controllerV4.engineRunning) 
            StopCoroutine(playerCar.controllerV4.StartEngineDelayed());
        
        if (playerCarSeat.Value.isDriver) {
            playerCar.controllerV4.KillEngine();
            playerCar.driverSeat.used = false;
        }

        playerCar.controllerV4.handbrakeInput = 1f;
        playerCarSeat.Value.FreetheSeat();
        playerCarSeat = null;

        navMeshAgent.enabled = false;
        playerCar.fx_CloseDoorSound();

        if (orbitCamera.IgnoredColliders.Contains(playerCar.VehicleCollider))
            orbitCamera.IgnoredColliders.Remove(playerCar.VehicleCollider);

        UiManager.Instance.ShowControllerPage();
        pa.FadeOutActionLayer();

        invehicle = false;
        CurrentCharacterState = CharacterState.Idle;
    }

    private void UpdateVehicleAnimation() {
        if (playerCar != null && playerCar.controllerV4 != null) {
            bool isReversing = playerCar.controllerV4.direction == -1 && playerCar.controllerV4.speed > 5f;

            var newsteer = Mathf.Clamp(playerCar.controllerV4.FrontLeftWheelCollider.WheelCollider.steerAngle, -1, 1);
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
                pa.UpdateStrafeAnimation();
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

    public override void OnStartClient() {
        base.OnStartClient();
        SetupPlayerCharacter();
    }

    private void SetupPlayerCharacter() {
        if (!IsOwner) {
            if (orbitCamera != null) Destroy(orbitCamera.gameObject);
            gameObject.name = "NETWORK_PLAYER_" + Owner.ClientId;
        }
        else {
            gameObject.name = "LOCAL_PLAYER";
            
            if (orbitCamera != null) {
                orbitCamera.transform.parent = null;
                orbitCamera.gameObject.name = "Local Player Cam";
            }

            // Subscribe to UI events
            UiManager.Instance.OnCardoorDriverButtonClicked += OnCardoorDriverClicked;
            UiManager.Instance.OnCardoorPassangerButtonClicked += OnCardoorPassangerClicked;
            UiManager.Instance.OnCardoorExitButtonClicked += OnCardoorExitClicked;
            UiManager.Instance.OnJumpPressed += InstanceOnOnJumpPressed;
            UiManager.Instance.SetupEventListeners();

            GameManager.Instance.DisableMainCamera();
        }
    }

    private void InstanceOnOnJumpPressed() {
        throw new NotImplementedException();
    }

    private void OnDestroy() {
        if (IsOwner && UiManager.Instance != null) {
            UiManager.Instance.OnCardoorDriverButtonClicked -= OnCardoorDriverClicked;
            UiManager.Instance.OnCardoorPassangerButtonClicked -= OnCardoorPassangerClicked;
            UiManager.Instance.OnCardoorExitButtonClicked -= OnCardoorExitClicked;
            
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