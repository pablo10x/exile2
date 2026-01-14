namespace FewClicksDev.TextureChannelArranger
{
    using FewClicksDev.Core;
    using UnityEditor;
    using UnityEngine;

    public static class ChannelArrangerGenericMenu
    {
        private static readonly GUIContent SELECT_CONTENT = new GUIContent("Select", "Select the preset in the project view.");
        private static readonly GUIContent ADD_TO_FAVORITES_CONTENT = new GUIContent("Add to favorites", "Add the preset to the favorites list.");
        private static readonly GUIContent REMOVE_FROM_FAVORITES_CONTENT = new GUIContent("Remove from favorites", "Remove the preset from the favorites list.");
        private static readonly GUIContent EDIT_CONTENT = new GUIContent("Edit", "Edit the preset.");

        private static readonly GUIContent CLEAR_LIST_CONTENT = new GUIContent("Clear list", "Clear the list of found textures.");
        private static readonly GUIContent REMOVE_NULL_REFERENCES_CONTENT = new GUIContent("Remove null references", "Remove null references from the list of found textures.");
        private static readonly GUIContent FILL_WITH_WHITE_TEXTURES_CONTENT = new GUIContent("Fill with white textures", "Fill the list of found textures with white textures.");
        private static readonly GUIContent FILL_WITH_GRAY_TEXTURES_CONTENT = new GUIContent("Fill with gray textures", "Fill the list of found textures with gray textures.");
        private static readonly GUIContent FILL_WITH_BLACK_TEXTURES_CONTENT = new GUIContent("Fill with black textures", "Fill the list of found textures with black textures.");
        private static readonly GUIContent FILL_WITH_CUSTOM_TEXTURES_CONTENT = new GUIContent("Fill with custom textures", "Fill the list of found textures with custom textures.");

        private const string GRAPHIC_FILE_EXTENSIONS = "png,jpg,jpeg,tga,tif,tiff,bmp,psd,exr";
        private const string SELECT_TEXTURE = "Select texture";
        private const string NO_TEXTURE_FILE_SELECTED = "No texture file was selected.";
        private const string SELECTED_FILE_IS_NOT_TEXTURE = "The selected file is not a texture.";

        public static void ShowForPreset(TextureChannelArrangerWindow _window, Event _currentEvent, ChannelArrangerPreset _preset)
        {
            GenericMenu _menu = new GenericMenu();

            _menu.AddDisabledItem(new GUIContent(_preset.DisplayName));
            _menu.AddSeparator(string.Empty);
            _menu.AddItem(SELECT_CONTENT, false, _selectAndPing);

            bool _visible = ChannelArrangerUserPreferences.IsPresetVisible(_preset);

            if (_visible)
            {
                _menu.AddItem(REMOVE_FROM_FAVORITES_CONTENT, false, _hide);
            }
            else
            {
                _menu.AddItem(ADD_TO_FAVORITES_CONTENT, false, _show);
            }

            _menu.AddItem(EDIT_CONTENT, false, _edit);
            _menu.ShowAsContext();

            _currentEvent.Use();

            void _selectAndPing()
            {
                Selection.activeObject = _preset;
                EditorGUIUtility.PingObject(_preset);
            }

            void _hide()
            {
                ChannelArrangerUserPreferences.SetPresetVisibility(_preset, false);
            }

            void _show()
            {
                ChannelArrangerUserPreferences.SetPresetVisibility(_preset, true);
            }

            void _edit()
            {
                _window.EnableEditMode(_preset);
            }
        }

        public static void ShowForBatchInput(TextureInput _input, Event _currentEvent, int _maxTexturesCount)
        {
            GenericMenu _menu = new GenericMenu();
            int _numberOfFoundTextures = _input.NumberOfFoundTextures;
            bool _shouldAddSeparator = false;

            if (_numberOfFoundTextures > 0)
            {
                _menu.AddItem(CLEAR_LIST_CONTENT, false, _clearFoundTextures);
                _menu.AddItem(REMOVE_NULL_REFERENCES_CONTENT, false, _removeNullReferences);
                _shouldAddSeparator = true;
            }

            if (_input.NumberOfFoundTextures < _maxTexturesCount)
            {
                if (_shouldAddSeparator)
                {
                    _menu.AddSeparator(string.Empty);
                }

                _menu.AddItem(FILL_WITH_WHITE_TEXTURES_CONTENT, false, _fillWithWhiteTextures);
                _menu.AddItem(FILL_WITH_GRAY_TEXTURES_CONTENT, false, _fillWithGrayTextures);
                _menu.AddItem(FILL_WITH_BLACK_TEXTURES_CONTENT, false, _fillWithBlackTextures);
                _menu.AddItem(FILL_WITH_CUSTOM_TEXTURES_CONTENT, false, _fillWithCustomTextures);
            }

            _currentEvent.Use();
            _menu.ShowAsContext();

            void _clearFoundTextures()
            {
                _input.ClearFoundTextures();
            }

            void _removeNullReferences()
            {
                _input.RemoveNullReferences();
            }

            void _fillWithWhiteTextures()
            {
                _input.FillWithWhiteTextures(_maxTexturesCount);
            }

            void _fillWithGrayTextures()
            {
                _input.FillWithGrayTextures(_maxTexturesCount);
            }

            void _fillWithBlackTextures()
            {
                _input.FillWithBlackTextures(_maxTexturesCount);
            }

            void _fillWithCustomTextures()
            {
                string _filePath = EditorUtility.OpenFilePanel(SELECT_TEXTURE, Application.dataPath, GRAPHIC_FILE_EXTENSIONS);

                if (_filePath.IsNullEmptyOrWhitespace())
                {
                    TextureChannelArranger.Error(NO_TEXTURE_FILE_SELECTED);
                    return;
                }

                Texture2D _loadedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetsUtilities.ConvertAbsolutePathToDataPath(_filePath));

                if (_loadedTexture == null)
                {
                    TextureChannelArranger.Error(SELECTED_FILE_IS_NOT_TEXTURE);
                    return;
                }

                _input.FillWithCustomTextures(_maxTexturesCount, _loadedTexture);
            }
        }
    }
}
