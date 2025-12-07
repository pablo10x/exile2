using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Professional editor tool for generating inventory icons from 3D prefabs.
/// Provides customizable camera settings, lighting, and batch processing capabilities.
/// </summary>
public class IconGeneratorTool : EditorWindow {
    #region Serialized Fields

    [Header("Prefab Settings")] [SerializeField] private GameObject targetPrefab;
    [SerializeField]                             private Vector2Int iconResolution = new Vector2Int(512, 512);

    [Header("Camera Settings")] [SerializeField] private Vector3 cameraOffset    = new Vector3(0f, 0.5f, -2f);
    [SerializeField]                             private Vector3 cameraRotation  = new Vector3(15f, 20f, 0f);
    [SerializeField]                             private float   fieldOfView     = 30f;
    [SerializeField]                             private Color   backgroundColor = new Color(0f, 0f, 0f, 0f);

    public Vector3 objectPosition;

    public Vector3 objectRotation;

    [Range(0.1f, 5f)] public float objectScale = 1f;

    [Header("Lighting Settings")] [SerializeField] private bool    useCustomLighting  = true;
    [SerializeField]                               private Color   mainLightColor     = Color.white;
    [SerializeField]                               private float   mainLightIntensity = 1f;
    [SerializeField]                               private Vector3 mainLightRotation  = new Vector3(50f, -30f, 0f);
    [SerializeField]                               private Color   fillLightColor     = new Color(0.5f, 0.5f, 0.8f, 1f);
    [SerializeField]                               private float   fillLightIntensity = 0.5f;
    [SerializeField]                               private Vector3 fillLightRotation  = new Vector3(-20f, 150f, 0f);

    [Header("Post Processing")] [SerializeField] private bool usePostProcessing = true;
    [Header("LivePreviewUpdate")] [SerializeField] private bool useLivePreviewUpdate = true;

    [Header("Output Settings")] [SerializeField] private string        outputPath    = "Assets/Icons/";
    [SerializeField]                             private string        iconPrefix    = "Icon_";
    [SerializeField]                             private TextureFormat textureFormat = TextureFormat.PNG;

    #endregion

    #region Private Variables

    private GameObject    previewInstance;
    private Camera        renderCamera;
    private Light         mainLight;
    private Light         fillLight;
    private RenderTexture renderTexture;
    private Vector2       scrollPosition;
    //private bool          showAdvancedSettings = false;
    private Texture2D     previewTexture;

    #endregion

    #region Menu Item

    [MenuItem("Tools/Icon Generator")]
    public static void ShowWindow() {
        IconGeneratorTool window = GetWindow<IconGeneratorTool>("Icon Generator");
        window.minSize = new Vector2(400, 600);
        window.Show();
    }

    #endregion

    #region Unity Lifecycle

    private void OnEnable() {
        LoadPreferences();
    }

    private void OnDisable() {
        SavePreferences();
        CleanupPreview();
    }

    private void OnGUI() {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        DrawHeader();
        DrawPrefabSection();
        DrawCameraSection();
        DrawObjectTransformSection();
        DrawLightingSection();
        DrawOutputSection();
        DrawPreviewSection();
        DrawActionButtons();

        EditorGUILayout.EndScrollView();
    }

    #endregion

    #region GUI Drawing Methods

