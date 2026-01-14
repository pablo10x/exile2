using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace core.ui {
    
    public class CanvasGroupTogglerManager : EditorWindow {
        
        private Vector2 scrollPosition;
        private List<CanvasGroupToggler> rootTogglers = new List<CanvasGroupToggler>();
        private Dictionary<CanvasGroupToggler, bool> foldoutStates = new Dictionary<CanvasGroupToggler, bool>();
        private GUIStyle headerStyle;
        private GUIStyle toggleButtonStyle;
        private GUIStyle labelStyle;
        private bool stylesInitialized = false;
        
        [MenuItem("Tools/Canvas Group Manager")]
        public static void ShowWindow() {
            var window = GetWindow<CanvasGroupTogglerManager>("Panel Manager");
            window.minSize = new Vector2(400, 300);
        }
        
        private void OnEnable() {
            RefreshTogglers();
        }
        
        private void OnFocus() {
            RefreshTogglers();
        }
        
        private void InitializeStyles() {
            if (stylesInitialized) return;
            
            // Header style
            headerStyle = new GUIStyle(EditorStyles.boldLabel) {
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(5, 5, 5, 5)
            };
            
            // Toggle button style
            toggleButtonStyle = new GUIStyle(GUI.skin.button) {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                fixedHeight = 28
            };
            
            // Label style
            labelStyle = new GUIStyle(EditorStyles.label) {
                fontSize = 12
            };
            
            stylesInitialized = true;
        }
        
        private void RefreshTogglers() {
            // Get all togglers in the scene, including inactive ones
            var allTogglers = FindObjectsByType<CanvasGroupToggler>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            // Find root togglers (those without a toggler parent)
            rootTogglers = allTogglers
                .Where(t => !HasTogglerParent(t))
                .OrderBy(t => t.gameObject.name)
                .ToList();
        }
        
        private bool HasTogglerParent(CanvasGroupToggler toggler) {
            Transform parent = toggler.transform.parent;
            while (parent != null) {
                if (parent.GetComponent<CanvasGroupToggler>() != null) {
                    return true;
                }
                parent = parent.parent;
            }
            return false;
        }
        
        private List<CanvasGroupToggler> GetChildTogglers(CanvasGroupToggler parent) {
            List<CanvasGroupToggler> children = new List<CanvasGroupToggler>();
            foreach (Transform child in parent.transform) {
                var toggler = child.GetComponent<CanvasGroupToggler>();
                if (toggler != null) {
                    children.Add(toggler);
                }
                // Recursively search deeper
                children.AddRange(GetChildTogglersRecursive(child));
            }
            return children.OrderBy(t => t.gameObject.name).ToList();
        }
        
        private List<CanvasGroupToggler> GetChildTogglersRecursive(Transform parent) {
            List<CanvasGroupToggler> children = new List<CanvasGroupToggler>();
            foreach (Transform child in parent.transform) {
                var toggler = child.GetComponent<CanvasGroupToggler>();
                if (toggler != null) {
                    children.Add(toggler);
                }
                children.AddRange(GetChildTogglersRecursive(child));
            }
            return children;
        }

        private int CountAllTogglers() {
            return FindObjectsByType<CanvasGroupToggler>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Length;
        }

        
        private void OnGUI() {
            InitializeStyles();
            
            // Header
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Canvas Group Panel Manager", headerStyle);
            
            // Toolbar
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Refresh", GUILayout.Height(24))) {
                RefreshTogglers();
            }
            
            if (GUILayout.Button("Enable All", GUILayout.Height(24))) {
                var togglers = FindObjectsByType<CanvasGroupToggler>(
                    FindObjectsInactive.Exclude,   // or Include if you want inactive as well
                    FindObjectsSortMode.None
                );

                foreach (var toggler in togglers) {
                    toggler.Disable();
                }
            }

            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Panel count
            int totalCount = CountAllTogglers();
            EditorGUILayout.LabelField($"Panels: {totalCount} total, {rootTogglers.Count} root", 
                EditorStyles.miniLabel);
            
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space(5);
            
            // Scroll view with panels
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            if (rootTogglers.Count == 0) {
                EditorGUILayout.Space(20);
                EditorGUILayout.LabelField("No CanvasGroupToggler components found", 
                    EditorStyles.centeredGreyMiniLabel);
            } else {
                foreach (var toggler in rootTogglers) {
                    if (toggler == null) continue;
                    
                    DrawTogglerPanel(toggler, 0);
                }
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawTogglerPanel(CanvasGroupToggler toggler, int indentLevel) {
            bool isActive = toggler.Active;
            var children = GetChildTogglers(toggler);
            bool hasChildren = children.Count > 0;
            
            // Ensure foldout state exists
            if (!foldoutStates.ContainsKey(toggler)) {
                foldoutStates[toggler] = true;
            }
            
            // Apply indent based on level
            if (indentLevel > 0) {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(indentLevel * 20);
                EditorGUILayout.BeginVertical();
            }
            
            // Subtle background color based on state
            Color originalBg = GUI.backgroundColor;
            GUI.backgroundColor = isActive ? new Color(0.9f, 1f, 0.9f) : new Color(1f, 0.95f, 0.95f);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = originalBg;
            
            EditorGUILayout.BeginHorizontal();
            
            // Foldout arrow if has children
            if (hasChildren) {
                bool newFoldout = EditorGUILayout.Foldout(foldoutStates[toggler], "", true);
                foldoutStates[toggler] = newFoldout;
            } else {
                GUILayout.Space(15);
            }
            
            // Object name (clickable to select in hierarchy)
            string displayName = toggler.gameObject.name;
            if (hasChildren) {
                displayName += $" ({children.Count})";
            }
            
            if (GUILayout.Button(displayName, EditorStyles.label, GUILayout.ExpandWidth(true))) {
                Selection.activeGameObject = toggler.gameObject;
                EditorGUIUtility.PingObject(toggler.gameObject);
            }
            
            // Toggle button with clear ON/OFF state
            Color btnColor = isActive ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.7f, 0.7f, 0.7f);
            GUI.backgroundColor = btnColor;
            
            string btnText = isActive ? "ON" : "OFF";
            if (GUILayout.Button(btnText, toggleButtonStyle, GUILayout.Width(50))) {
                toggler.Toggle();
            }
            
            GUI.backgroundColor = originalBg;
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(2);
            
            // Close indent
            if (indentLevel > 0) {
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }
            
            // Draw children if foldout is open
            if (hasChildren && foldoutStates[toggler]) {
                foreach (var child in children) {
                    if (child != null) {
                        DrawTogglerPanel(child, indentLevel + 1);
                    }
                }
            }
        }
    }
}