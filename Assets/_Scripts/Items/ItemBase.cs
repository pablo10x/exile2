using Exile.Inventory;
using Exile.Inventory.Examples;
using FishNet.Object;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Exile.Inventory
{
    /// <summary>
    /// Scriptable Object representing an Inventory Item
    /// </summary>
    [CreateAssetMenu(fileName = "Item", menuName = "Inventory/Item", order = 1)]
    public class ItemBase : ScriptableObject, IInventoryItem
    {
        [PreviewField(150, ObjectFieldAlignment.Center)] [SerializeField]
        private Sprite _sprite = null;

        [SerializeField] private InventoryShape _shape = null;
        [SerializeField] private string _name;
        [SerializeField] private ItemType _type = ItemType.Any;
        [SerializeField] private ItemTier _tier = ItemTier.Common;

        //stacking
        [BoxGroup("Stacking")] [SerializeField]
        private bool _Stackable = false;

        [BoxGroup("Stacking")] [ShowIf("Stackable")] [SerializeField]
        private int _maxQuantity = 1;

        [BoxGroup("Stacking")] [ShowIf("Stackable")] [SerializeField]
        private int _quantity = 1;

        [BoxGroup("Durability")] public bool _useDurability;

        [BoxGroup("Durability")] [ShowIf("_useDurability")]
        public float _Durability;

        [BoxGroup("Durability")] [ShowIf("_useDurability")]
        public float Max_Durability;

        [SerializeField] private bool _canDrop = true;
        [SerializeField, HideInInspector] private Vector2Int _position = Vector2Int.zero;

        [SerializeField, HideInInspector] private int _id = -1;

        /// <summary>
        /// The unique Id of the item
        /// </summary>
        [ReadOnly]
        [ShowInInspector]
        public int Id
        {
            get => _id;
            internal set => _id = value;
        }

        /// <summary>
        /// The type of the item
        /// </summary>
        public ItemType Type => _type;

        public string ItemName
        {
            get => _name;
            set => _name = value;
        }

        public int ID => Id;
        public int RuntimeID { get; set; }

        /// <inheritdoc />
        public Sprite sprite
        {
            get => _sprite;
            set => _sprite = value;
        }

        /// <inheritdoc />
        public int width
        {
            get => _shape.width;
            set => _shape.width = value;
        }

        public InventoryShape Shape
        {
            get => _shape;
            set => _shape = value;
        }

        public ItemTier ItemTier
        {
            get => _tier;
            set => _tier = value;
        }

        public int maxQuantity
        {
            get => _maxQuantity;
            set => _maxQuantity = value;
        }

        public int Quantity
        {
            get => _quantity;
            set => _quantity = value;
        }

        public bool Stackable
        {
            get => _Stackable;
            set => _Stackable = value;
        }

        private InventoryProvider _provider = new InventoryProvider(InventoryRenderMode.Layered, -1, ItemType.Any);

        public InventoryProvider i_provider
        {
            get => _provider;
            set { }
        }

        public bool _iscontainer = false;

        public bool isContainer
        {
            get => _iscontainer;
            set => _iscontainer = value;
        }

        [ShowIf("_iscontainer")]
        [FoldoutGroup("Container size")]
        [SerializeField]
        public InventoryShape container_shape = null;

        public InventoryManager ContainerInventory { get; set; }

        /// <inheritdoc />
        public int height
        {
            get => _shape.height;
            set => _shape.height = value;
        }

        /// <inheritdoc />
        public Vector2Int position
        {
            get => _position;
            set => _position = value;
        }

        /// <inheritdoc />
        public bool IsPartOfShape(Vector2Int localPosition)
        {
            return _shape.IsPartOfShape(localPosition);
        }

        /// <inheritdoc />
        public bool canDrop
        {
            get => _canDrop;
            set => _canDrop = value;
        }

        public bool Rotated { get; set; }

        public bool useDurability => _useDurability;
        public float MaxDurability => Max_Durability;
        

        public float Durability
        {
            get => _Durability;
            set => _Durability = Mathf.Clamp(value, 0, Max_Durability);
        }

        /// <summary>
        /// Creates a copy if this scriptable object
        /// </summary>
        
        public IInventoryItem CreateInstance(int runtimeID)
        {
           
            var clone = Instantiate(this);
            clone.RuntimeID = runtimeID;
            if (_iscontainer)
            {
                
                clone.i_provider = new InventoryProvider(InventoryRenderMode.Layered, -1, ItemType.Any);
                clone.ContainerInventory =
                    new InventoryManager(clone.i_provider, container_shape.width, container_shape.height, true);
            }

            clone.name = clone.name.Substring(0, clone.name.Length - 7); // Remove (Clone) from name
            return clone;
        }
    }
}