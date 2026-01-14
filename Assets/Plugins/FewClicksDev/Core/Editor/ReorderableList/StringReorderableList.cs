namespace FewClicksDev.Core.ReorderableList
{
    using UnityEditor;

    public class StringReorderableList : ReorderableList<string>
    {
        protected override SerializedObject getSerializedObject()
        {
            return new SerializedObject(this);
        }
    }
}