using System;
using Animancer;
using core.Managers;
using core.player;
using core.Vehicles;
using FishNet.Component.Spawning;
using FishNet.Demo.Prediction.CharacterControllers;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using FishNet.Utility.Template;
using GameKit.Dependencies.Utilities;
using KinematicCharacterController;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class Character : TickNetworkBehaviour, ICharacterController {
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
    [FoldoutGroup("Reconciliation")]  public float ReconsileSmoothTime = 25.5f;
    // Smoothing state
    private Vector3    _reconcilePositionVelocity;
    private float      _reconcileRotationVelocity;
    private Vector3    _targetReconcilePosition;
    private Quaternion _targetReconcileRotation;
    private bool       _isReconciling;
    private float      _reconcileStartTime;
    public  Quaternion _replicatedCameraRotation = Quaternion.identity;

    /// <summary>
    /// Input data that gets sent from client to server every tick
    /// This is SMALL - only the button presses, not positions!
    /// </summary>
    public struct ReplicateData : IReplicateData { // what we send
        public float      Horizontal;              // Joystick left/right
        public float      Vertical;                // Joystick forward/back
        public bool       Jump;                    // Jump button pressed
        public bool       Crouch;                  // Crouch button pressed
        public Quaternion CamRotation;             //

        /// <summary>
        /// The tick at which this data was created.
        /// </summary>
        // Constructor to easily create the data
        public ReplicateData(float horizontal, float vertical, bool jump, bool crouch, Quaternion camRotation) {
            Horizontal  = horizontal;
            Vertical    = vertical;
            Jump        = jump;
            Crouch      = crouch;
            _tick       = 0;
            CamRotation = camRotation;
        }

        // Required by Fish-Net
        /// <summary>
        /// The tick at which this data was created.
        /// </summary>
        private uint _tick;

        /// <summary>
        /// Gets the tick at which this data was created.
        /// </summary>
        public uint GetTick() => _tick;

        /// <summary>
        /// Sets the tick at which this data was created.
        /// </summary>
        public void SetTick(uint value) => _tick = value;

        public void Dispose() { }
    }

    private ReplicateData _lastTickedReplicateData = default;

    /// <summary>
    /// State data that gets sent from server to client for corrections
    /// This contains the "truth" - where you really are
    /// </summary>
    public struct ReconcileData : IReconcileData { //what server send
        public Vector3                  Position;
        public Quaternion               Rotation;
        public Vector3                  Velocity;
        public Character.CharacterState State;

        public ReconcileData(Vector3 position, Quaternion rotation, Vector3 velocity, CharacterState state) {
            Position = position;
            Rotation = rotation;
            Velocity = velocity;
            State    = state;
            _tick    = 0;
        }

        // Required by Fish-Net
        private uint _tick;
        public  uint GetTick()           => _tick;
        public  void SetTick(uint value) => _tick = value;
        public  void Dispose()           { }
    }

    #endregion

    public CharacterState CurrentCharacterState {
        get => _currentCharacterState;
        set {
            if (_currentCharacterState == value) return;
            if (value == CharacterState.Falling) is_Falling = true;
            _currentCharacterState = value;
            //OnCharacterStateChanged?.Invoke(value);
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
    private Vector3 _lookInputVector;
    private Vector3 _moveInputVector;

    [FoldoutGroup("Movement/Transitions")] public float movementTransitionSpeed = 8f;
    private                                       float currentMoveSpeed;
    private                                       float targetMoveSpeed;
    private                                       float speedChangeVelocity;

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
    private Quaternion _camerarotation;

    #endregion

    private void InitializeAnimationLockManager() {
        animationLockManager = new AnimationLockManager(this);
    }

    #endregion

    private void Awake() {
        navMeshAgent.transform.SetParent(transform, false);
        motor.CharacterController = this;


        InitializeAnimationLockManager();


        SetTickCallbacks(TickCallback.Tick);
    }

    public override void OnStartNetwork() {
        base.OnStartNetwork();


        // Subscribe to Fish-Net's tick events
        //  TimeManager.OnTick     += TimeManager_OnTick;
        //TimeManager.OnPostTick += TimeManager_OnPostTick;
        UiManager.Instance.ShowControllerPage();
    }

    /// <summary>
    /// This replaces Update() for prediction
    /// Called at a fixed rate synchronized between client and server
    /// </summary>
    protected override void TimeManager_OnTick() {
        PerformReplicate(BuildMoveData());
        CreateReconcile();
    }

    /// <summary>
    /// Called after physics/movement is done
    /// Good place for animations and visual updates
    /// </summary>
    protected override void TimeManager_OnPostTick() { }

    /// <summary>
    /// Packages the input we gathered into ReplicateData
    /// This gets sent to the server
    /// </summary>
    /// 
    private ReplicateData BuildMoveData() {
        if (!IsOwner)
            return default;

        // Ensure we always have a valid camera rotation
        var cameraRotation = orbitCamera != null
                                 ? orbitCamera.transform.rotation
                                 : Quaternion.identity;

        ReplicateData replicateData = new ReplicateData(_lastHorizontal, _lastVertical, _lastJumpInput, _crouchTogglePressed, cameraRotation);

        _lastJumpInput       = false;
        _crouchTogglePressed = false;

        return replicateData;
    }

    public override void CreateReconcile() {
        ReconcileData rd = new ReconcileData(motor.TransientPosition,
                                             motor.TransientRotation,
                                             motor != null
                                                 ? motor.Velocity
                                                 : Vector3.zero,
                                             _currentCharacterState);

        PerformReconcile(rd);
    }

    [Replicate]
    private void PerformReplicate(ReplicateData rd, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable) {
        // Don't process movement if in vehicle (handle separately)
        if (CurrentCharacterState == CharacterState.InVehicleDriver || CurrentCharacterState == CharacterState.InVehiclePassenger) {
            return;
        }

        // Always use the tickDelta as your delta when performing actions inside replicate.
        float delta            = (float)TimeManager.TickDelta;
       // bool  useDefaultForces = false;

        /* When client only run some checks to
         * further predict the clients future movement.
         * This can keep the object more inlined with real-time by
         * guessing what the clients input might be before we
         * actually receive it.
         *
         * Doing this does risk a chance of graphical jitter in the
         * scenario a de-synchronization occurs, but if only predicting
         * a couple ticks the chances are low. */
        // See https:// fish-networking.gitbook.io/docs/manual/guides/prediction/version-2/creating-code/predicting-states
        // if (!IsServerStarted && !IsOwner) {
        //     /* If ticked then set last ticked value.
        //      * Ticked means the replicate is being run from the tick cycle, more
        //      * specifically NOT from a replay/reconcile. */
        //     if (state.ContainsTicked()) {
        //         /* Dispose of old should it have anything that needs to be cleaned up.
        //          * If you are only using value types in your data you do not need to call Dispose.
        //          * You must implement dispose manually to cache any non-value types, if you wish. */
        //         _lastTickedReplicateData.Dispose();
        //         // Set new.
        //         _lastTickedReplicateData = rd;
        //     }
        //     /* In the future means there is no way the data can be known to this client
        //      * yet. For example, the client is running this script locally and due to
        //      * how networking works, they have not yet received the latest information from
        //      * the server.
        //      *
        //      * If in the future then we are only going to predict up to
        //      * a certain amount of ticks in the future. This is us assuming that the
        //      * server (or client which owns this in this case) is going to use the
        //      * same input for at least X number of ticks. You can predict none, or as many
        //      * as you like, but the more inputs you predict the higher likeliness of guessing
        //      * wrong. If you do however predict wrong often smoothing will cover up the mistake. */
        //     else if (state.IsFuture()) {
        //         /* Predict up to 1 tick more. */
        //         if (rd.GetTick() - _lastTickedReplicateData.GetTick() > 1) {
        //             useDefaultForces = true;
        //         }
        //         else {
        //             /* If here we are predicting the future. */
        //
        //             /* You likely do not need to dispose rd here since it would be default
        //              * when state is 'not created'. We are simply doing it for good practice, should your ReplicateData
        //              * contain any garbage collection. */
        //             rd.Dispose();
        //
        //             rd = _lastTickedReplicateData;
        //
        //             /* There are some fields you might not want to predict, for example
        //              * jump. The odds of a client pressing jump two ticks in a row is unlikely.
        //              * The stamina check below would likely prevent such a scenario.
        //              *
        //              * We're going to unset jump for this reason. */
        //             //rd.Jump = false;
        //
        //             /* Be aware that future predicting is not a one-size fits all
        //              * feature. How much you predict into the future, if at all, depends
        //              * on your game mechanics and your desired outcome. */
        //         }
        //     }
        // }


        ProcessReplicatedInput(rd);

        // KinematicCharacterSystem.PreSimulationInterpolationUpdate(delta);
        //KinematicCharacterSystem.Simulate(delta, KinematicCharacterSystem.CharacterMotors, KinematicCharacterSystem.PhysicsMovers);
        // KinematicCharacterSystem.PostSimulationInterpolationUpdate(delta);


        // The KinematicCharacterMotor will call our UpdateVelocity
        // and UpdateRotation methods automatically
        // This is where the actual movement happens
    }

    /// <summary>
    /// Corrects the client if prediction was wrong
    /// [Reconcile] attribute tells Fish-Net this is for corrections
    /// </summary>
    /// <param name="rd">The correction data from server</param>
    /// <param name="channel">Network channel</param>
    [Reconcile]
    private void PerformReconcile(ReconcileData rd, Channel channel = Channel.Unreliable) {
        float delta = (float)TimeManager.TickDelta;
        // --- Debug Comparison ---
        // Calculate the difference between the server's state (rd) and the client's current predicted state.
        float positionError = Vector3.Distance(rd.Position, motor.TransientPosition);
        float rotationError = Quaternion.Angle(rd.Rotation, motor.TransientRotation);

        // Log the error for debugging. You can adjust the threshold to only log significant deviations.
        if (positionError > 0.5f) {
           // Debug.Log($"Reconcile difference on client {Owner.ClientId}. Pos error: {positionError:F4}");
            if (positionError > positionSnapThreshold) {
                motor.SetPosition(rd.Position, false);
            }
            else {
                Vector3 velocityCorrection = (rd.Position - motor.TransientPosition) / ReconsileSmoothTime;
                AddVelocity(velocityCorrection);
            }

        }

        if (rotationError > 20.1f) {
            //    Debug.Log($"Reconcile >> {Owner.ClientId}.  Rot error: {rotationError:F2} degrees.");
            //motor.SetRotation(rd.Rotation);
        }

        // --- End Debug Comparison ---

        // Update state
        if (_currentCharacterState != rd.State) {
            //   Debug.Log($"Character State mismatch  current: {_currentCharacterState} || RD State: {rd.State}");
            // _currentCharacterState = rd.State;
            SetState(rd.State);
        }
    }

    

    private void Start() {
        currentGravity   = initialGravity;
        currentMoveSpeed = 0f;
    }

    private void Update() {
        // Only the owner gathers input
        if (!IsOwner)
            return;

        if (Input.GetKeyDown(KeyCode.X)) motor.SetPosition(new Vector3(motor.TransientPosition.x + 2, motor.TransientPosition.y + 0.2f, motor.TransientPosition.z));

        // Only gather input - don't process movement!
        // Movement happens in TimeManager_OnTick
        GatherInputForPrediction();
    }

    /// <summary>
    /// Gathers input and stores it in local variables
    /// This runs every frame (Update) to capture all input
    /// The actual movement happens in TimeManager_OnTick
    /// </summary>
    private void GatherInputForPrediction() {
        if (!IsOwner)
            return;
        // Skip input if animation is locked
        if (animationLockManager != null && animationLockManager.ShouldBlockInput()) {
            _lastHorizontal      = 0f;
            _lastVertical        = 0f;
            _lastJumpInput       = false;
            _crouchTogglePressed = false;
            return;
        }

        // Handle vehicle input separately (not predicted)
        if (CurrentCharacterState == CharacterState.InVehicleDriver) {
            GatherVehicleInput();
            return;
        }

        // Get joystick input
        _lastHorizontal = UiManager.Instance.ultimateJoystick != null
                              ? UiManager.Instance.ultimateJoystick.HorizontalAxis
                              : 0f;

        _lastVertical = UiManager.Instance.ultimateJoystick != null
                            ? UiManager.Instance.ultimateJoystick.VerticalAxis
                            : 0f;

#if UNITY_EDITOR || !UNITY_ANDROID
        // Keyboard override for testing
        if (Input.GetKey(KeyCode.W)) _lastVertical   = 1f;
        if (Input.GetKey(KeyCode.S)) _lastVertical   = -1f;
        if (Input.GetKey(KeyCode.A)) _lastHorizontal = -1f;
        if (Input.GetKey(KeyCode.D)) _lastHorizontal = 1f;

        // Jump - GetKeyDown means "pressed this frame"
        if (Input.GetKeyDown(KeyCode.Space))
            _lastJumpInput = true;

        // Crouch toggle
        if (Input.GetKeyDown(KeyCode.C))
            _crouchTogglePressed = true;
#endif
    }

    private void ProcessReplicatedInput(ReplicateData md) {
        // Handle jump input
        if (md.Jump) {
            SetJumpInput(true);
        }

        // Handle crouch toggle
        if (md.Crouch) {
            ToggleCrouch();
        }

        // STORE the replicated camera rotation for use in UpdateRotation
        _replicatedCameraRotation = md.CamRotation;

        // Calculate the camera's forward and right vectors
        var cameraForward = Vector3.ProjectOnPlane(_replicatedCameraRotation * Vector3.forward, motor.CharacterUp)
                                   .normalized;
        var cameraRight = Vector3.Cross(Vector3.up, cameraForward)
                                 .normalized;

        // Calculate movement vector relative to camera
        _moveInputVector = (cameraForward * md.Vertical + cameraRight * md.Horizontal).normalized;
        _moveInputVector = Vector3.ClampMagnitude(_moveInputVector, 1f);

        // Set look direction to camera forward (for rotation)
        _lookInputVector = cameraForward;

        // Update movement state based on input magnitude
        if (!animationLockManager.ShouldBlockInput()) {
            UpdateMovementState(new Vector2(md.Horizontal, md.Vertical));
        }
    }

    #region Input Handling

    /// <summary>
    /// Gathers vehicle control input
    /// </summary>
    private void GatherVehicleInput() {
        if (playerCar == null) return;

        playerCar.controllerV4.fuelInput  = UiManager.Instance.GetInput(UiManager.Instance.gasButton);
        playerCar.controllerV4.brakeInput = UiManager.Instance.GetInput(UiManager.Instance.brakeButton);
        playerCar.controllerV4.steerInput = -UiManager.Instance.GetInput(UiManager.Instance.leftButton) + UiManager.Instance.GetInput(UiManager.Instance.rightButton);

#if UNITY_EDITOR || !UNITY_ANDROID
        // Keyboard override for vehicle
        if (Input.GetKey(KeyCode.Space)) playerCar.controllerV4.handbrakeInput = 1f;
        else playerCar.controllerV4.handbrakeInput                             = 0f;
        if (Input.GetKey(KeyCode.Z)) playerCar.controllerV4.fuelInput  = 1f;
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

    // And ADD this new method:
    private void OnJumpButtonPressed() {
        // Just set the flag, it will be read in GatherInputForPrediction
        _lastJumpInput = true;
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

    private void ExitVehicle() {
        if (playerCar is null || playerCarSeat is null) return;

        motor.transform.parent     = null;
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

        invehicle             = false;
        CurrentCharacterState = CharacterState.Idle;
    }

    private void UpdateVehicleAnimation() {
        if (playerCar != null && playerCar.controllerV4 != null) {
            bool isReversing = playerCar.controllerV4.direction == -1 && playerCar.controllerV4.speed > 5f;

            var newsteer = Mathf.Clamp(playerCar.controllerV4.FrontLeftWheelCollider.wheelCollider.steerAngle, -1, 1);
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

    public override void OnStartClient() {
        SetupPlayerCharacter();
        GameManager.Instance.PlayerSpawnedEventDispatcher();
    }

    public override void OnStopClient() {
        base.OnStopClient();
        if (IsOwner && GameManager.Instance != null)
            GameManager.Instance.EnableMainCamera();
    }

    public override void OnStartServer() {
        base.OnStartServer();
        bool isHostPlayer = Owner.IsLocalClient;
        if (orbitCamera != null && !isHostPlayer) {
            Destroy(orbitCamera.gameObject);
        }

        gameObject.name = ">Server::NET_PLAYER__" + Owner.ClientId;
    }

    private void SetupPlayerCharacter() {
        if (!IsOwner) {
            if (orbitCamera != null) {
                Destroy(orbitCamera.gameObject);
            }

            gameObject.name = "NET_PLAYER__" + Owner.ClientId;
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
        }
    }

    private void OnCrouchPressed() {
        _crouchTogglePressed = true;
    }

    private void OnDestroy() {
        if (IsOwner && UiManager.Instance != null) {
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