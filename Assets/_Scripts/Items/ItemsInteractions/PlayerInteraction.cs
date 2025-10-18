using System;
using FishNet.Object;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// Handles player interaction with interactable objects detected by CameraRaycastController.
/// Processes input and sends interaction requests to the server.
/// Only runs on the local player (owner).
/// </summary>
[DisallowMultipleComponent]
public class PlayerInteraction : NetworkBehaviour 
{
    #region Inspector Fields
    
    [BoxGroup("References"), Required]
    [Tooltip("Reference to the camera raycast controller")]
    [SerializeField] private CameraRaycastController raycastController;
    
    [BoxGroup("References")]
    [Tooltip("Reference to the character component")]
    [SerializeField] private Character character;

    [BoxGroup("Input Settings")]
    [Tooltip("Key to press for interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    
    [BoxGroup("Debug")]
    [Tooltip("Enable debug logging for interactions")]
    [SerializeField] private bool enableDebugLogs = false;
    
    #endregion
    
    #region Private Fields
    
    private IInteractable _currentInteractable;
    private GameObject _currentInteractableObject;
    
    #endregion
    
    #region Properties
    
    /// <summary>
    /// Returns true if there is an interactable object currently available.
    /// </summary>
    public bool CanInteract => _currentInteractable != null;
    
    /// <summary>
    /// The currently available interactable object.
    /// </summary>
    public IInteractable CurrentInteractable => _currentInteractable;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        ValidateReferences();
    }

    public override void OnStartClient() 
    {
        base.OnStartClient();

        // Only enable for local player
        if (!base.IsOwner) 
        {
            enabled = false;
            return;
        }
        
        SubscribeToRaycastEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromRaycastEvents();
    }

    private void Update() 
    {
        if (!base.IsOwner) return;

        HandleInteractionInput();
    }
    
    #endregion
    
    #region Initialization & Validation
    
    private void ValidateReferences()
    {
        if (raycastController == null)
        {
            raycastController = GetComponent<CameraRaycastController>();
            
            if (raycastController == null)
            {
                Debug.LogError($"[{nameof(PlayerInteraction)}] CameraRaycastController component is missing!", this);
            }
        }
        
        if (character == null)
        {
            Debug.LogWarning($"[{nameof(PlayerInteraction)}] Character reference is missing.", this);
        }
    }
    
    #endregion
    
    #region Event Subscription
    
    private void SubscribeToRaycastEvents()
    {
        if (raycastController != null)
        {
            raycastController.OnInteractableDetected += HandleInteractableDetected;
            raycastController.OnInteractableLost += HandleInteractableLost;
        }
    }
    
    private void UnsubscribeFromRaycastEvents()
    {
        if (raycastController != null)
        {
            raycastController.OnInteractableDetected -= HandleInteractableDetected;
            raycastController.OnInteractableLost -= HandleInteractableLost;
        }
    }
    
    #endregion
    
    #region Interactable Detection Handlers
    
    private void HandleInteractableDetected(IInteractable interactable, GameObject gameObject)
    {
        _currentInteractable = interactable;
        _currentInteractableObject = gameObject;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{nameof(PlayerInteraction)}] Interactable available: {gameObject.name}", gameObject);
        }
        
        ShowInteractionPrompt(true, interactable);
    }
    
    private void HandleInteractableLost()
    {
        if (enableDebugLogs && _currentInteractable != null)
        {
            Debug.Log($"[{nameof(PlayerInteraction)}] Interactable lost");
        }
        
        _currentInteractable = null;
        _currentInteractableObject = null;
        
        ShowInteractionPrompt(false, null);
    }
    
    #endregion
    
    #region Input Handling
    
    private void HandleInteractionInput()
    {
        if (Input.GetKeyDown(interactKey) && CanInteract)
        {
            TryInteract();
        }
    }
    
    #endregion
    
    #region Interaction Logic
    
    /// <summary>
    /// Attempts to interact with the currently available interactable object.
    /// </summary>
    public void TryInteract()
    {
        if (!CanInteract)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning($"[{nameof(PlayerInteraction)}] No interactable available");
            }
            return;
        }
        
        // Handle different interactable types
        if (_currentInteractable is MiningNode miningNode)
        {
            InteractWithMiningNode(miningNode);
        }
        // Add more interactable type checks here as needed
        // else if (_currentInteractable is Door door) { ... }
        // else if (_currentInteractable is Chest chest) { ... }
        else
        {
            // Generic interaction fallback
            if (enableDebugLogs)
            {
                Debug.Log($"[{nameof(PlayerInteraction)}] Interacting with: {_currentInteractableObject.name}", _currentInteractableObject);
            }
        }
    }
    
    private void InteractWithMiningNode(MiningNode node)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[{nameof(PlayerInteraction)}] Mining node: {_currentInteractableObject.name}", _currentInteractableObject);
        }
        
        // Send mining request to server
        ServerMineNode(node);
    }
    
    #endregion
    
    #region Server RPCs
    
    [ServerRpc]
    private void ServerMineNode(MiningNode node)
    {
        if (node == null)
        {
            Debug.LogWarning($"[{nameof(PlayerInteraction)}] Server received null mining node");
            return;
        }
        
        // Server processes the mining request
        node.TryMine(base.NetworkObject);
    }
    
    #endregion
    
    #region UI Management
    
    private void ShowInteractionPrompt(bool show, IInteractable interactable)
    {
        // TODO: Implement UI prompt system
        // Examples:
        // UIManager.Instance?.ShowInteractionPrompt(show, GetPromptText(interactable));
        // Or: InteractionUI.Instance?.SetVisible(show, interactable);
        
        // Temporary debug output
        if (enableDebugLogs)
        {
            string promptText = show ? GetPromptText(interactable) : "";
            Debug.Log($"[{nameof(PlayerInteraction)}] Prompt: {promptText}");
        }
    }
    
    private string GetPromptText(IInteractable interactable)
    {
        if (interactable == null) return "";
        
        // Customize prompt based on interactable type
        if (interactable is MiningNode)
        {
            return $"Press {interactKey} to mine";
        }
        
        // Generic fallback
        return $"Press {interactKey} to interact";
    }
    
    #endregion
    
    #region Public API
    
    /// <summary>
    /// Manually trigger an interaction check. Useful for alternative input methods.
    /// </summary>
    public void ForceInteractionCheck()
    {
        raycastController?.ForceInteractableCheck();
    }
    
    #endregion
}