namespace FewClicksDev.Core.ReorderableList
{
    using UnityEditor;

    public class FloatReorderableList : ReorderableList<float>
    {
        protected override SerializedObject getSerializedObject()
        {
            return new SerializedObject(this);
        }
    }
}