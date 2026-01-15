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
    [FoldoutGroup("Movement")] [SerializeField] private float MaxStableMoveSpeed = 6;
    [FoldoutGroup("Movement")] public  float orientationSharpness    = 10;
    [FoldoutGroup("Movement")] public  float StableMovementSharpness = 15;
    [FormerlySerializedAs("WALKING_SPEED"), FoldoutGroup("Movement")] public  float STRAFE_SPEED = 3;
    [FoldoutGroup("Movement")] [SerializeField] private float RUNNING_SPEED = 5;
    [FoldoutGroup("Movement")] [SerializeField] private float CROUCH_SPEED = 2;
    [FoldoutGroup("Movement/Air")] public float airOrientationSharpness = 5f;
    [FoldoutGroup("Movement/Air")] public float drag = 0.1f;
    [FoldoutGroup("Movement/Jumping")] public  float RunningjumpForce = 10f;
    [FoldoutGroup("Movement/Jumping")] public  float IdlejumpForce = 10f;
    [FoldoutGroup("Movement/Jumping")] [SerializeField] private bool  isJumping;
    [FoldoutGroup("Movement/Jumping")] public  float coyoteTime = 0.15f;
    [FoldoutGroup("Movement/Jumping")] public  float jumpBufferTime = 0.1f;
    [FoldoutGroup("Movement/Jumping")] public  float maxJumpTime = 5f;
    [FoldoutGroup("Vehicle")] public CarController playerCar;
    [FoldoutGroup("Vehicle")] public bool invehicle;
    [FoldoutGroup("Gravity")] public  float  initialGravity = -9.81f;
    [FoldoutGroup("Gravity")] public  float  maxGravity = -30f;
    [FoldoutGroup("Gravity")] public  float  gravityBuildUpTime = 0.5f;
    [FoldoutGroup("Gravity")] [SerializeField] private float  FallTime;
    [FoldoutGroup("Gravity")] [SerializeField] private double fallThreshold = 1.1f;
    #endregion

    [BoxGroup("Current Character state")] [SerializeField] private CharacterState _currentCharacterState = CharacterState.Idle;
    #endregion

    #region Prediction Data Structures
    [FoldoutGroup("Reconciliation")] public float positionSnapThreshold = 3f;
    [FoldoutGroup("Reconciliation")] public float ReconsileSmoothTime = 25.5f;
    private Vector3 _reconcilePositionVelocity;
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
    public event Action<Collider> onCharacterDetectCollider;
    #endregion

    #region Movement Input
    private Vector3 _internalVelocityAdd = Vector3.zero;
    public  float   Steeringsmoothness = 5f;
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
    public bool isFrozen;
    public AnimationLockManager animationLockManager;
    #region Prediction Fields
    private float _lastHorizontal;
    private float _lastVertical;
    private bool _lastJumpInput;
    private bool _crouchTogglePressed;
    #endregion
    private void InitializeAnimationLockManager() => animationLockManager = new AnimationLockManager(this);
    #endregion

    private void Awake() {
        navMeshAgent.transform.SetParent(transform, false);
        motor.CharacterController = this;
        InitializeAnimationLockManager();
        currentGravity = initialGravity;
    }

    protected override void OnNetworkInitialize()
    {
        base.OnNetworkInitialize();
        if(NetworkGameManager.Instance  == null) return;
        if (NetworkGameManager.Instance.LocalPlayer == this)
        {
            SetupPlayerCharacter();
        }
        else
        {
            if (orbitCamera != null)
            {
                Destroy(orbitCamera.gameObject);
            }
            gameObject.name = $"NET_PROXY_{OwnerId}";
        }
    }

    public override void OnServerInput(PlayerInputPacket input)
    {
        ProcessInput(input);
        
        var state = new PlayerStatePacket
        {
            PlayerId = OwnerId,
            Tick = input.Tick,
            Position = motor.TransientPosition,
            Rotation = motor.TransientRotation,
            Velocity = motor.Velocity,
            StateId = (byte)_currentCharacterState
        };
        
        if (ServerManager.Instance != null)
        {
            ServerManager.Instance.BroadcastToAll(state, DeliveryMethod.Unreliable);
        }
    }

    public override void OnClientState(PlayerStatePacket state)
    {
        if (IsLocalPlayer)
        {
             float positionError = Vector3.Distance(state.Position, motor.TransientPosition);
             if (positionError > positionSnapThreshold)
             {
                 motor.SetPosition(state.Position, true);
                 motor.SetRotation(state.Rotation);
             }
        }
        else
        {
            motor.SetPosition(state.Position, true);
            motor.SetRotation(state.Rotation);
            CurrentCharacterState = (CharacterState)state.StateId;
        }
    }

    private void FixedUpdate()
    {
        if (IsLocalPlayer)
        {
            var input = GatherInputForPrediction();
            ProcessInput(input);
            
            if (ClientManager.Instance != null)
            {
                ClientManager.Instance.SendPacket(input, DeliveryMethod.ReliableOrdered);
            }
        }
    }

    private PlayerInputPacket GatherInputForPrediction() {
        var input = new PlayerInputPacket();
        if (ClientManager.Instance != null)
        {
            input.Tick = ClientManager.Instance.CurrentTick;
        }
        
        if (animationLockManager != null && animationLockManager.ShouldBlockInput()) {
            return input;
        }

        if (CurrentCharacterState == CharacterState.InVehicleDriver) {
             return input;
        }

        input.Horizontal = UiManager.Instance.ultimateJoystick != null ? UiManager.Instance.ultimateJoystick.HorizontalAxis : 0f;
        input.Vertical = UiManager.Instance.ultimateJoystick != null ? UiManager.Instance.ultimateJoystick.VerticalAxis : 0f;

#if UNITY_EDITOR || !UNITY_ANDROID
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

        if (orbitCamera != null)
             input.CameraYaw = orbitCamera.transform.eulerAngles.y;

        return input;
    }

    private void ProcessInput(PlayerInputPacket input) {
        if (input.Jump) SetJumpInput(true);
        if (input.Crouch) ToggleCrouch();

        _replicatedCameraRotation = Quaternion.Euler(0, input.CameraYaw, 0);
        var cameraForward = Vector3.ProjectOnPlane(_replicatedCameraRotation * Vector3.forward, motor.CharacterUp).normalized;
        var cameraRight = Vector3.Cross(Vector3.up, cameraForward).normalized;
        _moveInputVector = Vector3.ClampMagnitude((cameraForward * input.Vertical + cameraRight * input.Horizontal).normalized, 1f);

        if (!animationLockManager.ShouldBlockInput()) {
            UpdateMovementState(new Vector2(input.Horizontal, input.Vertical));
        }
    }

    #region Character Controller Interface
    public void BeforeCharacterUpdate(float deltaTime) { }
    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime) {
        if (_replicatedCameraRotation == Quaternion.identity) {
            currentRotation = Quaternion.LookRotation(motor.CharacterForward, motor.CharacterUp);
            return;
        }
        Vector3 cameraForward = Vector3.ProjectOnPlane(_replicatedCameraRotation * Vector3.forward, motor.CharacterUp).normalized;
        Vector3 targetDirection = (useInputForRotation && _moveInputVector.sqrMagnitude > 0.01f) ? _moveInputVector.normalized : cameraForward;
        float sharpness = motor.GroundingStatus.IsStableOnGround ? orientationSharpness : airOrientationSharpness;
        Vector3 smoothedDirection = Vector3.Slerp(motor.CharacterForward, targetDirection, 1 - Mathf.Exp(-sharpness * deltaTime)).normalized;
        currentRotation = Quaternion.LookRotation(smoothedDirection, motor.CharacterUp);
    }
    public void AddVelocity(Vector3 velocity) => _internalVelocityAdd += velocity;
    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime) {
        if (motor.GroundingStatus.IsStableOnGround) HandleGroundedMovement(ref currentVelocity, deltaTime);
        else HandleAirborneMovement(ref currentVelocity, deltaTime);
        if (isFrozen) currentVelocity = Vector3.zero;
        if (_internalVelocityAdd.sqrMagnitude > 0f) {
            currentVelocity += _internalVelocityAdd;
            _internalVelocityAdd = Vector3.zero;
        }
    }
    public void AfterCharacterUpdate(float deltaTime) {
        HandleLanding();
        if (playerCar != null && Vector3.Distance(playerCar.transform.position, motor.transform.position) > 5f) {
            playerCar = null;
            UiManager.Instance.ShowVehicleEnterExitButtons(false, false, false);
        }
    }
    public bool IsColliderValidForCollisions(Collider coll) => true;
    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) {
        if (hitCollider.CompareTag("Vehicle")) {
            var nearbyVehicle = hitCollider.GetComponentInParent<CarController>();
            if (nearbyVehicle == null) return;
            UiManager.Instance.ShowVehicleEnterExitButtons(true, nearbyVehicle.driverSeat.used, nearbyVehicle.GetFreeCarSeats().Count > 0);
            if (playerCar == null || playerCar != nearbyVehicle) playerCar = nearbyVehicle;
        }
        if (hitCollider.gameObject.layer == LayerMask.NameToLayer("Interactable")) onCharacterDetectCollider?.Invoke(hitCollider);
    }
    public void PostGroundingUpdate(float deltaTime) { if (motor.GroundingStatus.IsStableOnGround) ResetJump(); }
    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) { }
    public void OnDiscreteCollisionDetected(Collider hitCollider) { }
    #endregion

    #region Movement State & Logic
    private void UpdateMovementState(Vector2 directions) {
        if (!motor.GroundingStatus.IsStableOnGround && !isJumping) {
            if (CurrentCharacterState != CharacterState.Falling) CurrentCharacterState = CharacterState.Falling;
            return;
        }
        if (isJumping || CurrentCharacterState is CharacterState.InVehicleDriver or CharacterState.InVehiclePassenger) return;
        if (CurrentCharacterState == CharacterState.Crouched) {
            if (directions.magnitude > 0.2f) {
                MaxStableMoveSpeed = CROUCH_SPEED;
                pa.UpdateCrouchAnimation(new Vector2(directions.x, directions.y));
                useInputForRotation = directions.y > 0.4f;
            } else {
                MaxStableMoveSpeed = 0f;
                useInputForRotation = false;
                pa.resetCrouchAnimation();
                pa.PlayAnimation(pa.crouch_idle, 0.3f);
            }
            return;
        }
        float inputMagnitude = directions.magnitude;
        if (inputMagnitude < 0.1f) {
            if (CurrentCharacterState != CharacterState.Idle) SetState(CharacterState.Idle);
        } else if (directions.y > 0.5f && inputMagnitude > 0.2f) {
            if (CurrentCharacterState != CharacterState.Running) SetState(CharacterState.Running);
        } else if (inputMagnitude > 0.1f) {
            if (CurrentCharacterState != CharacterState.Strafe) SetState(CharacterState.Strafe);
            if (!isJumping) pa.UpdateStrafeAnimation(new Vector2(directions.x, directions.y));
            useInputForRotation = directions.y > 0.4f;
        }
    }
    private void SetState(CharacterState newState) {
        if (CurrentCharacterState == newState) return;
        HandleOldStateExitActions(CurrentCharacterState);
        switch (newState) {
            case CharacterState.Idle: MaxStableMoveSpeed = 0f; break;
            case CharacterState.Strafe: MaxStableMoveSpeed = STRAFE_SPEED; break;
            case CharacterState.Running: MaxStableMoveSpeed = RUNNING_SPEED; useInputForRotation = true; break;
            case CharacterState.Crouched: MaxStableMoveSpeed = CROUCH_SPEED; break;
            case CharacterState.InVehicleDriver or CharacterState.InVehiclePassenger: invehicle = true; break;
        }
        CurrentCharacterState = newState;
        OnCharacterStateChanged?.Invoke(newState);
    }
    private void HandleOldStateExitActions(CharacterState oldState) {
        switch (oldState) {
            case CharacterState.Strafe: pa.ResetStrafeAnimation(); break;
            case CharacterState.Running: useInputForRotation = false; break;
        }
    }
    private void HandleGroundedMovement(ref Vector3 currentVelocity, float deltaTime) {
        if (jumpInput && CanJump()) InitiateJump(ref currentVelocity);
        else {
            lastGroundedTime = Time.time;
            currentVelocity = ReorientVelocityOnSlope(currentVelocity);
            Vector3 targetMovementVelocity = CalculateTargetGroundVelocity();
            currentVelocity = SmoothVelocity(currentVelocity, targetMovementVelocity, StableMovementSharpness, deltaTime);
        }
    }
    private void HandleAirborneMovement(ref Vector3 currentVelocity, float deltaTime) {
        if (isJumping && jumpTime < 0.2f) currentVelocity.y = Mathf.Max(currentVelocity.y, 0.8f * (CurrentCharacterState == CharacterState.Idle ? IdlejumpForce : RunningjumpForce));
        else ApplySmoothGravity(ref currentVelocity, deltaTime);
        if (isJumping) UpdateJumpState(deltaTime);
        else CheckForFalling();
        currentVelocity *= 1f / (1f + drag * deltaTime);
    }
    private Vector3 ReorientVelocityOnSlope(Vector3 velocity) => motor.GetDirectionTangentToSurface(velocity, motor.GroundingStatus.GroundNormal) * velocity.magnitude;
    private Vector3 CalculateTargetGroundVelocity() => Vector3.Cross(motor.GroundingStatus.GroundNormal, Vector3.Cross(_moveInputVector, motor.CharacterUp)).normalized * _moveInputVector.magnitude * MaxStableMoveSpeed;
    private Vector3 SmoothVelocity(Vector3 current, Vector3 target, float smoothing, float delta) => Vector3.Lerp(current, target, 1 - Mathf.Exp(-smoothing * delta));
    private void ApplySmoothGravity(ref Vector3 velocity, float delta) => velocity.y += Mathf.SmoothDamp(velocity.y, maxGravity, ref gravityVelocity, gravityBuildUpTime) * delta;
    #endregion

    #region Jumping, Falling & Landing
    public void SetJumpInput(bool state) { if (state) lastJumpPressedTime = Time.time; jumpInput = state; }
    private bool CanJump() => (Time.time - lastGroundedTime < coyoteTime || motor.GroundingStatus.IsStableOnGround) && Time.time - lastJumpPressedTime < jumpBufferTime && !isJumping;
    private void InitiateJump(ref Vector3 velocity) {
        if (CurrentCharacterState == CharacterState.Crouched) { ToggleCrouch(); jumpInput = false; return; }
        if (CurrentCharacterState == CharacterState.Jumping) return;
        AnimancerState anim = (CurrentCharacterState == CharacterState.Idle) ? pa.PlayAnimation(pa.anim_jump_inplace, 0.2f, FadeMode.FromStart) : pa.PlayAnimation(pa.anim_jump_start, 0.2f, FadeMode.FromStart);
        if (anim != null) anim.Events(this).OnEnd = () => { isJumping = false; CheckForFalling(); };
        isJumping = true;
        jumpTime = 0f;
        CurrentCharacterState = CharacterState.Jumping;
        velocity.y = Mathf.Max(velocity.y + (CurrentCharacterState == CharacterState.Idle ? IdlejumpForce : RunningjumpForce), (CurrentCharacterState == CharacterState.Idle ? IdlejumpForce : RunningjumpForce));
        motor.ForceUnground();
    }
    private void UpdateJumpState(float deltaTime) {
        jumpTime += deltaTime;
        if (jumpTime >= maxJumpTime) {
            if (CurrentCharacterState != CharacterState.Falling) {
                isJumping = false; is_Falling = true; fallStartTime = Time.time; FallTime = 0f; CurrentCharacterState = CharacterState.Falling;
            }
        } else if (CurrentCharacterState != CharacterState.Jumping) CurrentCharacterState = CharacterState.Jumping;
    }
    private void ResetJump() { if (isJumping) isJumping = false; }
    private void CheckForFalling() {
        if (CurrentCharacterState == CharacterState.Falling) {
            FallTime += Time.deltaTime;
            if (FallTime >= 0.5f && FallTime < 1f && !pa.isPlayingAnimation(pa.anim_falling_second)) pa.PlayAnimation(pa.anim_falling_second, 0.3f);
            if (FallTime > 1f) pa.PlayAnimation(pa.anim_falling_loop, 0.5f);
            return;
        }
        if (isJumping) return;
        if (!is_Falling) { fallStartTime = Time.time; is_Falling = true; }
        else if (Time.time - fallStartTime >= fallThreshold) CurrentCharacterState = CharacterState.Falling;
    }
    private void HandleLanding() {
        if (motor.GroundingStatus.IsStableOnGround && (is_Falling || CurrentCharacterState == CharacterState.Falling)) {
            AnimancerState landingState = null;
            if (FallTime > 0.8f && !pa.isPlayingAnimation(pa.anim_land_med)) landingState = pa.PlayAnimation(pa.anim_land_med, 0.2f);
            else if (FallTime > 0.2f && !pa.isPlayingAnimation(pa.anim_land_low)) landingState = pa.PlayAnimation(pa.anim_land_low, 0.2f);
            if (landingState != null) animationLockManager.LockForAnimation(landingState, AnimationLockManager.AnimationLockType.HardLanding, () => { CurrentCharacterState = CharacterState.Idle; pa.PlayAnimation(pa.anim_idle); });
            is_Falling = false;
            FallTime = 0f;
        }
    }
    #endregion

    #region Vehicle
    private void OnCardoorDriverClicked() {
        if (playerCar != null) {
            playerCarSeat = playerCar.SetPlayerInVehicle(this, pa, true);
            if (playerCarSeat != null) CurrentCharacterState = playerCarSeat.Value.isDriver ? CharacterState.InVehicleDriver : CharacterState.InVehiclePassenger;
        }
    }
    private void OnCardoorPassangerClicked() {
        if (playerCar != null) {
            playerCarSeat = playerCar.SetPlayerInVehicle(this, pa);
            pa.PlayAnimation(pa.anim_Vehicle_Passenger_sit);
        }
    }
    private void OnCardoorExitClicked() { if (playerCar != null) playerCar.RemovePlayerFromVehicle(this); }
    public void TogglePlayerCollisionDetection(bool enable) {
        motor.SetCapsuleCollisionsActivation(enable);
        motor.SetGroundSolvingActivation(enable);
        motor.Capsule.enabled = enable;
    }
    #endregion

    #region Animation
    private void UpdateAnimation() {
        switch (CurrentCharacterState) {
            case CharacterState.Idle: pa.PlayAnimation(pa.anim_idle, 0.4f); break;
            case CharacterState.Strafe: pa.UpdateStrafeAnimation(new Vector2(_lastHorizontal, _lastVertical)); break;
            case CharacterState.Running: pa.PlayAnimation(pa.anim_run, 0.4f); break;
            case CharacterState.Jumping: break;
            case CharacterState.Falling: pa.PlayAnimation(pa.anim_falling_frist, 0.6f); break;
            case CharacterState.InVehicleDriver: HandleVehicleAnimation(true); break;
            case CharacterState.InVehiclePassenger: HandleVehicleAnimation(false); break;
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
    
    private void HandleVehicleAnimation(bool isDriver) {
        if (playerCar == null) return;
        if (isDriver)
            pa.UpdateVehicleSteering_Mixer(0, Steeringsmoothness);
        else
            pa.PlayAnimation(pa.anim_Vehicle_Passenger_sit);
    }
    #endregion

    #region Network Setup
    private void SetupPlayerCharacter() {
        GameManager.Instance.character = this;
        gameObject.name = $"LOCAL_PLAYER_{OwnerId}";
        if (orbitCamera != null) {
            orbitCamera.gameObject.SetActive(true);
            orbitCamera.transform.parent = null;
            orbitCamera.gameObject.name = "Local Player Cam";
        }
        UiManager.Instance.OnCardoorDriverButtonClicked += OnCardoorDriverClicked;
        UiManager.Instance.OnCardoorPassangerButtonClicked += OnCardoorPassangerClicked;
        UiManager.Instance.OnCardoorExitButtonClicked += OnCardoorExitClicked;
        UiManager.Instance.OnJumpPressed += OnJumpButtonPressed;
        UiManager.Instance.OnCrouchPressed += OnCrouchPressed;
        UiManager.Instance.SetupEventListeners();
        GameManager.Instance.DisableMainCamera();
        GameManager.Instance.PlayerSpawnedEventDispatcher();
    }
    private void OnJumpButtonPressed() { }
    private void OnCrouchPressed() => _crouchTogglePressed = true;
    protected override void OnDestroy() {
        base.OnDestroy();
        if (IsLocalPlayer && UiManager.Instance != null) {
            UiManager.Instance.OnCardoorDriverButtonClicked -= OnCardoorDriverClicked;
            UiManager.Instance.OnCardoorPassangerButtonClicked -= OnCardoorPassangerClicked;
            UiManager.Instance.OnCardoorExitButtonClicked -= OnCardoorExitClicked;
            UiManager.Instance.OnJumpPressed -= OnJumpButtonPressed;
        }
        if (orbitCamera != null) Destroy(orbitCamera.gameObject);
    }
    #endregion
}