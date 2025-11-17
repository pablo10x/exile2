using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Exile.Inventory;

#if UNITY_EDITOR
using System;
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
        // Use LINQ to find the item with the matching ID.
        return _items.FirstOrDefault(item => item.Id == id);
    }

    public ItemBase GetItem(string name)
    {
        return _items.FirstOrDefault(item => item.ItemName == name);
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

        // Find the highest existing ID to continue incrementing from there.
        int nextId = 1000;
        if (_items.Any(item => item.Id >= 1000))
        {
            nextId = _items.Max(item => item.Id) + 1;
        }

        // Assign a new, unique, incremental ID to any item that doesn't have one.
        foreach (var item in _items)
        {
            if (item != null && item.Id <= 0) // Using <= 0 to catch -1 and default 0.
            {
                item.Id = nextId++;
                EditorUtility.SetDirty(item);
            }
        }
        
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
        Debug.Log($"Item Database Updated. {_items.Count} items found.");
    }
#endif
}
