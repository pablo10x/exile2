namespace FewClicksDev.Core.ReorderableList
{
    using UnityEditor;
    using UnityEngine;

    public class Texture2DReorderableList : ReorderableList<Texture2D>
    {
        protected override SerializedObject getSerializedObject()
        {
            return new SerializedObject(this);
        }
    }
}