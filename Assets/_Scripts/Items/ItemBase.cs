using Exile.Inventory;
using Exile.Inventory.Examples;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Exile.Inventory {
    /// <summary>
    /// Scriptable Object representing an Inventory Item
    /// </summary>
    [CreateAssetMenu(fileName = "Item", menuName = "Inventory/Item", order = 1)]
    public class ItemBase : ScriptableObject, IInventoryItem {
        [PreviewField(150, ObjectFieldAlignment.Center)] [SerializeField] private Sprite         _sprite      = null;
        [SerializeField]                                                  private InventoryShape _shape       = null;
        [SerializeField]                                                  private ItemType       _type        = ItemType.Any;
        [SerializeField]                                                  private ItemTier       _tier        = ItemTier.Common;
        //stacking
        [BoxGroup("Stacking")] [SerializeField]                                                  private bool           _Stackable   = false;
        [BoxGroup("Stacking")] [ShowIf("Stackable")] [SerializeField]                            private int            _maxQuantity = 1;
        [BoxGroup("Stacking")] [ShowIf("Stackable")] [SerializeField]                            private int            _quantity    = 1;
        
        
        [SerializeField]                                                  private bool           _canDrop     = true;
        [SerializeField, HideInInspector]                                 private Vector2Int     _position    = Vector2Int.zero;

        /// <summary>
        /// The name of the item
        /// </summary>
        public string Name;

        /// <summary>
        /// The type of the item
        /// </summary>
        public ItemType Type => _type;

        public string ItemName => Name;

        /// <inheritdoc />
        public Sprite sprite => _sprite;

        /// <inheritdoc />
        public int width {
            get => _shape.width;
            set => _shape.width = value;
        }

        public ItemTier ItemTier    => _tier;
        public int      maxQuantity => _maxQuantity;
        public int      Quantity    => _quantity;
        public bool     Stackable   => _Stackable;

        private InventoryProvider _provider = new InventoryProvider(InventoryRenderMode.Layered, -1, ItemType.Any);

        public InventoryProvider i_provider {
            get => _provider;
            set { }
        }

        public bool _iscontainer = false;

        public bool isContainer {
            get => _iscontainer;
            set => _iscontainer = value;
        }

        [ShowIf("_iscontainer")] [FoldoutGroup("Container size")] [SerializeField] private InventoryShape   container_shape = null;
        public                                                                             InventoryManager ContainerInventory { get; set; }

        /// <inheritdoc />
        public int height {
            get => _shape.height;
            set => _shape.height = value;
        }

        /// <inheritdoc />
        public Vector2Int position {
            get => _position;
            set => _position = value;
        }

        /// <inheritdoc />
        public bool IsPartOfShape(Vector2Int localPosition) {
            return _shape.IsPartOfShape(localPosition);
        }

        /// <inheritdoc />
        public bool canDrop => _canDrop;

        public bool Rotated { get; set; }

        

        /// <summary>
        /// Creates a copy if this scriptable object
        /// </summary>
        public IInventoryItem CreateInstance() {
            var clone = Instantiate(this);
            if (_iscontainer) {
                clone.i_provider         = new InventoryProvider(InventoryRenderMode.Layered, -1, ItemType.Any);
                clone.ContainerInventory = new InventoryManager(clone.i_provider, container_shape.width, container_shape.height, true);
            }

            clone.name = clone.name.Substring(0, clone.name.Length - 7); // Remove (Clone) from name
            return clone;
        }
    }
}