// Terrain To Mesh <https://u3d.as/2x99>
// Copyright (c) Amazing Assets <https://amazingassets.world>

using UnityEngine;
using UnityEditor;


namespace AmazingAssets.TerrainToMesh.Editor
{
    [CustomEditor(typeof(ReadMe))]
    [InitializeOnLoad]
    public class ReadMeEditor : UnityEditor.Editor
    {
        const float k_Space = 16f;

        GUIStyle LinkStyle
        {
            get { return m_LinkStyle; }
        }
        [SerializeField]
        GUIStyle m_LinkStyle;

        GUIStyle TitleStyle
        {
            get { return m_TitleStyle; }
        }
        [SerializeField]
        GUIStyle m_TitleStyle;

        GUIStyle HeadingStyle
        {
            get { return m_HeadingStyle; }
        }
        [SerializeField]
        GUIStyle m_HeadingStyle;

        bool m_Initialized;


        protected override void OnHeaderGUI()
        {
            var readMe = (ReadMe)target;
            if (readMe.logo == null)
                readMe.logo = Texture2D.whiteTexture;

            Init();

            var iconWidth = Mathf.Min(EditorGUIUtility.currentViewWidth / 3f - 20f, 128f);

            GUILayout.BeginHorizontal("In BigTitle");
            {
                GUILayout.Space(k_Space);
                Rect logoRect = EditorGUILayout.GetControlRect(GUILayout.Width(iconWidth), GUILayout.Height(iconWidth));
                if (GUI.Button(logoRect, readMe.logo))
                    Application.OpenURL(TerrainToMeshAbout.storeURL);

                EditorGUIUtility.AddCursorRect(logoRect, MouseCursor.Link);

                GUILayout.Space(k_Space);
                GUILayout.BeginVertical();
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(TerrainToMeshAbout.name, TitleStyle);
                    GUILayout.Label("Version " + TerrainToMeshAbout.version, EditorStyles.centeredGreyMiniLabel);
                    GUILayout.FlexibleSpace();
                }

                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();
        }
        public override void OnInspectorGUI()
        {
            var readMe = (ReadMe)target;
            Init();

            if (readMe.sections == null || readMe.sections.Length != 5)
            {
                readMe.sections = new ReadMe.Section[] { new ReadMe.Section(),
                                                         new ReadMe.Section("Documentation", string.Empty, "Open online documentation", TerrainToMeshAbout.documentationURL),
                                                         new ReadMe.Section("Forum", string.Empty, "Get answers", TerrainToMeshAbout.forumURL),
                                                         new ReadMe.Section("Support and bug report", string.Empty, "Submit a report", TerrainToMeshAbout.supportMail, ReadMe.URLType.MailTo),
                                                         new ReadMe.Section("More Assets", string.Empty, "Open publisher page", TerrainToMeshAbout.publisherPage) };
            }

            foreach (var section in readMe.sections)
            {
                if (!string.IsNullOrEmpty(section.heading))
                {
                    GUILayout.Label(section.heading, HeadingStyle);
                }

                if (!string.IsNullOrEmpty(section.text))
                {
                    GUILayout.Label(section.text);
                }

                if (!string.IsNullOrEmpty(section.linkText))
                {
                    if (LinkLabel(new GUIContent(section.linkText)))
                    {
                        switch (section.urlType)
                        {
                            case ReadMe.URLType.OpenPage:
                                Application.OpenURL(section.url);
                                break;
                            case ReadMe.URLType.MailTo:
                                Application.OpenURL("mailto:" + section.url);
                                break;
                            default:
                                break;
                        }
                    }
                }

                GUILayout.Space(k_Space);
            }
        }


        void Init()
        {
            if (m_Initialized)
                return;


            m_TitleStyle = new GUIStyle(EditorStyles.boldLabel);
            m_TitleStyle.alignment = TextAnchor.MiddleCenter;
            m_TitleStyle.wordWrap = true;
            m_TitleStyle.fontSize = 18;

            m_HeadingStyle = new GUIStyle(EditorStyles.boldLabel);
            m_HeadingStyle.fontSize = 16;
            m_HeadingStyle.wordWrap = true;

            m_LinkStyle = new GUIStyle(EditorStyles.label);
            m_LinkStyle.richText = true;
            m_LinkStyle.fontSize = 13;

            // Match selection color which works nicely for both light and dark skins
            m_LinkStyle.normal.textColor = new Color(0x00 / 255f, 0x78 / 255f, 0xDA / 255f, 1f);
            m_LinkStyle.stretchWidth = false;

            m_Initialized = true;
        }
        bool LinkLabel(GUIContent label, params GUILayoutOption[] options)
        {
            var position = GUILayoutUtility.GetRect(label, LinkStyle, options);

            Handles.BeginGUI();
            Handles.color = LinkStyle.normal.textColor;
            Handles.DrawLine(new Vector3(position.xMin, position.yMax), new Vector3(position.xMax, position.yMax));
            Handles.color = Color.white;
            Handles.EndGUI();

            EditorGUIUtility.AddCursorRect(position, MouseCursor.Link);

            return GUI.Button(position, label, LinkStyle);
        }
    }
}