    private void DrawHeader() {
        EditorGUILayout.Space(10);
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
        EditorGUILayout.LabelField("Icon Generator Tool", headerStyle);
        EditorGUILayout.LabelField("Generate high-quality icons from 3D prefabs", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.Space(10);
    }

    private void DrawPrefabSection() {
        
        DrawSectionHeader("Prefab Settings");
        EditorGUI.BeginChangeCheck(); // Start checking for changes
        targetPrefab = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Target Prefab", "The prefab to generate an icon from"), targetPrefab, typeof(GameObject), false);

        iconResolution = EditorGUILayout.Vector2IntField(new GUIContent("Icon Resolution", "Output texture resolution (width x height)"), iconResolution);

        // Clamp resolution to reasonable values
        iconResolution.x = Mathf.Clamp(iconResolution.x, 64, 4096);
        iconResolution.y = Mathf.Clamp(iconResolution.y, 64, 4096);

        EditorGUILayout.Space(5);
        if (EditorGUI.EndChangeCheck()) // If any slider changed
        {
            OnValuesChanged(); // Call your method
        }
    }

    private void DrawCameraSection() {
        DrawSectionHeader("Camera Settings");

        EditorGUI.BeginChangeCheck(); 
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Auto Frame Object", GUILayout.Width(145));
        if (GUILayout.Button("Frame Now", GUILayout.Height(25))) {
            AutoFrameObject();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(3);

        cameraOffset = EditorGUILayout.Vector3Field(new GUIContent("Camera Offset", "Camera position relative to the object"), cameraOffset);

        cameraRotation = EditorGUILayout.Vector3Field(new GUIContent("Camera Rotation", "Camera rotation in degrees (X, Y, Z)"), cameraRotation);

        fieldOfView = EditorGUILayout.Slider(new GUIContent("Field of View", "Camera field of view (smaller = more zoomed in)"), fieldOfView, 5f, 90f);

        backgroundColor = EditorGUILayout.ColorField(new GUIContent("Background Color", "Background color (alpha for transparency)"), backgroundColor);

        EditorGUILayout.Space(5);
        if (EditorGUI.EndChangeCheck()) // If any slider changed
        {
            OnValuesChanged(); // Call your method
        }
    }

    // Keep track of foldout state
    private bool showRotation = true;

    private void DrawObjectTransformSection() {
        DrawSectionHeader("Object Transform");
        EditorGUI.BeginChangeCheck(); // Start checking for changes

        // Foldout for object rotation
        showRotation = EditorGUILayout.Foldout(showRotation, "Object Rotation", true);
        if (showRotation) {
            objectRotation.x = EditorGUILayout.Slider(new GUIContent("Object Rotation X", "Rotate the object around the X axis"), objectRotation.x, 0f, 360f);

            objectRotation.y = EditorGUILayout.Slider(new GUIContent("Object Rotation Y", "Rotate the object around the Y axis"), objectRotation.y, 0f, 360f);

            objectRotation.z = EditorGUILayout.Slider(new GUIContent("Object Rotation Z", "Rotate the object around the Z axis"), objectRotation.z, 0f, 360f);
        }


        // objectRotation = EditorGUILayout.Vector3Field(
        //     new GUIContent("Object Rotation", "Rotate the object in the preview"),
        //     objectRotation
        // );

        objectPosition = EditorGUILayout.Vector3Field(new GUIContent("Object Position", "Adjust object position"), objectPosition);

        objectScale = EditorGUILayout.Slider(new GUIContent("Object Scale", "Scale the object uniformly"), objectScale, 0.1f, 5f);

        if (EditorGUI.EndChangeCheck()) // If any slider changed
        {
            OnValuesChanged(); // Call your method
        }
        EditorGUILayout.Space(5);
    }

    private void DrawLightingSection() {
        DrawSectionHeader("Lighting Settings");
        EditorGUI.BeginChangeCheck(); // Start checking for changes
        useCustomLighting = EditorGUILayout.Toggle(new GUIContent("Use Custom Lighting", "Enable custom lighting setup"), useCustomLighting);

        if (useCustomLighting) {
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField("Main Light", EditorStyles.boldLabel);
            mainLightColor     = EditorGUILayout.ColorField("Color", mainLightColor);
            mainLightIntensity = EditorGUILayout.Slider("Intensity", mainLightIntensity, 0f, 3f);
            mainLightRotation  = EditorGUILayout.Vector3Field("Rotation", mainLightRotation);

            EditorGUILayout.Space(3);

            EditorGUILayout.LabelField("Fill Light", EditorStyles.boldLabel);
            fillLightColor     = EditorGUILayout.ColorField("Color", fillLightColor);
            fillLightIntensity = EditorGUILayout.Slider("Intensity", fillLightIntensity, 0f, 2f);
            fillLightRotation  = EditorGUILayout.Vector3Field("Rotation", fillLightRotation);

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(5);

        DrawSectionHeader("Post Processing");

        usePostProcessing = EditorGUILayout.Toggle(new GUIContent("Use Post Processing", "Apply scene post-processing effects to icons"), usePostProcessing);

        if (usePostProcessing) {
            EditorGUILayout.HelpBox("Post-processing from the scene's volume will be applied to the icon.", MessageType.Info);
        }

        EditorGUILayout.Space(5);

        if (EditorGUI.EndChangeCheck()) // If any slider changed
        {
            OnValuesChanged(); // Call your method
        }
    }

    private void DrawOutputSection() {
        DrawSectionHeader("Output Settings");

        EditorGUILayout.BeginHorizontal();
        outputPath = EditorGUILayout.TextField(new GUIContent("Output Path", "Directory to save generated icons"), outputPath);
        if (GUILayout.Button("Browse", GUILayout.Width(60))) {
            string path = EditorUtility.OpenFolderPanel("Select Output Folder", "Assets", "");
            if (!string.IsNullOrEmpty(path)) {
                if (path.StartsWith(Application.dataPath)) {
                    outputPath = "Assets" + path.Substring(Application.dataPath.Length);
                }
                else {
                    EditorUtility.DisplayDialog("Invalid Path", "Please select a folder within the Assets directory.", "OK");
                }
            }
        }

        EditorGUILayout.EndHorizontal();

        iconPrefix = EditorGUILayout.TextField(new GUIContent("Icon Prefix", "Prefix for generated icon filenames"), iconPrefix);

        textureFormat = (TextureFormat)EditorGUILayout.EnumPopup(new GUIContent("Texture Format", "Output file format"), textureFormat);

        EditorGUILayout.Space(5);
    }

    private void OnValuesChanged() {
        if (targetPrefab != null && useLivePreviewUpdate)
            GeneratePreview();
    }

    private void DrawPreviewSection() {
        DrawSectionHeader("Preview");
        useLivePreviewUpdate = EditorGUILayout.Toggle(new GUIContent("Live Preview Update", "Immidiatly update the preview icon"), useLivePreviewUpdate);
        if (targetPrefab != null) {
            if (GUILayout.Button("Generate Preview", GUILayout.Height(30))) {
                GeneratePreview();
            }

            if (previewTexture != null) {
                EditorGUILayout.Space(5);
                Rect previewRect = GUILayoutUtility.GetRect(256, 256);
                EditorGUI.DrawPreviewTexture(previewRect, previewTexture, null, ScaleMode.ScaleToFit);
            }
        }
        else {
            EditorGUILayout.HelpBox("Please assign a prefab to generate a preview.", MessageType.Info);
        }

        EditorGUILayout.Space(5);
    }

    private void DrawActionButtons() {
        EditorGUILayout.Space(10);

        EditorGUI.BeginDisabledGroup(targetPrefab == null);

        if (GUILayout.Button("Generate and Save Icon", GUILayout.Height(40))) {
            GenerateAndSaveIcon();
        }

        EditorGUI.EndDisabledGroup();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Reset to Defaults")) {
            ResetToDefaults();
        }

        if (GUILayout.Button("Clear Preview")) {
            CleanupPreview();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawSectionHeader(string title) {
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.Space(2);
    }

    #endregion

    #region Icon Generation Methods

    /// <summary>
    /// Generates a preview of the icon without saving it.
    /// </summary>
    private void GeneratePreview() {
        if (targetPrefab == null) return;

        try {
            SetupPreviewScene();
            previewTexture = CaptureIcon();
            if (previewTexture != null) {
                Debug.Log(" Preview generated successfully.");
            }
           
        }
        catch (System.Exception e) {
            Debug.LogError($"Failed to generate preview: {e.Message}");
        }
    }

    /// <summary>
    /// Generates the icon and saves it to the specified output path.
    /// </summary>
    private void GenerateAndSaveIcon() {
        if (targetPrefab == null) {
            EditorUtility.DisplayDialog("Error", "Please assign a prefab first.", "OK");
            return;
        }

        try {
            SetupPreviewScene();
            Texture2D icon = CaptureIcon();

            if (icon != null) {
                SaveIcon(icon);
                EditorUtility.DisplayDialog("Success", $"Icon saved successfully to {outputPath}", "OK");
            }
        }
        catch (System.Exception e) {
            EditorUtility.DisplayDialog("Error", $"Failed to generate icon: {e.Message}", "OK");
            Debug.LogError($"Icon generation failed: {e}");
        }
        finally {
            CleanupPreview();
        }
    }

    /// <summary>
    /// Sets up the temporary scene for rendering the icon.
    /// </summary>
    private void SetupPreviewScene() {
        CleanupPreview();

        // Create render texture
        renderTexture              = new RenderTexture(iconResolution.x, iconResolution.y, 24, RenderTextureFormat.ARGB32);
        renderTexture.antiAliasing = 8;

        // Instantiate prefab
        previewInstance                      = Instantiate(targetPrefab);
        previewInstance.hideFlags            = HideFlags.HideAndDontSave;
        previewInstance.transform.position   = objectPosition;
        previewInstance.transform.rotation   = Quaternion.Euler(objectRotation);
        previewInstance.transform.localScale = Vector3.one * objectScale;

        // Create camera
        GameObject cameraObj = new GameObject("IconCamera");
        cameraObj.hideFlags             = HideFlags.HideAndDontSave;
        renderCamera                    = cameraObj.AddComponent<Camera>();
        renderCamera.transform.position = objectPosition + cameraOffset;
        renderCamera.transform.rotation = Quaternion.Euler(cameraRotation);
        renderCamera.fieldOfView        = fieldOfView;
        renderCamera.backgroundColor    = backgroundColor;
        renderCamera.clearFlags         = CameraClearFlags.SolidColor;
        renderCamera.targetTexture      = renderTexture;
        renderCamera.nearClipPlane      = 0.01f;
        renderCamera.enabled            = false;


        // Enable post-processing if requested
        if (usePostProcessing) {
#if UNITY_POST_PROCESSING_STACK_V2
            var postProcessLayer = cameraObj.AddComponent<UnityEngine.Rendering.PostProcessing.PostProcessLayer>();
            postProcessLayer.volumeLayer = -1; // Everything
            postProcessLayer.volumeTrigger = renderCamera.transform;
#elif UNITY_PIPELINE_URP || UNITY_PIPELINE_HDRP
            // For URP/HDRP, the camera will automatically pick up post-processing volumes
            // Just need to ensure the camera is set up correctly
            var additionalCameraData = cameraObj.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            additionalCameraData.renderPostProcessing = true;
            additionalCameraData.antialiasing         = UnityEngine.Rendering.Universal.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            additionalCameraData.antialiasingQuality  = UnityEngine.Rendering.Universal.AntialiasingQuality.High;
            additionalCameraData.requiresColorTexture = true;
            additionalCameraData.requiresDepthTexture = false;
#endif
        }

        // Setup lighting
        if (useCustomLighting) {
            GameObject mainLightObj = new GameObject("MainLight");
            mainLightObj.hideFlags       = HideFlags.HideAndDontSave;
            mainLight                    = mainLightObj.AddComponent<Light>();
            mainLight.type               = LightType.Directional;
            mainLight.color              = mainLightColor;
            mainLight.intensity          = mainLightIntensity;
            mainLight.transform.rotation = Quaternion.Euler(mainLightRotation);

            GameObject fillLightObj = new GameObject("FillLight");
            fillLightObj.hideFlags       = HideFlags.HideAndDontSave;
            fillLight                    = fillLightObj.AddComponent<Light>();
            fillLight.type               = LightType.Directional;
            fillLight.color              = fillLightColor;
            fillLight.intensity          = fillLightIntensity;
            fillLight.transform.rotation = Quaternion.Euler(fillLightRotation);
        }
    }

    /// <summary>
    /// Captures the icon from the render camera.
    /// </summary>
    private Texture2D CaptureIcon() {
        if (renderCamera == null || renderTexture == null) return null;

        renderCamera.Render();

        RenderTexture.active = renderTexture;
        Texture2D icon = new Texture2D(iconResolution.x, iconResolution.y, UnityEngine.TextureFormat.ARGB32, false);
        icon.ReadPixels(new Rect(0, 0, iconResolution.x, iconResolution.y), 0, 0);
        icon.Apply();
        RenderTexture.active = null;

        return icon;
    }

    /// <summary>
    /// Saves the generated icon to disk.
    /// </summary>
    private void SaveIcon(Texture2D icon) {
        if (!Directory.Exists(outputPath)) {
            Directory.CreateDirectory(outputPath);
        }

        string filename = $"{iconPrefix}{targetPrefab.name}.{GetFileExtension()}";
        string fullPath = Path.Combine(outputPath, filename);

        byte[] bytes = textureFormat == TextureFormat.PNG
                           ? icon.EncodeToPNG()
                           : icon.EncodeToJPG(90);

        File.WriteAllBytes(fullPath, bytes);
        AssetDatabase.Refresh();

        // Configure import settings
        TextureImporter importer = AssetImporter.GetAtPath(fullPath) as TextureImporter;
        if (importer != null) {
            importer.textureType         = TextureImporterType.Sprite;
            importer.spriteImportMode    = SpriteImportMode.Single;
            importer.alphaSource         = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled       = false;
            importer.maxTextureSize      = Mathf.Max(iconResolution.x, iconResolution.y);
            AssetDatabase.ImportAsset(fullPath, ImportAssetOptions.ForceUpdate);
        }

        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Texture2D>(fullPath));
    }

    /// <summary>
    /// Cleans up temporary objects used for preview/rendering.
    /// </summary>
    private void CleanupPreview() {
        if (previewInstance != null) DestroyImmediate(previewInstance);
        if (renderCamera != null) DestroyImmediate(renderCamera.gameObject);
        if (mainLight != null) DestroyImmediate(mainLight.gameObject);
        if (fillLight != null) DestroyImmediate(fillLight.gameObject);
        if (renderTexture != null) renderTexture.Release();

        previewInstance = null;
        renderCamera    = null;
        mainLight       = null;
        fillLight       = null;
        renderTexture   = null;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Automatically frames the camera to fit the target object perfectly.
    /// </summary>
    private void AutoFrameObject() {
        if (targetPrefab == null) {
            EditorUtility.DisplayDialog("Error", "Please assign a prefab first.", "OK");
            return;
        }

        // Temporarily instantiate the prefab to calculate bounds
        GameObject tempInstance = Instantiate(targetPrefab);
        tempInstance.transform.position   = objectPosition;
        tempInstance.transform.rotation   = Quaternion.Euler(objectRotation);
        tempInstance.transform.localScale = Vector3.one * objectScale;

        // Calculate bounds
        Bounds bounds = CalculateBounds(tempInstance);

        if (bounds.size == Vector3.zero) {
            DestroyImmediate(tempInstance);
            EditorUtility.DisplayDialog("Warning", "Could not calculate object bounds. Make sure the prefab has renderers.", "OK");
            return;
        }

        // Calculate optimal camera position
        Vector3 objectCenter = bounds.center;
        float   objectSize   = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);

        // Calculate distance based on FOV and object size
        float distance = (objectSize * 0.5f) / Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);
        distance *= 1.2f; // Add 20% padding

        // Apply camera rotation to get the offset direction
        Quaternion camRot    = Quaternion.Euler(cameraRotation);
        Vector3    direction = camRot * Vector3.back;

        cameraOffset = (direction * distance) + (objectCenter - objectPosition);

        DestroyImmediate(tempInstance);

        Debug.Log($"Camera auto-framed! Distance: {distance:F2}, Object size: {objectSize:F2}");
        Repaint();
    }

    /// <summary>
    /// Calculates the combined bounds of all renderers in the object.
    /// </summary>
    private Bounds CalculateBounds(GameObject obj) {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
            return new Bounds(obj.transform.position, Vector3.zero);

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++) {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    private string GetFileExtension() {
        return textureFormat == TextureFormat.PNG
                   ? "png"
                   : "jpg";
    }

    private void ResetToDefaults() {
        iconResolution     = new Vector2Int(512, 512);
        cameraOffset       = new Vector3(0f, 0.5f, -2f);
        cameraRotation     = new Vector3(15f, 20f, 0f);
        fieldOfView        = 30f;
        backgroundColor    = new Color(0f, 0f, 0f, 0f);
        objectRotation     = Vector3.zero;
        objectPosition     = Vector3.zero;
        objectScale        = 1f;
        useCustomLighting  = true;
        mainLightColor     = Color.white;
        mainLightIntensity = 1f;
        mainLightRotation  = new Vector3(50f, -30f, 0f);
        fillLightColor     = new Color(0.5f, 0.5f, 0.8f, 1f);
        fillLightIntensity = 0.5f;
        fillLightRotation  = new Vector3(-20f, 150f, 0f);
        usePostProcessing  = true;
        outputPath         = "Assets/Icons/";
        iconPrefix         = "Icon_";
        textureFormat      = TextureFormat.PNG;

        CleanupPreview();
    }

    #endregion

    #region Preferences

    private void SavePreferences() {
        EditorPrefs.SetString("IconGen_OutputPath", outputPath);
        EditorPrefs.SetString("IconGen_IconPrefix", iconPrefix);
        EditorPrefs.SetInt("IconGen_TextureFormat", (int)textureFormat);
    }

    private void LoadPreferences() {
        outputPath    = EditorPrefs.GetString("IconGen_OutputPath", "Assets/Icons/");
        iconPrefix    = EditorPrefs.GetString("IconGen_IconPrefix", "Icon_");
        textureFormat = (TextureFormat)EditorPrefs.GetInt("IconGen_TextureFormat", 0);
    }

    #endregion

    #region Enums

    public enum TextureFormat {
        PNG,
        JPG
    }

    #endregion
}