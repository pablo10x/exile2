namespace FewClicksDev.TextureChannelArranger
{
    using FewClicksDev.Core;
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(ChannelArrangerPresetSetup))]
    public class ChannelArrangerPresetSetupPropertyDrawer : CustomPropertyDrawerBase
    {
        private const float LABEL_WIDTH = 220f;
        private const float TOGGLE_WIDTH = 60f;

        private const string PRESET_PROPERTY = "preset";
        private const string IS_VISIBLE_PROPERTY = "isVisible";

        public override void OnGUI(Rect _position, SerializedProperty _property, GUIContent _label)
        {
            if (_property.serializedObject == null)
            {
                return;
            }

            EditorGUI.BeginProperty(_position, _label, _property);

            SerializedProperty _presetProperty = _property.FindPropertyRelative(PRESET_PROPERTY);
            ChannelArrangerPreset _preset = _presetProperty.objectReferenceValue as ChannelArrangerPreset;

            Rect _totalRect = new Rect(_position.x, _position.y, _position.width - TOGGLE_WIDTH, singleLineHeight);
            Rect _labelRect = new Rect(_totalRect.x, _totalRect.y, LABEL_WIDTH, singleLineHeight);
            EditorGUI.LabelField(_labelRect, _preset.DisplayName);

            using (new DisabledScope())
            {
                Rect _objectRect = new Rect(_labelRect.x + LABEL_WIDTH, _position.y, _position.width - LABEL_WIDTH - TOGGLE_WIDTH, singleLineHeight);
                EditorGUI.PropertyField(_objectRect, _presetProperty, GUIContent.none);
            }

            SerializedProperty _isVisibleProperty = _property.FindPropertyRelative(IS_VISIBLE_PROPERTY);
            Rect _isVisibleRect = new Rect(_labelRect.x + _position.width - TOGGLE_WIDTH + 25f, _position.y, TOGGLE_WIDTH, singleLineHeight);
            bool _changed = false;

            using (var _changeCheck = new ChangeCheckScope())
            {
                EditorGUI.PropertyField(_isVisibleRect, _isVisibleProperty, GUIContent.none);
                _changed = _changeCheck.changed;
            }

            EditorGUI.EndProperty();

            if (_changed)
            {
                _property.serializedObject.ApplyModifiedProperties();
                ChannelArrangerPresetSetup.ForceInvokeAnEvent();
            }
        }

        public override float GetPropertyHeight(SerializedProperty _property, GUIContent _label)
        {
            return lineHeightWithSpacing;
        }
    }
}
