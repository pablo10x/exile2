namespace FewClicksDev.TextureChannelArranger
{
    using FewClicksDev.Core;
    using UnityEditor;
    using UnityEditor.Presets;
    using UnityEngine;

    using static FewClicksDev.Core.EditorDrawer;

    [CustomEditor(typeof(ChannelArrangerPreset))]
    public class ChannelArrangerPresetEditor : CustomInspectorBase
    {
        public static GUIStyle TitleStyle
        {
            get
            {
                if (titleStyle == null)
                {
                    titleStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
                    titleStyle.fontSize = 16;
                    titleStyle.alignment = TextAnchor.MiddleCenter;
                }

                return titleStyle;
            }
        }

        public static GUIStyle DescriptionStyle
        {
            get
            {
                if (descriptionStyle == null)
                {
                    descriptionStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
                    descriptionStyle.richText = true;
                    descriptionStyle.alignment = TextAnchor.MiddleCenter;
                    descriptionStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
                }

                return descriptionStyle;
            }
        }

        private const string SAVE = "Save";
        private const string EDIT = "Edit";
        private const string OPEN_IN_WINDOW = "Open in the Channel Arranger window";
        private const string IMPORT_PRESET = "Import preset";

        private const string OPEN_IN_WINDOW_INFO = "You can use a button below to apply this preset in the Texture Channel Arranger window.";

        private static GUIStyle titleStyle = null;
        private static GUIStyle descriptionStyle = null;

        private const float SINGLE_LINE_HEIGHT = 24f;
        private const float INDEX_WIDTH = 22f;
        private const float COLOR_INDICATOR_WIDTH = 8f;
        private const float CHANNEL_WIDTH = 120f;

        private ChannelArrangerPreset preset = null;

        private bool isInEditMode = false;

        protected override void OnEnable()
        {
            base.OnEnable();

            preset = (ChannelArrangerPreset) target;
        }

        protected override void drawInspectorGUI()
        {
            if (isInEditMode == false)
            {
                drawScript();
                drawDefaultView();
            }
            else
            {
                drawDefaultInspector();
            }

            NormalSpace();

            using (new HorizontalScope())
            {
                float _buttonWidth = inspectorWidth / 2f;
                FlexibleSpace();

                string _editModeText = isInEditMode ? SAVE : EDIT;

                if (DrawBoxButton(_editModeText, FixedWidthAndHeight(_buttonWidth, SINGLE_LINE_HEIGHT)))
                {
                    if (isInEditMode)
                    {
                        preset.SetDirtyAndSave();
                    }

                    isInEditMode = !isInEditMode;
                }

                FlexibleSpace();
            }

            NormalSpace();
            EditorGUILayout.HelpBox(OPEN_IN_WINDOW_INFO, MessageType.Info);
            NormalSpace();

            using (new HorizontalScope())
            {
                float _buttonWidth = inspectorWidth * 0.7f;
                FlexibleSpace();

                if (DrawBoxButton(OPEN_IN_WINDOW, FixedWidthAndHeight(_buttonWidth, SINGLE_LINE_HEIGHT)))
                {
                    TextureChannelArrangerWindow.OpenWindow(preset);
                }

                FlexibleSpace();
            }

            SmallSpace();
        }

        private void drawDefaultView()
        {
            SmallSpace();

            using (new ScopeGroup(new HorizontalScope(), ColorScope.Content(TextureChannelArranger.LOGS_COLOR)))
            {
                FlexibleSpace();
                GUILayout.Label(preset.DisplayName, TitleStyle);
                FlexibleSpace();
            }

            if (preset.Description.IsNullEmptyOrWhitespace() == false)
            {
                SmallSpace();

                using (new HorizontalScope())
                {
                    FlexibleSpace();
                    EditorGUILayout.LabelField($"<i>{preset.Description}</i>", DescriptionStyle, FixedWidth(inspectorWidth * 0.75f));
                    FlexibleSpace();
                }
            }

            for (int i = 0; i < preset.NumberOfOutputs; i++)
            {
                NormalSpace();
                DrawBoldLabel($"{preset.GetOutputAtIndex(i).TextureName}");
                SmallSpace();

                using (new DisabledScope())
                {
                    for (int j = 0; j < preset.GetOutputAtIndex(i).ChannelSetups.Length; j++)
                    {
                        drawChannelSetup(preset.GetOutputAtIndex(i), j, TextureChannelArranger.CHANNELS[j], TextureChannelArranger.GetColorFromChannel((TextureChannel) j));
                    }

                    VerySmallSpace();
                    EditorGUILayout.ObjectField(IMPORT_PRESET, preset.GetOutputAtIndex(i).CustomPreset, typeof(Preset), false);
                }
            }
        }

        private void drawChannelSetup(TextureOutput _output, int _channelIndex, string _label, Color _indicatorColor)
        {
            ChannelSetup _currentSetup = _output.GetChannelSetup(_channelIndex);
            _currentSetup.Channel = (TextureChannel) _channelIndex;

            using (new HorizontalScope())
            {
                GUILayout.Label($"{_label}", Styles.CustomizedButton(SINGLE_LINE_HEIGHT, TextAnchor.MiddleCenter, new RectOffset(0, 0, 0, 0)), FixedWidth(INDEX_WIDTH));

                using (ColorScope.BackgroundAndContent(_indicatorColor))
                {
                    GUILayout.Box(string.Empty, Styles.ClearBox, FixedWidthAndHeight(COLOR_INDICATOR_WIDTH, SINGLE_LINE_HEIGHT));
                }

                using (new HorizontalScope(Styles.BoxButton, FixedHeight(SINGLE_LINE_HEIGHT)))
                {
                    Space(4f);
                    EditorGUILayout.Popup(0, new string[1] { _currentSetup.TextureSetup });
                }

                switch (_currentSetup.Source)
                {
                    case ChannelSource.None:
                        GUILayout.Label(string.Empty, Styles.BoxButton, FixedWidthAndHeight(CHANNEL_WIDTH, SINGLE_LINE_HEIGHT));
                        GUILayout.Label(string.Empty, Styles.BoxButton, FixedWidthAndHeight(SINGLE_LINE_HEIGHT));
                        break;

                    case ChannelSource.SolidColor:

                        using (new HorizontalScope(Styles.BoxButton, FixedWidthAndHeight(CHANNEL_WIDTH, SINGLE_LINE_HEIGHT)))
                        {
                            Space(4f);
                            GUILayout.HorizontalSlider(_currentSetup.ColorGrayscale, 0f, 1f, FixedWidth(CHANNEL_WIDTH - 50f));
                            EditorGUILayout.FloatField(_currentSetup.ColorGrayscale, GUILayout.Width(40f));
                        }

                        using (new HorizontalScope(Styles.BoxButton, FixedWidthAndHeight(SINGLE_LINE_HEIGHT)))
                        {
                            FlexibleSpace();

                            using (new VerticalScope())
                            {
                                FlexibleSpace();

                                using (ColorScope.BackgroundAndContent(new Color(_currentSetup.ColorGrayscale, _currentSetup.ColorGrayscale, _currentSetup.ColorGrayscale)))
                                {
                                    GUILayout.Box(string.Empty, Styles.ClearBox, FixedWidthAndHeight(SINGLE_LINE_HEIGHT - NORMAL_SPACE));
                                }

                                FlexibleSpace();
                            }

                            FlexibleSpace();
                        }

                        break;

                    default:

                        using (new HorizontalScope(Styles.BoxButton, FixedWidthAndHeight(CHANNEL_WIDTH, SINGLE_LINE_HEIGHT)))
                        {
                            Space(4f);
                            EditorGUILayout.EnumPopup(_currentSetup.InputChannel);
                        }

                        GUILayout.Toggle(_currentSetup.Invert, string.Empty, Styles.FixedToggle(SINGLE_LINE_HEIGHT), FixedWidthAndHeight(SINGLE_LINE_HEIGHT));
                        break;
                }
            }
        }
    }
}
