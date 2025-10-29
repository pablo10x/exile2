using core.Managers;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;


public enum NodeType {
    stone,copper,ti,iron,silver,gold
}
public class MiningNode : NetworkBehaviour , IInteractable
{
    
    private readonly SyncVar<int> quantity = new SyncVar<int>();



    [SerializeField] private GlobalDataSO _globalDataSo;
    [SerializeField] private NodeType nodeType;
    [SerializeField] private int maxQuantity = 5;
    private float miningCooldown = 1f;
    private float respawnTimer = 1f;
    
    private float lastMineTime;

    private void Start()
    {
        quantity.OnChange += OnQuantityChanged;
        
    }

    public override void OnStartServer() {
        base.OnStartServer();

        switch (nodeType) {
            case NodeType.stone:
                quantity.Value = _globalDataSo.stone_maxQuantity;
                miningCooldown = _globalDataSo.stone_coolDown;
                respawnTimer = _globalDataSo.stone_respawnTime;
                break;
        }
        
       
    }

    // Called when a player interacts with this node
    public void TryMine(NetworkObject player)
    {
        // Only execute on server
        if (!base.IsServerStarted) return;
        
        // Check cooldown
        if (Time.time - lastMineTime < miningCooldown) return;
        
        // Check if node has resources
        if (quantity.Value <= 0)
        {
            Debug.Log("Mining node is depleted!");
            return;
        }
        
        // Mine the resource
        quantity.Value--;
        lastMineTime = Time.time;
        
        Debug.Log($"Mined! Remaining quantity: {quantity.Value}");
        
        // Optionally give resource to player
        // player.GetComponent<PlayerInventory>()?.AddResource(ResourceType.Ore, 1);
        
        // If depleted, handle accordingly
        if (quantity.Value <= 0)
        {
            OnNodeDepleted();
        }
    }

    private void OnNodeDepleted()
    {
        // You can destroy the object, disable it, or respawn it later
        Debug.Log("Node depleted!");
        
        // Option 1: Destroy
        // base.Despawn();
        
        // Option 2: Respawn after delay
        Invoke(nameof(RespawnNode), respawnTimer);
    }
 
    private void RespawnNode()
    {
        if (base.IsServerStarted)
        {
            Debug.Log($"node respawned!");
            transform.localScale = Vector3.one;
            quantity.Value = maxQuantity;
        }
    }

    // Called when quantity changes (on all clients)
    private void OnQuantityChanged(int oldValue, int newValue, bool asServer)
    {
        
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        // Update the visual appearance based on quantity
        // For example, change material, scale, or particle effects
        float scale = Mathf.Lerp(0.5f, 1f, (float)quantity.Value / maxQuantity);
        transform.localScale = Vector3.one * scale;
    }

    // Optional: Display current quantity
    private void OnMouseOver()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            // Local player tries to mine
            var player = FindLocalPlayer();
            if (player != null)
            {
                RequestMine(player);
            }
        }
    }

    // Client requests to mine (sends to server)
    [ServerRpc(RequireOwnership = false)]
    private void RequestMine(NetworkObject player)
    {
        TryMine(player);
    }

    private NetworkObject FindLocalPlayer()
    {
        // Find the local player's NetworkObject
        foreach (var conn in base.NetworkManager.ClientManager.Clients.Values)
        {
            foreach (var obj in conn.Objects)
            {
                if (obj.IsOwner)
                {
                    return obj;
                }
            }
        }
        return null;
    }

    public string GetInteractionPrompt() => $"Mine {nodeType.ToString()}";
    public Vector3 GetPosition() => transform.position;
    
    
    
    public void Interact(PlayerInteraction player) {
        throw new System.NotImplementedException();
    }


    public void Register()   => InteractionManager.Instance?.RegisterInteractable(this);
    public void Unregister() => InteractionManager.Instance?.UnregisterInteractable(this);
}