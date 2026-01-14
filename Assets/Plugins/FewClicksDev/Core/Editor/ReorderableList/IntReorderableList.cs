namespace FewClicksDev.Core.ReorderableList
{
    using UnityEditor;

    public class IntReorderableList : ReorderableList<int>
    {
        protected override SerializedObject getSerializedObject()
        {
            return new SerializedObject(this);
        }
    }
}