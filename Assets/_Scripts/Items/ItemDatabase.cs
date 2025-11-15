using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Exile.Inventory;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Exile/Inventory/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [SerializeField]
    private List<ItemBase> _items = new List<ItemBase>();

    public List<ItemBase> Items => _items;

    public ItemBase GetItem(int id)
    {
        if (id >= 0 && id < _items.Count)
        {
            return _items[id];
        }
        return null;
    }

    public ItemBase GetItem(string name)
    {
        return _items.FirstOrDefault(item => item.name == name);
    }

#if UNITY_EDITOR
    public void UpdateDatabase()
    {
        _items.Clear();
        var guids = AssetDatabase.FindAssets("t:ItemBase");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var item = AssetDatabase.LoadAssetAtPath<ItemBase>(path);
            if (item != null)
            {
                _items.Add(item);
            }
        }
        
        // Assign IDs based on the list index
        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i] != null && _items[i].Id == -1)
            {
                _items[i].Id = i;
                EditorUtility.SetDirty(_items[i]);
            }
        }
        
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
        Debug.Log($"Item Database Updated. {_items.Count} items found.");
    }
#endif
}
