using Exile.Inventory;
using FishNet.Object;
using UnityEngine;

public class ItemPickup : NetworkBehaviour, IInteractable
{
    public ItemBase item;

    public string GetInteractionPrompt()
    {
        return $"Pick up {item.name}";
    }

    public void Interact(PlayerInteraction player)
    {
        player.TryPickupItem(this);
    }

    public Vector3 GetPosition()
    {
        return transform.position;
    }

    public void Register()
    {
        // Not needed for this implementation
    }

    public void Unregister()
    {
        // Not needed for this implementation
    }
}