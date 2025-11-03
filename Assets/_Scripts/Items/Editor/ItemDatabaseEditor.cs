using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ItemDatabase))]
public class ItemDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        ItemDatabase database = (ItemDatabase)target;

        if (GUILayout.Button("Update Database"))
        {
            database.UpdateDatabase();
        }
    }
}
