using UnityEngine;

public interface IInteractable {
    /// <summary>
    /// Display name or prompt for the player UI.
    /// </summary>
    string GetInteractionPrompt();

    /// <summary>
    /// Called when player interacts.
    /// </summary>
    void Interact(PlayerInteraction player);

    /// <summary>
    /// Returns world position (used for proximity detection).
    /// </summary>
    Vector3 GetPosition();

    /// <summary>
    /// Automatically registers/unregisters in the manager.
    /// </summary>
    void Register();

    void Unregister();
}