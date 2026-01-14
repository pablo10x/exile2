namespace FewClicksDev.Core.Versioning
{
    using FewClicksDev.Core;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    using static FewClicksDev.Core.EditorDrawer;

    public class ChangelogWindow : CustomEditorWindow
    {
        public enum WindowMode
        {
            Update = 0,
            OtherTools = 1
        }

        private List<Changelog> changelogsToShow = new List<Changelog>();
        private PackageInformation[] allPackages = null;

        private const float MAIN_TOOLBAR_WIDTH = 0.775f;
        private const float ICON_SIZE = 64f;

        private const string CHANGE_SYMBOL = "» ";
        private const string LEFT_ARROWS = "<<";
        private const string RIGHT_ARROWS = ">>";
        private const string GO_TO_THE_ASSET_STORE = "Go to the Asset Store Page";
        private const string FREE = "(free)";
        private const string OPEN = "Open";
        private const string AVAILABLE_SOON = "(Available soon)";
        private const string CHANGELOG = "CHANGELOG";

        protected override string windowName => "Changelog";
        protected override string version => "1.0.0";
        protected override Vector2 minWindowSize => new Vector2(520f, 720f);
        protected override Color mainColor => new Color(0.15f, 0.15f, 0.15f, 1f);

        private WindowMode windowMode = WindowMode.Update;
        private int currentChangelogIndex = 0;
        private Vector2 updateTabScrollPosition = Vector2.zero;
        private Vector2 otherToolsScrollPosition = Vector2.zero;

        protected override void drawWindowGUI()
        {
            NormalSpace();
            windowMode = this.DrawEnumToolbar(windowMode, MAIN_TOOLBAR_WIDTH, mainColor);

            SmallSpace();
            DrawLine();
            SmallSpace();

            switch (windowMode)
            {
                case WindowMode.Update:
                    drawUpdateTab();
                    break;

                case WindowMode.OtherTools:

                    if (allPackages.IsNullOrEmpty())
                    {
                        findAllPackages();
                    }

                    using (var _scroll = new ScrollViewScope(otherToolsScrollPosition))
                    {
                        otherToolsScrollPosition = _scroll.scrollPosition;
                        drawOtherToolsTab();
                    }

                    break;
            }
        }

        private void drawUpdateTab()
        {
            if (changelogsToShow.IsNullOrEmpty())
            {
                addAllChangelogs();

                if (changelogsToShow.IsNullOrEmpty())
                {
                    return;
                }
            }

            currentChangelogIndex = Mathf.Clamp(currentChangelogIndex, 0, changelogsToShow.Count - 1);

            Color _color = changelogsToShow[currentChangelogIndex].PackageInfo.ToolMainColor;
            string _label = $"{changelogsToShow[currentChangelogIndex].PackageInfo.ToolName}";

            NormalSpace();
            drawPackage(changelogsToShow[currentChangelogIndex].PackageInfo);
            SmallSpace();

            using (new HorizontalScope())
            {
                if (changelogsToShow.Count > 1)
                {
                    FlexibleSpace();

                    if (DrawBoxButton(LEFT_ARROWS, FixedWidthAndHeight(30f, DEFAULT_LINE_HEIGHT)))
                    {
                        currentChangelogIndex--;

                        if (currentChangelogIndex < 0)
                        {
                            currentChangelogIndex = changelogsToShow.Count - 1;
                        }
                    }

                    SmallSpace();

                    if (DrawBoxButton(RIGHT_ARROWS, FixedWidthAndHeight(30f, DEFAULT_LINE_HEIGHT)))
                    {
                        currentChangelogIndex++;

                        if (currentChangelogIndex >= changelogsToShow.Count)
                        {
                            currentChangelogIndex = 0;
                        }
                    }
                }
            }

            NormalSpace();
            DrawCenteredBoldLabel(CHANGELOG);
            drawChangelog(changelogsToShow[currentChangelogIndex]);
        }

        private void drawChangelog(Changelog _changelog)
        {
            SmallSpace();
            DrawLine(1f);
            GUIStyle _biggerLabelStyle = new GUIStyle(EditorStyles.boldLabel).WithFontSize(14);

            using (var _scroll = new ScrollViewScope(updateTabScrollPosition))
            {
                updateTabScrollPosition = _scroll.scrollPosition;

                foreach (var _version in _changelog.Versions)
                {
                    EditorGUILayout.LabelField(_version.VersionString, _biggerLabelStyle);
                    SmallSpace();

                    using (new IndentScope())
                    {
                        foreach (var _change in _version.Changes)
                        {
                            EditorGUILayout.LabelField(CHANGE_SYMBOL + _change, EditorStyles.wordWrappedLabel);
                        }
                    }

                    NormalSpace();
                }
            }

            SmallSpace();
        }

        private void drawOtherToolsTab()
        {
            foreach (var _package in allPackages)
            {
                if (_package == null)
                {
                    continue;
                }

                using (new HorizontalScope())
                {
                    drawPackage(_package);
                    SmallSpace();
                }

                SmallSpace();
            }
        }

        private static void drawPackage(PackageInformation _package)
        {
            Color _toolMainColor = _package.ToolMainColor;

            using (new HorizontalScope(Styles.NotInteractiveBoxButton, FixedHeight(ICON_SIZE + 2 * NORMAL_SPACE)))
            {
                NormalSpace();

                using (new VerticalScope(FixedWidth(ICON_SIZE)))
                {
                    NormalSpace();
                    GUIStyle _toolIconStyle = new GUIStyle(EditorStyles.label);
                    _toolIconStyle.SetBackgroundForAllStates(_package.ToolIcon);

                    EditorGUILayout.LabelField(GUIContent.none, _toolIconStyle, FixedWidthAndHeight(ICON_SIZE));

                    if (_package.IsFree)
                    {
                        EditorGUILayout.LabelField(FREE, EditorStyles.centeredGreyMiniLabel, FixedWidth(ICON_SIZE));
                        SmallSpace();
                    }
                }

                LargeSpace();

                using (new VerticalScope())
                {
                    NormalSpace();

                    using (ColorScope.Content(_toolMainColor))
                    {
                        EditorGUILayout.LabelField(_package.ToolName, EditorStyles.boldLabel.WithFontSize(15).WithColor(Color.white));
                    }

                    NormalSpace();

                    GUIStyle _style = new GUIStyle(EditorStyles.wordWrappedLabel);
                    _style.richText = true;

                    EditorGUILayout.LabelField(_package.ToolDescription, _style);
                    SmallSpace();
                    DrawLine(1f);
                    SmallSpace();

                    using (new HorizontalScope())
                    {
                        FlexibleSpace();

                        if (_package.IsLinkValid)
                        {
                            if (DrawBoxButton(GO_TO_THE_ASSET_STORE, FixedWidthAndHeight(180f, DEFAULT_LINE_HEIGHT)))
                            {
                                _package.OpenAssetStoreLink();
                            }
                        }
                        else
                        {
                            GUILayout.Label(AVAILABLE_SOON, EditorStyles.label.WithTextAlignment(TextAnchor.MiddleRight), FixedWidthAndHeight(180f, DEFAULT_LINE_HEIGHT));
                        }

                        if (_package.IsInstalled)
                        {
                            NormalSpace();

                            if (DrawClearBoxButton(OPEN, _package.ToolMainColor, FixedWidthAndHeight(80f, DEFAULT_LINE_HEIGHT)))
                            {
                                _package.OpenWindow();
                            }
                        }
                    }

                    NormalSpace();
                }

                NormalSpace();
                FlexibleSpace();
            }
        }

        private void findAllPackages()
        {
            allPackages = AssetsUtilities.GetAssetsOfType<PackageInformation>();

            foreach (var _package in allPackages)
            {
                _package?.CacheIsInstalled();
            }
        }

        private void addAllChangelogs()
        {
            Changelog[] _allChangelogs = AssetsUtilities.GetAssetsOfType<Changelog>();

            foreach (var _changelog in _allChangelogs)
            {
                addChangelogToShow(_changelog);
            }
        }

        private void addChangelogToShow(Changelog _changelog)
        {
            changelogsToShow.AddUnique(_changelog);
        }

        public static void ShowChangelog(Changelog _changelog)
        {
            if (_changelog == null)
            {
                return;
            }

            if (HasOpenInstances<ChangelogWindow>())
            {
                GetWindow<ChangelogWindow>().addChangelogToShow(_changelog);
            }
            else
            {
                var _newWindow = GetWindow<ChangelogWindow>();
                _newWindow.findAllPackages();
                _newWindow.Show();
            }
        }

        [MenuItem("Window/FewClicks Dev/Changelogs", priority = 150)]
        public static void OpenWindow()
        {
            var _window = GetWindow<ChangelogWindow>();
            _window.addAllChangelogs();
            _window.findAllPackages();
            _window.Show();
        }
    }
}