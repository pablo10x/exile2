#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace UtilityAI.Editor
{
    /// <summary>
    /// Utility AI Designer window.
    /// - Browse and create Action assets (derived from ActionBase)
    /// - Edit selected Action: name, weight, curve, considerations list
    /// - Add existing or create new Considerations (derived from ConsiderationBase)
    /// - Assign actions to selected UtilityAIController on a GameObject
    /// </summary>
    public class UtilityAIEditorWindow : EditorWindow
    {
        private Vector2 _leftScroll;
        private Vector2 _rightScroll;
        private string _search = string.Empty;

        // Default folder where newly created Utility AI actions/considerations will be placed
        private const string kDefaultAssetFolder = "Assets/_Assets/Scripts/Utility ai/Actions";

        private List<ActionBase> _allActions = new List<ActionBase>();
        private ActionBase _selectedAction;

        private SerializedObject _selectedSO;
        private SerializedProperty _propName;
        private SerializedProperty _propConsiderations;
        private SerializedProperty _propUseCurve;
        private SerializedProperty _propCurve;
        private SerializedProperty _propWeight;

        private ReorderableList _considerationsList;

        private GUIStyle _headerStyle;
        private Color _accent = new Color(0.2f, 0.65f, 0.95f);

        // Cache editors for considerations so we can render their inspectors inline
        private readonly Dictionary<ConsiderationBase, UnityEditor.Editor> _considerationEditorCache = new Dictionary<ConsiderationBase, UnityEditor.Editor>();

        [MenuItem("Window/AI/Utility AI Designer")] 
        public static void ShowWindow()
        {
            var wnd = GetWindow<UtilityAIEditorWindow>(false, "Utility AI", true);
            wnd.minSize = new Vector2(840, 420);
            wnd.RefreshDatabase();
            wnd.Show();
        }

        private void OnEnable()
        {
            RefreshDatabase();
            SetupStyles();
        }

        private void SetupStyles()
        {
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13
            };
        }

        private void RefreshDatabase()
        {
            string[] guids = AssetDatabase.FindAssets("t:ActionBase");
            _allActions = guids
                .Select(g => AssetDatabase.GUIDToAssetPath(g))
                .Select(AssetDatabase.LoadAssetAtPath<ActionBase>)
                .Where(a => a != null)
                .OrderBy(a => a.name)
                .ToList();

            // Re-select asset if it exists
            if (_selectedAction == null && _allActions.Count > 0)
                SelectAction(_allActions[0]);
            else if (_selectedAction != null && !_allActions.Contains(_selectedAction))
                _selectedAction = null;
        }

        private void OnGUI()
        {
            DrawToolbar();
            var rect = position;
            rect.y = 20; rect.height -= 20;

            EditorGUILayout.BeginHorizontal();
            DrawLeftPanel(rect);
            DrawRightPanel(rect);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                // Search
                GUILayout.Label("Actions", EditorStyles.toolbarButton, GUILayout.Width(60));
                _search = GUILayout.TextField(_search, GUI.skin.FindStyle("ToolbarSeachTextField") ?? EditorStyles.toolbarTextField);

                if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(50)))
                    _search = string.Empty;

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("New Action", EditorStyles.toolbarButton, GUILayout.Width(90)))
                {
                    ShowCreateActionMenu();
                }

                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
                {
                    RefreshDatabase();
                }
            }
        }

        private void DrawLeftPanel(Rect fullRect)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(280)))
            {
                _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll, GUILayout.ExpandHeight(true));
                var filtered = string.IsNullOrWhiteSpace(_search) ? _allActions : _allActions.Where(a => a.name.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

                foreach (var action in filtered)
                {
                    DrawActionListItem(action);
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawActionListItem(ActionBase action)
        {
            var isSelected = action == _selectedAction;
            var bg = isSelected ? new Color(0.25f, 0.5f, 0.8f, 0.25f) : new Color(0, 0, 0, 0);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                var rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(24), GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(rect, bg);

                var labelRect = new Rect(rect.x + 6, rect.y + 3, rect.width - 140, rect.height - 6);
                EditorGUI.LabelField(labelRect, action.name, EditorStyles.boldLabel);

                var selRect = new Rect(rect.xMax - 130, rect.y + 2, 60, rect.height - 4);
                if (GUI.Button(selRect, isSelected ? "Selected" : "Select"))
                    SelectAction(action);

                var pingRect = new Rect(rect.xMax - 65, rect.y + 2, 60, rect.height - 4);
                if (GUI.Button(pingRect, "Ping"))
                    EditorGUIUtility.PingObject(action);
            }
        }

        private void SelectAction(ActionBase action)
        {
            _selectedAction = action;
            // Clear cached editors when switching selection to avoid stale refs
            _considerationEditorCache.Clear();
            if (_selectedAction != null)
            {
                _selectedSO = new SerializedObject(_selectedAction);
                _propName = _selectedSO.FindProperty("_name");
                _propConsiderations = _selectedSO.FindProperty("considerations");
                _propUseCurve = _selectedSO.FindProperty("useCurve");
                _propCurve = _selectedSO.FindProperty("scoreCurve");
                _propWeight = _selectedSO.FindProperty("weight");

                _considerationsList = new ReorderableList(_selectedSO, _propConsiderations, true, true, true, true);
                _considerationsList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Considerations");
                _considerationsList.drawElementCallback = DrawConsiderationElement;
                _considerationsList.onAddDropdownCallback = OnAddConsiderationDropdown;
                _considerationsList.onSelectCallback = _ => Repaint();
            }
            else
            {
                _selectedSO = null;
                _considerationsList = null;
            }

            Repaint();
        }

        private void DrawRightPanel(Rect fullRect)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
            {
                if (_selectedAction == null)
                {
                    DrawWelcome();
                    return;
                }

                _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll);

                // Header
                using (new EditorGUILayout.HorizontalScope())
                {
                    var c = GUI.color; GUI.color = _accent;
                    GUILayout.Label("Action Details", _headerStyle);
                    GUI.color = c;
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Duplicate", GUILayout.Width(90))) DuplicateSelectedAction();
                    if (GUILayout.Button("Delete", GUILayout.Width(70))) DeleteSelectedAction();
                }

                EditorGUILayout.Space(6);

                // Action properties
                EditorGUI.BeginChangeCheck();
                _selectedSO.Update();
                EditorGUILayout.PropertyField(_propName, new GUIContent("Display Name"));
                EditorGUILayout.PropertyField(_propWeight);
                EditorGUILayout.PropertyField(_propUseCurve);
                if (_propUseCurve.boolValue)
                {
                    EditorGUILayout.PropertyField(_propCurve);
                }
                EditorGUILayout.Space();

                // Considerations list
                _considerationsList?.DoLayoutList();

                // Inline editor for the currently selected consideration
                DrawSelectedConsiderationInspector();

                _selectedSO.ApplyModifiedProperties();
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(_selectedAction);
                }

                EditorGUILayout.Space(8);
                DrawControllerAssignSection();

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawWelcome()
        {
            GUILayout.FlexibleSpace();
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new GUILayout.VerticalScope())
                {
                    var c = GUI.color; GUI.color = _accent;
                    GUILayout.Label("Utility AI Designer", new GUIStyle(EditorStyles.largeLabel) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter });
                    GUI.color = c;
                    GUILayout.Space(6);
                    GUILayout.Label("Create and edit actions, manage considerations, and assign them to your AI controllers.", new GUIStyle(EditorStyles.wordWrappedLabel) { alignment = TextAnchor.MiddleCenter, richText = true }, GUILayout.Width(460));
                    GUILayout.Space(10);
                    if (GUILayout.Button("Create New Action", GUILayout.Width(200), GUILayout.Height(28)))
                        ShowCreateActionMenu();
                }
                GUILayout.FlexibleSpace();
            }
            GUILayout.FlexibleSpace();
        }

        private void DrawConsiderationElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = _propConsiderations.GetArrayElementAtIndex(index);
            var objProp = element;

            rect.y += 2; rect.height = EditorGUIUtility.singleLineHeight;

            var label = new Rect(rect.x, rect.y, 20, rect.height);
            EditorGUI.LabelField(label, index.ToString());

            var fieldRect = new Rect(rect.x + 26, rect.y, rect.width - 26 - 210, rect.height);
            EditorGUI.PropertyField(fieldRect, objProp, GUIContent.none);

            var editRect = new Rect(rect.x + rect.width - 210, rect.y, 60, rect.height);
            var pingRect = new Rect(rect.x + rect.width - 146, rect.y, 60, rect.height);
            var removeRect = new Rect(rect.x + rect.width - 82, rect.y, 80, rect.height);

            using (new EditorGUI.DisabledGroupScope(objProp.objectReferenceValue == null))
            {
                if (GUI.Button(editRect, "Edit"))
                {
                    Selection.activeObject = objProp.objectReferenceValue;
                }
                if (GUI.Button(pingRect, "Ping"))
                {
                    EditorGUIUtility.PingObject(objProp.objectReferenceValue);
                }
            }
            if (GUI.Button(removeRect, "Remove"))
            {
                _propConsiderations.DeleteArrayElementAtIndex(index);
                _selectedSO.ApplyModifiedProperties();
            }
        }

        private void OnAddConsiderationDropdown(Rect buttonRect, ReorderableList list)
        {
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("Create New/From Type..."), false, () =>
            {
                CreateNewConsiderationViaPicker(addToAction: true);
            });

            menu.AddSeparator("");

            // Existing assets in project
            string[] guids = AssetDatabase.FindAssets("t:ConsiderationBase");
            if (guids.Length == 0)
            {
                menu.AddDisabledItem(new GUIContent("No existing consideration assets found"));
            }
            else
            {
                foreach (var g in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(g);
                    var asset = AssetDatabase.LoadAssetAtPath<ConsiderationBase>(path);
                    if (asset == null) continue;
                    menu.AddItem(new GUIContent($"Add Existing/{asset.name}"), false, () =>
                    {
                        int newIndex = _propConsiderations.arraySize;
                        _propConsiderations.InsertArrayElementAtIndex(newIndex);
                        _propConsiderations.GetArrayElementAtIndex(newIndex).objectReferenceValue = asset;
                        _selectedSO.ApplyModifiedProperties();
                    });
                }
            }

            menu.DropDown(buttonRect);
        }

        private void ShowCreateActionMenu()
        {
            var menu = new GenericMenu();
            var types = TypeCache.GetTypesDerivedFrom<ActionBase>().Where(t => !t.IsAbstract).OrderBy(t => t.Name);
            foreach (var t in types)
            {
                menu.AddItem(new GUIContent(t.Name), false, () => CreateActionAsset(t));
            }
            menu.ShowAsContext();
        }

        private void CreateActionAsset(Type t)
        {
            string folder = GetSelectedFolderOrFallback();
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{ObjectNames.NicifyVariableName(t.Name)}.asset");
            var asset = CreateInstance(t);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshDatabase();
            SelectAction(asset as ActionBase);
            EditorGUIUtility.PingObject(asset);
        }

        private void CreateNewConsiderationViaPicker(bool addToAction)
        {
            var menu = new GenericMenu();
            var types = TypeCache.GetTypesDerivedFrom<ConsiderationBase>().Where(t => !t.IsAbstract).OrderBy(t => t.Name);
            foreach (var t in types)
            {
                menu.AddItem(new GUIContent(t.Name), false, () =>
                {
                    string folder = GetSelectedFolderOrFallback();
                    string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{ObjectNames.NicifyVariableName(t.Name)}.asset");
                    var asset = CreateInstance(t);
                    AssetDatabase.CreateAsset(asset, path);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    EditorGUIUtility.PingObject(asset);

                    if (addToAction && _selectedSO != null)
                    {
                        _selectedSO.Update();
                        int newIndex = _propConsiderations.arraySize;
                        _propConsiderations.InsertArrayElementAtIndex(newIndex);
                        _propConsiderations.GetArrayElementAtIndex(newIndex).objectReferenceValue = asset;
                        _selectedSO.ApplyModifiedProperties();
                    }
                });
            }
            menu.ShowAsContext();
        }

        private string GetSelectedFolderOrFallback()
        {
            // If user has a folder selected in the Project view, use it.
            var obj = Selection.activeObject;
            var path = obj != null ? AssetDatabase.GetAssetPath(obj) : null;

            string result;
            if (string.IsNullOrEmpty(path))
            {
                result = kDefaultAssetFolder;
            }
            else if (System.IO.Directory.Exists(path))
            {
                result = path; // folder selected
            }
            else
            {
                var dir = System.IO.Path.GetDirectoryName(path);
                result = string.IsNullOrEmpty(dir) ? kDefaultAssetFolder : dir.Replace("\\", "/");
            }

            // Ensure the folder exists (create if necessary).
            EnsureFolderExists(result);
            return result;
        }

        private static void EnsureFolderExists(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath)) return;

            // Normalize slashes
            folderPath = folderPath.Replace("\\", "/");

            if (AssetDatabase.IsValidFolder(folderPath)) return;

            // Create nested folders starting from Assets
            var parts = folderPath.Split('/');
            if (parts.Length == 0 || parts[0] != "Assets") return;

            string current = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
            AssetDatabase.Refresh();
        }

        private void DuplicateSelectedAction()
        {
            if (_selectedAction == null) return;
            string srcPath = AssetDatabase.GetAssetPath(_selectedAction);
            string dstPath = AssetDatabase.GenerateUniqueAssetPath(srcPath);
            if (AssetDatabase.CopyAsset(srcPath, dstPath))
            {
                AssetDatabase.SaveAssets();
                RefreshDatabase();
                var copy = AssetDatabase.LoadAssetAtPath<ActionBase>(dstPath);
                SelectAction(copy);
                EditorGUIUtility.PingObject(copy);
            }
        }

        private void DeleteSelectedAction()
        {
            if (_selectedAction == null) return;
            if (EditorUtility.DisplayDialog("Delete Action", $"Are you sure you want to delete '{_selectedAction.name}'?", "Delete", "Cancel"))
            {
                string path = AssetDatabase.GetAssetPath(_selectedAction);
                SelectAction(null);
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.SaveAssets();
                RefreshDatabase();
            }
        }

        private void DrawControllerAssignSection()
        {
            EditorGUILayout.Space(10);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Label("Assign To Controller", _headerStyle);

                var go = Selection.activeGameObject;
                if (go == null)
                {
                    EditorGUILayout.HelpBox("Select a GameObject with UtilityAIController to assign actions.", MessageType.Info);
                    return;
                }

                var ctrl = go.GetComponent<UtilityAIController>();
                if (ctrl == null)
                {
                    EditorGUILayout.HelpBox($"'{go.name}' has no UtilityAIController.", MessageType.Warning);
                    if (GUILayout.Button("Add UtilityAIController"))
                    {
                        Undo.AddComponent<UtilityAIController>(go);
                    }
                    return;
                }

                var so = new SerializedObject(ctrl);
                var actionsProp = so.FindProperty("actions");
                EditorGUILayout.PropertyField(so.FindProperty("context"));
                EditorGUILayout.PropertyField(so.FindProperty("evaluateEveryUpdate"));
                if (!so.FindProperty("evaluateEveryUpdate").boolValue)
                {
                    EditorGUILayout.PropertyField(so.FindProperty("evaluateInterval"));
                }

                EditorGUILayout.Space(4);
                GUILayout.Label("Controller Actions", EditorStyles.boldLabel);

                // Display list of actions
                int removeIndex = -1;
                for (int i = 0; i < actionsProp.arraySize; i++)
                {
                    var elem = actionsProp.GetArrayElementAtIndex(i);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(elem, GUIContent.none);
                        if (GUILayout.Button("Ping", GUILayout.Width(44)))
                            EditorGUIUtility.PingObject(elem.objectReferenceValue);
                        if (GUILayout.Button("X", GUILayout.Width(24)))
                            removeIndex = i;
                    }
                }
                if (removeIndex >= 0)
                {
                    actionsProp.DeleteArrayElementAtIndex(removeIndex);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Add Selected Action"))
                    {
                        int newIdx = actionsProp.arraySize;
                        actionsProp.InsertArrayElementAtIndex(newIdx);
                        actionsProp.GetArrayElementAtIndex(newIdx).objectReferenceValue = _selectedAction;
                    }
                    if (GUILayout.Button("Add Existing..."))
                    {
                        var path = EditorUtility.OpenFilePanel("Select Action Asset", Application.dataPath, "asset");
                        if (!string.IsNullOrEmpty(path))
                        {
                            var relative = "Assets" + path.Replace(Application.dataPath, string.Empty);
                            var asset = AssetDatabase.LoadAssetAtPath<ActionBase>(relative);
                            if (asset != null)
                            {
                                int newIdx = actionsProp.arraySize;
                                actionsProp.InsertArrayElementAtIndex(newIdx);
                                actionsProp.GetArrayElementAtIndex(newIdx).objectReferenceValue = asset;
                            }
                        }
                    }
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Apply", GUILayout.Width(80)))
                    {
                        so.ApplyModifiedProperties();
                        EditorUtility.SetDirty(ctrl);
                    }
                }

                so.ApplyModifiedProperties();
            }
        }

        private UnityEditor.Editor GetConsiderationEditor(ConsiderationBase cons)
        {
            if (cons == null) return null;
            if (_considerationEditorCache.TryGetValue(cons, out var ed) && ed != null)
                return ed;
            var newEd = UnityEditor.Editor.CreateEditor(cons);
            _considerationEditorCache[cons] = newEd;
            return newEd;
        }

        private void DrawSelectedConsiderationInspector()
        {
            if (_considerationsList == null || _propConsiderations == null) return;
            int idx = _considerationsList.index;
            if (idx < 0 || idx >= _propConsiderations.arraySize) return;

            var elem = _propConsiderations.GetArrayElementAtIndex(idx);
            var cons = elem.objectReferenceValue as ConsiderationBase;
            if (cons == null) return;

            EditorGUILayout.Space(6);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Label("Selected Consideration", _headerStyle);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(cons.name, EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                        Selection.activeObject = cons;
                    if (GUILayout.Button("Ping", GUILayout.Width(50)))
                        EditorGUIUtility.PingObject(cons);
                }

                var ed = GetConsiderationEditor(cons);
                if (ed != null)
                {
                    ed.OnInspectorGUI();
                    if (GUI.changed)
                        EditorUtility.SetDirty(cons);
                }
            }
        }
    }

    /// <summary>
    /// Simple custom inspectors to make runtime assets look nicer in inspector as well.
    /// </summary>
    [CustomEditor(typeof(ActionBase), true)]
    public class ActionBaseEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }
    }

    [CustomEditor(typeof(ConsiderationBase), true)]
    public class ConsiderationBaseEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }
    }
}
#endif
