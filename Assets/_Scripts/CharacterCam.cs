using System;
using System.Collections.Generic;
using System.Linq;
using core.Managers;
using DG.Tweening;
using FishNet.Object.Prediction;
using Sirenix.OdinInspector;
using UI_;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Serialization;

public enum CameraState {
    Character,
    Vehicle,
    Death
}

/// <summary>
/// Manages the camera behavior for both character and vehicle perspectives.
/// </summary>
public class CharacterCam : MonoBehaviour {
    #region Inspector Fields

    public GlobalDataSO globalData;

    [BoxGroup("FPP-TPP")] public bool useFPP;

    [BoxGroup("General")] public CameraState cameraState;
    [BoxGroup("General")] public GameObject  camTarget;
    [SerializeField] private float smoothSpeed = 8f; // How fast to interpolate
    private Vector3 desiredLocalPosition;
    private Vector3 velocity = Vector3.zero;
    [Space]
    
    [BoxGroup("General")] public float   CamSensitivity     = 0.2f;
    [BoxGroup("Framing")] public Vector2 FollowPointFraming = new Vector2(0f, 0f);
    [BoxGroup("Framing")] public float   FollowingSharpness = 10000f;

    [BoxGroup("Distance")] public float DefaultDistance           = 2.5f;
    [BoxGroup("Distance")] public float MinDistance               = 1f;
    [BoxGroup("Distance")] public float MaxDistance               = 3.2f;
    [BoxGroup("Distance")] public float DistanceMovementSpeed     = 5f;
    [BoxGroup("Distance")] public float DistanceMovementSharpness = 10f;

    [BoxGroup("Rotation")]                    public bool  InvertX;
    [BoxGroup("Rotation")]                    public bool  InvertY;
    [BoxGroup("Rotation")] [Range(-90f, 90f)] public float DefaultVerticalAngle = 20f;
    [BoxGroup("Rotation")] [Range(-90f, 90f)] public float MinVerticalAngle     = -90f;
    [BoxGroup("Rotation")] [Range(-90f, 90f)] public float MaxVerticalAngle     = 90f;
    [BoxGroup("Rotation")]                    public float RotationSpeed        = 1f;

    [BoxGroup("Rotation")]    public float RotationSharpness      = 10000f;
    [BoxGroup("Obstruction")] public float ObstructionCheckRadius = 0.2f;

    [BoxGroup("Obstruction")] public LayerMask      ObstructionLayers    = -1;
    [BoxGroup("Obstruction")] public float          ObstructionSharpness = 10000f;
    [BoxGroup("Obstruction")] public List<Collider> IgnoredColliders     = new List<Collider>();

    [BoxGroup("Vehicle")] public float VehicleRotationSharpness = 5f;
    [BoxGroup("Vehicle")] public float VehicleMinDistance       = 6f;
    [BoxGroup("Vehicle")] public float VehicleMaxDistance       = 9f;
    [BoxGroup("Vehicle")] public float VehicleRotationSpeed     = 0.5f;
    [BoxGroup("Vehicle")] public float VehicleVerticalAngle     = 10f;
    [BoxGroup("Vehicle")] public float VehicleMinVerticalAngle  = -20f;
    [BoxGroup("Vehicle")] public float VehicleMaxVerticalAngle  = 80f;

    [BoxGroup("UI")] public UiDrag uiDrag;

    [FormerlySerializedAs("CameraDistanceMinMax")] public Vector2 cameraDistanceMinMax = new(0.5f, 5f);

    #endregion

    #region Private Fields

    private Camera    PlayerCam;
    private Transform Transform;
    private Transform FollowTransform;
    private Vector3   PlanarDirection;
    private float     TargetDistance;
    private bool      _distanceIsObstructed;
    private float     _currentDistance;
    private float     _targetVerticalAngle;
    private Vector3   _currentFollowPosition;
    private Collider  _currentCollider;
    private Vector3   _cameraDirection;
    private Character _character;
    public  float     zoomInput;

    private float _lastRotationInputTime;

    #endregion

    // Add these at the top of your Character class, after the fields

    #region Unity Lifecycle Methods

    /// <summary>
    /// Validates and clamps inspector values.
    /// </summary>
    void OnValidate() {
        DefaultDistance      = Mathf.Clamp(DefaultDistance, MinDistance, MaxDistance);
        DefaultVerticalAngle = Mathf.Clamp(DefaultVerticalAngle, MinVerticalAngle, MaxVerticalAngle);
        VehicleVerticalAngle = Mathf.Clamp(VehicleVerticalAngle, VehicleMinVerticalAngle, VehicleMaxVerticalAngle);
    }

