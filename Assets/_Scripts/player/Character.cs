using System;
using Animancer;
using core.Managers;
using core.player;
using core.Vehicles;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using KinematicCharacterController;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using FishNet.Object;
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

    public enum CharacterType {
        Player,
        NPC,
        NetworkPlayer
    }

    //public VariableJoystick joystick;
    public CharacterCam orbitCamera;

    public                        Transform               cameraTarget;
    public                        KinematicCharacterMotor motor;
    public                        NavMeshAgent            navMeshAgent;
    [Required("Required")] public PlayerStatus            PlayerStatus;

    //Rotation depends on camera or input
    private bool useInputForRotation = true;

    //set character type
    public CharacterType characterType = CharacterType.Player;

    [FoldoutGroup("Animation Parameters")] public PlayerAnimator pa;

    #region Movement

    //=== Movement
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

    [FoldoutGroup("Gravity")]                  public  float  initialGravity     = -9.81f; // Initial gravity force
    [FoldoutGroup("Gravity")]                  public  float  maxGravity         = -30f;   // Maximum gravity force
    [FoldoutGroup("Gravity")]                  public  float  gravityBuildUpTime = 0.5f;   // Time to reach max gravity
    [FoldoutGroup("Gravity")] [SerializeField] private float  FallTime;
    [FoldoutGroup("Gravity")] [SerializeField] private double fallThreshold = 1.1f;

    #endregion

    [BoxGroup("Current Character state")] [SerializeField] private CharacterState _currentCharacterState = CharacterState.Idle;

    //ai
    public enum MovementSpeed {
        Walk,
        Run
    }

    private const float StoppingDistance = 0.3f;

    MovementSpeed  currentMovementSpeed = MovementSpeed.Walk;
    public  Action OnNpcReachedDestination;
    private bool   shouldMove;

    #endregion

    private void OnStateChanged(CharacterState prev, CharacterState next, bool asServer) {
        // Update animations or other visuals based on the new state.
        // The 'asServer' flag is true if this is running on the server.
        if (!asServer) {
            UpdateAnimation();
        }
    }

    public CharacterState CurrentCharacterState {
        get => _currentCharacterState;
        set {
            if (_currentCharacterState == value) return;

            // if (_currentCharacterState == CharacterState.InVehicleDriver && value != CharacterState.InVehicleDriver)
            //     Debug.Log($"{_currentCharacterState} to {value}");
            if (value == CharacterState.Falling) is_Falling = true;
            _currentCharacterState = value;
            OnCharacterStateChanged?.Invoke(value);
            UpdateAnimation();
        }
    }

    // [ServerRpc]
    // private void ServerSetCharacterState(CharacterState newState) {
    //     // This runs on the server. The server has authority over the state.
    //     SetState(newState);
    //     Debug.Log($"SERVER: Changing user state");
    // }

    #region ------- Events

    public event Action<CharacterState> OnCharacterStateChanged;

    #endregion

    #region Movement Input

    public  float   Steeringsmoothness = 5f;
    private float   _forwardAxis;
    private Vector3 _lookInputVector;
    private Vector3 _moveInputVector;
    private float   _rightAxis;
    private Vector3 moveInputVector;

    // Movement transition variables
    [FoldoutGroup("Movement/Transitions")] public float movementTransitionSpeed = 8f; // How fast to transition between movement states
    private                                  float currentMoveSpeed;
    private                                  float targetMoveSpeed;
    private                                  float speedChangeVelocity; // Used for SmoothDamp

    #endregion

    #region Gravity and Falling

    private float currentGravity;
    private float fallStartTime;
    private float gravityVelocity;
    private bool  is_Falling;

    #endregion

    #region Jumping

    private float jumpTime;
    private float jumpDurationTime;
    private bool  jumpInput;
    private float lastGroundedTime;
    private float lastJumpPressedTime;

    #endregion

    #region Vehicle

    public CarSeat? playerCarSeat;

    #endregion

    #region --- Misc

    public bool isFrozen;
