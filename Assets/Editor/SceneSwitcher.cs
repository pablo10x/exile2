using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

// Addressables
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

public class SceneSwitcher : EditorWindow
{
    private Vector2 scrollPos;
    private string searchFilter = "";

    [MenuItem("Tools/Scene Switcher")]
    public static void ShowWindow()
    {
        GetWindow<SceneSwitcher>("Scene Switcher");
    }

    private void OnGUI()
    {
        GUILayout.Space(5);
        GUILayout.Label("🔀 Scene Switcher", new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        });
        GUILayout.Space(5);

        // Search bar
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        searchFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarSearchField);
        if (GUILayout.Button("X", EditorStyles.toolbarButton, GUILayout.Width(20)))
        {
            searchFilter = "";
            GUI.FocusControl(null);
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(5);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // Find all scenes in Assets/Scenes
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });

        if (sceneGuids.Length == 0)
        {
            EditorGUILayout.HelpBox("No scenes found in Assets/Scenes", MessageType.Info);
        }

        foreach (string guid in sceneGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string sceneName = Path.GetFileNameWithoutExtension(path);

            // Apply search filter
            if (!string.IsNullOrEmpty(searchFilter) && !sceneName.ToLower().Contains(searchFilter.ToLower()))
                continue;

            // Box row
            GUIStyle boxStyle = new GUIStyle("box")
            {
                padding = new RectOffset(10, 10, 5, 5),
                margin = new RectOffset(5, 5, 5, 5)
            };

            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.BeginHorizontal();

            // Status dot
            bool isActive = (EditorSceneManager.GetActiveScene().path == path);
            string iconName = isActive ? "sv_icon_dot3_pix16_gizmo" : "sv_icon_dot0_pix16_gizmo";
            GUIContent dot = EditorGUIUtility.IconContent(iconName);
            GUILayout.Label(dot, GUILayout.Width(20), GUILayout.Height(20));

            // Scene name
            GUIStyle nameStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                fontStyle = isActive ? FontStyle.Bold : FontStyle.Normal,
                normal = { textColor = isActive ? Color.green : Color.white }
            };

            if (GUILayout.Button(sceneName, nameStyle))
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(path);
                }
            }

            // ✅ Addressable check
            string groupName;
            if (IsSceneAddressable(path, out groupName))
            {
                GUIStyle adrStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    fontSize = 10,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white },
                    padding = new RectOffset(4, 4, 2, 2)
                };

                Color oldColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.2f, 0.6f, 1f); // bluish
                GUILayout.Label(new GUIContent("ADR", $"Addressable Group: {groupName}"), adrStyle, GUILayout.Width(35), GUILayout.Height(18));
                GUI.backgroundColor = oldColor;
            }

            GUILayout.FlexibleSpace();

            // Load button
            GUIStyle loadButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fixedWidth = 60,
                fixedHeight = 22
            };

            if (GUILayout.Button("Load ➔", loadButtonStyle))
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(path);
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// Checks if the scene is marked as Addressable and returns its group name if so.
    /// </summary>
    private bool IsSceneAddressable(string assetPath, out string groupName)
    {
        groupName = null;

        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null) return false;

        string guid = AssetDatabase.AssetPathToGUID(assetPath);
        AddressableAssetEntry entry = settings.FindAssetEntry(guid);

        if (entry != null)
        {
            groupName = entry.parentGroup != null ? entry.parentGroup.Name : "Default";
            return true;
        }

        return false;
    }
}
