namespace FewClicksDev.TextureChannelArranger
{
    using FewClicksDev.Core.ReorderableList;
    using UnityEditor;
    using UnityEngine;

    public class PresetSetupReorderableList : ReorderableList<ChannelArrangerPresetSetup>
    {
        private const string VISIBLE = "Visible   ";

        protected override SerializedObject getSerializedObject()
        {
            return new SerializedObject(this);
        }

        protected override void drawHeader(Rect _rect)
        {
            base.drawHeader(_rect);

            GUIStyle _rightAlignedLabel = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleRight
            };

            EditorGUI.LabelField(_rect, VISIBLE, _rightAlignedLabel);
        }
    }
}
