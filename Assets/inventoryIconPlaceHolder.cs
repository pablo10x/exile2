using Exile.Inventory;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(InventoryRenderer))]
public class inventoryIconPlaceHolder : MonoBehaviour {
    [BoxGroup("Icon placeholder")] [SerializeField] private Image _image;
    [SerializeField] private InventoryRenderer _inventoryRenderer;
    [SerializeField] private InventoryView      inventoryView;
    
    private void Start() {
        if (_inventoryRenderer is null) _inventoryRenderer = GetComponent<InventoryRenderer>();
       
    }


    public void SetupEvents(InventoryManager inventoryManager) {

        inventoryManager.onItemAdded   += OnItemAdded;
        inventoryManager.onItemRemoved += OnItemRemoved;
            
        
    }
    
    private void OnItemRemoved(IInventoryItem obj) {
        _image.enabled = true;
        _inventoryRenderer.ClearSelection();
    }

    private void OnItemAdded(IInventoryItem obj) {
        _image.enabled = false;
    }

    
}