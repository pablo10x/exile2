using FishNet.Object;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using Sirenix.OdinInspector;
using System;

/// <summary>
/// Manages camera-based raycasting for head-look IK targeting and interactable object detection.
/// Optimized for mobile with configurable update intervals and rotation thresholds.
/// Only runs on the local player (owner).
/// </summary>
[DisallowMultipleComponent]
public class CameraRaycastController : NetworkBehaviour 
{
    #region Events
    
    /// <summary>
    /// Invoked when an interactable object is detected or lost.
    /// Parameters: (IInteractable interactable, GameObject gameObject)
    /// </summary>
    public event Action<IInteractable, GameObject> OnInteractableDetected;
    public event Action OnInteractableLost;
    
    #endregion
    
    #region Inspector Fields
    
    [BoxGroup("References"), Required]
    [Tooltip("The player's camera transform used for raycasting")]
    public Transform PlayerCam;
    
    [BoxGroup("References"), Required]
    [Tooltip("The target transform for Animation Rigging (head-look constraint)")]
    public Transform Target;
    
    [BoxGroup("References"), Required]
    [Tooltip("Reference to the character component for state management")]
    public Character Character;

    [BoxGroup("Raycast Settings")]
    [Tooltip("Maximum raycast distance in world units")]
    [Range(10f, 500f)]
    public float RaycastMaxDistance = 100f;
    
    [BoxGroup("Raycast Settings")] [Tooltip("distance for interaction objects to be triggered")] 
    [Range(1f,20f)]
    public float InteractableDistance = 5f;
    
    
    
    [BoxGroup("Raycast Settings")]
    [Tooltip("Layers to include in raycast detection")]
    public LayerMask RaycastLayers = -1;

    [FoldoutGroup("Animation Rigs")]
    [FoldoutGroup("Animation Rigs/Head Rig")]
    [Tooltip("Head look rig to control weight based on character state")]
    public Rig HeadLookRig;
    
    [FoldoutGroup("Animation Rigs/Spine")]
    public Rig SpineRig;
  
    [BoxGroup("Performance")]
    [Tooltip("Update raycast every N frames (1 = every frame, 2 = every other, etc.)")]
    [Range(1, 10)]
    public int UpdateInterval = 2;

    [BoxGroup("Performance")]
    [Tooltip("Minimum camera rotation change (degrees) required to trigger raycast update")]
    [Range(0.1f, 5f)]
    public float MinRotationChange = 0.5f;

    [BoxGroup("Smoothing")]
    [Tooltip("Enable smooth interpolation of target position")]
    public bool SmoothTarget = true;

    [BoxGroup("Smoothing")]
    
    [Tooltip("Speed of position smoothing (higher = faster response)")]
    [Range(1f, 30f)]
    public float SmoothSpeed = 15f;
    
    [BoxGroup("Debug")]
    [Tooltip("Enable debug logging for interactable detection")]
    public bool EnableDebugLogs = false;
    
    #endregion
    
    #region Private Fields
    
    // Raycast cache to avoid allocations
    private Ray _ray;
    private RaycastHit _hit;
    
    // Position tracking
    private Vector3 _defaultTargetPoint;
    private Vector3 _targetPosition;
    
    // Optimization tracking
    private Quaternion _lastCameraRotation;
    private int _frameCounter;
    
    // Interactable tracking
    private IInteractable _currentInteractable;
    private GameObject _currentInteractableObject;
    private Collider _lastHitCollider;
    
    #endregion
    
    #region Properties
    
    /// <summary>
    /// The currently detected interactable object, or null if none.
    /// </summary>
    public IInteractable CurrentInteractable => _currentInteractable;
    
    /// <summary>
    /// The GameObject containing the current interactable component.
    /// </summary>
    public GameObject CurrentInteractableObject => _currentInteractableObject;
    
    /// <summary>
    /// True if an interactable object is currently in view.
    /// </summary>
    public bool HasInteractable => _currentInteractable != null;
    
    /// <summary>
    /// The current target position for the head-look constraint.
    /// </summary>
    public Vector3 CurrentTargetPosition => _targetPosition;
    
    /// <summary>
    /// The last successful raycast hit information.
    /// </summary>
    public RaycastHit LastRaycastHit => _hit;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake() 
    {
        // Detach from hierarchy to prevent transform inheritance issues
        transform.SetParent(null);
        
        ValidateReferences();
        SubscribeToCharacterEvents();
    }

    public override void OnStartClient() 
    {
        base.OnStartClient();
        
        // Only run for local player to avoid unnecessary processing
        if (!base.IsOwner)
        {
            enabled = false;
            return;
        }
        
        InitializeController();
    }

    private void OnDestroy()
    {
        UnsubscribeFromCharacterEvents();
    }

    private void LateUpdate() 
    {
        // Early exit if not owner or missing references
        if (!base.IsOwner || !ValidateRuntimeReferences()) return;

        UpdateRaycast();
        UpdateTargetPosition();
    }
    
    #endregion
    
    #region Initialization
    
    private void ValidateReferences()
    {
        if (PlayerCam == null)
            Debug.LogError($"[{nameof(CameraRaycastController)}] PlayerCam reference is missing!", this);
        
        if (Target == null)
            Debug.LogError($"[{nameof(CameraRaycastController)}] Target reference is missing!", this);
        
        if (Character == null)
            Debug.LogWarning($"[{nameof(CameraRaycastController)}] Character reference is missing. State-based rig control will be disabled.", this);
    }
    
    private void InitializeController()
    {
        if (PlayerCam != null)
        {
            _lastCameraRotation = PlayerCam.rotation;
        }

        if (Target != null)
        {
            _targetPosition = Target.position;
        }
        
        _frameCounter = 0;
        _currentInteractable = null;
        _currentInteractableObject = null;
        _lastHitCollider = null;
    }
    
