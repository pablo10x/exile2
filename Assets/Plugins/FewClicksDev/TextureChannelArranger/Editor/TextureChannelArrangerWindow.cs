namespace FewClicksDev.TextureChannelArranger
{
    using FewClicksDev.Core;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.Presets;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.Rendering;

    using static FewClicksDev.Core.EditorDrawer;
    using Preferences = ChannelArrangerUserPreferences;

    public class TextureChannelArrangerWindow : CustomEditorWindow
    {
        public enum WindowMode
        {
            Arrange = 0,
            BatchConvert = 1,
            Presets = 2,
            Settings = 3
        }

        public enum Side
        {
            Right = 0,
            Left = 1
        }

        public static GUIStyle SingleLineLabelStyle
        {
            get
            {
                if (singleLineLabelStyle == null)
                {
                    singleLineLabelStyle = Styles.CustomizedButton(SINGLE_LINE_HEIGHT, TextAnchor.MiddleCenter, new RectOffset(0, 0, 0, 0));
                }

                return singleLineLabelStyle;
            }
        }

        public static GUIStyle SingleLineButtonStyle
        {
            get
            {
                if (singleLineButtonStyle == null)
                {
                    singleLineButtonStyle = Styles.CustomizedButton(SINGLE_LINE_HEIGHT, TextAnchor.MiddleLeft, new RectOffset(5, 0, 0, 0));
                }

                return singleLineButtonStyle;
            }
        }

        private static GUIStyle singleLineLabelStyle = null;
        private static GUIStyle singleLineButtonStyle = null;

        private static readonly int IS_SRGB_PROPERTY = Shader.PropertyToID("_isSRGB");
        private static readonly int IS_NORMAL_PROPERTY = Shader.PropertyToID("_isNormal");
        private static readonly int PREVIEW_SINGLE_CHANNEL_PROPERTY = Shader.PropertyToID("_previewSingleChannel");
        private static readonly int PREVIEW_MODE_PROPERTY = Shader.PropertyToID("_previewMode");

        private const int MAX_INPUTS_AND_OUTPUTS = 4;
        private const int MAX_VISIBLE_PRESETS = 8;

        private const float MAIN_TOOLBAR_WIDTH = 0.72f;
        private const float TOOLBAR_WIDTH = 300f;
        private const float SINGLE_LINE_HEIGHT = 22f;
        private const float INDEX_WIDTH = 22f;
        private const float INPUT_WIDTH = 120f;
        private const float INPUT_HEIGHT = 105f;
        private const float TEXTURE_PREVIEW_WIDTH = 90f;
        private const float COLOR_INDICATOR_WIDTH = 8f;
        private const float INPUT_TYPE_WIDTH = 120f;
        private const float CHANNEL_WIDTH = 100f;
        private const float SHORT_LABEL_WIDTH = 100f;
        private const float EDIT_MODE_OUTPUT_LABEL_WIDTH = 120f;
        private const float TOOLBAR_LABEL_WIDTH = 110f;
        private const float BOLD_LABEL_WIDTH = 120f;
        private const float CONNECTIONS_WIDTH = 60f;
        private const float CONNECTION_LINE_OFFSET = 15f;
        private const float SMALL_BUTTONS_HEIGHT = 14f;
        private const float SETTINGS_BUTTON_SIZE = 18f;

        private const string PRESET = "Preset";
        private const string ALL_PRESETS = "All presets";
        private const string PRESETS = "Presets";
        private const string SEARCH = "Search";
        private const string NO_PRESETS_FOUND = "No presets have been found.";
        private const string TOOLBAR_SIDE = "Toolbar side";
        private const string EXPORT_FORMAT = "Export format";
        private const string DEFAULT_EXPORT_PATH = "Default export path";
        private const string CREATE_PRESET = "Create Preset";
        private const string SHOW_TEXTURES_RESOLUTION = "Show resolutions";
        private const string SAVE_PRESET = "Save Preset";
        private const string PRESET_NAME = "Name";
        private const string CHANNEL_PREVIEW = "Channel preview";
        private const string SAVE_CURRENT_TO_NEW_PRESET = "Save current view to a new preset";
        private const string EXPORT_MODE = "Export mode";
        private const string FILE_NAME = "File name";
        private const string FILE_PATH = "File path";
        private const string FILE_TO_OVERWRITE = "File to overwrite";
        private const string INPUT = "Input_1";
        private const string ADD_INPUT = "Add input";
        private const string OUTPUT = "Output";
        private const string IMPORT_MODE = "Import mode";
        private const string ADD_OUTPUT = "Add output";
        private const string PRESET_PREFIX = "arrangerPreset_";
        private const string EXPORT = "Export";
        private const string EXPORT_ALL = "Export all";
        private const string CHOOSE_BATCH_PRESET = "Please choose which preset to use during the batch converting. You can use an object field below or the presets list.";
        private const string EXPORT_ALL_INFO = "You can use the button below to export all outputs at once.";
        private const string SEARCH_SETTINGS = "Search settings";
        private const string NAME_FILTER = "Name filter";
        private const string PREFIX = "Prefix";
        private const string SUFFIX = "Suffix";
        private const string CONNECTOR = "Connector";
        private const string FOLDER_PATH = "Folder path";
        private const string SELECT_A_FOLDER = "Select a folder";
        private const string AT_LEAST_ONE_OUTPUT = "Please make sure that you have at least one output with correct export paths and generated name!";
        private const string AT_LEAST_ONE_INPUT = "Please make sure that you have at least one texture input and the number of textures in each input is equal.";
        private const string EXPORT_PATH = "Export path";
        private const string FIND_TEXTURES = "Find textures";
        private const string TEXTURES = "Textures";
        private const string CONVENTION = "Convention";
        private const string NAME = "Name";
        private const string SEPARATOR = "Separator";
        private const string DIGITS = "Digits";
        private const string OUTPUT_NAME = "Output name";
        private const string ASSET = "asset";
        private const string CLEAR_INPUT_TEXTURES = "Clear input textures";
        private const string CLEAR_TEXTURES_QUESTION = "Do you want to clear the input textures after saving the preset?";
        private const string PRESETS_PATH_ERROR = "The preset must be saved within the current project assets folder!";
        private const string CLEAR_TEXTURES_LIST = "Clear all textures lists";
        private const string REMOVE_NULL_REFERENCES = "Remove null references";
        private const string OPEN_EXPORT_WINDOW = "Open export window";
        private const string ADD_MODE = "Add mode";
        private const string CHOOSE_VISIBLE_PRESETS_INFO = "Here you can choose which presets should be visible in the toolbar and in which order they should appear.";
        private const string SET_ALL_PRESETS_AS_VISIBLE = "Set all presets as visible";
        private const string REMOVE_ALL_PRESETS_FROM_VISIBLE = "Remove all presets from visible";
        private const string RESET_PRESETS_ORDER_TO_DEFAULT = "Reset presets order to default";
        private const string PRINT_LOGS = "Print logs";
        private const string DEFAULT_PRESET_NAME = "Default preset name";
        private const string DEFAULT_PRESET_PATH = "Default preset path";
        private const string APPLY_AT_START = "Apply at start";
        private const string SRGB = "sRGB";
        private const string LINEAR = "Linear";
        private const string NORMAL_MAP = "Normal map";
        private const string SINGLE_CHANNEL = "Single channel";
        private const string OPEN_PREFERENCES = "Open preferences";
        private const string PREFERENCES_SECTION = "FewClicks Dev/Texture Channel Arranger";
        private const string DEBUG = "Debug";
        private const string BLIT_MATERIAL = "Blit material";
        private const string INSERT_OUTPUT_NAME_INFO = "Please insert a name for the output texture";
        private const string ASSING_TEXTURE_TO_OVERWRITE_INFO = "Please assign a texture to overwite";
        private const string OVERWRITE_THE_TEXTURE = "Overwrite the texture";
        private const string UNSUPPORTED_FILE_FORMAT = "Unsupported file format";
        private const string EXPORT_ANYWAY = "Export anyway";
        private const string DIFFERENT = "Different";
        private const string EDIT_MODE = "Edit mode";
        private const string DISABLE_EDIT_MODE = "Disable edit mode";
        private const string DIFFERENT_FORMAT_INFO = "The format of the texture you are trying to overwrite is different than the one specified in the settings. What would yout like to do?";
        private const string SELECT_AT_LEAST_ONE_PRESET_INFO = "Please select at least one preset that will be visible in the toolbar. You can do it in the 'Presets' tab.";

        private const string SOLID_COLOR = "Solid Color";
        private const string NONE = "None";
        private const string SAVE_WINDOW_TITLE_TEXT = "Select an output path for the texture";

        protected override string windowName => TextureChannelArranger.NAME;
        protected override string version => TextureChannelArranger.VERSION;
        protected override Vector2 minWindowSize => new Vector2(1280f, 800f);
        protected override Color mainColor => TextureChannelArranger.MAIN_COLOR;

        protected override bool hasDocumentation => true;
        protected override string documentationURL => "https://docs.google.com/document/d/16LhCXYpJPX4JRvAT5o2wSiFzccIRTb9XZXcf9O1V-8Y/edit?usp=sharing";
        protected override bool askForReview => true;
        protected override string reviewURL => "https://assetstore.unity.com/packages/slug/285287";

        private WindowMode windowMode = WindowMode.Arrange;
        private bool showDebugOptions = false;

        private string presetSearchFilter = string.Empty;
        private List<ChannelArrangerPreset> foundPresets = null;
        private List<ChannelArrangerPreset> visiblePresets = new List<ChannelArrangerPreset>();
        private Vector2 presetsScrollPosition = Vector2.zero;

        private List<TextureInput> inputs = new List<TextureInput>();
        private List<TextureOutput> outputs = new List<TextureOutput>();
        private List<InputConnection> arrangeTabConnections = new List<InputConnection>();
        private List<string> inputsStrings = new List<string>();

        private bool isInEditMode = false;
        private ChannelArrangerPreset editedPreset = null;
        private SerializedObject editedPresetSerializedObjects = null;

        private ChannelArrangerPreset batchPreset = null;
        private List<TextureInput> batchInputs = new List<TextureInput> { };
        private List<TextureOutput> batchOutputs = new List<TextureOutput> { };
        private Vector2 batchConvertScrollPosition = Vector2.zero;

        private PresetSetupReorderableList presetsList = null;
        private Vector2 presetsTabScrollPosition = Vector2.zero;

        protected override void OnEnable()
        {
            base.OnEnable();

            Preferences.LoadPreferences();

            recreateInputStrings();
            recreateInputTexturesLists();
            recalculateArrangeTabConnections();
            regenerateOutputTextures();
            disableEditMode();

            if (foundPresets.IsNullOrEmpty())
            {
                refreshPresets();
            }

            ChannelArrangerPresetSetup.OnAnySetupChanged -= recalculateVisiblePresets; //Just to be sure :) 
            ChannelArrangerPresetSetup.OnAnySetupChanged += recalculateVisiblePresets;

            AssetsPostprocessor.OnAssetsImported -= onAssetsImported;
            AssetsPostprocessor.OnAssetsImported += onAssetsImported;
        }

        private void OnDestroy()
        {
            Preferences.SavePreferences();
            ChannelArrangerPresetSetup.OnAnySetupChanged -= recalculateVisiblePresets;

            AssetsPostprocessor.OnAssetsImported -= onAssetsImported;

            presetsList.OnReorder -= updatePresetsOrderList;
            presetsList?.Destroy();
            clearTexturesLists();
        }

        protected override void drawWindowGUI()
        {
            NormalSpace();

            using (new DisabledScope(isInEditMode))
            {
                windowMode = this.DrawEnumToolbar(windowMode, MAIN_TOOLBAR_WIDTH, mainColor);
            }

            SmallSpace();
            DrawLine();
            SmallSpace();

            switch (windowMode)
            {
                case WindowMode.Arrange:
                    drawArrangeTab();
                    break;

                case WindowMode.BatchConvert:
                    drawBatchConvertTab();
                    break;

                case WindowMode.Presets:
                    drawPresetsTab();
                    break;

                case WindowMode.Settings:
                    drawSettingsTab();
                    break;
            }
        }

        #region Arrange

        private void drawArrangeTab()
        {
            using (new HorizontalScope())
            {
                if (Preferences.ToolbarSide is Side.Left)
                {
                    using (new VerticalScope(FixedWidth(TOOLBAR_WIDTH)))
                    {
                        drawArrangeTabToolbar();
                    }

                    DrawVerticalLine(3f);

                    using (new VerticalScope())
                    {
                        drawInputsAndOutputs();
                    }
                }
                else
                {
                    using (new VerticalScope())
                    {
                        drawInputsAndOutputs();
                    }

                    DrawVerticalLine(3f);

                    using (new VerticalScope(FixedWidth(TOOLBAR_WIDTH)))
                    {
                        drawArrangeTabToolbar();
                    }
                }
            }
        }

        private void drawArrangeTabToolbar()
        {
            using (new HorizontalScope())
            {
                NormalSpace();

                using (new VerticalScope())
                {
                    if (isInEditMode)
                    {
                        drawEditModeToolbar();
                    }
                    else
                    {
                        drawPresets(applyPreset);
                        drawCreatePreset();
                        drawBaseSettings(true);

                        if (outputs.Count > 1)
                        {
                            bool _canExportAll = outputs.All(o => o.CanExportTexture());

                            if (_canExportAll)
                            {
                                NormalSpace();
                                EditorGUILayout.HelpBox(EXPORT_ALL_INFO, MessageType.Info);
                                SmallSpace();

                                using (new ScopeGroup(new HorizontalScope(), ColorScope.Background(TextureChannelArranger.LOGS_COLOR)))
                                {
                                    FlexibleSpace();

                                    if (DrawBoxButton(EXPORT_ALL, FixedWidthAndHeight(120f, SINGLE_LINE_HEIGHT)))
                                    {
                                        foreach (var _output in outputs)
                                        {
                                            exportTexture(_output);
                                        }
                                    }

                                    FlexibleSpace();
                                }
                            }
                        }
                    }
                }

                NormalSpace();
            }
        }

        private void drawInputsAndOutputs()
        {
            if (isInEditMode)
            {
                drawPresetEditMode();
                return;
            }

            SmallSpace();

            using (new HorizontalScope())
            {
                LargeSpace();
                drawInputs();
                Space(CONNECTIONS_WIDTH);
                drawOutputs();
                LargeSpace();
            }

            drawArrangeTabConnections();
        }

        private void drawInputs()
        {
            using (new VerticalScope(FixedWidth(INPUT_WIDTH)))
            {
                FlexibleSpace();

                for (int i = 0; i < inputs.Count; i++)
                {
                    bool _allowRemove = inputs.Count > 1;
                    bool _removed = drawInput(inputs[i], _allowRemove);

                    if (_removed)
                    {
                        return;
                    }

                    if (i < inputs.Count - 1)
                    {
                        NormalSpace();
                    }
                }

                LargeSpace();

                if (inputs.Count < MAX_INPUTS_AND_OUTPUTS)
                {
                    using (new HorizontalScope())
                    {
                        FlexibleSpace();

                        if (DrawBoxButton(new GUIContent(ADD_INPUT), FixedWidthAndHeight(100f, SINGLE_LINE_HEIGHT)))
                        {
                            inputs.Add(new TextureInput(ObjectNames.GetUniqueName(inputs.Select(i => i.TextureName).ToArray(), INPUT)));
                            recreateInputStrings();
                        }

                        FlexibleSpace();
                    }
                }

                FlexibleSpace();
            }
        }

        private void drawOutputs()
        {
            using (new VerticalScope())
            {
                FlexibleSpace();

                for (int i = 0; i < outputs.Count; i++)
                {
                    bool _removed = drawOutput(outputs[i]);

                    if (_removed)
                    {
                        return;
                    }

                    if (i < outputs.Count - 1)
                    {
                        NormalSpace();
                    }
                }

                LargeSpace();

                if (outputs.Count < MAX_INPUTS_AND_OUTPUTS)
                {
                    using (new HorizontalScope())
                    {
                        FlexibleSpace();

                        if (DrawBoxButton(new GUIContent(ADD_OUTPUT), FixedWidthAndHeight(160f, SINGLE_LINE_HEIGHT)))
                        {
                            outputs.Add(new TextureOutput(ObjectNames.GetUniqueName(outputs.Select(i => i.TextureName).ToArray(), OUTPUT)));
                            recreateInputStrings();
                        }

                        FlexibleSpace();
                    }
                }

                FlexibleSpace();
            }
        }

        private void drawArrangeTabConnections()
        {
            if (isInEditMode == false)
            {
                foreach (var _connection in arrangeTabConnections)
                {
                    if (_connection.InputChannel is TextureChannelInput.RGB)
                    {
                        drawCombinedConnection(inputs[_connection.InputTextureIndex], outputs[_connection.OutputTextureIndex], _connection.OutputChannel);
                        continue;
                    }

                    if (_connection.IsValid(inputs, outputs) == false)
                    {
                        continue;
                    }

                    Vector2 _start = getStartPosition(_connection);
                    Vector2 _end = getEndPosition(_connection);
                    drawHorizontalConnection(_start, _end, TextureChannelArranger.GetColorFromChannel(_connection.OutputChannel));
                }
            }
            else
            {
                foreach (var _connection in arrangeTabConnections)
                {
                    if (_connection.InputChannel is TextureChannelInput.RGB)
                    {
                        drawCombinedConnection(inputs[_connection.InputTextureIndex], editedPreset.GetOutputAtIndex(_connection.OutputTextureIndex), _connection.OutputChannel);
                        continue;
                    }

                    if (_connection.IsValid(inputs, editedPreset.Outputs) == false)
                    {
                        continue;
                    }

                    Vector2 _start = getStartPosition(_connection);
                    Vector2 _end = getEndPosition(_connection);
                    drawHorizontalConnection(_start, _end, TextureChannelArranger.GetColorFromChannel(_connection.OutputChannel));
                }
            }
        }

        private void drawCreatePreset()
        {
            NormalSpace();
            DrawCenteredBoldLabel(CREATE_PRESET, BOLD_LABEL_WIDTH);
            SmallSpace();

            using (new LabelWidthScope(TOOLBAR_LABEL_WIDTH))
            {
                Preferences.PresetName = EditorGUILayout.TextField(PRESET_NAME, Preferences.PresetName);
            }

            SmallSpace();

            if (DrawClearBoxButton(SAVE_CURRENT_TO_NEW_PRESET, mainColor, FixedHeight(SINGLE_LINE_HEIGHT)))
            {
                createNewPreset();
            }
        }

        private bool drawInput(TextureInput _input, bool _allowRemove = true)
        {
            bool _removed = false;
            float _height = _input.Reference == null ? SINGLE_LINE_HEIGHT : INPUT_HEIGHT;

            using (new HorizontalScope(EditorStyles.helpBox, FixedWidthAndHeight(INPUT_WIDTH, _height)))
            {
                using (new VerticalScope())
                {
                    using (new VerticalScope(FixedWidth(TEXTURE_PREVIEW_WIDTH)))
                    {
                        GUILayout.Label(_input.TextureName, EditorStyles.centeredGreyMiniLabel);

                        using (new HorizontalScope())
                        {
                            SmallSpace();
                            Texture2D _textureReference = EditorGUILayout.ObjectField(_input.Reference, typeof(Texture2D), false, FixedWidth(TEXTURE_PREVIEW_WIDTH)) as Texture2D;

                            if (_textureReference != _input.Reference)
                            {
                                _input.SetTexture(_textureReference);
                                regenerateOutputTextures();
                            }

                            SmallSpace();
                        }
                    }

                    drawTexturePreview(_input, _input.Reference);

                    using (new HorizontalScope())
                    {
                        FlexibleSpace();

                        GUIStyle _new = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
                        _new.normal.textColor = new Color(0.8f, 0.8f, 0.8f);

                        if (_allowRemove)
                        {
                            if (GUILayout.Button(REMOVE, _new, FixedHeight(SMALL_BUTTONS_HEIGHT)))
                            {
                                _removed = true;
                                removeInput(inputs.IndexOf(_input));
                            }

                            SmallSpace();
                        }

                        if (_input.Reference != null)
                        {
                            if (GUILayout.Button(CLEAR, _new, FixedHeight(SMALL_BUTTONS_HEIGHT)))
                            {
                                _input.SetTexture(null);
                                regenerateOutputTextures();
                            }
                        }

                        FlexibleSpace();
                    }
                }

                if (_input.Reference != null)
                {
                    drawTextureChannels(_input);
                    SmallSpace();
                }
            }

            return _removed;
        }

        private void removeInput(int _index)
        {
            inputs.RemoveAt(_index);
            refreshArrangeTab();
        }

        private bool drawOutput(TextureOutput _output)
        {
            bool _removed = false;

            using (new HorizontalScope(EditorStyles.helpBox, FixedHeight(INPUT_HEIGHT)))
            {
                SmallSpace();
                drawTextureChannels(_output);

                using (new VerticalScope(FixedWidth(COLOR_INDICATOR_WIDTH + INPUT_WIDTH + CHANNEL_WIDTH + SINGLE_LINE_HEIGHT)))
                {
                    FlexibleSpace();

                    for (int i = 0; i < TextureChannelArranger.NUMBER_OF_CHANNELS; i++)
                    {
                        drawChannelSetup(inputsStrings, _output, i, TextureChannelArranger.CHANNELS[i], TextureChannelArranger.GetColorFromChannel((TextureChannel) i));
                    }

                    FlexibleSpace();
                }

                SmallSpace();

                using (new VerticalScope(FixedWidth(TEXTURE_PREVIEW_WIDTH)))
                {
                    using (new VerticalScope(FixedWidth(TEXTURE_PREVIEW_WIDTH)))
                    {
                        FlexibleSpace();
                        GUILayout.Label(_output.TextureName, EditorStyles.centeredGreyMiniLabel);
                        drawTexturePreview(_output, _output.GeneratedTexture);
                        FlexibleSpace();
                    }

                    using (new HorizontalScope())
                    {
                        if (outputs.Count <= 1)
                        {
                            GUILayout.Label(string.Empty, FixedHeight(SMALL_BUTTONS_HEIGHT));
                        }
                        else
                        {
                            FlexibleSpace();

                            GUIStyle _new = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
                            _new.normal.textColor = new Color(0.8f, 0.8f, 0.8f);

                            if (GUILayout.Button(REMOVE, _new, FixedHeight(SMALL_BUTTONS_HEIGHT)))
                            {
                                _removed = true;
                                removeOutput(outputs.IndexOf(_output));
                            }

                            FlexibleSpace();
                        }
                    }
                }

                using (new VerticalScope())
                {
                    FlexibleSpace();

                    using (new LabelWidthScope(SHORT_LABEL_WIDTH))
                    {
                        _output.ExportMode = (TextureExportMode) EditorGUILayout.EnumPopup(EXPORT_MODE, _output.ExportMode);

                        switch (_output.ExportMode)
                        {
                            case TextureExportMode.CreateNew:

                                _output.ExportNamingConvention = (TextureNamingConvention) EditorGUILayout.EnumPopup(CONVENTION, _output.ExportNamingConvention);

                                switch (_output.ExportNamingConvention)
                                {
                                    case TextureNamingConvention.AddPrefix:
                                        _output.ExportPrefix = EditorGUILayout.TextField(PREFIX, _output.ExportPrefix);
                                        _output.GeneratedName = _output.ExportPrefix + _getFirstValidInputName();
                                        _drawGeneratedName();
                                        break;

                                    case TextureNamingConvention.AddSuffix:
                                        _output.ExportSuffix = EditorGUILayout.TextField(SUFFIX, _output.ExportSuffix);
                                        _output.GeneratedName = _getFirstValidInputName() + _output.ExportSuffix;
                                        _drawGeneratedName();
                                        break;

                                    case TextureNamingConvention.Custom:
                                        _output.FileName = EditorGUILayout.TextField(FILE_NAME, _output.FileName);
                                        _output.GeneratedName = _output.FileName;
                                        break;
                                }

                                break;

                            case TextureExportMode.Overwrite:
                                _output.FileToOverwrite = EditorGUILayout.ObjectField(FILE_TO_OVERWRITE, _output.FileToOverwrite, typeof(Texture2D), false, FixedHeight(SingleLineHeight)) as Texture2D;
                                break;
                        }

                        using (var _changeScope = new ChangeCheckScope())
                        {
                            _output.ImportMode = (TextureImportMode) EditorGUILayout.EnumPopup(IMPORT_MODE, _output.ImportMode);

                            if (_changeScope.changed)
                            {
                                regenerateOutput(_output);
                            }
                        }

                        if (_output.ImportMode is TextureImportMode.Custom)
                        {
                            _output.CustomPreset = EditorGUILayout.ObjectField(PRESET, _output.CustomPreset, typeof(Preset), false) as Preset;
                        }
                        else
                        {
                            using (new HorizontalScope())
                            {
                                DrawDefaultLabel(PRESET);

                                using (new DisabledScope())
                                {
                                    EditorGUILayout.ObjectField(Preferences.GetDefaultPreset(_output.ImportMode), typeof(Preset), false);
                                }
                            }
                        }
                    }

                    if (_output.CanExportTexture())
                    {
                        SmallSpace();

                        using (new HorizontalScope())
                        {
                            FlexibleSpace();

                            if (DrawClearBoxButton(EXPORT, mainColor, FixedWidthAndHeight(120f, SINGLE_LINE_HEIGHT)))
                            {
                                exportTexture(_output);
                            }

                            FlexibleSpace();
                        }
                    }
                    else
                    {
                        SmallSpace();

                        using (new ScopeGroup(new HorizontalScope(), ColorScope.BackgroundAndContent(RED)))
                        {
                            FlexibleSpace();

                            switch (_output.ExportMode)
                            {
                                case TextureExportMode.CreateNew:
                                    GUILayout.Label(INSERT_OUTPUT_NAME_INFO);
                                    break;

                                case TextureExportMode.Overwrite:
                                    GUILayout.Label(ASSING_TEXTURE_TO_OVERWRITE_INFO);
                                    break;
                            }

                            FlexibleSpace();
                        }
                    }

                    FlexibleSpace();
                }

                SmallSpace();
            }

            return _removed;

            string _getFirstValidInputName()
            {
                foreach (var _input in inputs)
                {
                    if (_input.Reference != null)
                    {
                        return _input.Reference.GetNameIfNotNull();
                    }
                }

                return string.Empty;
            }

            void _drawGeneratedName()
            {
                using (new HorizontalScope())
                {
                    DrawDefaultLabel(FILE_NAME);

                    using (new DisabledScope())
                    {
                        EditorGUILayout.TextField(_output.GeneratedName);
                    }
                }
            }
        }

        private void removeOutput(int _index)
        {
            outputs.RemoveAt(_index);
            refreshArrangeTab();
        }

        private void refreshArrangeTab()
        {
            recreateInputStrings();
            regenerateOutputTextures();
            recalculateArrangeTabConnections();
        }

        private void drawChannelSetup(List<string> _inputStrings, TextureOutput _output, int _channelIndex, string _label, Color _indicatorColor)
        {
            ChannelSetup _currentSetup = _output.GetChannelSetup(_channelIndex);
            _currentSetup.Channel = (TextureChannel) _channelIndex;

            if (_currentSetup.TextureSetup.IsNullEmptyOrWhitespace())
            {
                _currentSetup.TextureSetup = NONE;
            }

            bool _scopeChanged = false;
            bool _disabled = _output.ShouldChannelBeDisabled(_currentSetup);

            using (new ScopeGroup(new DisabledScope(_disabled), new HorizontalScope()))
            {
                using (ColorScope.BackgroundAndContent(_indicatorColor))
                {
                    GUILayout.Box(string.Empty, Styles.ClearBox, FixedWidthAndHeight(COLOR_INDICATOR_WIDTH, SINGLE_LINE_HEIGHT));
                }

                using (var _changeScope = new ChangeCheckScope())
                {
                    ChannelSource _source = _disabled ? ChannelSource.None : _currentSetup.Source;
                    string _baseTextureSetup = _currentSetup.TextureSetup;
                    int _index = _disabled ? _inputStrings.Count - 1 : Mathf.Clamp(_inputStrings.IndexOf(_currentSetup.TextureSetup), 0, _inputStrings.Count - 1);

                    using (new HorizontalScope(Styles.BoxButton, FixedWidthAndHeight(INPUT_TYPE_WIDTH, SINGLE_LINE_HEIGHT)))
                    {
                        Space(4f);
                        int _newIndex = EditorGUILayout.Popup(_index, _inputStrings.ToArray());

                        if (_disabled == false)
                        {
                            _currentSetup.TextureSetup = _inputStrings[_newIndex];
                            _currentSetup.Source = getSourceFromSetup(_currentSetup.TextureSetup);

                            if (_currentSetup.TextureSetup != _baseTextureSetup)
                            {
                                refreshArrangeTab();
                            }
                        }
                    }

                    switch (_source)
                    {
                        case ChannelSource.None:
                            GUILayout.Label(string.Empty, Styles.BoxButton, FixedWidthAndHeight(CHANNEL_WIDTH, SINGLE_LINE_HEIGHT));
                            GUILayout.Label(string.Empty, Styles.BoxButton, FixedWidthAndHeight(SINGLE_LINE_HEIGHT));
                            break;

                        case ChannelSource.SolidColor:

                            using (new HorizontalScope(Styles.BoxButton, FixedWidthAndHeight(CHANNEL_WIDTH, SINGLE_LINE_HEIGHT)))
                            {
                                Space(4f);
                                _currentSetup.ColorGrayscale = GUILayout.HorizontalSlider(_currentSetup.ColorGrayscale, 0f, 1f, FixedWidth(CHANNEL_WIDTH - 50f));
                                _currentSetup.ColorGrayscale = EditorGUILayout.FloatField(_currentSetup.ColorGrayscale, GUILayout.Width(40f));
                                _currentSetup.ColorGrayscale = Mathf.Clamp01(_currentSetup.ColorGrayscale);
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
                                _currentSetup.InputChannel = (TextureChannelInput) EditorGUILayout.EnumPopup(_currentSetup.InputChannel);
                            }

                            _currentSetup.Invert = GUILayout.Toggle(_currentSetup.Invert, string.Empty, Styles.FixedToggle(SINGLE_LINE_HEIGHT), FixedWidthAndHeight(SINGLE_LINE_HEIGHT));
                            break;
                    }

                    if (_changeScope.changed)
                    {
                        _scopeChanged = true;
                    }
                }
            }

            _output.UpdateChannelSetup(_channelIndex, _currentSetup);

            if (_scopeChanged)
            {
                regenerateOutputTextures();
            }
        }

        private void drawTextureChannels(TextureReference _reference)
        {
            int _numberOfChannels = _reference.NumberOfChannels;
            Event _current = Event.current;

            using (new VerticalScope(FixedWidth(INDEX_WIDTH)))
            {
                FlexibleSpace();

                for (int i = 0; i < _numberOfChannels; i++)
                {
                    Color _guiColor = _isSelected(i) ? DEFAULT_GRAY : Color.white;

                    using (ColorScope.Background(_guiColor))
                    {
                        if (DrawBoxButton(TextureChannelArranger.CHANNELS[i], FixedWidthAndHeight(INDEX_WIDTH, SINGLE_LINE_HEIGHT)))
                        {
                            if (_reference.IsNormalMap == false) //Unity doesn't provide a way to preview normal maps channels
                            {
                                _reference.UpdateTexturePreviewMask(_getMask(i));
                            }
                        }
                    }

                    Rect _lastRect = GetLastRect();

                    if (_lastRect.x != 0)
                    {
                        _reference.SetChannelRect(i, _lastRect);
                    }
                }

                FlexibleSpace();
            }

            bool _isSelected(int _channelIndex)
            {
                return _reference.PreviewMask == _getMask(_channelIndex);
            }

            ColorWriteMask _getMask(int _channelIndex)
            {
                return _channelIndex switch
                {
                    0 => ColorWriteMask.Red,
                    1 => ColorWriteMask.Green,
                    2 => ColorWriteMask.Blue,
                    3 => ColorWriteMask.Alpha,
                    _ => ColorWriteMask.All
                };
            }
        }

        private void createNewPreset()
        {
            bool _clearInputTextures = EditorUtility.DisplayDialog(CLEAR_INPUT_TEXTURES, CLEAR_TEXTURES_QUESTION, YES, NO);

            var _newPreset = ScriptableObject.CreateInstance<ChannelArrangerPreset>();
            _newPreset.CreatePresetFromTheWindow(Preferences.PresetName, inputs, outputs, _clearInputTextures);
            _newPreset.name = $"{PRESET_PREFIX}{Preferences.PresetName.EveryWordToUpper().RemoveSpaces()}";

            var _path = EditorUtility.SaveFilePanel(SAVE_PRESET, Preferences.PresetPath, _newPreset.name, ASSET);

            if (_path.IsNullEmptyOrWhitespace())
            {
                return;
            }

            if (_path.StartsWith(Application.dataPath))
            {
                AssetDatabase.CreateAsset(_newPreset, AssetsUtilities.ConvertAbsolutePathToDataPath(_path));
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                AssetsUtilities.Ping(_newPreset);
                refreshPresets();
            }
            else
            {
                TextureChannelArranger.Error(PRESETS_PATH_ERROR);
            }
        }

        private void regenerateOutputTextures()
        {
            for (int i = 0; i < outputs.Count; i++)
            {
                regenerateOutput(outputs[i]);
            }

            recalculateArrangeTabConnections();
        }

        private void regenerateOutput(TextureOutput _output)
        {
            var _texturesWithChannels = getTexturesWithChannelsForTheTexture(_output);
            _output.RegenerateTextureForPreview(_texturesWithChannels);
        }

        private void recalculateArrangeTabConnections()
        {
            arrangeTabConnections.Clear();

            for (int i = 0; i < outputs.Count; i++)
            {
                foreach (var _channelSetup in outputs[i].ChannelSetups)
                {
                    if (_channelSetup.Source != ChannelSource.Texture)
                    {
                        continue;
                    }

                    InputConnection _connection = new InputConnection();
                    _connection.InputTextureIndex = inputsStrings.IndexOf(_channelSetup.TextureSetup);

                    if (_connection.InputTextureIndex < 0 || _connection.InputTextureIndex >= inputs.Count)
                    {
                        continue;
                    }

                    if (inputs[_connection.InputTextureIndex].Reference == null)
                    {
                        continue;
                    }

                    if (_channelSetup.InputChannel != TextureChannelInput.RGB
                        && inputs[_connection.InputTextureIndex].NumberOfChannels <= (int) _channelSetup.InputChannel) //E.g. texture doesn't have an alpha channel
                    {
                        continue;
                    }

                    _connection.InputChannel = _channelSetup.InputChannel;
                    _connection.OutputTextureIndex = i;
                    _connection.OutputChannel = _channelSetup.Channel;

                    arrangeTabConnections.Add(_connection);
                }
            }
        }

        private void recreateInputStrings()
        {
            inputsStrings.Clear();

            foreach (var _input in inputs)
            {
                inputsStrings.Add(_input.TextureName);
            }

            inputsStrings.Add(SOLID_COLOR);
            inputsStrings.Add(NONE);
        }

        #endregion

        #region Batch Convert

        private void drawBatchConvertTab()
        {
            using (new HorizontalScope())
            {
                if (Preferences.ToolbarSide is Side.Left)
                {
                    using (new VerticalScope(FixedWidth(TOOLBAR_WIDTH)))
                    {
                        drawBatchConvertTabToolbar();
                    }

                    DrawVerticalLine(3f);

                    using (new VerticalScope())
                    {
                        drawBatchConvert();
                    }
                }
                else
                {
                    using (new VerticalScope())
                    {
                        drawBatchConvert();
                    }

                    DrawVerticalLine(3f);

                    using (new VerticalScope(FixedWidth(TOOLBAR_WIDTH)))
                    {
                        drawBatchConvertTabToolbar();
                    }
                }
            }
        }

        private void drawBatchConvert()
        {
            using (var _scrollScope = new ScrollViewScope(batchConvertScrollPosition))
            {
                if (batchPreset == null)
                {
                    SmallSpace();

                    using (new HorizontalScope())
                    {
                        LargeSpace();
                        EditorGUILayout.HelpBox(CHOOSE_BATCH_PRESET, MessageType.Warning);
                        LargeSpace();
                    }
                }

                SmallSpace();

                using (new ScopeGroup(new HorizontalScope(), new LabelWidthScope(100f)))
                {
                    LargeSpace();
                    var _batchPreset = EditorGUILayout.ObjectField(PRESET, batchPreset, typeof(ChannelArrangerPreset), false) as ChannelArrangerPreset;

                    if (_batchPreset != batchPreset)
                    {
                        updateBatchPreset(_batchPreset);
                    }

                    LargeSpace();
                }

                if (batchPreset == null)
                {
                    return;
                }

                LargeSpace();

                using (new HorizontalScope())
                {
                    LargeSpace();
                    FlexibleSpace();
                    drawBatchOutputs();
                    FlexibleSpace();
                    LargeSpace();
                }

                Space(CONNECTIONS_WIDTH);

                using (new HorizontalScope())
                {
                    LargeSpace();
                    FlexibleSpace();
                    drawBatchInputs();
                    FlexibleSpace();
                    LargeSpace();
                }

                drawBatchConvertConnections();
                batchConvertScrollPosition = _scrollScope.scrollPosition;
            }
        }

        private void drawBatchConvertConnections()
        {
            for (int i = 0; i < batchOutputs.Count; i++)
            {
                foreach (var _input in batchOutputs[i].ChannelSetups)
                {
                    if (_input.Source is ChannelSource.Texture)
                    {
                        int _textureIndex = getBatchInputIndex(_input.TextureSetup);

                        if (_textureIndex == -1)
                        {
                            continue;
                        }

                        drawVerticalConnection(batchOutputs[i].BatchConvertRect, batchInputs[_textureIndex].BatchConvertRect, TextureChannelArranger.GetColorFromChannel(_input.Channel));
                    }
                }
            }
        }

        private int getBatchInputIndex(string _name)
        {
            for (int i = 0; i < batchInputs.Count; i++)
            {
                if (batchInputs[i].TextureName == _name)
                {
                    return i;
                }
            }

            return -1;
        }

        private void updateBatchPreset(ChannelArrangerPreset _preset)
        {
            batchPreset = _preset;
            clearTexturesLists();

            if (batchPreset == null)
            {
                return;
            }

            for (int i = 0; i < batchPreset.NumberOfInputs; i++)
            {
                batchInputs.Add(batchPreset.GetInputCopyAtIndex(i));
            }

            recreateInputTexturesLists();

            for (int i = 0; i < batchPreset.NumberOfOutputs; i++)
            {
                batchOutputs.Add(batchPreset.GetOutputCopyAtIndex(i));
            }

            foreach (var _output in batchOutputs)
            {
                _output.UpdateGeneratedName(batchInputs);
            }
        }

        private void recreateInputTexturesLists()
        {
            foreach (var _input in batchInputs)
            {
                _input.CreateTexturesList();
            }
        }

        private void clearTexturesLists()
        {
            foreach (var _input in batchInputs)
            {
                _input.DestroyTexturesList();
            }

            batchInputs.Clear();
            batchOutputs.Clear();
        }

        private void drawBatchInputs()
        {
            using (new HorizontalScope())
            {
                for (int i = 0; i < batchInputs.Count; i++)
                {
                    drawSingleBatchInput(batchInputs[i]);

                    Rect _lastRect = GetLastRect();

                    if (_lastRect.width != 0)
                    {
                        batchInputs[i].BatchConvertRect = _lastRect;
                    }

                    if (i != batchInputs.Count - 1)
                    {
                        NormalSpace();
                    }
                }
            }
        }

        private void drawSingleBatchInput(TextureInput _input)
        {
            using (new HorizontalScope(EditorStyles.helpBox, MaxWidth(340f)))
            {
                SmallSpace();

                using (new VerticalScope())
                {
                    SmallSpace();

                    using (new HorizontalScope())
                    {
                        FlexibleSpace();
                        GUILayout.Label($"{_input.TextureName} [{_getOutputSetup()}]", EditorStyles.centeredGreyMiniLabel);
                        FlexibleSpace();
                    }

                    using (new LabelWidthScope(80f))
                    {
                        SmallSpace();
                        _input.AddMode = (TextureAddMode) EditorGUILayout.EnumPopup(ADD_MODE, _input.AddMode);

                        if (_input.AddMode is TextureAddMode.Search)
                        {
                            NormalSpace();
                            DrawCenteredBoldLabel(SEARCH_SETTINGS, BOLD_LABEL_WIDTH);

                            _input.NameFilter = EditorGUILayout.TextField(NAME_FILTER, _input.NameFilter);
                            _input.Prefix = EditorGUILayout.TextField(PREFIX, _input.Prefix);
                            _input.Suffix = EditorGUILayout.TextField(SUFFIX, _input.Suffix);

                            string _newPath = DrawFolderPicker(FOLDER_PATH, _input.FolderPath, SELECT_A_FOLDER);

                            if (_newPath.IsNullEmptyOrWhitespace())
                            {
                                _newPath = Application.dataPath;
                            }

                            _input.FolderPath = _newPath;
                        }
                    }

                    if (_input.AddMode is TextureAddMode.Search && _input.FolderPath.IsNullEmptyOrWhitespace() == false)
                    {
                        SmallSpace();

                        using (new HorizontalScope())
                        {
                            FlexibleSpace();

                            if (DrawClearBoxButton(FIND_TEXTURES, mainColor, FixedWidthAndHeight(180f, 20f)))
                            {
                                _input.FindMatchingTextures();
                            }

                            FlexibleSpace();
                        }
                    }

                    NormalSpace();
                    DrawCenteredBoldLabel(TEXTURES, BOLD_LABEL_WIDTH);
                    _input.TexturesList.Draw();
                    SmallSpace();
                }

                int _maxNumberOfTextures = _getMaxNumberOfFoundTextures();

                if (_input.NumberOfFoundTextures > 0 || _input.NumberOfFoundTextures < _maxNumberOfTextures)
                {
                    Rect _lastRect = GetLastRect();
                    Rect _buttonRect = new Rect(_lastRect.x + _lastRect.width - SETTINGS_BUTTON_SIZE - 4f, _lastRect.y + 2f, SETTINGS_BUTTON_SIZE, SETTINGS_BUTTON_SIZE);

                    if (GUI.Button(_buttonRect, string.Empty, Styles.SettingsButton))
                    {
                        Event _event = Event.current;
                        ChannelArrangerGenericMenu.ShowForBatchInput(_input, _event, _maxNumberOfTextures);
                    }
                }

                SmallSpace();
            }

            string _getOutputSetup()
            {
                string _outputString = string.Empty;

                foreach (var _output in batchOutputs)
                {
                    foreach (var _channelSetup in _output.ChannelSetups)
                    {
                        if (_channelSetup.Source is ChannelSource.Texture && _channelSetup.TextureSetup == _input.TextureName)
                        {
                            _outputString += TextureChannelArranger.CHANNELS[(int) _channelSetup.InputChannel];
                        }
                    }
                }

                return _outputString;
            }

            int _getMaxNumberOfFoundTextures()
            {
                int _maxNumber = 0;

                foreach (var _input in batchInputs)
                {
                    _maxNumber = Mathf.Max(_maxNumber, _input.NumberOfFoundTextures);
                }

                return _maxNumber;
            }
        }

        private void drawBatchOutputs()
        {
            using (new HorizontalScope())
            {
                for (int i = 0; i < batchOutputs.Count; i++)
                {
                    drawSingleBatchOutput(batchOutputs[i]);

                    Rect _lastRect = GetLastRect();

                    if (_lastRect.width != 0)
                    {
                        batchOutputs[i].BatchConvertRect = _lastRect;
                    }

                    if (i != batchOutputs.Count - 1)
                    {
                        NormalSpace();
                    }
                }
            }
        }

        private void drawSingleBatchOutput(TextureOutput _output)
        {
            using (new HorizontalScope(EditorStyles.helpBox, MaxWidth(340f)))
            {
                SmallSpace();
                using (new VerticalScope())
                {
                    SmallSpace();

                    using (new HorizontalScope())
                    {
                        FlexibleSpace();
                        GUILayout.Label(_output.TextureName, EditorStyles.centeredGreyMiniLabel);
                        FlexibleSpace();
                    }

                    SmallSpace();

                    using (new LabelWidthScope(80f))
                    {
                        using (var _changeScope = new ChangeCheckScope())
                        {
                            _output.BatchExportNamingConvention = (BatchTextureNamingConvention) EditorGUILayout.EnumPopup(CONVENTION, _output.BatchExportNamingConvention);

                            switch (_output.BatchExportNamingConvention)
                            {
                                case BatchTextureNamingConvention.NameWithIndex:
                                    _output.ExportName = EditorGUILayout.TextField(NAME, _output.ExportName);
                                    _output.NumberSeparator = EditorGUILayout.TextField(SEPARATOR, _output.NumberSeparator);
                                    _output.NumberOfDigits = EditorGUILayout.IntField(DIGITS, _output.NumberOfDigits);
                                    break;

                                case BatchTextureNamingConvention.AddPrefixToInput:
                                    _output.ExportPrefix = EditorGUILayout.TextField(PREFIX, _output.ExportPrefix);
                                    break;

                                case BatchTextureNamingConvention.AddSuffixToInput:
                                    _output.ExportSuffix = EditorGUILayout.TextField(SUFFIX, _output.ExportSuffix);
                                    break;

                                case BatchTextureNamingConvention.CombineInputs:
                                    _output.Connector = EditorGUILayout.TextField(CONNECTOR, _output.Connector);
                                    break;
                            }

                            _output.ImportMode = (TextureImportMode) EditorGUILayout.EnumPopup(IMPORT_MODE, _output.ImportMode);

                            if (_output.ImportMode is TextureImportMode.Custom)
                            {
                                _output.CustomPreset = EditorGUILayout.ObjectField(PRESET, _output.CustomPreset, typeof(Preset), false) as Preset;
                            }
                            else
                            {
                                using (new HorizontalScope())
                                {
                                    DrawDefaultLabel(PRESET);

                                    using (new DisabledScope())
                                    {
                                        EditorGUILayout.ObjectField(Preferences.GetDefaultPreset(_output.ImportMode), typeof(Preset), false);
                                    }
                                }
                            }

                            SmallSpace();
                            _output.ExportPath = DrawFolderPicker(EXPORT_PATH, _output.ExportPath, SELECT_A_FOLDER);

                            using (new HorizontalScope())
                            {
                                DrawDefaultLabel(OUTPUT_NAME);

                                using (new DisabledScope())
                                {
                                    EditorGUILayout.TextField(_output.GeneratedName);
                                }
                            }

                            if (_changeScope.changed)
                            {
                                _output.UpdateGeneratedName(batchInputs);
                            }
                        }
                    }

                    SmallSpace();
                }

                SmallSpace();
            }
        }

        private void drawBatchConvertTabToolbar()
        {
            using (new HorizontalScope())
            {
                NormalSpace();

                using (new VerticalScope())
                {
                    drawPresets(updateBatchPreset);
                    drawBaseSettings();
                    drawBatchConvertHelpers();
                }

                NormalSpace();
            }
        }

        private void drawBatchConvertHelpers()
        {
            if (batchPreset == null)
            {
                return;
            }

            if (_areOutputsValid() == false)
            {
                NormalSpace();
                EditorGUILayout.HelpBox(AT_LEAST_ONE_OUTPUT, MessageType.Warning);
                return;
            }

            NormalSpace();
            DrawCenteredBoldLabel(HELPERS, BOLD_LABEL_WIDTH);
            SmallSpace();

            if (DrawBoxButton(CLEAR_TEXTURES_LIST, FixedHeight(SINGLE_LINE_HEIGHT)))
            {
                foreach (var _input in batchInputs)
                {
                    _input.ClearFoundTextures();
                }
            }

            VerySmallSpace();

            if (DrawBoxButton(REMOVE_NULL_REFERENCES, FixedHeight(SINGLE_LINE_HEIGHT)))
            {
                foreach (var _input in batchInputs)
                {
                    _input.RemoveNullReferences();
                }
            }

            if (areBatchInputsValid() == false)
            {
                NormalSpace();
                EditorGUILayout.HelpBox(AT_LEAST_ONE_INPUT, MessageType.Warning);
                return;
            }

            VerySmallSpace();

            using (ColorScope.Background(BLUE))
            {
                if (DrawBoxButton(OPEN_EXPORT_WINDOW, FixedHeight(SINGLE_LINE_HEIGHT)))
                {
                    ExportWindow.OpenWindow(batchPreset);
                    int _numberOfFoundTextures = batchInputs[0].NumberOfFoundTextures; //It's equal or button would be hidden

                    for (int i = 0; i < _numberOfFoundTextures; i++)
                    {
                        foreach (var _output in batchOutputs)
                        {
                            var _texturesWithChannels = getTexturesWithChannelsForTheTexture(_output, i);
                            ExportWindow.AddTextureToExport(_output, Preferences.ExportFormat, _texturesWithChannels, i);
                        }
                    }
                }
            }

            bool _areOutputsValid()
            {
                if (batchOutputs.Count < 1)
                {
                    return false;
                }

                foreach (var _output in batchOutputs)
                {
                    if (_output.IsOutputValid() == false)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private bool areBatchInputsValid()
        {
            if (batchInputs.Count < 1)
            {
                return false;
            }

            int _baseTexturesCount = batchInputs[0].NumberOfFoundTextures;

            foreach (var _input in batchInputs)
            {
                if (_input.NumberOfFoundTextures == 0 || _input.NumberOfFoundTextures != _baseTexturesCount)
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Presets

        private void drawPresetsTab()
        {
            using (new HorizontalScope())
            {
                if (Preferences.ToolbarSide is Side.Left)
                {
                    using (new VerticalScope(FixedWidth(TOOLBAR_WIDTH)))
                    {
                        drawArrangeTabToolbar();
                    }

                    DrawVerticalLine(3f);

                    using (new VerticalScope())
                    {
                        drawPresetsList();
                    }
                }
                else
                {
                    using (new VerticalScope())
                    {
                        drawPresetsList();
                    }

                    DrawVerticalLine(3f);

                    using (new VerticalScope(FixedWidth(TOOLBAR_WIDTH)))
                    {
                        drawPresetsTabToolbar();
                    }
                }
            }
        }

        private void drawPresetsList()
        {
            NormalSpace();

            if (presetsList == null || presetsList.IsValid == false)
            {
                refreshPresets();
            }

            using (var _scrollScope = new ScrollViewScope(presetsTabScrollPosition))
            {
                using (new HorizontalScope())
                {
                    LargeSpace();
                    EditorGUILayout.HelpBox(CHOOSE_VISIBLE_PRESETS_INFO, MessageType.Info);
                    LargeSpace();
                }

                NormalSpace();

                using (new HorizontalScope())
                {
                    LargeSpace();
                    presetsList.Draw();
                    LargeSpace();
                }

                presetsTabScrollPosition = _scrollScope.scrollPosition;
            }
        }

        private void drawPresetsTabToolbar()
        {
            using (new HorizontalScope())
            {
                NormalSpace();

                using (new VerticalScope())
                {
                    drawPresets(applyPreset);
                    drawPresetsTabHelpers();
                }

                NormalSpace();
            }
        }

        private void drawPresetsTabHelpers()
        {
            NormalSpace();
            DrawCenteredBoldLabel(HELPERS, BOLD_LABEL_WIDTH);
            SmallSpace();

            if (DrawBoxButton(SET_ALL_PRESETS_AS_VISIBLE, FixedHeight(SINGLE_LINE_HEIGHT)))
            {
                Preferences.SetAllPresetsVisible(true);
            }

            VerySmallSpace();

            if (DrawBoxButton(REMOVE_ALL_PRESETS_FROM_VISIBLE, FixedHeight(SINGLE_LINE_HEIGHT)))
            {
                Preferences.SetAllPresetsVisible(false);
            }

            VerySmallSpace();

            if (DrawBoxButton(RESET_PRESETS_ORDER_TO_DEFAULT, FixedHeight(SINGLE_LINE_HEIGHT)))
            {
                Preferences.ResetPresetsToDefault(false);
                refreshPresets();
            }
        }

        private void drawPresets(UnityAction<ChannelArrangerPreset> _actionToInvoke)
        {
            DrawCenteredBoldLabel(PRESETS, BOLD_LABEL_WIDTH);
            SmallSpace();

            ChangeCheckScope _changeScope = null;

            using (new ScopeGroup(new LabelWidthScope(60f), _changeScope = new ChangeCheckScope()))
            {
                presetSearchFilter = EditorGUILayout.TextField(SEARCH, presetSearchFilter);

                if (_changeScope.changed)
                {
                    recalculateVisiblePresets();
                }
            }

            NormalSpace();

            if (foundPresets.IsNullOrEmpty())
            {
                EditorGUILayout.HelpBox(NO_PRESETS_FOUND, MessageType.Info);
                drawCreatePreset();
                drawBaseSettings();
                return;
            }

            int _index = 1;
            int _visiblePresets = visiblePresets.Count;

            if (_visiblePresets == 0)
            {
                EditorGUILayout.HelpBox(SELECT_AT_LEAST_ONE_PRESET_INFO, MessageType.Info);
                return;
            }

            float _scrollViewHeight = Mathf.Min(_visiblePresets, MAX_VISIBLE_PRESETS) * SINGLE_LINE_HEIGHT;

            using (var _scrollView = new ScrollViewScope(presetsScrollPosition, FixedHeight(_scrollViewHeight)))
            {
                foreach (var _preset in visiblePresets)
                {
                    if (_preset == null)
                    {
                        refreshPresets();
                        return;
                    }

                    if (presetSearchFilter.IsNullEmptyOrWhitespace() == false)
                    {
                        if (_preset.DisplayName.ToLower().Contains(presetSearchFilter.ToLower()) == false)
                        {
                            continue;
                        }
                    }

                    if (_preset.DisplayName.ToLower().Contains(presetSearchFilter.ToLower()))
                    {
                        using (new HorizontalScope())
                        {
                            GUILayout.Label($"{_index}", SingleLineLabelStyle, FixedWidth(INDEX_WIDTH));

                            if (DrawBoxButton(new GUIContent(_preset.DisplayName, _preset.Description), FixedHeight(SINGLE_LINE_HEIGHT)))
                            {
                                _actionToInvoke(_preset);
                            }

                            if (GUILayout.Button(string.Empty, Styles.FixedSettings(SINGLE_LINE_HEIGHT), FixedWidthAndHeight(SINGLE_LINE_HEIGHT)))
                            {
                                ChannelArrangerGenericMenu.ShowForPreset(this, Event.current, _preset);
                            }

                            VerySmallSpace();
                        }
                    }

                    _index++;
                }

                presetsScrollPosition = _scrollView.scrollPosition;
            }
        }

        private void refreshPresets()
        {
            foundPresets = AssetsUtilities.GetAssetsOfType<ChannelArrangerPreset>().ToList();
            Preferences.UpdateSavedPresets(foundPresets);

            foundPresets.Clear();

            foreach (var _preset in Preferences.PresetsWithSetup)
            {
                foundPresets.Add(_preset.Preset);
            }

            refreshPresetsList();
            recalculateVisiblePresets();
        }

        private void refreshPresetsList()
        {
            if (presetsList != null)
            {
                DestroyImmediate(presetsList);
            }

            presetsList = ScriptableObject.CreateInstance<PresetSetupReorderableList>();
            presetsList.Init(this, Preferences.PresetsWithSetup, ALL_PRESETS, false, false);
            presetsList.OnReorder += updatePresetsOrderList;

            Preferences.SavePreferences();
        }

        private void recalculateVisiblePresets()
        {
            visiblePresets.Clear();

            if (presetSearchFilter.IsNullEmptyOrWhitespace())
            {
                foreach (var _preset in foundPresets)
                {
                    if (Preferences.IsPresetVisible(_preset))
                    {
                        visiblePresets.Add(_preset);
                    }
                }

                Preferences.SavePreferences();
                return;
            }

            visiblePresets = foundPresets.Where(p => p.DisplayName.ToLower().Contains(presetSearchFilter.ToLower())).ToList();
            Preferences.SavePreferences();
        }

        private void updatePresetsOrderList()
        {
            for (int i = 0; i < presetsList.ObjectsList.Count; i++)
            {
                presetsList.ObjectsList[i].SetOrderInTheList(i);
            }

            Preferences.SortPresetsByOrder();
            foundPresets.Clear();

            foreach (var _preset in Preferences.PresetsWithSetup)
            {
                foundPresets.Add(_preset.Preset);
            }

            recalculateVisiblePresets();
        }

        private void applyPreset(ChannelArrangerPreset _preset)
        {
            if (_preset == null)
            {
                return;
            }

            inputs = new List<TextureInput>();
            outputs = new List<TextureOutput>();

            for (int i = 0; i < _preset.NumberOfInputs; i++)
            {
                inputs.Add(_preset.GetInputCopyAtIndex(i));
            }

            foreach (var _input in inputs)
            {
                _input.RecalculateNumberOfChannels();
            }

            for (int i = 0; i < _preset.NumberOfOutputs; i++)
            {
                outputs.Add(_preset.GetOutputCopyAtIndex(i));
            }

            recreateInputStrings();
            regenerateOutputTextures();
        }

        #endregion

        #region Settings

        private void drawSettingsTab()
        {
            ChangeCheckScope _changeScope = null;

            using (new ScopeGroup(new LabelWidthScope(250f), _changeScope = new ChangeCheckScope()))
            {
                SmallSpace();
                DrawBoldLabel(SETTINGS);
                Preferences.ToolbarSide = (Side) EditorGUILayout.EnumPopup(TOOLBAR_SIDE, Preferences.ToolbarSide);
                Preferences.ExportFormat = (TextureExportFormat) EditorGUILayout.EnumPopup(EXPORT_FORMAT, Preferences.ExportFormat);
                Preferences.ChannelPreview = (ChannelPreviewMode) EditorGUILayout.EnumPopup(CHANNEL_PREVIEW, Preferences.ChannelPreview);

                using (new HorizontalScope())
                {
                    Preferences.DefaultExportPath = EditorGUILayout.TextField(DEFAULT_EXPORT_PATH, Preferences.DefaultExportPath);

                    if (GUILayout.Button(DOTS, FixedWidth(21f)))
                    {
                        string _newPath = EditorUtility.OpenFolderPanel(DEFAULT_EXPORT_PATH, string.Empty, string.Empty);

                        if (_newPath.IsNullOrEmpty() == false)
                        {
                            Preferences.DefaultExportPath = _newPath;
                        }
                    }
                }

                NormalSpace();
                DrawBoldLabel(LOGS);
                Preferences.PrintLogs = EditorGUILayout.Toggle(PRINT_LOGS, Preferences.PrintLogs);

                NormalSpace();
                DrawBoldLabel(PRESETS);
                Preferences.PresetName = EditorGUILayout.TextField(DEFAULT_PRESET_NAME, Preferences.PresetName);
                Preferences.PresetPath = DrawFolderPicker(DEFAULT_PRESET_PATH, Preferences.PresetPath, SELECT_A_FOLDER);

                SmallSpace();
                Preferences.ApplyAtStartPreset = EditorGUILayout.ObjectField(APPLY_AT_START, Preferences.ApplyAtStartPreset, typeof(ChannelArrangerPreset), false) as ChannelArrangerPreset;
                Preferences.SRGBPreset = EditorGUILayout.ObjectField(SRGB, Preferences.SRGBPreset, typeof(Preset), false) as Preset;
                Preferences.LinearPreset = EditorGUILayout.ObjectField(LINEAR, Preferences.LinearPreset, typeof(Preset), false) as Preset;
                Preferences.NormalMapPreset = EditorGUILayout.ObjectField(NORMAL_MAP, Preferences.NormalMapPreset, typeof(Preset), false) as Preset;
                Preferences.SingleChannelPreset = EditorGUILayout.ObjectField(SINGLE_CHANNEL, Preferences.SingleChannelPreset, typeof(Preset), false) as Preset;

                if (_changeScope.changed)
                {
                    Preferences.SavePreferences();
                }
            }

            SmallSpace();

            using (new HorizontalScope())
            {
                FlexibleSpace();

                if (DrawBoxButton(OPEN_PREFERENCES, FixedWidthAndHeight(windowWidthScaled(0.6f), DEFAULT_LINE_HEIGHT)))
                {
                    showPreferences();
                }

                FlexibleSpace();
            }
        }

        private void showPreferences()
        {
            EditorExtensions.ShowPreferencesSection(PREFERENCES_SECTION);
        }

        private void drawBaseSettings(bool _drawChannelPreview = false)
        {
            NormalSpace();
            DrawCenteredBoldLabel(SETTINGS, BOLD_LABEL_WIDTH);
            SmallSpace();

            var _changeScope = new ChangeCheckScope();

            using (new ScopeGroup(new LabelWidthScope(TOOLBAR_LABEL_WIDTH), _changeScope))
            {
                Preferences.ToolbarSide = (Side) EditorGUILayout.EnumPopup(TOOLBAR_SIDE, Preferences.ToolbarSide);
                Preferences.ExportFormat = (TextureExportFormat) EditorGUILayout.EnumPopup(EXPORT_FORMAT, Preferences.ExportFormat);

                if (_drawChannelPreview)
                {
                    Preferences.ChannelPreview = (ChannelPreviewMode) EditorGUILayout.EnumPopup(CHANNEL_PREVIEW, Preferences.ChannelPreview);
                }

                Preferences.DefaultExportPath = DrawFolderPicker(EXPORT_PATH, Preferences.DefaultExportPath, SAVE_WINDOW_TITLE_TEXT);
                Preferences.ShowTexturesResolution = EditorGUILayout.Toggle(SHOW_TEXTURES_RESOLUTION, Preferences.ShowTexturesResolution);

                if (_changeScope.changed)
                {
                    Preferences.SavePreferences();
                }
            }

            if (showDebugOptions)
            {
                NormalSpace();
                DrawCenteredBoldLabel(DEBUG, BOLD_LABEL_WIDTH);
                SmallSpace();

                using (new LabelWidthScope(SHORT_LABEL_WIDTH))
                {
                    EditorGUILayout.ObjectField(BLIT_MATERIAL, TextureChannelArranger.BlitMaterial, typeof(Material), false);
                }
            }
        }

        #endregion

        #region Edit Mode

        public void EnableEditMode(ChannelArrangerPreset _preset)
        {
            applyPreset(_preset);

            isInEditMode = true;
            editedPreset = _preset;
            editedPresetSerializedObjects = new SerializedObject(_preset);

            windowMode = WindowMode.Arrange;
        }

        private void disableEditMode()
        {
            isInEditMode = false;
            editedPreset = null;
            editedPresetSerializedObjects = null;
        }

        private void drawPresetEditMode()
        {
            SmallSpace();

            using (new HorizontalScope())
            {
                LargeSpace();
                drawEditModeInputs();
                Space(CONNECTIONS_WIDTH);
                drawEditModeOutputs();
                LargeSpace();
            }

            editedPresetSerializedObjects.ApplyModifiedProperties();
            drawArrangeTabConnections();
        }

        private void drawEditModeToolbar()
        {
            SmallSpace();
            DrawCenteredBoldLabel(EDIT_MODE, BOLD_LABEL_WIDTH);
            SmallSpace();

            using (new ScopeGroup(new LabelWidthScope(60f), new HorizontalScope()))
            {
                DrawDefaultLabel(PRESET);

                using (new DisabledScope())
                {
                    EditorGUILayout.ObjectField(editedPreset, typeof(ChannelArrangerPreset), false);
                }
            }

            NormalSpace();
            DrawCenteredBoldLabel(HELPERS, BOLD_LABEL_WIDTH);
            SmallSpace();

            if (DrawBoxButton(ADD_INPUT, FixedHeight(SINGLE_LINE_HEIGHT)))
            {

            }

            VerySmallSpace();

            if (DrawBoxButton(ADD_OUTPUT, FixedHeight(SINGLE_LINE_HEIGHT)))
            {

            }

            VerySmallSpace();

            using (new ScopeGroup(ColorScope.Background(TextureChannelArranger.LOGS_COLOR), new HorizontalScope()))
            {
                if (DrawBoxButton(DISABLE_EDIT_MODE, FixedHeight(SINGLE_LINE_HEIGHT)))
                {
                    disableEditMode();
                }
            }
        }

        private void drawEditModeInputs()
        {
            using (new VerticalScope(FixedWidth(INPUT_WIDTH)))
            {
                FlexibleSpace();
                SerializedProperty _inputs = editedPresetSerializedObjects.FindProperty("inputs");

                for (int i = 0; i < _inputs.arraySize; i++)
                {
                    drawEditModeInput(_inputs.GetArrayElementAtIndex(i), inputs[i]);

                    if (i < _inputs.arraySize - 1)
                    {
                        NormalSpace();
                    }
                }

                FlexibleSpace();
            }
        }

        private void drawEditModeInput(SerializedProperty _serializedInput, TextureInput _input)
        {
            using (new HorizontalScope(EditorStyles.helpBox, FixedWidth(300f)))
            {
                SmallSpace();

                using (new ScopeGroup(new LabelWidthScope(100f), new VerticalScope()))
                {
                    DrawBoldLabel("Arrange");
                    EditorGUILayout.PropertyField(_serializedInput.FindPropertyRelative("textureName"));
                    EditorGUILayout.PropertyField(_serializedInput.FindPropertyRelative("description"));
                    EditorGUILayout.PropertyField(_serializedInput.FindPropertyRelative("reference"));

                    SmallSpace();
                    DrawBoldLabel("Batch");
                    SerializedProperty _addModeProperty = _serializedInput.FindPropertyRelative("addMode");
                    EditorGUILayout.PropertyField(_addModeProperty);

                    TextureAddMode _addMode = (TextureAddMode) _addModeProperty.enumValueIndex;

                    if (_addMode is TextureAddMode.Search)
                    {
                        EditorGUILayout.PropertyField(_serializedInput.FindPropertyRelative("folderPath"));
                        EditorGUILayout.PropertyField(_serializedInput.FindPropertyRelative("nameFilter"));
                        EditorGUILayout.PropertyField(_serializedInput.FindPropertyRelative("prefix"));
                        EditorGUILayout.PropertyField(_serializedInput.FindPropertyRelative("suffix"));
                    }
                }

                if (_input.Reference != null)
                {
                    NormalSpace();

                    using (new DisabledScope())
                    {
                        drawTextureChannels(_input);
                    }
                }
            }
        }

        private void drawEditModeOutputs()
        {
            using (new VerticalScope())
            {
                FlexibleSpace();
                SerializedProperty _outputs = editedPresetSerializedObjects.FindProperty("outputs");

                for (int i = 0; i < _outputs.arraySize; i++)
                {
                    drawEditModeOutput(_outputs.GetArrayElementAtIndex(i), editedPreset.GetOutputAtIndex(i));

                    if (i < outputs.Count - 1)
                    {
                        NormalSpace();
                    }
                }

                FlexibleSpace();
            }
        }

        private void drawEditModeOutput(SerializedProperty _serializedOutput, TextureOutput _output)
        {
            using (new HorizontalScope(EditorStyles.helpBox))
            {
                SmallSpace();

                using (new DisabledScope())
                {
                    drawTextureChannels(_output);
                }

                var _changeCheckScope = new ChangeCheckScope();

                using (new ScopeGroup(_changeCheckScope, new VerticalScope(FixedWidth(COLOR_INDICATOR_WIDTH + INPUT_WIDTH + CHANNEL_WIDTH + SINGLE_LINE_HEIGHT))))
                {
                    FlexibleSpace();

                    for (int i = 0; i < TextureChannelArranger.NUMBER_OF_CHANNELS; i++)
                    {
                        drawChannelSetup(inputsStrings, _output, i, TextureChannelArranger.CHANNELS[i], TextureChannelArranger.GetColorFromChannel((TextureChannel) i));
                    }

                    if (_changeCheckScope.changed)
                    {
                        editedPreset.SetAsDirty();
                    }

                    FlexibleSpace();
                }

                SmallSpace();

                using (new VerticalScope())
                {
                    FlexibleSpace();

                    using (new LabelWidthScope(EDIT_MODE_OUTPUT_LABEL_WIDTH))
                    {
                        SerializedProperty _exportModeProperty = _serializedOutput.FindPropertyRelative("exportMode");
                        EditorGUILayout.PropertyField(_exportModeProperty);
                        TextureExportMode _exportMode = (TextureExportMode) _exportModeProperty.enumValueIndex;

                        switch (_exportMode)
                        {
                            case TextureExportMode.CreateNew:

                                SerializedProperty _exportNamingConventionProperty = _serializedOutput.FindPropertyRelative("exportNamingConvention");
                                EditorGUILayout.PropertyField(_exportNamingConventionProperty);
                                TextureNamingConvention _namingConvention = (TextureNamingConvention) _exportNamingConventionProperty.enumValueIndex;

                                switch (_namingConvention)
                                {
                                    case TextureNamingConvention.AddPrefix:
                                        EditorGUILayout.PropertyField(_serializedOutput.FindPropertyRelative("exportPrefix"));
                                        break;

                                    case TextureNamingConvention.AddSuffix:
                                        EditorGUILayout.PropertyField(_serializedOutput.FindPropertyRelative("exportSuffix"));
                                        break;

                                    case TextureNamingConvention.Custom:
                                        EditorGUILayout.PropertyField(_serializedOutput.FindPropertyRelative("fileName"));
                                        break;
                                }

                                break;

                            case TextureExportMode.Overwrite:
                                EditorGUILayout.PropertyField(_serializedOutput.FindPropertyRelative("fileToOverwrite"));
                                break;
                        }

                        using (var _changeScope = new ChangeCheckScope())
                        {
                            _output.ImportMode = (TextureImportMode) EditorGUILayout.EnumPopup(IMPORT_MODE, _output.ImportMode);

                            if (_changeScope.changed)
                            {
                                regenerateOutput(_output);
                            }
                        }

                        if (_output.ImportMode is TextureImportMode.Custom)
                        {
                            _output.CustomPreset = EditorGUILayout.ObjectField(PRESET, _output.CustomPreset, typeof(Preset), false) as Preset;
                        }
                        else
                        {
                            using (new HorizontalScope())
                            {
                                DrawDefaultLabel(PRESET);

                                using (new DisabledScope())
                                {
                                    EditorGUILayout.ObjectField(Preferences.GetDefaultPreset(_output.ImportMode), typeof(Preset), false);
                                }
                            }
                        }
                    }

                    FlexibleSpace();
                }

                SmallSpace();
            }
        }

        #endregion

        #region Helpers

        private static void drawTexturePreview(TextureReference _reference, Texture2D _texture)
        {
            if (_texture == null)
            {
                return;
            }

            using (new HorizontalScope(FixedHeight(TEXTURE_PREVIEW_WIDTH)))
            {
                SmallSpace();
                EditorGUILayout.LabelField(string.Empty, FixedWidthAndHeight(TEXTURE_PREVIEW_WIDTH));

                if (_reference.PreviewMask is ColorWriteMask.Alpha)
                {
                    EditorGUI.DrawTextureAlpha(GetLastRect(), _texture, ScaleMode.ScaleToFit);
                }
                else
                {
                    TextureChannelArranger.PreviewMaterial.SetFloat(IS_SRGB_PROPERTY, _reference.IsSRGB ? 1f : 0f);
                    TextureChannelArranger.PreviewMaterial.SetFloat(IS_NORMAL_PROPERTY, _reference.IsNormalMap ? 1f : 0f);
                    TextureChannelArranger.PreviewMaterial.SetFloat(PREVIEW_SINGLE_CHANNEL_PROPERTY, _reference.PreviewMask != ColorWriteMask.All ? 1f : 0f);
                    TextureChannelArranger.PreviewMaterial.SetFloat(PREVIEW_MODE_PROPERTY, Preferences.ChannelPreview is ChannelPreviewMode.Default ? 0f : 1f);

                    Rect _lastRect = GetLastRect();

                    if (_reference is TextureInput && _reference.IsNormalMap)
                    {
                        EditorGUI.DrawPreviewTexture(_lastRect, _texture, null, ScaleMode.ScaleToFit, 0f, 0, _reference.PreviewMask);
                    }
                    else
                    {
                        EditorGUI.DrawPreviewTexture(_lastRect, _texture, TextureChannelArranger.PreviewMaterial, ScaleMode.ScaleToFit, 0f, 0, _reference.PreviewMask);
                    }

                    if (Preferences.ShowTexturesResolution)
                    {
                        string _label = $"{_reference.Reference.width}\nx\n{_reference.Reference.height}";
                        EditorGUI.LabelField(_lastRect, _label, Styles.GetCenteredBoldLabelStyle().WithColor(Color.white));
                    }
                }

                SmallSpace();
            }
        }

        private void exportTexture(TextureOutput _output)
        {
            var _time = System.DateTime.Now;
            var _texturesWithChannels = getTexturesWithChannelsForTheTexture(_output);
            var _maxWidthAndHeight = TextureChannelArranger.GetMaxWidthAndHeight(_texturesWithChannels);

            Preset _presetToApply = _output.ImportMode is TextureImportMode.Custom ? _output.CustomPreset : Preferences.GetDefaultPreset(_output.ImportMode);
            Texture2D _generatedTexture = TextureChannelArranger.GenerateOutputTexture(_output, _maxWidthAndHeight.Item1, _maxWidthAndHeight.Item2, _texturesWithChannels);

            if (_output.ExportMode is TextureExportMode.CreateNew)
            {
                TextureChannelArranger.SaveTheTexture(Preferences.DefaultExportPath, _generatedTexture, _output.GeneratedName, _generatedTexture.graphicsFormat, Preferences.ExportFormat, _presetToApply, true);
            }
            else
            {
                bool _overwrite = EditorUtility.DisplayDialog(OVERWRITE_THE_TEXTURE, $"Are you sure you would like to overwrite the {_output.FileToOverwrite.name}?", YES, NO);

                if (_overwrite == false)
                {
                    return;
                }

                TextureExportFormat _format = Preferences.ExportFormat;
                string _extension = _output.FileToOverwrite.GetFileExtension();

                if (_isExtensionSupported(_extension) == false)
                {
                    bool _whatToDo = EditorUtility.DisplayDialog(UNSUPPORTED_FILE_FORMAT, $"The format of the texture you are trying to overwrite is not supported: [{_extension}]. File can't be overwritten.", EXPORT_ANYWAY, CANCEL);

                    if (_whatToDo == false)
                    {
                        return;
                    }
                }
                else
                {
                    TextureExportFormat _targetFormat = TextureChannelArranger.GetFormatFromString(_extension);

                    if (_targetFormat != _format)
                    {
                        bool _whatToDo = EditorUtility.DisplayDialog(DIFFERENT, DIFFERENT_FORMAT_INFO, $"Change format to {_targetFormat} and export", CANCEL);

                        if (_whatToDo == false)
                        {
                            return;
                        }

                        _format = _targetFormat;
                    }
                }

                TextureChannelArranger.SaveTheTexture(_output.FileToOverwrite.GetFolderPath(), _generatedTexture, _output.FileToOverwrite.name, _generatedTexture.graphicsFormat, _format, _presetToApply, true);
            }

            TextureChannelArranger.Log($"Generated a new texture in {(System.DateTime.Now - _time).TotalSeconds} seconds.");

            bool _isExtensionSupported(string _extension)
            {
                return TextureChannelArranger.SUPPORTED_FILE_FORMATS.Contains(_extension);
            }
        }

        private List<TextureWithSetup> getTexturesWithChannelsForTheTexture(TextureOutput _output)
        {
            var _texturesWithChannels = new List<TextureWithSetup>();

            foreach (var _setup in _output.ChannelSetups)
            {
                if (_setup.Source is ChannelSource.None || _output.ShouldChannelBeDisabled(_setup))
                {
                    continue;
                }

                if (_setup.Source is ChannelSource.SolidColor)
                {
                    _texturesWithChannels.Add(new TextureWithSetup(null, _setup));
                    continue;
                }

                int _inputTextureIndex = inputsStrings.IndexOf(_setup.TextureSetup);

                if (_inputTextureIndex == -1)
                {
                    continue;
                }

                _texturesWithChannels.Add(new TextureWithSetup(inputs[_inputTextureIndex].Reference, _setup));
            }

            return _texturesWithChannels;
        }

        private List<TextureWithSetup> getTexturesWithChannelsForTheTexture(TextureOutput _output, int _foundTextureIndex)
        {
            var _texturesWithChannels = new List<TextureWithSetup>();

            foreach (var _setup in _output.ChannelSetups)
            {
                if (_setup.Source is ChannelSource.None)
                {
                    continue;
                }

                if (_setup.Source is ChannelSource.SolidColor)
                {
                    _texturesWithChannels.Add(new TextureWithSetup(null, _setup));
                    continue;
                }

                int _inputTextureIndex = _getTextureIndex(_setup.TextureSetup);
                _texturesWithChannels.Add(new TextureWithSetup(batchInputs[_inputTextureIndex].GetFoundTextureAtIndex(_foundTextureIndex), _setup));
            }

            return _texturesWithChannels;

            int _getTextureIndex(string _name)
            {
                for (int i = 0; i < batchInputs.Count; i++)
                {
                    if (batchInputs[i].TextureName == _name)
                    {
                        return i;
                    }
                }

                return -1;
            }
        }

        private void onAssetsImported()
        {
            foreach (var _input in inputs)
            {
                _input.RecalculateNumberOfChannels();
            }
        }

        private Vector2 getStartPosition(InputConnection _connection)
        {
            Rect _rect = inputs[_connection.InputTextureIndex].GetChannelRect((int) _connection.InputChannel);
            return new Vector2(_rect.x + _rect.width, _rect.y + _rect.height / 2f);
        }

        private Vector2 getEndPosition(InputConnection _connection)
        {
            Rect _rect = default;

            if (isInEditMode)
            {
                _rect = editedPreset.Outputs[_connection.OutputTextureIndex].GetChannelRect((int) _connection.OutputChannel);
            }
            else
            {
                _rect = outputs[_connection.OutputTextureIndex].GetChannelRect((int) _connection.OutputChannel);
            }

            return new Vector2(_rect.x, _rect.y + _rect.height / 2f);
        }

        private void drawHorizontalConnection(Vector2 _start, Vector2 _end, Color _color)
        {
            Vector2 _startWithOffset = _start + new Vector2(CONNECTION_LINE_OFFSET, 0f);
            Vector2 _endWithOffset = _end + new Vector2(-CONNECTION_LINE_OFFSET, 0f);

            DrawHandlesLine(_start, _startWithOffset, _color);
            DrawHandlesLine(_startWithOffset, _endWithOffset, _color);
            DrawHandlesLine(_endWithOffset, _end, _color);
        }

        private void drawVerticalConnection(Rect _outputRect, Rect _inputRect, Color _color)
        {
            Vector2 _start = new Vector2(_outputRect.x + _outputRect.width / 2f, _outputRect.y + _outputRect.height);
            Vector2 _end = new Vector2(_inputRect.x + _inputRect.width / 2f, _inputRect.y);

            Vector2 _startWithOffset = _start + new Vector2(0f, CONNECTION_LINE_OFFSET);
            Vector2 _endWithOffset = _end + new Vector2(0f, -CONNECTION_LINE_OFFSET);

            DrawHandlesLine(_start, _startWithOffset, _color);
            DrawHandlesLine(_startWithOffset, _endWithOffset, _color);
            DrawHandlesLine(_endWithOffset, _end, _color);
        }

        private void drawCombinedConnection(TextureInput _input, TextureOutput _output, TextureChannel _outputChannel)
        {
            if (_input.Reference == null)
            {
                return;
            }

            Rect _middleRect = _input.GetChannelRect(1);
            Vector2 _middlePosition = new Vector2(_middleRect.x + _middleRect.width, _middleRect.y + _middleRect.height / 2f);
            _middlePosition += new Vector2(30f, 0f);

            for (int i = 0; i < 3; i++)
            {
                Rect _rect = _input.GetChannelRect(i);
                Vector2 _start = new Vector2(_rect.x + _rect.width, _rect.y + _rect.height / 2f);
                Vector2 _startWithOffset = _start + new Vector2(CONNECTION_LINE_OFFSET, 0f);

                Color _colorFromChannel = TextureChannelArranger.GetColorFromChannel((TextureChannel) i);

                DrawHandlesLine(_start, _startWithOffset, _colorFromChannel);
                DrawHandlesLine(_startWithOffset, _middlePosition, _colorFromChannel);
            }

            Rect _outputRect = _output.GetChannelRect((int) _outputChannel);
            Vector2 _end = new Vector2(_outputRect.x, _outputRect.y + _outputRect.height / 2f);
            Vector2 _endWithOffset = _end + new Vector2(-CONNECTION_LINE_OFFSET, 0f);

            DrawHandlesLine(_middlePosition, _endWithOffset, Color.gray);
            DrawHandlesLine(_endWithOffset, _end, Color.gray);
        }

        private static ChannelSource getSourceFromSetup(string _textureSetup)
        {
            if (_textureSetup == NONE)
            {
                return ChannelSource.None;
            }
            else if (_textureSetup == SOLID_COLOR)
            {
                return ChannelSource.SolidColor;
            }
            else
            {
                return ChannelSource.Texture;
            }
        }

        #endregion

        [MenuItem("Window/FewClicks Dev/Texture Channel Arranger", priority = 105)]
        public static void OpenWindow()
        {
            Preferences.LoadPreferences();

            var _window = GetWindow<TextureChannelArrangerWindow>();
            _window.Show();
            _window.OnEnable();

            _window.applyPreset(Preferences.ApplyAtStartPreset);
        }

        public static void OpenWindow(ChannelArrangerPreset _preset)
        {
            var _window = GetWindow<TextureChannelArrangerWindow>();
            _window.Show();
            _window.OnEnable();

            _window.windowMode = WindowMode.Arrange;
            _window.applyPreset(_preset);
        }
    }
}