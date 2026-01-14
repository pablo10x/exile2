namespace FewClicksDev.Core.ReorderableList
{
    using UnityEditor;
    using UnityEngine;

    public class GameObjectReorderableList : ReorderableList<GameObject>
    {
        protected override SerializedObject getSerializedObject()
        {
            return new SerializedObject(this);
        }
    }
}