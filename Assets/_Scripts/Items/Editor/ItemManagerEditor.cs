
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Exile.Inventory;
using Exile.Inventory.Examples;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using Sirenix.OdinInspector;

public class ItemManagerEditor : OdinMenuEditorWindow
{
    [MenuItem("Tools/Item Manager")]
    private static void OpenWindow()
    {
        GetWindow<ItemManagerEditor>().Show();
    }

    protected override OdinMenuTree BuildMenuTree()
    {
        var tree = new OdinMenuTree();
        tree.DefaultMenuStyle.IconSize = 28.00f;
        tree.Config.DrawSearchToolbar = true;

        var allItems = AssetDatabase.FindAssets("t:ItemBase")
            .Select(guid => AssetDatabase.LoadAssetAtPath<ItemBase>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(item => item != null);

        var groupedItems = allItems.GroupBy(item => item.GetType());

        foreach (var group in groupedItems.OrderBy(g => g.Key.Name))
        {
            tree.Add(group.Key.Name, null); // Add a folder for the group

            foreach (var item in group.OrderBy(i => i.name))
            {
                tree.AddMenuItemAtPath(group.Key.Name, new OdinMenuItem(tree, item.name, item));
            }
        }

        return tree;
    }

    protected override void OnBeginDrawEditors()
    {
        OdinMenuTreeSelection selected = MenuTree.Selection;

        SirenixEditorGUI.BeginHorizontalToolbar();
        {
            if (SirenixEditorGUI.ToolbarButton("Refresh"))
            {
                ForceMenuTreeRebuild();
            }

            GUILayout.FlexibleSpace();

            if (SirenixEditorGUI.ToolbarButton("Create New Item"))
            {
                CreateNewItem<ItemBase>();
            }
            if (SirenixEditorGUI.ToolbarButton("Create New Cloth Item"))
            {
                CreateNewItem<ItemCloth>();
            }
        }
        SirenixEditorGUI.EndHorizontalToolbar();
    }

    private void CreateNewItem<T>() where T : ItemBase
    {
        T newItem = CreateInstance<T>();
        string path = "Assets/_Scripts/Items/Data/";

        if (!System.IO.Directory.Exists(path))
        {
            System.IO.Directory.CreateDirectory(path);
        }

        string fileName = $"New {typeof(T).Name}.asset";
        string fullPath = AssetDatabase.GenerateUniqueAssetPath(path + fileName);

        AssetDatabase.CreateAsset(newItem, fullPath);
        AssetDatabase.SaveAssets();

        // Select the new item in the project
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = newItem;




        // Rebuild the menu tree to include the new item
        ForceMenuTreeRebuild();
    }
}
