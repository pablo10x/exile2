namespace FewClicksDev.Core.Versioning
{
    using UnityEditor;
    using UnityEngine;

    using static FewClicksDev.Core.EditorDrawer;

    [CustomEditor(typeof(Changelog))]
    public class ChangelogEditor : CustomInspectorBase
    {
        public enum InspectorMode
        {
            Preview = 0,
            Edit = 1
        }

        private const float ICON_SIZE = 86f;
        private const string CHANGE_SYMBOL = "» ";
        private const string EDIT = "Edit";
        private const string PREVIEW = "Preview";
        private const string PACKAGE_INFORMATION_NOT_SET = "Package Information is not set!";
        private const string ICON_NOT_SET_INFO = "Tool Icon is not set!";

        private Changelog changelog = null;
        private PackageInformation packageInformation = null;

        private SerializedProperty packageInformationProperty = null;
        private SerializedProperty versionsProperty = null;

        private InspectorMode inspectorMode = InspectorMode.Preview;

        protected override void OnEnable()
        {
            base.OnEnable();

            changelog = target as Changelog;

            packageInformationProperty = serializedObject.FindProperty("packageInformation");
            versionsProperty = serializedObject.FindProperty("versions");

            packageInformation = packageInformationProperty.objectReferenceValue as PackageInformation;
        }

        protected override void drawInspectorGUI()
        {
            switch (inspectorMode)
            {
                case InspectorMode.Preview:
                    drawPreviewMode();
                    break;

                case InspectorMode.Edit:
                    drawDefaultInspector();
                    break;
            }

            float _buttonWidth = windowWidthWithPaddings / 2f;
            string _label = inspectorMode == InspectorMode.Preview ? EDIT : PREVIEW;

            LargeSpace();

            using (new HorizontalScope())
            {
                FlexibleSpace();

                if (DrawBoxButton(_label, FixedWidthAndHeight(_buttonWidth, DEFAULT_LINE_HEIGHT)))
                {
                    inspectorMode = inspectorMode is InspectorMode.Preview ? InspectorMode.Edit : InspectorMode.Preview;
                }

                FlexibleSpace();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void drawPreviewMode()
        {
            drawScript();
            NormalSpace();

            if (packageInformation == null)
            {
                EditorGUILayout.HelpBox(PACKAGE_INFORMATION_NOT_SET, MessageType.Warning);
                return;
            }

            if (packageInformation.ToolIcon == null)
            {
                EditorGUILayout.HelpBox(ICON_NOT_SET_INFO, MessageType.Warning);
                return;
            }

            Color _toolMainColor = packageInformation.ToolMainColor;

            using (new HorizontalScope(Styles.BoxButton, FixedHeight(ICON_SIZE + 2 * NORMAL_SPACE)))
            {
                NormalSpace();

                using (new VerticalScope())
                {
                    NormalSpace();
                    GUIStyle _toolIconStyle = new GUIStyle(EditorStyles.label);
                    _toolIconStyle.SetBackgroundForAllStates(packageInformation.ToolIcon);

                    EditorGUILayout.LabelField(GUIContent.none, _toolIconStyle, FixedWidthAndHeight(ICON_SIZE));
                }

                LargeSpace();

                using (new VerticalScope())
                {
                    NormalSpace();

                    using (ColorScope.Content(_toolMainColor))
                    {
                        EditorGUILayout.LabelField(packageInformation.ToolName, EditorStyles.boldLabel.WithFontSize(15).WithColor(Color.white));
                    }

                    NormalSpace();

                    GUIStyle _style = new GUIStyle(EditorStyles.wordWrappedLabel);
                    _style.richText = true;

                    EditorGUILayout.LabelField(packageInformation.ToolDescription, _style);
                    SmallSpace();
                }

                NormalSpace();
            }

            LargeSpace();
            GUIStyle _biggerLabelStyle = new GUIStyle(EditorStyles.boldLabel).WithFontSize(14);

            foreach (var _version in changelog.Versions)
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

                LargeSpace();
            }
        }
    }
}
