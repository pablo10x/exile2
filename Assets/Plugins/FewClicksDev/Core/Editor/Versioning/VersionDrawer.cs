namespace FewClicksDev.Core.Versioning
{
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(Version))]
    public class MP_VersionDrawer : PropertyDrawer
    {
        private const float DOT_WIDTH = 5f;

        private const string MAJOR = "Major";
        private const string MINOR = "Minor";
        private const string PATCH = "Patch";

        private SerializedProperty majorProperty = null;
        private SerializedProperty minorProperty = null;
        private SerializedProperty patchProperty = null;

        public override void OnGUI(Rect _position, SerializedProperty _property, GUIContent _label)
        {
            EditorGUI.BeginProperty(_position, _label, _property);

            Rect _labelRect = new Rect(_position.x, _position.y, EditorGUIUtility.labelWidth, _position.height);

            float _remainingWidth = _position.width - EditorGUIUtility.labelWidth - 4 * DOT_WIDTH;
            Rect _v1Rect = new Rect(_labelRect.x + _labelRect.width, _position.y, _remainingWidth / 3f, _position.height);
            Rect _dot1Rect = new Rect(_v1Rect.x + _v1Rect.width, _position.y, DOT_WIDTH, _position.height);
            Rect _v2Rect = new Rect(_dot1Rect.x + _dot1Rect.width, _position.y, _remainingWidth / 3f, _position.height);
            Rect _dot2Rect = new Rect(_v2Rect.x + _v2Rect.width, _position.y, DOT_WIDTH, _position.height);
            Rect _v3Rect = new Rect(_dot2Rect.x + _dot2Rect.width, _position.y, _remainingWidth / 3f, _position.height);

            EditorGUI.LabelField(_labelRect, _label);
            majorProperty.intValue = EditorGUI.IntField(_v1Rect, majorProperty.intValue);
            EditorGUI.LabelField(_dot1Rect, ".");
            minorProperty.intValue = EditorGUI.IntField(_v2Rect, minorProperty.intValue);
            EditorGUI.LabelField(_dot2Rect, ".");
            patchProperty.intValue = EditorGUI.IntField(_v3Rect, patchProperty.intValue);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty _property, GUIContent _label)
        {
            majorProperty = _property.FindPropertyRelative(MAJOR);
            minorProperty = _property.FindPropertyRelative(MINOR);
            patchProperty = _property.FindPropertyRelative(PATCH);

            return EditorGUIUtility.singleLineHeight;
        }
    }
}