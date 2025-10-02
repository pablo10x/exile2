using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Exile.Inventory
{
    /// <summary>
    /// Handles displaying shadow overlays (and optional progress bars) 
    /// for all items in an inventory.
    /// </summary>
    public class InventoryShadowOverlay : MonoBehaviour
    {
        [SerializeField] private RectTransform overlayLayer; // Assign in inspector
        [SerializeField] private Sprite shadowSprite;        // Gradient shadow sprite
        [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0.5f);

        private InventoryRenderer _renderer;
        private InventoryManager _inventory;

        // Keep a mapping of items -> their shadow overlay image
        private readonly Dictionary<IInventoryItem, Image> _shadows = new();

        void Awake()
        {
            _renderer = GetComponent<InventoryRenderer>();
            if (_renderer == null) throw new MissingComponentException("InventoryRenderer is required");
            _inventory = (InventoryManager)_renderer.GetInventoryManager();

            // Hook into events
            var controller = GetComponent<InventoryController>();
            if (controller != null)
            {
                controller.onItemAdded += _ => UpdateShadows();
                controller.onItemSwapped += _ => UpdateShadows();
                controller.onItemDropped += _ => UpdateShadows();
                controller.onItemReturned += _ => UpdateShadows();
                controller.onItemPickedUp += _ => UpdateShadows();
            }
        }

        /// <summary>
        /// Refreshes shadows for all items currently in inventory.
        /// </summary>
        public void UpdateShadows()
        {
            ClearShadows();

            foreach (var item in _inventory.allItems) // assuming InventoryManager exposes items
            {
                ShowShadow(item);

                // if (item is IHasDurability d)
                // {
                //     AttachProgressBar(item, d.DurabilityNormalized);
                // }
            }
        }

        private void ShowShadow(IInventoryItem item)
        {
            if (item == null) return;

            if (!_shadows.TryGetValue(item, out var img))
            {
                var go = new GameObject("Shadow_" + item.ItemName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(overlayLayer, false);

                img = go.GetComponent<Image>();
                img.sprite = shadowSprite;
                img.type = Image.Type.Sliced;
                img.color = shadowColor;

                _shadows[item] = img;
            }

            img.gameObject.SetActive(true);

            var rt = img.rectTransform;
            rt.sizeDelta = new Vector2(item.width * _renderer.cellSize.x, item.height * _renderer.cellSize.y);
            rt.anchoredPosition = GridToLocal(item.position);
        }

        private void AttachProgressBar(IInventoryItem item, float normalizedValue)
        {
            if (!_shadows.TryGetValue(item, out var shadow)) return;

            var bar = shadow.transform.Find("ProgressBar")?.GetComponent<Image>();
            if (bar == null)
            {
                var go = new GameObject("ProgressBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(shadow.transform, false);

                bar = go.GetComponent<Image>();
                bar.color = Color.green;
                bar.type = Image.Type.Filled;
                bar.fillMethod = Image.FillMethod.Horizontal;

                var rt = bar.rectTransform;
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(1, 0);
                rt.pivot = new Vector2(0.5f, 0);
                rt.sizeDelta = new Vector2(0, 5); // height of bar
            }

            bar.fillAmount = Mathf.Clamp01(normalizedValue);
        }

        private void ClearShadows()
        {
            foreach (var kv in _shadows)
            {
                kv.Value.gameObject.SetActive(false);
            }
        }

        private Vector2 GridToLocal(Vector2Int gridCell)
        {
            var pos = new Vector2(gridCell.x * _renderer.cellSize.x, gridCell.y * _renderer.cellSize.y);
            pos.x -= (_inventory.width * _renderer.cellSize.x) / 2f;
            pos.y -= (_inventory.height * _renderer.cellSize.y) / 2f;
            pos.y += _renderer.yoffset;
            pos += _renderer.cellSize / 2f;
            return pos;
        }
    }

    /// <summary>
    /// Optional durability interface for items that support progress bars.
    /// </summary>
    public interface IHasDurability
    {
        float Durability { get; }
        float MaxDurability { get; }
        float DurabilityNormalized => Mathf.Clamp01(Durability / MaxDurability);
    }
}
