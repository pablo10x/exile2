namespace FewClicksDev.Core
{
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    public class ColorScope : GUI.Scope
    {
        private Color defaultColor = UnityEngine.GUI.color;
        private Color defaultContentColor = UnityEngine.GUI.contentColor;
        private Color defaultBackgroundColor = UnityEngine.GUI.backgroundColor;

        public ColorScope() { }

        public static ColorScope Background(Color _backgroundColor)
        {
            ColorScope _returnScope = new ColorScope();
            UnityEngine.GUI.backgroundColor = _backgroundColor;

            return _returnScope;
        }

        public static ColorScope GUI(Color _guiColor)
        {
            ColorScope _returnScope = new ColorScope();
            UnityEngine.GUI.color = _guiColor;

            return _returnScope;
        }

        public static ColorScope Content(Color _contentColor)
        {
            ColorScope _returnScope = new ColorScope();
            UnityEngine.GUI.contentColor = _contentColor;

            return _returnScope;
        }

        public static ColorScope BackgroundAndContent(Color _backgroundColor)
        {
            return BackgroundAndContent(_backgroundColor, _backgroundColor);
        }

        public static ColorScope BackgroundAndContent(Color _backgroundColor, Color _contentColor)
        {
            ColorScope _returnScope = new ColorScope();
            UnityEngine.GUI.backgroundColor = _backgroundColor;
            UnityEngine.GUI.contentColor = _contentColor;

            return _returnScope;
        }

        public static ColorScope Full(Color _mainColor)
        {
            ColorScope _returnScope = new ColorScope();
            UnityEngine.GUI.backgroundColor = _mainColor;
            UnityEngine.GUI.contentColor = _mainColor;
            UnityEngine.GUI.color = _mainColor;

            return _returnScope;
        }

        protected override void CloseScope()
        {
            UnityEngine.GUI.color = defaultColor;
            UnityEngine.GUI.contentColor = defaultContentColor;
            UnityEngine.GUI.backgroundColor = defaultBackgroundColor;
        }
    }

    public class ChangeCheckScope : EditorGUI.ChangeCheckScope
    {
        public static implicit operator bool(ChangeCheckScope _scope) => _scope.changed;
    }

    public class DisabledScope : GUI.Scope
    {
        private bool defaultGUIEnabled = true;

        public DisabledScope(bool _disabled = true)
        {
            defaultGUIEnabled = GUI.enabled;
            GUI.enabled = !_disabled;
        }

        protected override void CloseScope()
        {
            GUI.enabled = defaultGUIEnabled;
        }
    }

    public class HorizontalScope : EditorGUILayout.HorizontalScope
    {
        private int defaultIndent = 0;
        private float defaultLabelWidth = 0;
        private bool defaultGUIEnabled = true;

        public HorizontalScope(GUIStyle _style) : base(_style)
        {
            defaultIndent = EditorGUI.indentLevel;
            defaultLabelWidth = EditorGUIUtility.labelWidth;
            defaultGUIEnabled = GUI.enabled;
        }

        public HorizontalScope(params GUILayoutOption[] _layoutOptions) : base(_layoutOptions)
        {
            defaultIndent = EditorGUI.indentLevel;
            defaultLabelWidth = EditorGUIUtility.labelWidth;
            defaultGUIEnabled = GUI.enabled;
        }

        public HorizontalScope(GUIStyle _style, params GUILayoutOption[] _layoutOptions) : base(_style, _layoutOptions)
        {
            defaultIndent = EditorGUI.indentLevel;
            defaultLabelWidth = EditorGUIUtility.labelWidth;
            defaultGUIEnabled = GUI.enabled;
        }

        public HorizontalScope() : base()
        {
            defaultIndent = EditorGUI.indentLevel;
            defaultLabelWidth = EditorGUIUtility.labelWidth;
            defaultGUIEnabled = GUI.enabled;
        }

        protected override void CloseScope()
        {
            base.CloseScope();

            EditorGUI.indentLevel = defaultIndent;
            EditorGUIUtility.labelWidth = defaultLabelWidth;
            GUI.enabled = defaultGUIEnabled;
        }
    }

    public class VerticalScope : EditorGUILayout.VerticalScope
    {
        private int defaultIndent = 0;
        private float defaultLabelWidth = 0;
        private bool defaultGUIEnabled = true;

        public VerticalScope(GUIStyle _style) : base(_style)
        {
            defaultIndent = EditorGUI.indentLevel;
            defaultLabelWidth = EditorGUIUtility.labelWidth;
            defaultGUIEnabled = GUI.enabled;
        }

        public VerticalScope(params GUILayoutOption[] _layoutOptions) : base(_layoutOptions)
        {
            defaultIndent = EditorGUI.indentLevel;
            defaultLabelWidth = EditorGUIUtility.labelWidth;
            defaultGUIEnabled = GUI.enabled;
        }

        public VerticalScope(GUIStyle _style, params GUILayoutOption[] _layoutOptions) : base(_style, _layoutOptions)
        {
            defaultIndent = EditorGUI.indentLevel;
            defaultLabelWidth = EditorGUIUtility.labelWidth;
            defaultGUIEnabled = GUI.enabled;
        }

        public VerticalScope() : base()
        {
            defaultIndent = EditorGUI.indentLevel;
            defaultLabelWidth = EditorGUIUtility.labelWidth;
            defaultGUIEnabled = GUI.enabled;
        }

        protected override void CloseScope()
        {
            base.CloseScope();

            EditorGUI.indentLevel = defaultIndent;
            EditorGUIUtility.labelWidth = defaultLabelWidth;
            GUI.enabled = defaultGUIEnabled;
        }
    }

    public class LabelWidthScope : GUI.Scope
    {
        private float defaultLabelWidth = 100f;

        public LabelWidthScope(float _labelWidth)
        {
            defaultLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = _labelWidth;
        }

        protected override void CloseScope()
        {
            EditorGUIUtility.labelWidth = defaultLabelWidth;
        }
    }

    public class WideModeScope : GUI.Scope
    {
        private bool wideModeOn = false;

        public WideModeScope(bool _wideMode = true)
        {
            wideModeOn = EditorGUIUtility.wideMode;
            EditorGUIUtility.wideMode = _wideMode;
        }

        protected override void CloseScope()
        {
            EditorGUIUtility.wideMode = wideModeOn;
        }
    }

    public class IndentScope : GUI.Scope
    {
        public static IndentScope WithoutIndent => new IndentScope(-EditorGUI.indentLevel);

        private int defaultIndent = 0;

        public IndentScope(int _relativeIndent = 1)
        {
            defaultIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = defaultIndent + _relativeIndent;
        }

        protected override void CloseScope()
        {
            EditorGUI.indentLevel = defaultIndent;
        }
    }

    public class ScrollViewScope : GUILayout.ScrollViewScope
    {
        public ScrollViewScope(Vector2 _scrollPosition, params GUILayoutOption[] _options) : base(_scrollPosition, _options) { }
        public ScrollViewScope(Vector2 _scrollPosition, GUIStyle _style, params GUILayoutOption[] _options) : base(_scrollPosition, _style, _options) { }
        public ScrollViewScope(Vector2 _scrollPosition, bool _alwaysShowHorizontal, bool _alwaysShowVertical, params GUILayoutOption[] _options) : base(_scrollPosition, _alwaysShowHorizontal, _alwaysShowVertical, _options) { }
    }

    public class ScopeGroup : GUI.Scope
    {
        public static ScopeGroup HorizontalCheck => new ScopeGroup(new ChangeCheckScope(), new HorizontalScope());
        public static ScopeGroup VerticalCheck => new ScopeGroup(new ChangeCheckScope(), new VerticalScope());

        private GUI.Scope[] scopes = null;
        private List<EditorGUI.ChangeCheckScope> changeCheckScopes = new List<EditorGUI.ChangeCheckScope>();

        public bool Changed
        {
            get
            {
                for (int i = 0; i < changeCheckScopes.Count; i++)
                {
                    if (changeCheckScopes[i].changed)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public ScopeGroup(params GUI.Scope[] _scopes)
        {
            scopes = _scopes;

            if (_scopes != null)
            {
                for (int i = 0; i < _scopes.Length; i++)
                {
                    if (_scopes[i] is EditorGUI.ChangeCheckScope _checkScope)
                    {
                        changeCheckScopes.Add(_checkScope);
                    }
                }
            }
        }

        protected override void CloseScope()
        {
            if (scopes != null)
            {
                for (int i = scopes.Length - 1; i >= 0; i--) //Have to be disposed in reversed order
                {
                    scopes[i].Dispose();
                }
            }
        }

        public static implicit operator bool(ScopeGroup _scope) => _scope.Changed;
    }
}