    private bool ValidateRuntimeReferences()
    {
        return PlayerCam != null && Target != null;
    }
    
    #endregion
    
    #region Character State Management
    
    private void SubscribeToCharacterEvents()
    {
        if (Character != null)
        {
            Character.OnCharacterStateChanged += HandleCharacterStateChanged;
        }
    }
    
    private void UnsubscribeFromCharacterEvents()
    {
        if (Character != null)
        {
            Character.OnCharacterStateChanged -= HandleCharacterStateChanged;
        }
    }
    
    private void HandleCharacterStateChanged(Character.CharacterState state)
    {
        if (HeadLookRig == null) return;
        
        // Adjust head-look rig weight based on character state
        switch (state)
        {
            case Character.CharacterState.Idle:
                // HeadLookRig.weight = 1f;
                // SpineRig.weight = 0f;
                break;
                
            case Character.CharacterState.Running:
            case Character.CharacterState.Jumping:
            case Character.CharacterState.Strafe:
                // HeadLookRig.weight = 0f;
                // SpineRig.weight = 0f;
                break;
                
            default:
                // Maintain current weight for other states
                break;
        }
    }
    
    #endregion
    
    #region Raycast Update Logic
    
    private void UpdateRaycast()
    {
        // Frame-based throttling
        _frameCounter++;
        if (_frameCounter < UpdateInterval) return;
        
        _frameCounter = 0;

        // Rotation-based throttling
        if (!HasCameraRotationChanged()) return;
        
        _lastCameraRotation = PlayerCam.rotation;
        PerformRaycast();
    }
    
    private bool HasCameraRotationChanged()
    {
        float angleDelta = Quaternion.Angle(_lastCameraRotation, PlayerCam.rotation);
        return angleDelta >= MinRotationChange;
    }
    
    private void PerformRaycast()
    {
        // Reuse ray struct to minimize allocations
        _ray.origin = PlayerCam.position;
        _ray.direction = PlayerCam.forward;

        if (Physics.Raycast(_ray, out _hit, RaycastMaxDistance, RaycastLayers, QueryTriggerInteraction.Ignore)) 
        {
            HandleRaycastHit();
        }
        else 
        {
            HandleRaycastMiss();
        }
    }
    
    private void HandleRaycastHit()
    {
        _targetPosition = _hit.point;
        
        // Check for interactable component
        bool hasInteractable = _hit.collider.TryGetComponent<IInteractable>(out var interactable);
        
        if (hasInteractable && Vector3.Distance(Character.motor.TransientPosition,_hit.collider.transform.position) < InteractableDistance)
        {
            // New interactable detected or different interactable
            if (_lastHitCollider != _hit.collider)
            {
                UpdateCurrentInteractable(interactable, _hit.collider.gameObject);
            }
        }
        else
        {
            // Hit something but it's not interactable
            if (_currentInteractable != null)
            {
                ClearCurrentInteractable();
            }
        }
        
        _lastHitCollider = _hit.collider;
    }
    
    private void HandleRaycastMiss()
    {
        // Calculate default point at max distance
        _defaultTargetPoint = _ray.origin + _ray.direction * RaycastMaxDistance;
        _targetPosition = _defaultTargetPoint;
        
        // Clear interactable if we lost sight of it
        if (_currentInteractable != null)
        {
            ClearCurrentInteractable();
        }
        
        _lastHitCollider = null;
    }
    
    #endregion
    
    #region Interactable Management
    
    private void UpdateCurrentInteractable(IInteractable interactable, GameObject gameObject)
    {
        _currentInteractable = interactable;
        _currentInteractableObject = gameObject;
        
        if (EnableDebugLogs)
        {
            Debug.Log($"[{nameof(CameraRaycastController)}] Interactable detected: {interactable} on {gameObject.name}", gameObject);
        }
        
        OnInteractableDetected?.Invoke(interactable, gameObject);
    }
    
    private void ClearCurrentInteractable()
    {
        if (EnableDebugLogs && _currentInteractable != null)
        {
            Debug.Log($"[{nameof(CameraRaycastController)}] Interactable lost: {_currentInteractable}");
        }
        
        _currentInteractable = null;
        _currentInteractableObject = null;
        OnInteractableLost?.Invoke();
    }
    
    /// <summary>
    /// Manually force an interactable check without waiting for the next update cycle.
    /// Useful for immediate interaction responses.
    /// </summary>
    public void ForceInteractableCheck()
    {
        if (!base.IsOwner || !ValidateRuntimeReferences()) return;
        
        _frameCounter = UpdateInterval; // Force next update
        PerformRaycast();
    }
    
    #endregion
    
    #region Target Position Update
    
    private void UpdateTargetPosition()
    {
        if (SmoothTarget) 
        {
            Target.position = Vector3.Lerp(
                Target.position, 
                _targetPosition, 
                Time.deltaTime * SmoothSpeed
            );
        }
        else 
        {
            Target.position = _targetPosition;
        }
    }
    
    #endregion
    
    #region Editor Utilities
    
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || PlayerCam == null) return;
        
        // Draw raycast direction
        Gizmos.color = _currentInteractable != null ? Color.green : Color.yellow;
        Gizmos.DrawRay(PlayerCam.position, PlayerCam.forward * RaycastMaxDistance);
        
        // Draw target position
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(_targetPosition, 0.3f);
        
        // Draw hit point if available
        if (_lastHitCollider != null)
        {
            Gizmos.color = _currentInteractable != null ? Color.green : Color.blue;
            Gizmos.DrawWireSphere(_hit.point, 0.2f);
        }
    }
#endif
    
    #endregion
}