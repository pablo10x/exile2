namespace FewClicksDev.Core.ReorderableList
{
    using UnityEditor;
    using UnityEngine;

    public class ColorReorderableList : ReorderableList<Color>
    {
        protected override SerializedObject getSerializedObject()
        {
            return new SerializedObject(this);
        }
    }
}