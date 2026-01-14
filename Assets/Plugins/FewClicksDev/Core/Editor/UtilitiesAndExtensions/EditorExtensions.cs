namespace FewClicksDev.Core
{
    using System;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

    public static class EditorExtensions
    {
        private const int NUMBER_OF_CHANNELS_IN_COLOR = 4;

        private const string PROJECT_BROWSER_TYPE = "UnityEditor.ProjectBrowser, UnityEditor";
        private const string SET_SEARCH_METHOD_NAME = "SetSearch";

        private const string PREFERENCES_MENU = "Edit/Preferences...";
        private const string PREFERENCES_TYPE = "UnityEditor.PreferenceSettingsWindow, UnityEditor";
        private const string SELECT_PREFERENCES_METHOD_NAME = "SelectProviderByName";

        public static void SetSearchString(string _searchString)
        {
            EditorUtility.FocusProjectWindow();

            Type _projectBrowserType = Type.GetType(PROJECT_BROWSER_TYPE);
            EditorWindow _window = EditorWindow.GetWindow(_projectBrowserType);

            MethodInfo _setSearchMethodInfo = _projectBrowserType.GetMethod(SET_SEARCH_METHOD_NAME, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance,
                null, new Type[] { typeof(string) }, null);

            _setSearchMethodInfo.Invoke(_window, new object[] { _searchString });
        }

        public static string GetPathToCurrentlyOpenedFolder()
        {
            string _folderPath = AssetDatabase.GetAssetPath(Selection.activeInstanceID);

            if (_folderPath.Contains("."))
            {
                _folderPath = _folderPath.Remove(_folderPath.LastIndexOf('/'));
            }

            return _folderPath;
        }

        public static Color LoadColor(string _name, Color _defaultValue)
        {
            Color _color = Color.white;
            string _colorString = EditorPrefs.GetString(_name, GetStringFromColor(_defaultValue));
            string[] _colorElements = _colorString.Split(':');

            if (_colorElements.Length < NUMBER_OF_CHANNELS_IN_COLOR)
            {
                return _color;
            }

            float.TryParse(_colorElements[0], out float _r);
            float.TryParse(_colorElements[1], out float _g);
            float.TryParse(_colorElements[2], out float _b);
            float.TryParse(_colorElements[3], out float _a);

            _color = new Color(_r, _g, _b, _a);

            return _color;
        }

        public static string GetStringFromColor(Color _color)
        {
            return $"{_color.r}:{_color.g}:{_color.b}:{_color.a}";
        }

        public static void ShowNotification(string _message)
        {
            EditorWindow _window = EditorWindow.focusedWindow;

            if (_window == null)
            {
                return;
            }

            _window.ShowNotification(new GUIContent(_message));
        }

        public static string[] GetStringArray(this SerializedProperty _property)
        {
            if (_property.isArray == false)
            {
                return null;
            }

            string[] _array = new string[_property.arraySize];

            for (int i = 0; i < _property.arraySize; i++)
            {
                _array[i] = _property.GetArrayElementAtIndex(i).stringValue;
            }

            return _array;
        }

        // Based on the https://gist.github.com/Refsa/e4c6c2308f8dc2273b9c67764cc68a44
        public static void ShowPreferencesSection(string _sectionName)
        {
            EditorApplication.ExecuteMenuItem(PREFERENCES_MENU);
            var _preferencesWindowType = Type.GetType(PREFERENCES_TYPE);
            var _preferencesWindow = EditorWindow.GetWindow(_preferencesWindowType);

            var _selectMethod = _preferencesWindowType.GetMethod(SELECT_PREFERENCES_METHOD_NAME, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            _selectMethod.Invoke(_preferencesWindow, new object[] { _sectionName });
        }
    }
}