    /// <summary>
    /// Initializes camera components and variables.
    /// </summary>
    void Awake() {
        PlayerCam            = GetComponent<Camera>();
        _currentCollider     = GetComponent<Collider>();
        Transform            = transform;
        _currentDistance     = DefaultDistance;
        TargetDistance       = _currentDistance;
        _targetVerticalAngle = 0f;
        PlanarDirection      = Vector3.forward;

        _character = GetComponentInParent<Character>();

        if (_character != null) {
            _character.OnCharacterStateChanged += OnCharacterStateChanged;
        }
    }

    /// <summary>
    /// Sets up initial camera direction and follow transform.
    /// </summary>
    private void Start() {
        _cameraDirection = PlayerCam.transform.localPosition.normalized;
        if (camTarget != null) SetFollowTransform(camTarget.transform);

        if (uiDrag == null) {
            if (UiManager.Instance != null && UiManager.Instance.uiDrag != null) {
                uiDrag = UiManager.Instance.uiDrag;
            }
            else {
                Debug.LogError("Please attach uiDrag Component for camera rotation");
            }
        }
    }

    /// <summary>
    /// Updates camera position and rotation each frame.
    /// </summary>
    private void LateUpdate() {
        UpdateWithInput(Time.deltaTime,
                        zoomInput,
                        uiDrag != null
                            ? uiDrag.Delta * CamSensitivity
                            : Vector3.zero);
    }


