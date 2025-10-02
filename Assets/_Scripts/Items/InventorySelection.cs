using Exile.Inventory;
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

            var allControllers = GameObject.FindObjectsOfType<InventoryController>();

            foreach (var controller in allControllers)
            {
                controller.onItemHovered += HandleItemHover;
            }
        }

        private void HandleItemHover(IInventoryItem item)
        {
            if (item != null)
            {
                _text.text = (item as ItemBase).Name;
            }
            else
            {
                _text.text = string.Empty;
            }
        }
    }
}