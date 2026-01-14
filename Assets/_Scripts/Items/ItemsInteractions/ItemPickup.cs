using Exile.Inventory;
using UnityEngine;

public class ItemPickup : MonoBehaviour, IInteractable
{
    public ItemBase item;
    
    [SerializeField] private ItemDatabase _database;
    [SerializeField] private SpriteRenderer _visualRenderer;
    
    private int _syncedItemId;

    private void Awake() {
        //_syncedItemId.OnChange += OnItemIdChanged;
        if (_visualRenderer == null) _visualRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void SetItem(ItemBase newItem) {
        if (newItem == null) return;
        item = newItem;
        _syncedItemId = newItem.Id;
        UpdateVisuals();
    }

    private void OnItemIdChanged(int prev, int next, bool asServer) {
        if (asServer) return;
        
        if (_database != null) {
            item = _database.GetItem(next);
            UpdateVisuals();
        }
    }

    private void UpdateVisuals() {
        if (item != null && _visualRenderer != null) {
            _visualRenderer.sprite = item.sprite;
        }
    }

    public void OnStartClient() {
        //base.OnStartClient();
        if (_syncedItemId != 0 && item == null && _database != null) {
             item = _database.GetItem(_syncedItemId);
             UpdateVisuals();
        }
    }

    public string GetInteractionPrompt()
    {
        return item != null ? $"Pick up {item.ItemName}" : "Pick up Item";
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