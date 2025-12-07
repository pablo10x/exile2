using UnityEngine;
using UnityEditor;

public class WaypointEditor : EditorWindow {
    private WaypointData waypointData;
    private bool         isEditing;
    private Color        waypointColor           = Color.red;
    private Color        lineColor               = Color.white;
    private Color        selectedColor           = Color.yellow;
    private Color        loopColor               = Color.green;
    private float        waypointSize            = 0.2f;
    private int          selectedWaypointIndex   = -1;
    private int          draggingWaypointIndex   = -1;
    private WaypointType currentWaypointType     = WaypointType.npcPath;
    private string       currentRoadName         = "";
   // private int          connectingWaypointIndex = -1;
    private bool         autoConnect             = false;

    private bool isConnectingWaypoints = false;
    private int  sourceWaypointIndex   = -1;

//draw on distance
    private float DrawingDistance = 50f;

    [MenuItem("Tools/Waypoint Editor")]
    public static void ShowWindow() {
        GetWindow<WaypointEditor>("Waypoint Editor");
    }

    private void OnEnable() {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable() {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnGUI() {
        GUILayout.Label("Waypoint Editor", EditorStyles.boldLabel);

        waypointData = (WaypointData)EditorGUILayout.ObjectField("Waypoint Data", waypointData, typeof(WaypointData), false);

        if (waypointData == null) {
            if (GUILayout.Button("Create New Waypoint Data")) {
                string path = EditorUtility.SaveFilePanelInProject("Save Waypoint Data", "New Waypoint Data", "asset", "Save Waypoint Data");
                if (!string.IsNullOrEmpty(path)) {
                    waypointData = CreateInstance<WaypointData>();
                    AssetDatabase.CreateAsset(waypointData, path);
                    AssetDatabase.SaveAssets();
                }
            }

            return;
        }

        isEditing             = GUILayout.Toggle(isEditing, "Enable Editing");
        waypointData.isLooped = GUILayout.Toggle(waypointData.isLooped, "Loop Path");

        if (GUILayout.Button("Clear Waypoints")) {
            Undo.RecordObject(waypointData, "Clear Waypoints");
            waypointData.waypoints.Clear();
            EditorUtility.SetDirty(waypointData);
        }

        EditorGUILayout.LabelField($"Waypoints: {waypointData.waypoints.Count}");

        waypointColor       = EditorGUILayout.ColorField("Waypoint Color", waypointColor);
        lineColor           = EditorGUILayout.ColorField("Line Color", lineColor);
        selectedColor       = EditorGUILayout.ColorField("Selected Color", selectedColor);
        waypointSize        = EditorGUILayout.Slider("Waypoint Size", waypointSize, 0.1f, 1f);
        autoConnect         = GUILayout.Toggle(autoConnect, "Auto Connect");
        currentWaypointType = (WaypointType)EditorGUILayout.EnumPopup("New Waypoint Type", currentWaypointType);

        currentRoadName = EditorGUILayout.TextField("Road Name", currentRoadName);

        if (GUILayout.Button("Save Waypoints")) {
            EditorUtility.SetDirty(waypointData);
            AssetDatabase.SaveAssets();
        }

        GUILayout.Label("Set Drawing Distance", EditorStyles.boldLabel);
        DrawingDistance = EditorGUILayout.Slider("Distance", DrawingDistance, 0f, 500f);
        EditorGUILayout.LabelField("Current Distance: " + DrawingDistance.ToString("F2"));

        EditorGUILayout.LabelField($"Selected Waypoint: {(selectedWaypointIndex != -1 ? selectedWaypointIndex.ToString() : "None")}");

        // Update the connection buttons section
        if (selectedWaypointIndex != -1) {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Connection Controls", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Alt + Click: Start/Complete connection\nESC: Cancel connection", MessageType.Info);

            if (isConnectingWaypoints) {
                EditorGUILayout.LabelField("Connecting from waypoint: " + sourceWaypointIndex);
                if (GUILayout.Button("Cancel Connection")) {
                    CancelWaypointConnection();
                }
            }
        }

        if (GUILayout.Button("Deselect Waypoint")) {
            selectedWaypointIndex = -1;
        }
    }

    private void OnSceneGUI(SceneView sceneView) {
        if (!isEditing || waypointData == null) return;

        Event e = Event.current;
        HandleMouseEvents(e, sceneView);
        DrawWaypoints(e);
        if (GUI.changed) {
            EditorUtility.SetDirty(waypointData);
        }
    }

    private void HandleMouseEvents(Event e, SceneView sceneView) {
        if (e.type == EventType.MouseDown && e.button == 0) {
            if (e.shift) {
                AddWaypoint(e, sceneView);
            }
            else if (e.alt) {
                HandleAltClick(e);
            }
            else {
                StartDragging(e);
            }
        }
        else if (e.type == EventType.MouseDrag && draggingWaypointIndex != -1) {
            DragWaypoint(e, sceneView);
        }
        else if (e.type == EventType.MouseUp && e.button == 0) {
            EndDragging(e, sceneView);
        }
        else if (e.type == EventType.KeyDown) {
            if (e.keyCode == KeyCode.R && e.shift && selectedWaypointIndex != -1) {
                RemoveWaypoint();
                e.Use();
            }
            else if (e.keyCode == KeyCode.Escape && isConnectingWaypoints) {
                CancelWaypointConnection();
                e.Use();
            }
        }
    }

    private void HandleAltClick(Event e) {
        for (int i = 0; i < waypointData.waypoints.Count; i++) {
            if (HandleUtility.DistanceToCircle(waypointData.waypoints[i].position, waypointSize) < 10f) {
                if (!isConnectingWaypoints) {
                    // Start connection
                    isConnectingWaypoints = true;
                    sourceWaypointIndex   = i;
                    selectedWaypointIndex = i;
                    ShowNotification(new GUIContent("Select target waypoint. Press ESC to cancel."));
                }
                else {
                    // Complete connection
                    if (i != sourceWaypointIndex) {
                        Undo.RecordObject(waypointData, "Connect Waypoints");
                        ConnectWaypoints(sourceWaypointIndex, i);
                        EditorUtility.SetDirty(waypointData);
                        ShowNotification(new GUIContent("Waypoints connected!"));
                    }

                    CancelWaypointConnection();
                }

                e.Use();
                break;
            }
        }
    }

    private void CancelWaypointConnection() {
        isConnectingWaypoints = false;
        sourceWaypointIndex   = -1;
        ShowNotification(new GUIContent("Connection cancelled"));
    }

    private void AddWaypoint(Event e, SceneView sceneView) {
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit)) {
            Undo.RecordObject(waypointData, "Add Waypoint");
            Waypointx newWaypointx = new Waypointx(hit.point, currentWaypointType, currentRoadName);

            int newIndex = waypointData.waypoints.Count;
            waypointData.waypoints.Add(newWaypointx);

            if (selectedWaypointIndex != -1) {
                ConnectWaypoints(selectedWaypointIndex, newIndex);
            }

            selectedWaypointIndex = newIndex;
            EditorUtility.SetDirty(waypointData);
            e.Use();
        }
    }

    private void StartDragging(Event e) {
        for (int i = 0; i < waypointData.waypoints.Count; i++) {
            if (HandleUtility.DistanceToCircle(waypointData.waypoints[i].position, waypointSize) < 10f) {
                draggingWaypointIndex = i;
                selectedWaypointIndex = i;
                e.Use();
                break;
            }
        }
    }

    private void DragWaypoint(Event e, SceneView sceneView) {
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit)) {
            Undo.RecordObject(waypointData, "Move Waypoint");
            waypointData.waypoints[draggingWaypointIndex].position = hit.point;
            EditorUtility.SetDirty(waypointData);
            e.Use();
        }
    }

    private void EndDragging(Event e, SceneView sceneView) {
        if (draggingWaypointIndex != -1) {
            for (int i = 0; i < waypointData.waypoints.Count; i++) {
                if (i != draggingWaypointIndex && HandleUtility.DistanceToCircle(waypointData.waypoints[i].position, waypointSize) < 10f) {
                    // Connect waypoints if dragged onto another
                    Undo.RecordObject(waypointData, "Connect Waypoints");
                    ConnectWaypoints(draggingWaypointIndex, i);
                    EditorUtility.SetDirty(waypointData);
                    break;
                }
            }

            draggingWaypointIndex = -1;
            e.Use();
        }
    }

    private void ConnectWaypoints(int index1, int index2) {
        if (!waypointData.waypoints[index1].connectedWaypoints.Contains(index2)) {
            waypointData.waypoints[index1].connectedWaypoints.Add(index2);
        }

        if (!waypointData.waypoints[index2].connectedWaypoints.Contains(index1)) {
            waypointData.waypoints[index2].connectedWaypoints.Add(index1);
        }
    }

    private void RemoveWaypoint() {
        Undo.RecordObject(waypointData, "Remove Waypoint");

        // Remove connections to the waypoint being deleted
        foreach (var waypoint in waypointData.waypoints) {
            waypoint.connectedWaypoints.Remove(selectedWaypointIndex);
            for (int i = 0; i < waypoint.connectedWaypoints.Count; i++) {
                if (waypoint.connectedWaypoints[i] > selectedWaypointIndex) {
                    waypoint.connectedWaypoints[i]--;
                }
            }
        }

        // Remove the waypoint
        waypointData.waypoints.RemoveAt(selectedWaypointIndex);

        EditorUtility.SetDirty(waypointData);
        if (selectedWaypointIndex >= waypointData.waypoints.Count) {
            selectedWaypointIndex = waypointData.waypoints.Count - 1;
        }
    }

    private void DrawWaypoints(Event e) {
        DrawConnectionLines();
        for (int i = 0; i < waypointData.waypoints.Count; i++) {
            if (Vector3.Distance(waypointData.waypoints[i].position, GetSceneViewCameraPosition()) > DrawingDistance) {
                continue;
            }

            Vector3 wp   = waypointData.waypoints[i].position;
            float   size = HandleUtility.GetHandleSize(wp) * waypointSize;

            Color waypointColor;
            if (i == selectedWaypointIndex) {
                waypointColor = selectedColor;
            }
            else if (i == draggingWaypointIndex) {
                waypointColor = Color.blue;
            }
            else if (isConnectingWaypoints && i == sourceWaypointIndex) {
                waypointColor = Color.yellow; // Highlight source waypoint during connection
            }
            else {
                waypointColor = GetWaypointColor(waypointData.waypoints[i].type);
            }

            Handles.color = waypointColor;
            DrawFlatBox(wp, waypointColor);
            if (Handles.Button(wp, Quaternion.identity, size, size, Handles.SphereHandleCap)) {
                selectedWaypointIndex = i;
                Repaint();
            }
        }

        // Draw preview line while connecting
        if (isConnectingWaypoints && sourceWaypointIndex != -1) {
            Handles.color = Color.yellow;
            Vector3 mousePosition = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition).GetPoint(0);
            Handles.DrawDottedLine(waypointData.waypoints[sourceWaypointIndex].position, mousePosition, 5f);
        }
    }

    private void DrawFlatBox(Vector3 center, Color color, float size = 1f) {
        Vector3   halfSize = Vector3.one * size * 0.7f;
        Vector3[] corners  = new Vector3[] { center + new Vector3(-halfSize.x, 0, -halfSize.z), center + new Vector3(halfSize.x, 0, -halfSize.z), center + new Vector3(halfSize.x, 0, halfSize.z), center + new Vector3(-halfSize.x, 0, halfSize.z) };


        Handles.DrawSolidRectangleWithOutline(corners, color, new Color(0.26f, 0.26f, 0.26f));
    }

    private Color GetWaypointColor(WaypointType type) {
        switch (type) {
            case WaypointType.npcPath:
                return Color.red;
            case WaypointType.RestingPlace:
                return Color.green;
            case WaypointType.TrafficLane:
                return Color.blue;

            case WaypointType.road:

                return Color.white;

            default:
                return waypointColor;
        }
    }

    private void DrawLines(int index, Vector3 waypoint) {
        if (index < waypointData.waypoints.Count - 1) {
            Handles.color = lineColor;
            Handles.DrawLine(waypoint, waypointData.waypoints[index + 1].position);
        }

        if (waypointData.isLooped && index == waypointData.waypoints.Count - 1) {
            Handles.color = loopColor;
            Handles.DrawLine(waypoint, waypointData.waypoints[0].position);
        }
    }

    private void DrawConnectionLines() {
        for (int i = 0; i < waypointData.waypoints.Count; i++) {
            if (Vector3.Distance(waypointData.waypoints[i].position, GetSceneViewCameraPosition()) > DrawingDistance) {
                continue;
            }

            foreach (int connectedIndex in waypointData.waypoints[i].connectedWaypoints) {
                if (connectedIndex >= 0 && connectedIndex < waypointData.waypoints.Count) {
                    Handles.color = lineColor;
                    Handles.DrawAAPolyLine(3f, waypointData.waypoints[i].position, waypointData.waypoints[connectedIndex].position);
                }
            }
        }
    }

    private static Vector3 GetSceneViewCameraPosition() {
        if (SceneView.lastActiveSceneView != null) {
            return SceneView.lastActiveSceneView.camera.transform.position;
        }

        return Vector3.zero;
    }
}