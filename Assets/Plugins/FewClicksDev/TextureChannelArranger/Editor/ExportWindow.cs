namespace FewClicksDev.TextureChannelArranger
{
    using FewClicksDev.Core;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEditor.Presets;
    using UnityEngine;

    using static FewClicksDev.Core.EditorDrawer;
    using Preferences = ChannelArrangerUserPreferences;

    public class ExportWindow : CustomEditorWindow
    {
        private static ExportWindow instance = null;

        private const int PREVIEW_TEXTURE_WIDTH = 256;
        private const int MIN_TEXTURE_SIZE = 16;
        private const float TEXTURE_PREVIEW_WIDTH = 128f;
        private const float BUTTON_WIDTH = 20f;
        private const float BUTTON_HEIGHT = 40f;
        private const float EXPORT_BUTTON_WIDTH_PERCENTAGE = 0.65f;

        private const string TEXTURE_PREVIEW = "Texture Preview";
        private const string LEFT_ARROW = "<";
        private const string RIGHT_ARROW = ">";
        private const string FORMAT = "Format";
        private const string FOLDER_PATH = "Folder path";
        private const string NAME = "Name";
        private const string OVERWRITE_EXISTING = "Overwrite existing";
        private const string TEXTURE_SIZE = "Texture Size";
        private const string PRESET_TO_APPLY = "Preset to apply";
        private const string REMOVE_FROM_EXPORT = "Remove from export";
        private const string EXPORT_CURRENT = "Export current";
        private const string EXPORT_ALL = "Export all";

        protected override string windowName => "Export Textures";
        protected override string version => TextureChannelArranger.VERSION;
        protected override Vector2 minWindowSize => new Vector2(340f, 500f);
        protected override Color mainColor => TextureChannelArranger.MAIN_COLOR;

        private ChannelArrangerPreset currentPreset = null;
        private List<TextureToExport> texturesToExport = new List<TextureToExport>();
        private Texture2D currentTexturePreview = null;

        private int currentTextureIndex = 0;

        protected override void drawWindowGUI()
        {
            if (texturesToExport.Count <= 0)
            {
                return;
            }

            LargeSpace();
            DrawCenteredBoldLabel(TEXTURE_PREVIEW);
            SmallSpace();

            using (new HorizontalScope(FixedHeight(TEXTURE_PREVIEW_WIDTH)))
            {
                FlexibleSpace();

                using (new VerticalScope(FixedWidth(BUTTON_WIDTH)))
                {
                    FlexibleSpace();

                    if (DrawBoxButton(LEFT_ARROW, FixedWidthAndHeight(BUTTON_WIDTH, BUTTON_HEIGHT)))
                    {
                        decrementCurrentTextureIndex();
                    }

                    FlexibleSpace();
                }

                NormalSpace();
                GUIStyle _texturePreview = new GUIStyle();
                _texturePreview.normal.background = currentTexturePreview;
                EditorGUILayout.LabelField(string.Empty, _texturePreview, FixedWidthAndHeight(TEXTURE_PREVIEW_WIDTH));
                NormalSpace();

                using (new VerticalScope(FixedWidth(BUTTON_WIDTH)))
                {
                    FlexibleSpace();

                    if (DrawBoxButton(RIGHT_ARROW, FixedWidthAndHeight(BUTTON_WIDTH, BUTTON_HEIGHT)))
                    {
                        incrementCurrentTextureIndex();
                    }

                    FlexibleSpace();
                }

                FlexibleSpace();
            }

            SmallSpace();
            DrawCenteredBoldLabel($"{currentTextureIndex + 1} / {texturesToExport.Count}");
            LargeSpace();

            using (new LabelWidthScope(120f))
            {
                var _textureToExport = texturesToExport[currentTextureIndex];
                _textureToExport.ExportFormat = (TextureExportFormat) EditorGUILayout.EnumPopup(FORMAT, _textureToExport.ExportFormat);
                _textureToExport.ExportFolderPath = EditorGUILayout.TextField(FOLDER_PATH, _textureToExport.ExportFolderPath);
                _textureToExport.ExportName = EditorGUILayout.TextField(NAME, _textureToExport.ExportName);
                _textureToExport.OverwriteExisting = EditorGUILayout.Toggle(OVERWRITE_EXISTING, _textureToExport.OverwriteExisting);

                using (new WideModeScope())
                {
                    SmallSpace();
                    _textureToExport.TextureSize = EditorGUILayout.Vector2IntField(TEXTURE_SIZE, _textureToExport.TextureSize);
                    _textureToExport.TextureSize = new Vector2Int(Mathf.Max(MIN_TEXTURE_SIZE, _textureToExport.TextureSize.x), Mathf.Max(MIN_TEXTURE_SIZE, _textureToExport.TextureSize.y));
                    _textureToExport.PresetToApply = (Preset) EditorGUILayout.ObjectField(PRESET_TO_APPLY, _textureToExport.PresetToApply, typeof(Preset), false);
                }

                texturesToExport[currentTextureIndex] = _textureToExport;
            }

            NormalSpace();

            using (new ScopeGroup(new HorizontalScope(), ColorScope.Background(RED)))
            {
                FlexibleSpace();

                if (DrawBoxButton(REMOVE_FROM_EXPORT, FixedWidthAndHeight(windowWidthScaled(EXPORT_BUTTON_WIDTH_PERCENTAGE), DEFAULT_LINE_HEIGHT)))
                {
                    texturesToExport.RemoveAt(currentTextureIndex);
                    decrementCurrentTextureIndex();
                }

                FlexibleSpace();
            }

            Space(3f);

            using (new ScopeGroup(new HorizontalScope(), ColorScope.Background(BLUE)))
            {
                FlexibleSpace();

                if (DrawBoxButton(EXPORT_CURRENT, FixedWidthAndHeight(windowWidthScaled(EXPORT_BUTTON_WIDTH_PERCENTAGE), DEFAULT_LINE_HEIGHT)))
                {
                    exportTexture(currentTextureIndex, true);
                }

                FlexibleSpace();
            }

            Space(3f);

            using (new HorizontalScope())
            {
                FlexibleSpace();

                if (DrawClearBoxButton(EXPORT_ALL, mainColor, FixedWidthAndHeight(windowWidthScaled(EXPORT_BUTTON_WIDTH_PERCENTAGE), DEFAULT_LINE_HEIGHT)))
                {
                    for (int i = 0; i < texturesToExport.Count; i++)
                    {
                        exportTexture(i, false);
                    }
                }

                FlexibleSpace();
            }
        }

        private void exportTexture(int _index, bool _ping)
        {
            TextureToExport _current = texturesToExport[_index];
            Texture2D _generatedTexture = TextureChannelArranger.GenerateOutputTexture(_current.Output, _current.TextureSize.x, _current.TextureSize.y, _current.Inputs);

            TextureChannelArranger.Log($"Generated a texture from {_current.Inputs.Count} inputs with format {_generatedTexture.graphicsFormat}.");
            TextureChannelArranger.SaveTheTexture(_current.ExportFolderPath, _generatedTexture, _current.ExportName, _generatedTexture.graphicsFormat, _current.ExportFormat, _current.PresetToApply, _ping);
        }

        private void decrementCurrentTextureIndex()
        {
            currentTextureIndex--;

            if (currentTextureIndex < 0)
            {
                currentTextureIndex = texturesToExport.Count - 1;
            }

            refreshCurrentTexturePreview();
        }

        private void incrementCurrentTextureIndex()
        {
            currentTextureIndex++;

            if (currentTextureIndex >= texturesToExport.Count)
            {
                currentTextureIndex = 0;
            }

            refreshCurrentTexturePreview();
        }

        private void refreshCurrentTexturePreview()
        {
            currentTexturePreview = TextureChannelArranger.GenerateOutputTexture(texturesToExport[currentTextureIndex].Output, PREVIEW_TEXTURE_WIDTH, PREVIEW_TEXTURE_WIDTH, texturesToExport[currentTextureIndex].Inputs);
        }

        private void initializePreset(ChannelArrangerPreset _preset)
        {
            currentTextureIndex = 0;
            texturesToExport.Clear();
            currentPreset = _preset;
        }

        public static void OpenWindow(ChannelArrangerPreset _preset)
        {
            instance = GetWindow<ExportWindow>();
            instance.Show();
            instance.initializePreset(_preset);
        }

        public static void AddTextureToExport(TextureOutput _output, TextureExportFormat _exportFormat, List<TextureWithSetup> _texturesWithSetup, int _index)
        {
            string _generatedName = _output.GenerateName(_texturesWithSetup, _index);

            instance.texturesToExport.Add(new TextureToExport(_output, _generatedName, _output.ExportPath, _exportFormat, Preferences.GetDefaultPreset(_output.ImportMode), _texturesWithSetup));
            instance.refreshCurrentTexturePreview();
        }
    }
}