    private void OnDestroy() {
        if (_character != null) {
            _character.OnCharacterStateChanged -= OnCharacterStateChanged;
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Sets the transform that the camera should follow.
    /// </summary>
    /// <param name="t">The transform to follow.</param>
    public void SetFollowTransform(Transform t) {
        FollowTransform        = t;
        PlanarDirection        = FollowTransform.forward;
        _currentFollowPosition = FollowTransform.position;
    }

    /// <summary>
    /// Updates the camera based on input and current state.
    /// </summary>
    /// <param name="deltaTime">Time since last frame.</param>
    /// <param name="zoomInput">Input for zooming.</param>
    /// <param name="rotationInput">Input for rotation.</param>
    public void UpdateWithInput(float deltaTime, float zoomInput, Vector3 rotationInput) {
        if (FollowTransform == null) return;

        switch (cameraState) {
            case CameraState.Character:
                UpdateCharacterCamera(deltaTime, zoomInput, rotationInput);
                break;
            case CameraState.Vehicle:
                UpdateVehicleCamera(deltaTime, zoomInput, rotationInput);
                break;
            case CameraState.Death:
                // Implement death camera behavior if needed
                break;
        }

        ApplyCameraTransform(deltaTime);
    }

    #endregion

    #region Private Methods

    private void Update() {
        updateCamSettings();
    }

    /// <summary>
    /// Handles camera behavior changes based on player state.
    /// </summary>
    /// <param name="state">The new player state.</param>
    private void OnCharacterStateChanged(Character.CharacterState state) {
        switch (state) {
            case Character.CharacterState.InVehicleDriver:
            case Character.CharacterState.InVehiclePassenger:
                if (_character.playerCar != null) {
                    FollowTransform = _character.playerCar.cameraPoint;
                    if (!IgnoredColliders.Contains(_character.playerCar.VehicleCollider))
                        IgnoredColliders.Add(_character.playerCar.VehicleCollider);
                }

                cameraState = CameraState.Vehicle;
                MinDistance = VehicleMinDistance;
                MaxDistance = VehicleMaxDistance;

                break;
            
            case Character.CharacterState.Crouched:
                desiredLocalPosition = new Vector3(0.0005719915f, 1.04f, 0.1516497f);
                // camTarget.gameObject.transform.localPosition = desiredLocalPosition;
                camTarget.gameObject.transform.DOLocalMove(desiredLocalPosition, 0.4f,false);
                
             //   camTarget.gameObject.transform.localPosition = Vector3.Slerp(camTarget.gameObject.transform.localPosition, desiredLocalPosition, smoothSpeed * Time.deltaTime);
                
                // camTarget.gameObject.transform.localPosition = Vector3.SmoothDamp(camTarget.gameObject.transform.localPosition,
                //                                                                   desiredLocalPosition,
                //                                                                   ref velocity,
                //                                                                   1f / smoothSpeed // smoothTime (inverse of speed)
                // );

                Debug.Log($"Updating cam target crouched");
                break;
            default:
                //check if he exited a car
                //todo remove the collider

                desiredLocalPosition = new Vector3(0.034f, 1.755f, 0.214f);
                camTarget.gameObject.transform.DOLocalMove(desiredLocalPosition, 0.4f,false);
                camTarget.gameObject.transform.localPosition = Vector3.Slerp(camTarget.gameObject.transform.localPosition, desiredLocalPosition, smoothSpeed * Time.deltaTime);
                // camTarget.gameObject.transform.localPosition = desiredLocalPosition;
                // camTarget.gameObject.transform.localPosition = Vector3.SmoothDamp(camTarget.gameObject.transform.localPosition,
                //                                                                   desiredLocalPosition,
                //                                                                   ref velocity,
                //                                                                   1f / smoothSpeed // smoothTime (inverse of speed)
                // );
                Debug.Log($"Updating cam target normal");
                updateCamSettings();
                FollowTransform = _character.cameraTarget;
                cameraState     = CameraState.Character;
                break;
        }
    }

    [Button("Update Character Camera")]
    public void updateCamSettings() {
        
        if (useFPP) {
            DefaultDistance           = globalData.FPP_DefaultDistance;
            MinDistance               = globalData.FPP_MinDistance;
            MaxDistance               = globalData.FPP_MaxDistance;
            CamSensitivity            = globalData.FPP_CamSensitivity;
            FollowPointFraming        = globalData.FPP_FollowPointFraming;
            FollowingSharpness        = globalData.FPP_FollowingSharpness;
            RotationSharpness         = globalData.FPP_RotationSharpness;
            RotationSpeed             = globalData.FPP_RotationSpeed;
            MinVerticalAngle          = globalData.FPP_MinVerticalAngle;
            MaxVerticalAngle          = globalData.FPP_MaxVerticalAngle;
            DefaultVerticalAngle      = globalData.FPP_DefaultVerticalAngle;
            DistanceMovementSharpness = globalData.FPP_DistanceMovementSharpness;
            DistanceMovementSpeed     = globalData.FPP_DistanceMovementSpeed;
        }
        else {
            DefaultDistance           = globalData.TPP_DefaultDistance;
            MinDistance               = globalData.TPP_MinDistance;
            MaxDistance               = globalData.TPP_MaxDistance;
            CamSensitivity            = globalData.TPP_CamSensitivity;
            FollowPointFraming        = globalData.TPP_FollowPointFraming;
            FollowingSharpness        = globalData.TPP_FollowingSharpness;
            RotationSharpness         = globalData.TPP_RotationSharpness;
            RotationSpeed             = globalData.TPP_RotationSpeed;
            MinVerticalAngle          = globalData.TPP_MinVerticalAngle;
            MaxVerticalAngle          = globalData.TPP_MaxVerticalAngle;
            DefaultVerticalAngle      = globalData.TPP_DefaultVerticalAngle;
            DistanceMovementSharpness = globalData.TPP_DistanceMovementSharpness;
            DistanceMovementSpeed     = globalData.TPP_DistanceMovementSpeed;
        }
    }

    /// <summary>
    /// Updates the camera for character perspective.
    /// </summary>
    private void UpdateCharacterCamera(float deltaTime, float zoomInput, Vector3 rotationInput) {
        if (InvertX) rotationInput.x *= -1f;
        if (InvertY) rotationInput.y *= -1f;

        Quaternion rotationFromInput = Quaternion.Euler(FollowTransform.up * (rotationInput.x * RotationSpeed));
        PlanarDirection      =  rotationFromInput * PlanarDirection;
        PlanarDirection      =  Vector3.Cross(FollowTransform.up, Vector3.Cross(PlanarDirection, FollowTransform.up));
        _targetVerticalAngle -= rotationInput.y * RotationSpeed;
        _targetVerticalAngle =  Mathf.Clamp(_targetVerticalAngle, MinVerticalAngle, MaxVerticalAngle);

        ProcessZoomInput(zoomInput);
    }

    /// <summary>
    /// Updates the camera for vehicle perspective.
    /// </summary>
    private void UpdateVehicleCamera(float deltaTime, float zoomInput, Vector3 rotationInput) {
        if (InvertX) rotationInput.x *= -1f;
        if (InvertY) rotationInput.y *= -1f;

        if (rotationInput.magnitude > 0f) {
            _lastRotationInputTime = Time.time;

            Quaternion rotationFromInput = Quaternion.Euler(FollowTransform.up * (rotationInput.x * VehicleRotationSpeed));
            PlanarDirection = rotationFromInput * PlanarDirection;
            PlanarDirection = Vector3.Cross(FollowTransform.up, Vector3.Cross(PlanarDirection, FollowTransform.up));

            _targetVerticalAngle -= rotationInput.y * VehicleRotationSpeed;
            _targetVerticalAngle =  Mathf.Clamp(_targetVerticalAngle, VehicleMinVerticalAngle, VehicleMaxVerticalAngle);
        }
        else if (_character.playerCar != null && Time.time - _lastRotationInputTime > 1.6f && _character.playerCar.controllerV4.speed > 35f) {
            // Gradually reset the camera to look behind the car
            _targetVerticalAngle = Mathf.Lerp(_targetVerticalAngle, VehicleVerticalAngle, deltaTime * 5);

            // Look behind if reversing, otherwise look forward
            PlanarDirection = Vector3.Lerp(PlanarDirection,
                                           _character.playerCar != null && _character.playerCar.controllerV4.direction == -1
                                               ? -FollowTransform.forward
                                               : FollowTransform.forward,
                                           deltaTime * VehicleRotationSharpness);
        }

        ProcessZoomInput(zoomInput);
    }

    /// <summary>
    /// Processes zoom input and updates target distance.
    /// </summary>
    private void ProcessZoomInput(float zoomInput) {
        if (_distanceIsObstructed && Mathf.Abs(zoomInput) > 0f) {
            TargetDistance = _currentDistance;
        }

        TargetDistance += zoomInput * DistanceMovementSpeed;
        TargetDistance =  Mathf.Clamp(TargetDistance, MinDistance, MaxDistance);
    }

    /// <summary>
    /// Applies the final camera transform based on calculated values.
    /// </summary>
    private void ApplyCameraTransform(float deltaTime) {
        _currentFollowPosition = Vector3.Lerp(_currentFollowPosition, FollowTransform.position, 1f - Mathf.Exp(-FollowingSharpness * deltaTime));

        Quaternion planarRot   = Quaternion.LookRotation(PlanarDirection, FollowTransform.up);
        Quaternion verticalRot = Quaternion.Euler(_targetVerticalAngle, 0, 0);
        var targetRotation = _character.CurrentCharacterState is Character.CharacterState.InVehicleDriver or Character.CharacterState.InVehiclePassenger
                                 ? Quaternion.Slerp(Transform.rotation, planarRot * verticalRot, 1f - Mathf.Exp(-VehicleRotationSharpness * deltaTime))
                                 : Quaternion.Slerp(Transform.rotation, planarRot * verticalRot, 1f - Mathf.Exp(-RotationSharpness * deltaTime));

        Transform.rotation = targetRotation;

        HandleObstructions(deltaTime);

        Vector3 targetPosition = _currentFollowPosition - targetRotation * Vector3.forward * _currentDistance;
        targetPosition += Transform.right * FollowPointFraming.x;
        targetPosition += Transform.up * FollowPointFraming.y;

        Transform.position = targetPosition;
    }

    /// <summary>
    /// Handles camera obstructions and adjusts camera distance accordingly.
    /// </summary>
    private void HandleObstructions(float deltaTime) {
        bool  isObstructed    = false;
        float closestDistance = Mathf.Infinity;

        Vector3 targetPosition = _currentFollowPosition - Transform.rotation * Vector3.forward * TargetDistance;
        Vector3 direction      = targetPosition - _currentFollowPosition;
        float   distance       = direction.magnitude;
        Ray     ray            = new Ray(_currentFollowPosition, direction.normalized);

        if (Physics.SphereCast(ray, ObstructionCheckRadius, out RaycastHit hit, distance, ObstructionLayers, QueryTriggerInteraction.Ignore)) {
            if (!IgnoredColliders.Contains(hit.collider)) {
                isObstructed    = true;
                closestDistance = hit.distance;
            }
        }

        // Additional linecast from camera to _currentFollowPosition
        if (!isObstructed && Physics.Linecast(Transform.position, _currentFollowPosition, out RaycastHit lineHit, ObstructionLayers, QueryTriggerInteraction.Ignore)) {
            if (!IgnoredColliders.Contains(lineHit.collider)) {
                isObstructed    = true;
                closestDistance = Vector3.Distance(Transform.position, lineHit.point);
            }
        }

        if (isObstructed) {
            _distanceIsObstructed = true;
            _currentDistance      = Mathf.Lerp(_currentDistance, closestDistance - 0.1f, 1 - Mathf.Exp(-ObstructionSharpness * deltaTime));
        }
        else {
            _distanceIsObstructed = false;
            _currentDistance      = Mathf.Lerp(_currentDistance, TargetDistance, 1 - Mathf.Exp(-DistanceMovementSharpness * deltaTime));
        }
    }

    #endregion
}