namespace FewClicksDev.ColorPicker
{
    using FewClicksDev.Core;
    using UnityEditor;
    using UnityEngine;

    using static FewClicksDev.Core.EditorDrawer;

    public class ColorPickerWindow : CustomEditorWindow
    {
        public enum WindowMode
        {
            First = 0,
            Second = 1
        }

        private const float TOOLBAR_WIDTH = 0.8f;
        private const float LABEL_WIDTH = 120f;

        private const string COLOR_LABEL = "Color";
        private const string CONVERTER_LABEL = "Converter";
        private const string COLOR_RGB01_LABEL = "RGB 0-1";
        private const string COLOR_HEX__LABEL = "HEX";
        private const string RGBA_FORMAT = "F6";
        private const string RGBA_TO_TRIM = "RGBA(";
        private const string END_BRACKET = ")";

        protected override string windowName => "Color Picker";
        protected override string version => "1.0.0";
        protected override Vector2 minWindowSize => new Vector2(450f, 220f);
        protected override Color mainColor => Color.clear;

        private WindowMode windowMode = WindowMode.First;

        private Color chosenColor = Color.gray;
        private string colorValueAsString01 = string.Empty;
        private string colorValueAsStringHEX = string.Empty;

        protected override void OnEnable()
        {
            base.OnEnable();
            refreshColorsStrings();
        }

        protected override void drawWindowGUI()
        {
            NormalSpace();
            windowMode = this.DrawEnumToolbar(windowMode, TOOLBAR_WIDTH, chosenColor);
            SmallSpace();
            DrawLine();
            SmallSpace();

            using (new LabelWidthScope(LABEL_WIDTH))
            {
                using (var _changeCheckScope = new ChangeCheckScope())
                {
                    chosenColor = EditorGUILayout.ColorField(COLOR_LABEL, chosenColor);

                    if (_changeCheckScope.changed)
                    {
                        refreshColorsStrings();
                    }
                }

                NormalSpace();
                DrawBoldLabel(CONVERTER_LABEL);
                EditorGUILayout.TextField(COLOR_RGB01_LABEL, colorValueAsString01);
                EditorGUILayout.TextField(COLOR_HEX__LABEL, colorValueAsStringHEX);
            }
        }

        private void refreshColorsStrings()
        {
            string[] _colorValueAsString01 = chosenColor.ToString(RGBA_FORMAT).TrimStart(RGBA_TO_TRIM).TrimEnd(END_BRACKET).Split(',');

            for (int i = 0; i < _colorValueAsString01.Length; i++)
            {
                _colorValueAsString01[i] = trimColor(_colorValueAsString01[i]);
            }

            colorValueAsString01 = _colorValueAsString01.Combine(", ");
            colorValueAsStringHEX = ColorUtility.ToHtmlStringRGB(chosenColor);
        }

        private string trimColor(string _string)
        {
            return _string.Trim().TrimEnd('0').TrimEnd('.') + "f";
        }

        [MenuItem("Window/FewClicks Dev/Color Picker")]
        private static void ShowWindow()
        {
            GetWindow<ColorPickerWindow>().Show();
        }
    }
}