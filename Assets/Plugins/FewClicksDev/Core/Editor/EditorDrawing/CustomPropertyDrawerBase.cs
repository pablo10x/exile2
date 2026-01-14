namespace FewClicksDev.Core
{
    using UnityEditor;

    public abstract class CustomPropertyDrawerBase : PropertyDrawer
    {
        protected float singleLineHeight => EditorGUIUtility.singleLineHeight;
        protected float verticalSpacing => EditorGUIUtility.standardVerticalSpacing;

        protected float lineHeightWithSpacing => singleLineHeight + verticalSpacing;
    }
}