// Add this field to your Character class
    public AnimationLockManager animationLockManager;

    private void InitializeAnimationLockManager() {
        animationLockManager = new AnimationLockManager(this);
    }

    #endregion

    private void Awake() {
        navMeshAgent.transform.SetParent(transform, false);
        motor.CharacterController = this;
        switch (characterType) {
            case CharacterType.Player:
            case CharacterType.NetworkPlayer:
            case CharacterType.NPC: {
                break;
            }
        }

        InitializeAnimationLockManager();
    }

    private void Start() {
        //gravity
        currentGravity   = initialGravity;
        currentMoveSpeed = 0f; // Initialize movement speed
    }

    private void OnJumpPressed() {
        SetJumpInput(true);
    }

    private void Update() 
    {
     HandleInput();
    }    

    /// <summary>
    ///     (Called by KinematicCharacterMotor during its update cycle)
    ///     This is called before the character begins its movement update
    /// </summary>
    public void BeforeCharacterUpdate(float deltaTime) { }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime) {
        if (characterType == CharacterType.Player && orbitCamera != null) {
            if (useInputForRotation && _moveInputVector.sqrMagnitude > 0.01f) {
                // Rotate towards joystick direction when running
                Vector3 moveDir = _moveInputVector;
                moveDir.y = 0f;
                moveDir.Normalize();
                Vector3 smoothedLookDirection = Vector3.Slerp(motor.CharacterForward, moveDir, 1 - Mathf.Exp(-orientationSharpness * deltaTime))
                                                       .normalized;
                currentRotation = Quaternion.LookRotation(smoothedLookDirection, motor.CharacterUp);
            }
            else {
                // Default: rotate towards camera forward
                Vector3 cameraForward = orbitCamera.transform.forward;


                // float angle       = Vector3.Angle(motor.CharacterForward, _lookInputVector);
                // float signedAngle = Vector3.SignedAngle(motor.CharacterForward, cameraForward, Vector3.up);
                //
                //
                // if (angle > 30f && CurrentCharacterState == CharacterState.Idle) {
                //     AnimancerState _turning_animationState;
                //     if (signedAngle > 0) {
                //
                //         if (!pa.isActionPlaying(pa.CHARACTER_ROTATE_RIGHT)) {
                //             pa.PlayTimedAction(pa.CHARACTER_ROTATE_RIGHT);
                //         }
                //         
                //         // Turn right
                //         // if (!pa.isPlayingAnimation(pa.anim_turnRight)) {
                //         //     _turning_animationState = pa.PlayAnimation(pa.anim_turnRight, 0.2f);
                //         //     _turning_animationState.Events(this)
                //         //                            .OnEnd = () => { pa.PlayAnimation(pa.anim_idle); };
                //         // }
                //     }
                //     else {
                //         if (!pa.isActionPlaying(pa.CHARACTER_ROTATE_LEFT)) {
                //             pa.PlayTimedAction(pa.CHARACTER_ROTATE_LEFT);
                //         }
                //         
                //         // Turn left
                //         // if (!pa.isPlayingAnimation(pa.anim_turnLeft)) {
                //         //     _turning_animationState = pa.PlayAnimation(pa.anim_turnLeft, 0.2f);
                //         //     _turning_animationState.Events(this)
                //         //                            .OnEnd = () => { pa.PlayAnimation(pa.anim_idle); };
                //         // }
                //     }
                // }


                cameraForward.y = 0f;
                cameraForward.Normalize();
                Vector3 smoothedLookDirection = Vector3.Slerp(motor.CharacterForward, cameraForward, 1 - Mathf.Exp(-orientationSharpness * deltaTime))
                                                       .normalized;
                currentRotation = Quaternion.LookRotation(smoothedLookDirection, motor.CharacterUp);
            }
        }
        else if (motor.GroundingStatus.IsStableOnGround) {
            // Keep existing logic for NPCs and others
            Vector3 smoothedLookInputDirection = Vector3.Slerp(motor.CharacterForward, _lookInputVector, 1 - Mathf.Exp(-orientationSharpness * deltaTime))
                                                        .normalized;
            currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, motor.CharacterUp);
        }
        else {
            // Air control
            if (!(_moveInputVector.sqrMagnitude > 0.01f)) return;
            Vector3 smoothedLookInputDirection = Vector3.Slerp(motor.CharacterForward, _lookInputVector, 1 - Mathf.Exp(-airOrientationSharpness * deltaTime))
                                                        .normalized;
            currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, motor.CharacterUp);
        }
    }

    public void SetDestination(Vector3 destination, MovementSpeed speed = MovementSpeed.Walk) {
        navMeshAgent.SetDestination(destination);
        shouldMove           = true;
        currentMovementSpeed = speed;

        navMeshAgent.speed = speed == MovementSpeed.Walk
                                 ? STRAFE_SPEED
                                 : RUNNING_SPEED;

        CurrentCharacterState = speed == MovementSpeed.Walk
                                    ? CharacterState.Strafe
                                    : CharacterState.Running;
        //UpdateAnimation();
    }

    /// <summary>
    ///     (Called by KinematicCharacterMotor during its update cycle)
    ///     This is where you tell your character what its velocity should be right now.
    ///     This is the ONLY place where you can set the character's velocity
    /// </summary>
    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime) {
        if (motor.GroundingStatus.IsStableOnGround) {
            switch (characterType) {
                case CharacterType.NPC:
                    Vector3 effectiveGroundNormal  = motor.GroundingStatus.GroundNormal;
                    Vector3 targetMovementVelocity = Vector3.ProjectOnPlane(moveInputVector * navMeshAgent.speed, effectiveGroundNormal);

                    currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1 - Mathf.Exp(-StableMovementSharpness * deltaTime));

                    break;
                case CharacterType.Player:
                    HandleGroundedMovement(ref currentVelocity, deltaTime);
                    break;
            }
        }
        else {
            HandleAirborneMovement(ref currentVelocity, deltaTime);
        }

        if (!animationLockManager.ShouldBlockInput()) {
            switch (characterType) {
                case CharacterType.Player:
                    UpdateMovementState(UiManager.Instance.ultimateJoystick.Directions);
                    break;
                case CharacterType.NPC:
                    UpdateMovementState(moveInputVector);
                    break;
            }
        }

        if (isFrozen) currentVelocity = Vector3.zero;
    }

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
                if (directions.y > 0.4f) {
                    useInputForRotation = true;
                }
                else useInputForRotation = false;
            }

            else {
                useInputForRotation = false;
                pa.resetCrouchAnimation();
                pa.PlayAnimation(pa.crouch_idle, 0.3f);
            }

            return;
        }


        //here get best suitable state from user input ( joystick direction)


        CharacterState newstate = CharacterState.Idle;
        if (directions.magnitude < 0.1f) {
            SetState(CharacterState.Idle);
            return;
        }

        if (directions.magnitude > 0.2f && directions.magnitude <= 0.8f || directions.y < 0.8f) {
            //Strafing require frequent updates to avoid animation stuck
            if (!isJumping)
                pa.UpdateStrafeAnimation();

            if (useInputForRotation) useInputForRotation = false;
            SetState(CharacterState.Strafe);
        }

        if (directions.y > 0.4f) {
            //  pa.ResetStrafeAnimation();
            SetState(CharacterState.Running);

            //  MaxStableMoveSpeed = 6;
        }
    }

    private void SetState(CharacterState newstae) {
        if (CurrentCharacterState == newstae) return;

        HandleOldStateExitActions(newstae);

        switch (newstae) {
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
            case CharacterState.Jumping:
                break;
            case CharacterState.Falling:
                break;
            case CharacterState.InVehicleDriver:
                invehicle = true;
                break;
            case CharacterState.InVehiclePassenger:
                invehicle = true;
                break;
        }

        CurrentCharacterState = newstae;
        OnCharacterStateChanged?.Invoke(newstae);
    }

    private void HandleOldStateExitActions(CharacterState oldstate) {
        switch (oldstate) {
            case CharacterState.Strafe:
                pa.ResetStrafeAnimation();
                break;
            case CharacterState.Running:
                useInputForRotation = false;
                break;
        }
    }

    public void StopMoving() {
        shouldMove      = false;
        moveInputVector = Vector3.zero;
        navMeshAgent.ResetPath();
        CurrentCharacterState = CharacterState.Idle;
        UpdateAnimation();
    }

    private void HandleGroundedMovement(ref Vector3 currentVelocity, float deltaTime) {
        // Check for jump input
        if (jumpInput && CanJump()) {
            //if character is crouched we uncrouch


            InitiateJump(ref currentVelocity);
        }

        else {
            // Update grounding status
            lastGroundedTime = Time.time;

            // Reorient velocity on a slopeo
            currentVelocity = ReorientVelocityOnSlope(currentVelocity);

            // Calculate and apply target velocity
            Vector3 targetMovementVelocity = CalculateTargetGroundVelocity();
            currentVelocity = SmoothVelocity(currentVelocity, targetMovementVelocity, StableMovementSharpness, deltaTime);
        }
    }

    private void HandleAirborneMovement(ref Vector3 currentVelocity, float deltaTime) {
        if (isJumping && jumpTime < 0.2f) // Maintain upward velocity for 0.2 seconds
        {
            // Maintain or slightly reduce upward velocity
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

        // Apply air drag
        currentVelocity *= 1f / (1f + drag * deltaTime);
    }

    private Vector3 ReorientVelocityOnSlope(Vector3 velocity) {
        return motor.GetDirectionTangentToSurface(velocity, motor.GroundingStatus.GroundNormal) * velocity.magnitude;
    }

    /// <summary>
    /// Calculate the target ground velocity based on the input direction and grounded normal
    /// </summary>
    /// <returns>The target ground velocity</returns>
    private Vector3 CalculateTargetGroundVelocity() {
        Vector3 inputRight = Vector3.Cross(_moveInputVector, motor.CharacterUp);
        Vector3 reorientedInput = Vector3.Cross(motor.GroundingStatus.GroundNormal, inputRight)
                                         .normalized *
                                  _moveInputVector.magnitude;
        return reorientedInput * MaxStableMoveSpeed;
    }

    /// <summary>
    /// Smoothly changes the current velocity towards the target velocity
    /// </summary>
    /// <param name="currentVelocity">The current velocity</param>
    /// <param name="targetVelocity">The target velocity</param>
    /// <param name="smoothing">The smoothing factor</param>
    /// <param name="deltaTime">The delta time</param>
    /// <returns>The smoothed velocity</returns>
    private Vector3 SmoothVelocity(Vector3 currentVelocity, Vector3 targetVelocity, float smoothing, float deltaTime) {
        return Vector3.Lerp(currentVelocity, targetVelocity, 1 - Mathf.Exp(-smoothing * deltaTime));
    }

    private void InitiateJump(ref Vector3 currentVelocity) {
        if (CurrentCharacterState == CharacterState.Crouched) {
            ToggleCrouch();
            jumpInput = false; // Prevent jump after uncrouch
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

        if (jumpingAnimationState != null)
            jumpingAnimationState.Events(this)
                                 .OnEnd = () => {
                isJumping = false;

                CheckForFalling();
            };


        isJumping             = true;
        jumpTime              = 0f;
        CurrentCharacterState = CharacterState.Jumping;
        ApplyJumpForce(ref currentVelocity);
    }

    private void UpdateJumpState(float deltaTime) {
        jumpTime += deltaTime; // ← MOVED THIS TO ALWAYS INCREMENT

        if (jumpTime >= maxJumpTime) {
            if (_currentCharacterState != CharacterState.Falling) {
                isJumping             = false;
                is_Falling            = true;      // ← ADD THIS
                fallStartTime         = Time.time; // ← ADD THIS
                FallTime              = 0f;        // ← ADD THIS - Reset fall timer
                CurrentCharacterState = CharacterState.Falling;
            }
        }
        else {
            // Ensure the character stays in the Jumping state for a minimum duration
            if (_currentCharacterState != CharacterState.Jumping) {
                CurrentCharacterState = CharacterState.Jumping;
            }
        }
    }

    private void UpdateJumpStatex(float deltaTime) {
        if (jumpTime >= maxJumpTime) {
            if (_currentCharacterState != CharacterState.Falling) {
                isJumping             = false;
                CurrentCharacterState = CharacterState.Falling;
            }
        }
        else {
            jumpTime += deltaTime;
            // Ensure the character stays in the Jumping state for a minimum duration
            if (_currentCharacterState != CharacterState.Jumping) {
                CurrentCharacterState = CharacterState.Jumping;
            }
        }
    }

    /// <summary>
    ///     (Called by KinematicCharacterMotor during its update cycle)
    ///     This is called after the character has finished its movement update
    /// </summary>

    #region core functions

    private void HandleLanding() {
        if (motor.GroundingStatus.IsStableOnGround) {
            if (is_Falling || CurrentCharacterState == CharacterState.Falling) {
                AnimancerState landingState = null;

                switch (FallTime) {
                    case > 0.8f: // Hard landing
                        if (!pa.isPlayingAnimation(pa.anim_land_med)) {
                            landingState = pa.PlayAnimation(pa.anim_land_med, 0.2f);

                            // Lock player control until animation finishes
                            animationLockManager.LockForAnimation(landingState,
                                                                  AnimationLockManager.AnimationLockType.HardLanding,
                                                                  onUnlock: () => {
                                                                      CurrentCharacterState = CharacterState.Idle;
                                                                      pa.PlayAnimation(pa.anim_idle);
                                                                  });
                        }

                        break;

                    case > 0.2f: // Medium landing
                        if (!pa.isPlayingAnimation(pa.anim_land_low)) {
                            landingState = pa.PlayAnimation(pa.anim_land_low, 0.2f);

                            // Optional: lock for medium landing too
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

    public void AfterCharacterUpdate(float deltaTime) {
        HandleLanding();
        // Check if we've landed
        // if (motor.GroundingStatus.IsStableOnGround) {
        //     if (is_Falling || CurrentCharacterState == CharacterState.Falling) {
        //         //todo more animations for higher falls
        //
        //         AnimancerState s = null;
        //         switch (FallTime) {
        //             case > 1f:
        //                 if (!pa.isPlayingAnimation(pa.anim_land_med))
        //                    // isFrozen = true;
        //                   s =  pa.PlayAnimation(pa.anim_land_med, 0.2f);
        //                 break;
        //             case > 0.5f:
        //                 if (!pa.isPlayingAnimation(pa.anim_land_low))
        //                     pa.PlayAnimation(pa.anim_land_low,0.2f);
        //                 break;
        //         }
        //
        //         if (s != null)
        //             s.Events(this)
        //              .OnEnd += () => {
        //                 Debug.Log("falling ended");
        //                 if (isFrozen) isFrozen = false;
        //
        //                 pa.PlayAnimation(pa.anim_idle);
        //                 CurrentCharacterState = CharacterState.Idle;
        //             };
        //
        //         is_Falling = false;
        //     }
        //
        //     FallTime = 0f;
        // }


        //check if the assigned vehicle is still in distance
        if (playerCar != null) {
            if (Vector3.Distance(playerCar.transform.position, motor.transform.position) > 5f) {
                playerCar = null;
                UiManager.Instance.ShowVehicleEnterExitButtons(false, false, false);
            }
        }
    }

    public bool IsColliderValidForCollisions(Collider coll) {
        return true;
    }

    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }

    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) {
        if (hitCollider.CompareTag("Vehicle")) {
            var nearbyVehicle = hitCollider.GetComponentInParent<CarController>();
            if (nearbyVehicle is null) return;

            //enable ui buttons

            if (characterType == CharacterType.Player)
                UiManager.Instance.ShowVehicleEnterExitButtons(true,
                                                               nearbyVehicle.driverSeat.used,
                                                               nearbyVehicle.GetFreeCarSeats()
                                                                            .Count >
                                                               0);

            if (playerCar is null || playerCar != nearbyVehicle) {
                //assign car
                playerCar = nearbyVehicle;
            }
        }


        // if (hitCollider.CompareTag("Vehicle"))
        // {
        //     if (playerCar is null)
        //     {
        //         playerCar = hitCollider.GetComponentInParent<CarController>();
        //
        //         if (playerCar != null)
        //         {
        //             if (!playerCar.driverSeat.used) UiManager.Instance.cardoorEnterDriver.gameObject.SetActive(true);
        //
        //             if (playerCar.GetFreeCarSeats().Count > 0)
        //                 UiManager.Instance.cardoorEnterPassanger.gameObject.SetActive(true);
        //         }
        //     }
        // }
        // else
        // {
        //     if (playerCar != null)
        //     {
        //         UiManager.Instance.cardoorEnterDriver.gameObject.SetActive(false);
        //         UiManager.Instance.cardoorEnterPassanger.gameObject.SetActive(false);
        //         playerCar = null;
        //     }
        // }
    }

    public void PostGroundingUpdate(float deltaTime) {
        if (motor.GroundingStatus.IsStableOnGround) {
            ResetJump();
        }
    }

    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) { }

    public void OnDiscreteCollisionDetected(Collider hitCollider) { }

    #endregion

    private void ApplySmoothGravity(ref Vector3 currentVelocity, float deltaTime) {
        // Gradually increase gravity
        currentGravity = Mathf.SmoothDamp(currentGravity, maxGravity, ref gravityVelocity, gravityBuildUpTime);

        // Apply gravity
        currentVelocity.y += currentGravity * deltaTime;
    }

    /// <summary>
    /// Checks and handles the falling state of the character.
    /// </summary>
    private void CheckForFalling() {
        if (CurrentCharacterState == CharacterState.Falling) {
            FallTime += Time.deltaTime;
            if (FallTime >= 0.5f && FallTime < 1f && !pa.isPlayingAnimation(pa.anim_falling_second)) {
                pa.PlayAnimation(pa.anim_falling_second, 0.3f);
            }

            if (FallTime > 1f)
                pa.PlayAnimation(pa.anim_falling_loop, 0.5f);

            return;
        }

        // If the character is jumping, don't check for falling
        if (isJumping) {
            return;
        }

        // If the character just started falling, record the start time
        if (!is_Falling) {
            fallStartTime = Time.time;
            is_Falling    = true;
        }
        // If the character has been falling for longer than the threshold, change state to Falling
        else if (Time.time - fallStartTime >= fallThreshold) {
            CurrentCharacterState = CharacterState.Falling;
        }
    }

    /// <summary>
    ///     entering a vehicle as a driver
    /// </summary>
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

    /// <summary>
    ///     entering a vehicle cand changing state
    /// </summary>
    private void OnCardoorPassangerClicked() {
        if (playerCar != null) {
            playerCarSeat = playerCar.SetPlayerInVehicle(this, pa);
            pa.PlayAnimation(pa.anim_Vehicle_Passenger_sit);
        }
    }

    /// <summary>
    ///     entering a vehicle cand changing state
    /// </summary>
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

    /// <summary>
    /// Handles the process of exiting a vehicle for the player character.
    /// </summary>
    private void ExitVehicle() {
        // Check if the player is currently in a vehicle
        if (playerCar is null || playerCarSeat is null) return;

        // Reset player's transform and enable collision detection
        motor.transform.parent     = null;
        motor.transform.localScale = Vector3.one;
        motor.SetPositionAndRotation(playerCarSeat.Value.exitpos.position, Quaternion.identity);
        TogglePlayerCollisionDetection(true);
        motor.enabled = true;

        // Handle vehicle-specific actions
        if (!playerCar.controllerV4.engineRunning) StopCoroutine(playerCar.controllerV4.StartEngineDelayed());
        if (playerCarSeat.Value.isDriver) {
            playerCar.controllerV4.KillEngine();
            playerCar.driverSeat.used = false;
        }

        // Set vehicle state
        playerCar.controllerV4.handbrakeInput = 1f;
        playerCarSeat.Value.FreetheSeat();
        playerCarSeat = null;

        // Disable navigation and play exit sound
        navMeshAgent.enabled = false;
        playerCar.fx_CloseDoorSound();

        // Update camera colliders
        if (orbitCamera.IgnoredColliders.Contains(playerCar.VehicleCollider))
            orbitCamera.IgnoredColliders.Remove(playerCar.VehicleCollider);

        // Update UI
        UiManager.Instance.ShowControllerPage();

        // Stop vehicle-related animations
        pa.FadeOutActionLayer();

        // Update character state
        invehicle             = false;
        CurrentCharacterState = CharacterState.Idle;
    }

    public void SetInputs(ref PlayerCharacterInputs inputs) {
        // Get the camera's forward and right vectors
        var cameraForward = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.forward, Vector3.up)
                                   .normalized;
        var cameraRight = Vector3.Cross(Vector3.up, cameraForward)
                                 .normalized;

        // Calculate move input based on camera orientation
        _moveInputVector = (cameraForward * inputs.MoveAxisVertical + cameraRight * inputs.MoveAxisHorizontal).normalized;

        // Clamp the magnitude of the input vector
        _moveInputVector = Vector3.ClampMagnitude(_moveInputVector, 1f);

        // Set look direction based on movement or camera forward if not moving
        _lookInputVector = _moveInputVector.sqrMagnitude > 0.01f
                               ? _moveInputVector
                               : cameraForward;
    }



    public override void OnStartClient() {
        base.OnStartClient();
        SetupPlayerCharacter();
    }

    private void SetupPlayerCharacter() {
        if (!IsOwner) {
            var OwnerClientId = NetworkManager.ClientManager.Connection;
            if (orbitCamera != null) Destroy(orbitCamera.gameObject);
            gameObject.name = "CHAR_ NETWORK_[ " + OwnerClientId.ClientId + " ]";
        }
        else {
            gameObject.name = "MAIN PLAYER ---- >>";
            if (orbitCamera != null) {
                orbitCamera.transform.parent = null;
                orbitCamera.gameObject.name  = "Local Player Cam";
            }


            UiManager.Instance.OnCardoorDriverButtonClicked    += OnCardoorDriverClicked;
            UiManager.Instance.OnCardoorPassangerButtonClicked += OnCardoorPassangerClicked;
            UiManager.Instance.OnCardoorExitButtonClicked      += OnCardoorExitClicked;
            UiManager.Instance.OnJumpPressed                   += OnJumpPressed;
            UiManager.Instance.SetupEventListeners(); // jump, run etc buttons

            GameManager.Instance.DisableMainCamera();
        }
    }

    private void SetupNPCCharacter() {
        gameObject.name = "CHAR_[NPC]";
        if (orbitCamera != null && orbitCamera.gameObject != null) {
            Destroy(orbitCamera.gameObject);
        }

        if (navMeshAgent != null) {
            navMeshAgent.stoppingDistance = StoppingDistance;
        }
    }

    private void SetupNetworkCharacter() {
        gameObject.name = "CHAR_[NETWORK]";
        if (orbitCamera != null && orbitCamera.gameObject != null) {
            Destroy(orbitCamera.gameObject);
        }
    }

    private void HandleInput() {
        switch (characterType) {
            case CharacterType.Player:
                HandlePlayerInput();
                break;
            case CharacterType.NPC:
                HandleAIMovement();
                break;
        }
    }

    private void HandlePlayerInput() {
        // This check is crucial! It ensures this code only runs
        // on the instance of the character that you own.
        // if (!IsOwner) {
        //     return;
        // }
        // Check if input should be blocked
        if (animationLockManager != null && animationLockManager.ShouldBlockInput()) {
            // Still allow camera movement if desired

            return;
        }

        var characterInputs = new PlayerCharacterInputs {
            MoveAxisHorizontal = UiManager.Instance.ultimateJoystick != null
                                     ? UiManager.Instance.ultimateJoystick.HorizontalAxis
                                     : 0f,
            MoveAxisVertical = UiManager.Instance.ultimateJoystick != null
                                   ? UiManager.Instance.ultimateJoystick.VerticalAxis
                                   : 0f,
            CameraRotation = orbitCamera != null
                                 ? orbitCamera.transform.rotation
                                 : Quaternion.identity
        };

        switch (CurrentCharacterState) {
            case CharacterState.InVehicleDriver:
                HandleVehicleDriverInput();
                break;
            case CharacterState.InVehiclePassenger:
                // Any specific passenger input handling can go here
                break;
            default:
                HandleNormalCharacterInput();
                break;
        }

#if UNITY_EDITOR || !UNITY_ANDROID
        if (Input.GetKeyDown(KeyCode.Space)) SetJumpInput(true);
        if (Input.GetKeyDown(KeyCode.C)) ToggleCrouch();
#endif

        SetInputs(ref characterInputs);
    }

    // Override input handling to respect locks
    private void HandlePlayerInputx() {
        // Check if input should be blocked
        if (animationLockManager != null && animationLockManager.ShouldBlockInput()) {
            // Still allow camera movement if desired
            Debug.Log("Input blocked");
            return;
        }

        // Your existing input handling code here
        var characterInputs = new PlayerCharacterInputs {
            MoveAxisHorizontal = UiManager.Instance.ultimateJoystick != null
                                     ? UiManager.Instance.ultimateJoystick.HorizontalAxis
                                     : 0f,
            MoveAxisVertical = UiManager.Instance.ultimateJoystick != null
                                   ? UiManager.Instance.ultimateJoystick.VerticalAxis
                                   : 0f,
            CameraRotation = orbitCamera != null
                                 ? orbitCamera.transform.rotation
                                 : Quaternion.identity
        };

        // Rest of your input handling...
    }

    private void ToggleCrouch() {
        if (_currentCharacterState == CharacterState.Crouched) {
            pa.resetCrouchAnimation();
            SetState(CharacterState.Idle);
        }
        else SetState(CharacterState.Crouched);
    }

    private void HandleAIMovement() {
        if (!shouldMove) {
            moveInputVector = Vector3.zero;
            return;
        }

        if (navMeshAgent != null) {
            navMeshAgent.transform.localPosition = Vector3.zero;
            if (navMeshAgent.pathPending) {
                return;
            }

            if (navMeshAgent.isOnNavMesh && navMeshAgent.remainingDistance <= StoppingDistance) {
                StopMoving();
                OnNpcReachedDestination?.Invoke();
                return;
            }

            moveInputVector  = navMeshAgent.desiredVelocity.normalized;
            _lookInputVector = moveInputVector;
        }
    }

    private void HandleNormalCharacterInput() {
        // Movement input is now handled in UpdateVelocity
        // We don't need to set the character state here anymore
    }

    private void HandleVehicleDriverInput() {
        if (playerCar != null) {
            playerCar.controllerV4.fuelInput  = UiManager.Instance.GetInput(UiManager.Instance.gasButton);
            playerCar.controllerV4.brakeInput = UiManager.Instance.GetInput(UiManager.Instance.brakeButton);
            playerCar.controllerV4.steerInput = -UiManager.Instance.GetInput(UiManager.Instance.leftButton) + UiManager.Instance.GetInput(UiManager.Instance.rightButton);


#if UNITY_EDITOR || !UNITY_ANDROID
            if (Input.GetKey(KeyCode.Space)) playerCar.controllerV4.handbrakeInput = 1f;
            else playerCar.controllerV4.handbrakeInput                             = 0f;
            if (Input.GetKey(KeyCode.Z)) playerCar.controllerV4.fuelInput  = 1f;
            if (Input.GetKey(KeyCode.S)) playerCar.controllerV4.brakeInput = 1;
            if (Input.GetKey(KeyCode.D)) playerCar.controllerV4.steerInput = 1;
            if (Input.GetKey(KeyCode.Q)) playerCar.controllerV4.steerInput = -1;
#endif

            UpdateVehicleAnimation();
        }
    }

    /// <summary>
    /// Updates the vehicle's animation based on its state, such as reversing or steering.
    /// </summary>
    private void UpdateVehicleAnimation() {
        if (playerCar != null && playerCar.controllerV4 != null) {
            bool isReversing = playerCar.controllerV4.direction == -1 && playerCar.controllerV4.speed > 5f;


            // else
            // {
            //     if (pa.IsActionLayerPlaying()) pa.FadeOutActionLayer(0.2f);
            // }

            var newsteer = Mathf.Clamp(playerCar.controllerV4.FrontLeftWheelCollider.WheelCollider.steerAngle, -1, 1);
            pa.UpdateVehicleSteering_Mixer(newsteer, Steeringsmoothness);

            if (isReversing) {
                if (!pa.isActionPlaying(pa.ACTION_DRIVER_REVERSE))
                    pa.PlayActionAnimation(pa.ACTION_DRIVER_REVERSE);
            }
        }
    }

    private void UpdateAnimation() {
        float targetAnimSpeed;

        switch (CurrentCharacterState) {
            case CharacterState.Idle:
                pa.PlayAnimation(pa.anim_idle, 0.4f);
                break;
            case CharacterState.Strafe:
                pa.UpdateStrafeAnimation();
                break;
            case CharacterState.Running:

                //pa.PlayAnimationWithSlowIn()
                pa.PlayAnimation(pa.anim_run, 0.4f);
                break;

            case CharacterState.Jumping:
                // Jump animations are handled in InitiateJump method
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

    private void HandleVehicleAnimation(bool isDriver) {
        if (playerCar == null) return;
        if (isDriver)
            pa.UpdateVehicleSteering_Mixer(0, Steeringsmoothness);
        else
            pa.PlayAnimation(pa.anim_Vehicle_Passenger_sit);
    }

    public struct PlayerCharacterInputs {
        public float MoveAxisVertical;

        public float MoveAxisHorizontal;

        public Quaternion CameraRotation;
    }

    #region Editor only

#if UNITY_EDITOR

    private void OnDrawGizmos() {
        if (navMeshAgent == null || !navMeshAgent.hasPath)
            return;

        Gizmos.color = Color.green;

        Vector3[] corners = navMeshAgent.path.corners;
        for (int i = 0; i < corners.Length - 1; i++) {
            Gizmos.DrawLine(corners[i], corners[i + 1]);
        }

        // Draw spheres in each corner for better visibility
        foreach (Vector3 corner in corners) {
            Gizmos.DrawSphere(corner, 0.1f);
        }

        // Draw a line and sphere to show the current target position
        if (navMeshAgent.hasPath) {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, navMeshAgent.destination);
            Gizmos.DrawSphere(navMeshAgent.destination, 0.2f);
        }
    }
#endif

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

    private void ApplyJumpForce(ref Vector3 currentVelocity) {
        float jumpForce = (CurrentCharacterState == CharacterState.Idle)
                              ? IdlejumpForce
                              : RunningjumpForce;
        currentVelocity.y = Mathf.Max(currentVelocity.y + jumpForce, jumpForce);
        motor.ForceUnground();
    }

    private void ResetJump() {
        if (isJumping) {
            isJumping = false;
        }
    }

    #endregion
}