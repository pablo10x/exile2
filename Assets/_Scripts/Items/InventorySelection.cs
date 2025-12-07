using UnityEngine;
using UnityEngine.UI;

namespace Exile.Inventory.Examples
{
    public class InventorySelection : MonoBehaviour
    {
        Text _text;

        void Start()
        {
            _text = GetComponentInChildren<Text>();
            _text.text = string.Empty;

            var allControllers = GameObject.FindObjectsByType<InventoryController>(FindObjectsSortMode.None);

            foreach (var controller in allControllers)
            {
                controller.onItemHovered += HandleItemHover;
            }
        }

        private void HandleItemHover(IInventoryItem item)
        {
            if (item != null)
            {
                _text.text = (item as ItemBase).name;
            }
            else
            {
                _text.text = string.Empty;
            }
        }
    }
}