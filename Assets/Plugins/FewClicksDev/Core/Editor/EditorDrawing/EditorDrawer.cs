namespace FewClicksDev.Core
{
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Helper class for editor drawers, colors and UnityEditor layout elements shortcuts
    /// </summary>
    public static class EditorDrawer
    {
        public static readonly Color WHITE = Color.white;
        public static readonly Color DEFAULT_GRAY = new Color(0.3f, 0.3f, 0.3f, 1f);
        public static readonly Color LIGHT_GRAY = new Color(0.9f, 0.9f, 0.9f, 1f);
        public static readonly Color BLUE = new Color(0.360404f, 0.7367904f, 0.8396226f, 1f);
        public static readonly Color LIGHT_BLUE = new Color(0.458437f, 0.930761f, 0.962264f, 1f);
        public static readonly Color RED = new Color(0.8301887f, 0.2780349f, 0.3079897f, 1f);
        public static readonly Color GREEN = new Color(0.2723389f, 0.6792453f, 0.3881041f, 1f);
        public static readonly Color YELLOW = new Color(0.8301887f, 0.8301887f, 0.3079897f, 1f);
        public static readonly Color ORANGE = new Color(0.8301887f, 0.5f, 0.3079897f, 1f);
        public static readonly Color PINK = new Color(0.622642f, 0.334817f, 0.51629f, 1f);
        public static readonly Color PURPLE = new Color(0.5f, 0.3079897f, 0.8301887f, 1f);
        public static readonly Color BROWN = new Color(0.559748f, 0.404984f, 0.283395f, 1f);

        public const string LEFT_ARROW = "◄";
        public const string RIGHT_ARROW = "►";
        public const string UP_ARROW = "▲";
        public const string DOWN_ARROW = "▼";
        public const char CORRECT_MARK = '✓';
        public const string PLUS = "+";
        public const string MINUS = "-";
        public const string X = "X";

        public const string EDITOR_DRAWER = "EDITOR DRAWER";
        public const string FOLDER_PATH_ERROR = "The folder path must be within the current project assets folder. Path changed to the Application.dataPath";

        public const string SETTINGS = "Settings";
        public const string LOGS = "Logs";
        public const string HELPERS = "Helpers";
        public const string CLEAR = "Clear";
        public const string RESET = "Reset";
        public const string DELETE = "Delete";
        public const string REMOVE = "Remove";
        public const string ADD = "Add";
        public const string SAVE = "Save";
        public const string APPLY = "Apply";
        public const string CANCEL = "Cancel";
        public const string CLOSE = "Close";
        public const string OK = "Ok";
        public const string YES = "Yes";
        public const string NO = "No";
        public const string USE = "Use";
        public const string DOTS = "...";

        public const float VERY_SMALL_SPACE = 3f;
        public const float SMALL_SPACE = 5f;
        public const float NORMAL_SPACE = 10f;
        public const float LARGE_SPACE = 15f;
        public const float HELP_BOX_HEIGHT = 38f;
        public const float SORT_ORDER_HEIGHT = 20f;

        public const float DEFAULT_TOOLBAR_HEIGHT = 26f;
        public const float DEFAULT_LINE_HEIGHT = 22f;
        public const int TOOLBARS_TEXT_SIZE = 14;

        public static float SingleLineHeight => EditorGUIUtility.singleLineHeight;
        public static float StandardVerticalSpacing => EditorGUIUtility.standardVerticalSpacing;
        public static float SingleLineHeightWithSpacing => SingleLineHeight + StandardVerticalSpacing;

        public static float CurrentLabelWidth => EditorGUIUtility.labelWidth;
        public static float CurrentFieldWidth => EditorGUIUtility.fieldWidth;
        public static float CurrentViewWidth => EditorGUIUtility.currentViewWidth;

        public static GUIStyle ToolbarStyle
        {
            get
            {
                if (toolbarStyle == null)
                {
                    toolbarStyle = new GUIStyle(Styles.ClearBox);
                    toolbarStyle.SetTextColorForNormalAndOtherStates(LIGHT_GRAY, WHITE);
                }

                return toolbarStyle;
            }
        }

        private static GUIStyle toolbarStyle = null;

        public static GUIStyle CenteredBoldLabelStyle
        {
            get
            {
                if (centeredBoldLabelStyle == null)
                {
                    centeredBoldLabelStyle = new GUIStyle(EditorStyles.boldLabel);
                    centeredBoldLabelStyle.alignment = TextAnchor.MiddleCenter;
                    centeredBoldLabelStyle.clipping = TextClipping.Overflow;
                    centeredBoldLabelStyle.wordWrap = true;
                }

                return centeredBoldLabelStyle;
            }
        }

        private static GUIStyle centeredBoldLabelStyle = null;

        private const float DOTTED_LINE_SEPARATOR_WIDTH = 1f;
        private const int DEFAULT_NUMBER_OF_DOTTED_LINE_SEPARATORS = 10;
        private const float DEFAULT_UNITY_HORIZONTAL_SPACE = 2f;

        private const string SCRIPT_PROPERTY_NAME = "m_Script";
        private const char UNDERSCORE = '_';
        private const char SLASH = '/';

        private static Dictionary<float, GUILayoutOption[]> fixedWidths = new Dictionary<float, GUILayoutOption[]>();
        private static Dictionary<float, GUILayoutOption[]> fixedHeights = new Dictionary<float, GUILayoutOption[]>();
        private static Dictionary<Vector2, GUILayoutOption[]> fixedWidthAndHeights = new Dictionary<Vector2, GUILayoutOption[]>();

        public static void Space(float _spaceSize)
        {
            GUILayout.Space(_spaceSize);
        }

        public static void VerySmallSpace()
        {
            GUILayout.Space(VERY_SMALL_SPACE);
        }

        public static void SmallSpace()
        {
            GUILayout.Space(SMALL_SPACE);
        }

        public static void NormalSpace()
        {
            GUILayout.Space(NORMAL_SPACE);
        }

        public static void LargeSpace()
        {
            GUILayout.Space(LARGE_SPACE);
        }

        public static void FlexibleSpace()
        {
            GUILayout.FlexibleSpace();
        }

        public static void DrawScriptProperty(this SerializedObject _serializedObject)
        {
            using (new DisabledScope())
            {
                EditorGUILayout.PropertyField(_serializedObject.FindProperty(SCRIPT_PROPERTY_NAME), true);
            }

            SmallSpace();
        }

        public static void DrawHeader(string _labelName)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(_labelName, EditorStyles.boldLabel);
        }

        public static bool DrawCenteredButton(string _label, float _width)
        {
            using (new GUILayout.HorizontalScope())
            {
                FlexibleSpace();

                if (GUILayout.Button(_label, FixedWidthAndHeight(_width, DEFAULT_LINE_HEIGHT)))
                {
                    return true;
                }

                FlexibleSpace();

            }

            return false;
        }

        public static bool DrawBoxButton(GUIContent _content, GUILayoutOption[] _options = null)
        {
            if (GUILayout.Button(_content, Styles.BoxButton, _options))
            {
                return true;
            }

            return false;
        }

        public static bool DrawBoxButton(string _label, GUILayoutOption[] _options = null)
        {
            if (GUILayout.Button(_label, Styles.BoxButton, _options))
            {
                return true;
            }

            return false;
        }

        public static bool DrawClearBoxButton(string _label, GUILayoutOption[] _options = null)
        {
            if (GUILayout.Button(_label, Styles.ClearBox, _options))
            {
                return true;
            }

            return false;
        }

        public static bool DrawClearBoxButton(string _label, Color _color, GUILayoutOption[] _options = null)
        {
            using (ColorScope.Background(_color))
            {
                if (GUILayout.Button(_label, Styles.ClearBox, _options))
                {
                    return true;
                }

                return false;
            }
        }

        public static bool DrawCloseButton(GUILayoutOption[] _options = null)
        {
            if (GUILayout.Button(string.Empty, Styles.Close, _options))
            {
                return true;
            }

            return false;
        }

        public static bool DrawDocumentationButton(GUILayoutOption[] _options = null)
        {
            if (GUILayout.Button(string.Empty, Styles.DocumentationButton, _options))
            {
                return true;
            }

            return false;
        }

        public static int DrawUpAndDownButtons(float _height, bool _activeUp = true, bool _activeDown = true)
        {
            float _buttonWidth = _height / 2f;

            using (new VerticalScope(FixedWidth(_buttonWidth)))
            {
                if (_activeUp)
                {
                    if (GUILayout.Button(string.Empty, Styles.FixedArrowUp(_buttonWidth)))
                    {
                        return 1;
                    }
                }
                else
                {
                    DrawBoxButton(string.Empty, FixedWidthAndHeight(_buttonWidth));
                }

                if (_activeDown)
                {
                    if (GUILayout.Button(string.Empty, Styles.FixedArrowDown(_buttonWidth)))
                    {
                        return -1;
                    }
                }
                else
                {
                    DrawBoxButton(string.Empty, FixedWidthAndHeight(_buttonWidth));
                }
            }

            return 0;
        }

        public static void DeselectGUIElements()
        {
            GUI.FocusControl(null);
        }

        public static string GetSimplifiedShaderName(this Shader _shader)
        {
            string _shaderName = _shader.GetShaderName();
            string[] _split = _shaderName.Split(UNDERSCORE);

            return _split[_split.Length - 1];
        }

        public static string GetShaderName(this Shader _shader)
        {
            string[] _split = _shader.name.Split(SLASH);

            return _split[_split.Length - 1];
        }

        public static void DrawLine()
        {
            DrawLine(Color.gray, 3f, float.MaxValue);
        }

        public static void DrawLine(float _height)
        {
            DrawLine(Color.gray, _height, float.MaxValue);
        }

        public static void DrawLine(float _height, float _width)
        {
            DrawLine(Color.gray, _height, _width);
        }

        public static void DrawLine(Color _color, float _height, float _width)
        {
            using (ColorScope.GUI(_color))
            {
                if (_width == float.MaxValue)
                {
                    GUILayout.Box(string.Empty, GUILayout.ExpandWidth(true), GUILayout.Height(_height));
                }
                else
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        FlexibleSpace();
                        GUILayout.Box(string.Empty, FixedWidthAndHeight(_width, _height));
                        FlexibleSpace();
                    }
                }
            }
        }

        public static void DrawHandlesLine(Vector2 _start, Vector2 _end)
        {
            DrawHandlesLine(_start, _end, Color.gray);
        }

        public static void DrawHandlesLine(Vector2 _start, Vector2 _end, Color _color)
        {
            Color _handlesColor = Handles.color;
            Handles.color = _color;
            Handles.DrawAAPolyLine(2f, _start, _end);
            Handles.color = _handlesColor;
        }

        public static void DrawDottedLine()
        {
            DrawDottedLine(DEFAULT_NUMBER_OF_DOTTED_LINE_SEPARATORS, Color.gray, 3f);
        }

        public static void DrawDottedLine(float _separators, Color _color, float _height = 3f)
        {
            Color _defaultColor = GUI.color;
            GUI.color = _color;

            float _numberOfLines = _separators + 1;
            float _lineWidth = (Screen.width - (_separators + _numberOfLines) * (DOTTED_LINE_SEPARATOR_WIDTH + DEFAULT_UNITY_HORIZONTAL_SPACE)) / _numberOfLines;

            using (new GUILayout.HorizontalScope())
            {
                for (int i = 0; i < _numberOfLines; i++)
                {
                    GUILayout.Box(string.Empty, FixedWidthAndHeight(_lineWidth, _height));

                    if (i != _numberOfLines - 1)
                    {
                        Space(DOTTED_LINE_SEPARATOR_WIDTH);
                    }
                }
            }

            GUI.color = _defaultColor;
        }

        public static void DrawVerticalLine(float _width)
        {
            DrawVerticalLine(Color.gray, _width);
        }

        public static void DrawVerticalLine(Color _color, float _width = 3f)
        {
            Color _defaultColor = GUI.color;
            GUI.color = _color;
            GUILayout.Box(string.Empty, GUILayout.Width(_width), GUILayout.ExpandHeight(true));
            GUI.color = _defaultColor;
        }

        public static void DrawBoldLabel(string _label, Color _color, GUILayoutOption[] _options = null)
        {
            using (ColorScope.Content(_color))
            {
                EditorGUILayout.LabelField(_label, EditorStyles.boldLabel, _options);
            }
        }

        public static void DrawBoldLabel(string _label, GUILayoutOption[] _options = null)
        {
            EditorGUILayout.LabelField(_label, EditorStyles.boldLabel, _options);
        }

        public static void DrawCenteredBoldLabel(string _label, float _width)
        {
            GUIStyle _centered = new GUIStyle(EditorStyles.boldLabel);
            _centered.alignment = TextAnchor.MiddleCenter;

            using (new EditorGUILayout.HorizontalScope())
            {
                FlexibleSpace();
                EditorGUILayout.LabelField(_label, _centered, FixedWidth(_width));
                FlexibleSpace();
            }
        }

        public static void DrawCenteredBoldLabel(string _label, float _width, Color _labelColor)
        {
            using (ColorScope.Content(_labelColor))
            {
                DrawCenteredBoldLabel(_label, _width);
            }
        }

        public static void DrawCenteredLabelWithThinLine(string _label)
        {
            SmallSpace();
            DrawCenteredBoldLabel(_label);
            DrawLine(1f, Screen.width - 15f);
            SmallSpace();
        }

        public static void DrawCenteredLabelWithThinLine(string _label, Color _color)
        {
            SmallSpace();

            using (ColorScope.Content(_color))
            {
                DrawCenteredBoldLabel(_label);
            }

            DrawLine(1f, Screen.width * 0.9f);
            SmallSpace();
        }

        public static void DrawCenteredLabelWithThinLine(string _label, float _width)
        {
            SmallSpace();
            DrawCenteredBoldLabel(_label);
            DrawLine(1f, _width);
            SmallSpace();
        }

        public static void DrawCenteredLabelWithThinLine(string _label, float _width, Color _color)
        {
            SmallSpace();

            using (ColorScope.Content(_color))
            {
                DrawCenteredBoldLabel(_label);
            }

            DrawLine(1f, _width);
            SmallSpace();
        }

        public static void DrawCenteredBoldLabel(string _label, GUILayoutOption[] _options = null)
        {
            using (new HorizontalScope(_options))
            {
                FlexibleSpace();
                EditorGUILayout.LabelField(_label, CenteredBoldLabelStyle);
                FlexibleSpace();
            }
        }

        public static GUILayoutOption MaxWidth(float _width)
        {
            return GUILayout.MaxWidth(_width);
        }

        public static GUILayoutOption[] FixedWidth(float _width)
        {
            if (fixedWidths.ContainsKey(_width) == false)
            {
                fixedWidths.Add(_width, new GUILayoutOption[] { GUILayout.MaxWidth(_width), GUILayout.MinWidth(_width) });
            }

            return fixedWidths[_width];
        }

        public static GUILayoutOption[] FixedHeight(float _height)
        {
            if (fixedHeights.ContainsKey(_height) == false)
            {
                fixedHeights.Add(_height, new GUILayoutOption[] { GUILayout.MinHeight(_height), GUILayout.MaxHeight(_height) });
            }

            return fixedHeights[_height];
        }

        public static GUILayoutOption[] FixedWidthAndHeight(float _widthAndHeight)
        {
            return FixedWidthAndHeight(_widthAndHeight, _widthAndHeight);
        }

        public static GUILayoutOption[] FixedWidthAndHeight(float _width, float _height)
        {
            Vector2 _widthAndHeight = new Vector2(_width, _height);

            if (fixedWidthAndHeights.ContainsKey(_widthAndHeight) == false)
            {
                fixedWidthAndHeights.Add(_widthAndHeight, new GUILayoutOption[] { GUILayout.MaxWidth(_width), GUILayout.MinWidth(_width), GUILayout.MinHeight(_height), GUILayout.MaxHeight(_height) });
            }

            return fixedWidthAndHeights[_widthAndHeight];
        }

        public static T DrawEnumToolbar<T>(this CustomEditorWindow _window, T _selected, float _widthAsPercent, Color _selectedColor) where T : Enum
        {
            return DrawEnumToolbar(_window, _selected, _widthAsPercent, _selectedColor, DEFAULT_TOOLBAR_HEIGHT, TOOLBARS_TEXT_SIZE);
        }

        public static T DrawEnumToolbar<T>(this CustomEditorWindow _window, T _selected, float _widthAsPercent, Color _selectedColor, float _height, int _fontSize) where T : Enum
        {
            string[] _names = Enum.GetNames(typeof(T));
            int _buttonWidth = (int) ((_window.Width * _widthAsPercent) / _names.Length);
            Color _defaultGUIColor = GUI.backgroundColor;

            using (new HorizontalScope())
            {
                SmallSpace();
                FlexibleSpace();

                for (int i = 0; i < _names.Length; i++)
                {
                    T _current = (T) Enum.Parse(typeof(T), _names[i]);

                    Color _guiColor = _current.Equals(_selected) ? _selectedColor : DEFAULT_GRAY;
                    GUI.backgroundColor = _guiColor;

                    if (GUILayout.Button(_names[i].InsertSpaceBeforeUpperCaseAndNumeric(), ToolbarStyle.WithFontSize(_fontSize), FixedWidthAndHeight(_buttonWidth, _height)))
                    {
                        return _current;
                    }
                }

                FlexibleSpace();
                SmallSpace();
            }

            GUI.backgroundColor = _defaultGUIColor;

            return _selected;
        }

        public static T DrawEnumWithOrder<T>(T _value, string _label, ref bool _order, float _windowMargins) where T : Enum
        {
            T _mode = default;

            using (new HorizontalScope())
            {
                DrawDefaultLabel(_label);
                _mode = (T) EditorGUILayout.EnumPopup(_value, GUILayout.ExpandWidth(true));
                _order = GUILayout.Toggle(_order, string.Empty, Styles.FixedSortOrder(SORT_ORDER_HEIGHT), FixedWidthAndHeight(SORT_ORDER_HEIGHT));
            }

            return _mode;
        }

        public static void DrawDefaultLabel(string _label)
        {
            EditorGUILayout.LabelField(_label, FixedWidth(CurrentLabelWidth - 1f));
        }

        public static string DrawFolderPicker(string _label, string _folderPath, string _pickMessage, bool _requireInDataPath = true)
        {
            using (new HorizontalScope())
            {
                DrawDefaultLabel(_label);

                using (new DisabledScope())
                {
                    EditorGUILayout.TextField(AssetsUtilities.ConvertAbsolutePathToDataPath(_folderPath));
                }

                if (GUILayout.Button(DOTS, FixedWidth(20f)))
                {
                    string _newPath = EditorUtility.OpenFolderPanel(_pickMessage, Application.dataPath, string.Empty);

                    if (_requireInDataPath && _newPath.StartsWith(Application.dataPath) == false)
                    {
                        BaseLogger.Error(EDITOR_DRAWER, FOLDER_PATH_ERROR, RED);
                    }
                    else
                    {
                        _folderPath = _newPath;
                    }
                }
            }

            return _folderPath;
        }

        public static void DrawFolderPicker(SerializedProperty _property, string _label, string _pickMessage, bool _requireInDataPath = true)
        {
            using (new HorizontalScope())
            {
                DrawDefaultLabel(_label);

                string _folderPath = _property.stringValue;

                using (new DisabledScope())
                {
                    EditorGUILayout.TextField(AssetsUtilities.ConvertAbsolutePathToDataPath(_folderPath));
                }

                if (GUILayout.Button(DOTS, FixedWidth(20f)))
                {
                    string _newPath = EditorUtility.OpenFolderPanel(_pickMessage, Application.dataPath, string.Empty);

                    if (_requireInDataPath && _newPath.StartsWith(Application.dataPath) == false)
                    {
                        BaseLogger.Error(EDITOR_DRAWER, FOLDER_PATH_ERROR, RED);
                    }
                    else
                    {
                        _property.stringValue = _newPath;
                        _property.serializedObject.ApplyModifiedProperties();

                        GUIUtility.ExitGUI();
                    }
                }
            }
        }

        public static Rect GetLastRect()
        {
            return GUILayoutUtility.GetLastRect();
        }

        public static void DrawDebugRect()
        {
            Rect _rect = GetLastRect();
            DrawDebugRect(_rect, Color.green);
        }

        public static void DrawDebugRect(Color _color)
        {
            Rect _rect = GetLastRect();
            DrawDebugRect(_rect, _color);
        }

        public static void DrawDebugRect(Rect _rect)
        {
            DrawDebugRect(_rect, Color.green);
        }

        public static void DrawDebugRect(Rect _rect, Color _color, bool _transparent = true)
        {
            if (_transparent == false)
            {
                EditorGUI.DrawRect(_rect, _color);
                return;
            }

            Color _new = _color;
            _new.a = 0.2f;

            EditorGUI.DrawRect(_rect, _new);
        }

        public static bool DrawArrayProperty(SerializedProperty _arrayProperty, bool _drawAddElement, bool _enableGUI, ref Vector2 _scrollViewPosition, int _numberOfElementsToShow = int.MaxValue, string _label = "")
        {
            _numberOfElementsToShow = _numberOfElementsToShow > _arrayProperty.arraySize ? _arrayProperty.arraySize : _numberOfElementsToShow;

            if (_arrayProperty.isArray == false)
            {
                Debug.LogWarning("EDITOR DRAWER :: Trying to draw normal property as an array! Aborting");
                return false;
            }

            if (_label.IsNullEmptyOrWhitespace() == false)
            {
                DrawHeader(_label.EveryWordToUpper().InsertSpaceBeforeUpperCaseAndNumeric());
            }

            using (var _scope = new EditorGUI.ChangeCheckScope())
            {
                if (_arrayProperty.arraySize <= _numberOfElementsToShow)
                {
                    _drawArray(_arrayProperty, _enableGUI);
                }
                else
                {
                    float _maxHeight = _numberOfElementsToShow * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing + 1);

                    using (var _scrollScope = new GUILayout.ScrollViewScope(_scrollViewPosition, FixedHeight(_maxHeight)))
                    {
                        _drawArray(_arrayProperty, _enableGUI);
                        _scrollViewPosition = _scrollScope.scrollPosition;
                    }
                }

                if (_drawAddElement)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();

                        if (GUILayout.Button("Add new element", FixedWidth(180f)))
                        {
                            _arrayProperty.InsertArrayElementAtIndex(_arrayProperty.arraySize);
                        }
                    }
                }

                return _scope.changed;
            }

            static void _drawArray(SerializedProperty _arrayProperty, bool _enableGUI)
            {
                for (int i = 0; i < _arrayProperty.arraySize; i++)
                {
                    SerializedProperty _singleProperty = _arrayProperty.GetArrayElementAtIndex(i);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"#{i + 1}", FixedWidth(40f));

                        bool _guiEnabled = GUI.enabled;
                        GUI.enabled = _enableGUI;
                        EditorGUILayout.PropertyField(_singleProperty, GUIContent.none);
                        GUI.enabled = _guiEnabled;

                        if (GUILayout.Button("X", FixedWidth(20f)))
                        {
                            _arrayProperty.DeleteArrayElementAtIndex(i);
                        }
                    }
                }
            }
        }
    }
}