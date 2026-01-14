namespace FewClicksDev.TextureChannelArranger
{
    using FewClicksDev.Core;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.Presets;
    using UnityEngine;

    using static FewClicksDev.Core.EditorDrawer;
    using static FewClicksDev.TextureChannelArranger.TextureChannelArrangerWindow;

    public static class ChannelArrangerUserPreferences
    {
        private const string PREFS_PATH = "FewClicks Dev/Texture Channel Arranger";
        private const string LABEL = "Texture Channel Arranger";
        private const SettingsScope SETTINGS_SCOPE = SettingsScope.User;

        private static readonly string PREFS_PREFIX = $"{PlayerSettings.productName}.FewClicksDev.{LABEL}.";
        private static readonly string[] KEYWORDS = new string[] { "FewClicks Dev", LABEL, "Texture", "Channel", "Arranger" };

        private const float LABEL_WIDTH = 250f;

        private const string TOOLBAR_SIDE = "Toolbar side";
        private const string EXPORT_FORMAT = "Export format";
        private const string EXPORT_PATH = "Default export path";
        private const string PRINT_LOGS_LABEL = "Print logs";
        private const string PRESET_NAME = "Default preset name";
        private const string PRESET_PATH = "Default preset path";
        private const string SHOW_TEXTURES_RESOLUTION = "Show textures resolution";
        private const string PRESETS = "Presets";
        private const string RESET_TO_DEFAULTS = "Reset to defaults";
        private const string SELECT_FOLDER = "Select a folder";
        private const string APPLY_AT_START = "Apply at start";
        private const string SRGB = "sRGB";
        private const string LINEAR = "Linear";
        private const string NORMAL_MAP = "Normal map";
        private const string SINGLE_CHANNEL = "Single channel";
        private const string SEMICOLON = ";";
        private const string CHANNEL_PREVIEW = "Channel preview";

        private const Side DEFAULT_SIDE = Side.Right;
        private const TextureExportFormat DEFAULT_EXPORT_FORMAT = TextureExportFormat.PNG;
        private const ChannelPreviewMode DEFAULT_CHANNEL_PREVIEW_MODE = ChannelPreviewMode.Default;

        private const string DEFAULT_EXPORT_PATH = "Assets/";
        private const bool DEFAULT_PRINT_LOGS = true;
        private const string DEFAULT_PRESET_NAME = "NewPreset";
        private const string DEFAULT_PRESET_PATH = "Assets/Plugins/FewClicksDev/TextureChannelsArranger/Presets";
        private const bool DEFAULT_SHOW_TEXTURES_RESOLUTION = false;

        private const string DEFAULT_APPLY_AT_START_PRESET = "584b837ff99832042a5d126bca76fd2a";
        private const string DEFAULT_SRGB_PRESET_GUID = "6c5227a1e5fdb8643923a2c8073ffe9d";
        private const string DEFAULT_LINEAR_PRESET_GUID = "f0f8da80a0c27aa4c81bfa4172a7a48b";
        private const string DEFAULT_NORMAL_MAP_PRESET_GUID = "1a2f57a9706c5fa40b01c16bb4ee531f";
        private const string DEFAULT_SINGLE_CHANNEL_PRESET_GUID = "13771865af843b944a6508e405ce3594";

        public static Side ToolbarSide = DEFAULT_SIDE;
        public static TextureExportFormat ExportFormat = DEFAULT_EXPORT_FORMAT;
        public static ChannelPreviewMode ChannelPreview = DEFAULT_CHANNEL_PREVIEW_MODE;
        public static string DefaultExportPath = DEFAULT_EXPORT_PATH;
        public static bool ShowTexturesResolution = DEFAULT_SHOW_TEXTURES_RESOLUTION;

        public static ChannelArrangerPreset ApplyAtStartPreset = null;
        public static Preset SRGBPreset = null;
        public static Preset LinearPreset = null;
        public static Preset NormalMapPreset = null;
        public static Preset SingleChannelPreset = null;

        public static List<ChannelArrangerPresetSetup> PresetsWithSetup = null;

        public static bool PrintLogs = DEFAULT_PRINT_LOGS;

        public static string PresetName = DEFAULT_PRESET_NAME;
        public static string PresetPath = DEFAULT_PRESET_PATH;

        private static bool arePrefsLoaded = false;

        static ChannelArrangerUserPreferences()
        {
            LoadPreferences();
        }

        [SettingsProvider]
        public static SettingsProvider PreferencesSettingsProvider()
        {
            SettingsProvider provider = new SettingsProvider(PREFS_PATH, SETTINGS_SCOPE)
            {
                label = LABEL,
                guiHandler = (searchContext) =>
                {
                    OnGUI();
                },

                keywords = new HashSet<string>(KEYWORDS)
            };

            return provider;
        }

        public static void OnGUI()
        {
            using (new IndentScope())
            {
                using (new LabelWidthScope(LABEL_WIDTH))
                {
                    if (arePrefsLoaded == false)
                    {
                        LoadPreferences();
                    }

                    DrawHeader(SETTINGS);
                    ToolbarSide = (Side) EditorGUILayout.EnumPopup(TOOLBAR_SIDE, ToolbarSide);
                    ExportFormat = (TextureExportFormat) EditorGUILayout.EnumPopup(EXPORT_FORMAT, ExportFormat);
                    ChannelPreview = (ChannelPreviewMode) EditorGUILayout.EnumPopup(CHANNEL_PREVIEW, ChannelPreview);
                    DefaultExportPath = EditorGUILayout.TextField(EXPORT_PATH, DefaultExportPath);
                    ShowTexturesResolution = EditorGUILayout.Toggle(SHOW_TEXTURES_RESOLUTION, ShowTexturesResolution);

                    SmallSpace();
                    DrawHeader(LOGS);
                    PrintLogs = EditorGUILayout.Toggle(PRINT_LOGS_LABEL, PrintLogs);

                    SmallSpace();
                    DrawHeader(PRESETS);
                    PresetName = EditorGUILayout.TextField(PRESET_NAME, PresetName);
                    PresetPath = DrawFolderPicker(PRESET_PATH, PresetPath, SELECT_FOLDER);

                    SmallSpace();
                    ApplyAtStartPreset = EditorGUILayout.ObjectField(APPLY_AT_START, ApplyAtStartPreset, typeof(ChannelArrangerPreset), false) as ChannelArrangerPreset;
                    SRGBPreset = EditorGUILayout.ObjectField(SRGB, SRGBPreset, typeof(Preset), false) as Preset;
                    LinearPreset = EditorGUILayout.ObjectField(LINEAR, LinearPreset, typeof(Preset), false) as Preset;
                    NormalMapPreset = EditorGUILayout.ObjectField(NORMAL_MAP, NormalMapPreset, typeof(Preset), false) as Preset;
                    SingleChannelPreset = EditorGUILayout.ObjectField(SINGLE_CHANNEL, SingleChannelPreset, typeof(Preset), false) as Preset;

                    NormalSpace();

                    using (new HorizontalScope())
                    {
                        FlexibleSpace();

                        if (DrawBoxButton(RESET_TO_DEFAULTS, FixedWidthAndHeight(EditorGUIUtility.currentViewWidth / 2f, 22f)))
                        {
                            ResetToDefaults();
                        }

                        FlexibleSpace();
                    }

                    if (GUI.changed == true)
                    {
                        SavePreferences();
                    }
                }
            }
        }

        public static void SavePreferences()
        {
            //Settings
            EditorPrefs.SetInt(PREFS_PREFIX + nameof(ToolbarSide), (int) ToolbarSide);
            EditorPrefs.SetInt(PREFS_PREFIX + nameof(ExportFormat), (int) ExportFormat);
            EditorPrefs.SetInt(PREFS_PREFIX + nameof(ChannelPreview), (int) ChannelPreview);
            EditorPrefs.SetString(PREFS_PREFIX + nameof(DefaultExportPath), DefaultExportPath);
            EditorPrefs.SetBool(PREFS_PREFIX + nameof(PrintLogs), PrintLogs);
            EditorPrefs.SetString(PREFS_PREFIX + nameof(PresetName), PresetName);
            EditorPrefs.SetString(PREFS_PREFIX + nameof(PresetPath), PresetPath);
            EditorPrefs.SetBool(PREFS_PREFIX + nameof(ShowTexturesResolution), ShowTexturesResolution);

            //Presets
            EditorPrefs.SetString(PREFS_PREFIX + nameof(ApplyAtStartPreset), ApplyAtStartPreset.GetAssetGUID());
            EditorPrefs.SetString(PREFS_PREFIX + nameof(SRGBPreset), SRGBPreset.GetAssetGUID());
            EditorPrefs.SetString(PREFS_PREFIX + nameof(LinearPreset), LinearPreset.GetAssetGUID());
            EditorPrefs.SetString(PREFS_PREFIX + nameof(NormalMapPreset), NormalMapPreset.GetAssetGUID());
            EditorPrefs.SetString(PREFS_PREFIX + nameof(SingleChannelPreset), SingleChannelPreset.GetAssetGUID());

            string _presetsWithSetup = string.Empty;

            foreach (var _presetWithSetup in PresetsWithSetup)
            {
                _presetsWithSetup += _presetWithSetup.GetStringToSave() + SEMICOLON;
            }

            EditorPrefs.SetString(PREFS_PREFIX + nameof(PresetsWithSetup), _presetsWithSetup);
        }

        public static void LoadPreferences()
        {
            //Settings
            ToolbarSide = (Side) EditorPrefs.GetInt(PREFS_PREFIX + nameof(ToolbarSide), (int) DEFAULT_SIDE);
            ExportFormat = (TextureExportFormat) EditorPrefs.GetInt(PREFS_PREFIX + nameof(ExportFormat), (int) DEFAULT_EXPORT_FORMAT);
            ChannelPreview = (ChannelPreviewMode) EditorPrefs.GetInt(PREFS_PREFIX + nameof(ChannelPreview), (int) DEFAULT_CHANNEL_PREVIEW_MODE);
            DefaultExportPath = EditorPrefs.GetString(PREFS_PREFIX + nameof(DefaultExportPath), DEFAULT_EXPORT_PATH);
            PrintLogs = EditorPrefs.GetBool(PREFS_PREFIX + nameof(PrintLogs), DEFAULT_PRINT_LOGS);
            PresetName = EditorPrefs.GetString(PREFS_PREFIX + nameof(PresetName), DEFAULT_PRESET_NAME);
            PresetPath = EditorPrefs.GetString(PREFS_PREFIX + nameof(PresetPath), DEFAULT_PRESET_PATH);
            ShowTexturesResolution = EditorPrefs.GetBool(PREFS_PREFIX + nameof(ShowTexturesResolution), DEFAULT_SHOW_TEXTURES_RESOLUTION);

            //Presets
            ApplyAtStartPreset = AssetsUtilities.LoadAsset<ChannelArrangerPreset>(EditorPrefs.GetString(PREFS_PREFIX + nameof(ApplyAtStartPreset)));
            SRGBPreset = AssetsUtilities.LoadAsset<Preset>(EditorPrefs.GetString(PREFS_PREFIX + nameof(SRGBPreset)));
            LinearPreset = AssetsUtilities.LoadAsset<Preset>(EditorPrefs.GetString(PREFS_PREFIX + nameof(LinearPreset)));
            NormalMapPreset = AssetsUtilities.LoadAsset<Preset>(EditorPrefs.GetString(PREFS_PREFIX + nameof(NormalMapPreset)));
            SingleChannelPreset = AssetsUtilities.LoadAsset<Preset>(EditorPrefs.GetString(PREFS_PREFIX + nameof(SingleChannelPreset)));

            string _loadedPresetsWithSetup = EditorPrefs.GetString(PREFS_PREFIX + nameof(PresetsWithSetup), string.Empty);
            loadPresetsWithSetup(_loadedPresetsWithSetup);

            arePrefsLoaded = true;
        }

        public static void ResetToDefaults()
        {
            //Settings
            ToolbarSide = DEFAULT_SIDE;
            ExportFormat = DEFAULT_EXPORT_FORMAT;
            ChannelPreview = DEFAULT_CHANNEL_PREVIEW_MODE;
            DefaultExportPath = DEFAULT_EXPORT_PATH;
            PrintLogs = DEFAULT_PRINT_LOGS;
            PresetName = DEFAULT_PRESET_NAME;
            PresetPath = DEFAULT_PRESET_PATH;
            ShowTexturesResolution = DEFAULT_SHOW_TEXTURES_RESOLUTION;

            //Presets
            ApplyAtStartPreset = AssetsUtilities.LoadAsset<ChannelArrangerPreset>(DEFAULT_APPLY_AT_START_PRESET);
            SRGBPreset = AssetsUtilities.LoadAsset<Preset>(DEFAULT_SRGB_PRESET_GUID);
            LinearPreset = AssetsUtilities.LoadAsset<Preset>(DEFAULT_LINEAR_PRESET_GUID);
            NormalMapPreset = AssetsUtilities.LoadAsset<Preset>(DEFAULT_NORMAL_MAP_PRESET_GUID);
            SingleChannelPreset = AssetsUtilities.LoadAsset<Preset>(DEFAULT_SINGLE_CHANNEL_PRESET_GUID);

            ResetPresetsToDefault(true);
            SavePreferences();
        }

        public static void UpdateSavedPresets(List<ChannelArrangerPreset> _presets)
        {
            bool _addedNewPreset = false;

            foreach (var _preset in _presets)
            {
                if (PresetsWithSetup.Any(_presetWithSetup => _presetWithSetup.Preset == _preset) == false)
                {
                    _addedNewPreset = true;
                    PresetsWithSetup.Add(new ChannelArrangerPresetSetup(_preset, true, PresetsWithSetup.Count));
                }
            }

            if (_addedNewPreset == true)
            {
                SavePreferences();
            }
        }

        public static bool IsPresetVisible(ChannelArrangerPreset _preset)
        {
            foreach (var _presetWithSetup in PresetsWithSetup)
            {
                if (_preset == _presetWithSetup.Preset)
                {
                    return _presetWithSetup.IsVisible;
                }
            }

            return false;
        }

        public static void SetPresetVisibility(ChannelArrangerPreset _preset, bool _visible)
        {
            foreach (var _presetWithSetup in PresetsWithSetup)
            {
                if (_preset == _presetWithSetup.Preset)
                {
                    _presetWithSetup.SetVisibility(_visible);
                }
            }

            SavePreferences();
        }

        public static Preset GetDefaultPreset(TextureImportMode _importMode)
        {
            return _importMode switch
            {
                TextureImportMode.BaseColorSRGB => SRGBPreset,
                TextureImportMode.BaseColorLinear => LinearPreset,
                TextureImportMode.NormalMap => NormalMapPreset,
                TextureImportMode.SingleChannel => SingleChannelPreset,
                _ => null,
            };
        }

        public static void SetAllPresetsVisible(bool _visible)
        {
            for (int i = 0; i < PresetsWithSetup.Count; i++)
            {
                PresetsWithSetup[i].SetVisibility(_visible);
            }

            SavePreferences();
        }

        public static void ResetPresetsToDefault(bool _changeVisibility)
        {
            PresetsWithSetup = PresetsWithSetup.OrderByDescending(_preset => _preset.Preset.Priority).ToList(); //Reset to default order

            for (int i = 0; i < PresetsWithSetup.Count; i++)
            {
                PresetsWithSetup[i].SetOrderInTheList(i);
                PresetsWithSetup[i].SetVisibility(true);
            }

            SavePreferences();
        }

        public static void SortPresetsByOrder() 
        {
            PresetsWithSetup = PresetsWithSetup.OrderBy(_preset => _preset.OrderInTheList).ToList();
            SavePreferences();
        }

        private static void loadPresetsWithSetup(string _saved)
        {
            PresetsWithSetup = new List<ChannelArrangerPresetSetup>();
            string[] _presetsSlit = _saved.Split(';');

            foreach (var _presetWithSetup in _presetsSlit)
            {
                if (string.IsNullOrEmpty(_presetWithSetup))
                {
                    continue;
                }

                string[] _splitPresetWithSetup = _presetWithSetup.Split(',');

                string _presetGUID = _splitPresetWithSetup[0];
                bool _isVisible = _splitPresetWithSetup[1] == "1";
                int _orderInTheList = int.Parse(_splitPresetWithSetup[2]);

                ChannelArrangerPreset _loadedPreset = AssetsUtilities.LoadAsset<ChannelArrangerPreset>(_presetGUID);

                if (_loadedPreset == null)
                {
                    TextureChannelArranger.Error($"Preset with GUID [{_presetGUID}] was not found.");
                    continue;
                }

                PresetsWithSetup.Add(new ChannelArrangerPresetSetup(_loadedPreset, _isVisible, _orderInTheList));
            }

            PresetsWithSetup.OrderByDescending(_preset => _preset.OrderInTheList);
            SavePreferences();
        }
    }
}