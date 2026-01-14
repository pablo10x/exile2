using UnityEditor;
using UnityEngine;
using UnityEditorInternal;

[CustomEditor(typeof(AzureSkyController))]
public class AzureSkyControllerEditor : Editor
{
	private string[] CloudMode = new string[]{"Off", "Static"};
	private string[] SkyMode   = new string[]{"Skydome", "Skybox"};
    private int checkSettingChange = 0;

	private Texture2D logoTex, tab;
	private int tabSize = 1;
	private int labelWidth = 114;
	private Color col1 = new Color(1,1,1,1);//Normal.
	private Color col2 = new Color(0,0,0,0);//All Transparent.
	private Color col3 = new Color(0.35f,0.65f,1,1);//Blue.
	private Color col4 = new Color(0.15f,0.5f,1,0.35f);//Blue semi transparent.
	private Color col5 = new Color(0.75f, 1.0f, 0.75f, 1.0f);//Green;
	private Color col6 = new Color(1.0f, 0.5f, 0.5f, 1.0f);//Red;
	private Color curveColor = Color.yellow;

	private string installPath;
	private string inspectorGUIPath;
	private Rect   bgRect;
	private float  curveValueWidth = 50;

	// Show/Hide strings.
	private string ShowHideTimeOfDay;
	private string ShowHideObjectsAndMaterial;
	private string ShowHideScattering;
	private string ShowHideNightSky;
	private string ShowHideCloud;
	private string ShowHideFog;
	private string ShowHideLighting;
	private string ShowHideOptions;
	private string ShowHideOutputs;

	// Gradient Colors.
	SerializedProperty MieSunColor;
	SerializedProperty RayleighSunColor;
	SerializedProperty MoonDiskColor;
	SerializedProperty MoonBrightColor;
	SerializedProperty LightColor;
	SerializedProperty AmbientSkyColor;
	SerializedProperty EquatorSkyColor;
	SerializedProperty GroundSkyColor;
	SerializedProperty CloudColor;

	// Outputs.
	private ReorderableList    reorderableCurveList;
	private ReorderableList    reorderableGradientList;
	private SerializedProperty serializedCurve;
	private SerializedProperty serializedGradient;

	void OnEnable()
	{
		string scriptLocation = AssetDatabase.GetAssetPath (MonoScript.FromScriptableObject (this));
		installPath           = scriptLocation.Replace ("/Editor/AzureSkyControllerEditor.cs", "");
		inspectorGUIPath      = installPath + "/Editor/InspectorGUI";

		// Gradient Color Serialize.
		//-------------------------------------------------------------------------------------------------------
		RayleighSunColor = serializedObject.FindProperty("rayleighGradientColor");
		MieSunColor = serializedObject.FindProperty("mieGradientColor");
		MoonDiskColor = serializedObject.FindProperty("moonDiskGradientColor");
		MoonBrightColor = serializedObject.FindProperty("moonBrightGradientColor");
		LightColor = serializedObject.FindProperty("lightGradientColor");
		AmbientSkyColor = serializedObject.FindProperty("ambientSkyGradientColor");
		EquatorSkyColor = serializedObject.FindProperty("equatorSkyGradientColor");
		GroundSkyColor = serializedObject.FindProperty("groundSkyGradientColor");
        CloudColor = serializedObject.FindProperty("cloudGradientColor");

		// Create Curve Outputs.
		//-------------------------------------------------------------------------------------------------------
		serializedCurve = serializedObject.FindProperty ("outputCurveList");
		reorderableCurveList = new ReorderableList (serializedObject, serializedCurve, false, true, true, true);
		reorderableCurveList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
		{
			rect.y += 2;
			EditorGUI.LabelField(rect, "element index " + index.ToString());
			EditorGUI.PropertyField(new Rect (rect.x+100, rect.y, rect.width-100, EditorGUIUtility.singleLineHeight), serializedCurve.GetArrayElementAtIndex(index), GUIContent.none);
		};
		reorderableCurveList.onAddCallback = (ReorderableList l) =>
		{
			var index = l.serializedProperty.arraySize;
			l.serializedProperty.arraySize++;
			l.index = index;
			serializedCurve.GetArrayElementAtIndex(index).animationCurveValue = AnimationCurve.Linear(0,0,24,0);
		};
		reorderableCurveList.drawHeaderCallback = (Rect rect) =>
		{
			EditorGUI.LabelField(rect, "Curve Output", EditorStyles.boldLabel);
		};
		reorderableCurveList.drawElementBackgroundCallback = (rect, index, active, focused) => {
			Texture2D tex = new Texture2D (1, 1);
			tex.SetPixel (0, 0, col4);
			tex.Apply ();
			if (active)
				GUI.DrawTexture (rect, tex as Texture);
		};

		// Create Gradient Outputs.
		//-------------------------------------------------------------------------------------------------------
		serializedGradient = serializedObject.FindProperty ("outputGradientList");
		reorderableGradientList = new ReorderableList (serializedObject, serializedGradient, false, true, true, true);
		reorderableGradientList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
		{
			rect.y += 2;
			EditorGUI.LabelField(rect, "element index " + index.ToString());
			EditorGUI.PropertyField(new Rect (rect.x+100, rect.y, rect.width-100, EditorGUIUtility.singleLineHeight), serializedGradient.GetArrayElementAtIndex(index), GUIContent.none);
		};
		reorderableGradientList.drawHeaderCallback = (Rect rect) =>
		{
			EditorGUI.LabelField(rect, "Gradient Output", EditorStyles.boldLabel);
		};
		reorderableGradientList.drawElementBackgroundCallback = (rect, index, active, focused) => {
			Texture2D tex = new Texture2D (1, 1);
			tex.SetPixel (0, 0, col4);
			tex.Apply ();
			if (active)
				GUI.DrawTexture (rect, tex as Texture);
		};
	}

	public override void OnInspectorGUI()
	{
		// Get target
		//-------------------------------------------------------------------------------------------------------
		AzureSkyController Target = (AzureSkyController)target;
		Undo.RecordObject (Target, "Undo Azure Sky Lite Properties");
		serializedObject.Update ();
		curveColor = Target.curveColorField;

		// Show and Hide text.
		//-------------------------------------------------------------------------------------------------------
		if (Target.showTimeOfDayTab) ShowHideTimeOfDay = "| Hide"; else ShowHideTimeOfDay = "| Show";
		if (Target.showObjectsAndMaterialsTab) ShowHideObjectsAndMaterial = "| Hide"; else ShowHideObjectsAndMaterial = "| Show";
		if (Target.showScatteringTab) ShowHideScattering = "| Hide"; else ShowHideScattering = "| Show";
		if (Target.showNightSkyTab) ShowHideNightSky = "| Hide"; else ShowHideNightSky = "| Show";
		if (Target.showLightingTab) ShowHideLighting = "| Hide"; else ShowHideLighting = "| Show";
		if (Target.showCloudTab) ShowHideCloud = "| Hide"; else ShowHideCloud = "| Show";
		if (Target.showFogTab) ShowHideFog = "| Hide"; else ShowHideFog = "| Show";
		if (Target.showOptionsTab) ShowHideOptions = "| Hide"; else ShowHideOptions = "| Show";
		if (Target.showOutputsTab) ShowHideOutputs = "| Hide"; else ShowHideOutputs = "| Show";

		// Get Logo Textures.
		//-------------------------------------------------------------------------------------------------------
		logoTex = AssetDatabase.LoadAssetAtPath (inspectorGUIPath + "/AzureSkyLiteLogo.png", typeof (Texture2D))as Texture2D;
		tab     = AssetDatabase.LoadAssetAtPath (inspectorGUIPath + "/InspectorTab.png", typeof (Texture2D))as Texture2D;
		EditorGUILayout.Space ();

		// Header.
		//-------------------------------------------------------------------------------------------------------
		bgRect = EditorGUILayout.GetControlRect ();
		GUI.DrawTexture (new Rect (bgRect.x,bgRect.y, 194,41), logoTex);
		EditorGUILayout.Space ();
		EditorGUILayout.Space ();
		EditorGUILayout.Space ();
		GUILayout.Label ("Version 1.2.0", EditorStyles.miniLabel);
		bgRect = EditorGUILayout.GetControlRect ();
		GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 1).x, GUILayoutUtility.GetRect (bgRect.width, 0).y-21, bgRect.width, 2), tab);

		#region Time of Day Tab
		// Time of Day Tab.
		//-------------------------------------------------------------------------------------------------------
		GUI.color = col2;
		if (GUI.Button(new Rect(bgRect.x, bgRect.y, bgRect.width, 15),"")) Target.showTimeOfDayTab = !Target.showTimeOfDayTab;
		GUI.color = col1;
		Target.showTimeOfDayTab = EditorGUI.Foldout(new Rect (bgRect.width+15, bgRect.y, bgRect.width, 15), Target.showTimeOfDayTab, "");
		GUI.Label (new Rect (bgRect.x, bgRect.y, bgRect.width, 15), "TIME OF DAY", EditorStyles.boldLabel);
		GUI.Label (new Rect (bgRect.width-40, bgRect.y, bgRect.width, 15), ShowHideTimeOfDay);
		GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 1).x, GUILayoutUtility.GetRect (bgRect.width, -4).y-5, bgRect.width, 2), tab);

		if (Target.showTimeOfDayTab)
		{
            // Timeline.
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Timeline", GUILayout.Width(labelWidth));
			Target.timeline = EditorGUILayout.Slider (Target.timeline, 0.0f, 24.0f);
			EditorGUILayout.EndHorizontal ();

			// Latitude.
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Latitude", GUILayout.Width(labelWidth));
			Target.latitude = EditorGUILayout.Slider (Target.latitude, -90.0f, 90.0f);
			EditorGUILayout.EndHorizontal ();

			// Longitude.
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Longitude", GUILayout.Width(labelWidth));
			Target.longitude = EditorGUILayout.Slider (Target.longitude, -180.0f, 180.0f);
			EditorGUILayout.EndHorizontal ();

            // UTC.
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label ("UTC", GUILayout.Width(labelWidth));
            Target.utc = EditorGUILayout.Slider(Target.utc, -12.0f, 12.0f);
            EditorGUILayout.EndHorizontal();

            // Day Cycle.
            EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Day Cycle in Minutes");
			Target.dayCycle = EditorGUILayout.FloatField(Target.dayCycle, GUILayout.Width(50));
			if ( Target.dayCycle < 0.0f ) { Target.dayCycle = 0.0f; }//Avoid negative values.
			EditorGUILayout.EndHorizontal ();

			// Time Curve.
			GUI.color = col4;
			EditorGUILayout.BeginVertical ("Box");
			GUI.color = col1;
			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.Space();
			GUILayout.Label ("Day and Night Length", EditorStyles.boldLabel);
			EditorGUILayout.EndHorizontal ();
			EditorGUILayout.Space ();

			// Set Time by Curve?
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Set Time of Day by Curve?");
			Target.setTimeByCurve = EditorGUILayout.Toggle (Target.setTimeByCurve, GUILayout.Width (15));
			EditorGUILayout.EndHorizontal ();

			// Day and Night Length Curve Field.
			EditorGUILayout.BeginHorizontal ();
			GUI.color = col3;
			if (GUILayout.Button ("R", GUILayout.Width(25), GUILayout.Height(25))) { Target.dayLengthCurve = AnimationCurve.Linear (0.0f, 0.0f, 24.0f, 24.0f); }
			GUI.color = col1;
			Target.dayLengthCurve = EditorGUILayout.CurveField (Target.dayLengthCurve, curveColor, new Rect (0.0f, 0.0f, 24.0f, 24.0f), GUILayout.Height (25));
			EditorGUILayout.EndHorizontal ();

			// Show Current Time of Day by Curve.
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Current Time by Curve:");
			GUILayout.TextField (Target.timeByCurve.ToString (), GUILayout.Width (50));
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.EndVertical ();
			GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 2).x, GUILayoutUtility.GetRect (bgRect.width, 0).y-3, bgRect.width, 2), tab);
		}
		EditorGUILayout.Space ();
		EditorGUILayout.Space ();
		#endregion

		#region References Tab
		// References Tab.
		//-------------------------------------------------------------------------------------------------------
		GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 1).x, GUILayoutUtility.GetRect (bgRect.width, -4).y-5, bgRect.width, 2), tab);
		bgRect = EditorGUILayout.GetControlRect ();
		GUI.color = col2;
		if (GUI.Button(new Rect(bgRect.x, bgRect.y, bgRect.width, 15),"")) Target.showObjectsAndMaterialsTab = !Target.showObjectsAndMaterialsTab;
		GUI.color = col1;
		Target.showObjectsAndMaterialsTab = EditorGUI.Foldout(new Rect (bgRect.width+15, bgRect.y, bgRect.width, 15), Target.showObjectsAndMaterialsTab, "");
		GUI.Label (new Rect (bgRect.x, bgRect.y, bgRect.width, 15), "REFERENCES", EditorStyles.boldLabel);
		GUI.Label (new Rect (bgRect.width-40, bgRect.y, bgRect.width, 15), ShowHideObjectsAndMaterial);
		GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 1).x, GUILayoutUtility.GetRect (bgRect.width, -4).y-4, bgRect.width, 2), tab);
		EditorGUILayout.Space ();
		if (Target.showObjectsAndMaterialsTab)
		{
            // Sun Transform.
            GUI.color = col5;
            if (!Target.sunTransform)
                GUI.color = col6;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Sun", GUILayout.Width(labelWidth));
            Target.sunTransform = (Transform)EditorGUILayout.ObjectField(Target.sunTransform, typeof(Transform), true);
            EditorGUILayout.EndHorizontal();
            GUI.color = col1;

            // Moon Transform.
            GUI.color = col5;
            if (!Target.moonTransform)
                GUI.color = col6;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Moon", GUILayout.Width(labelWidth));
            Target.moonTransform = (Transform)EditorGUILayout.ObjectField(Target.moonTransform, typeof(Transform), true);
            EditorGUILayout.EndHorizontal();
            GUI.color = col1;

            // Directional Light Transform.
            GUI.color = col5;
			if (!Target.lightTransform)
				GUI.color = col6;
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label("Light", GUILayout.Width(labelWidth));
			Target.lightTransform =  (Transform)EditorGUILayout.ObjectField (Target.lightTransform, typeof(Transform), true);
			EditorGUILayout.EndHorizontal ();
			GUI.color = col1;

            // Skydome Transform.
			GUI.color = col5;
			if(!Target.skydomeTransform)
				GUI.color = col6;
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label("Skydome", GUILayout.Width(labelWidth));
			Target.skydomeTransform =  (Transform)EditorGUILayout.ObjectField (Target.skydomeTransform, typeof(Transform), true);
			EditorGUILayout.EndHorizontal ();
			GUI.color = col1;

            // Starfield Cubemap.
            GUI.color = col5;
            if (!Target.starfieldTexture)
                GUI.color = col6;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Starfield", GUILayout.Width(labelWidth));
            Target.starfieldTexture = (Cubemap)EditorGUILayout.ObjectField(Target.starfieldTexture, typeof(Cubemap), true);
            EditorGUILayout.EndHorizontal();
            GUI.color = col1;

            // Sun Texture.
            GUI.color = col5;
            if (!Target.sunTexture)
                GUI.color = col6;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Sun Texture", GUILayout.Width(labelWidth));
            Target.sunTexture = (Texture)EditorGUILayout.ObjectField(Target.sunTexture, typeof(Texture), true);
            EditorGUILayout.EndHorizontal();
            GUI.color = col1;

            // Moon Texture.
            GUI.color = col5;
            if (!Target.moonTexture)
                GUI.color = col6;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Moon Texture", GUILayout.Width(labelWidth));
            Target.moonTexture = (Texture)EditorGUILayout.ObjectField(Target.moonTexture, typeof(Texture), true);
            EditorGUILayout.EndHorizontal();
            GUI.color = col1;

            // Cloud Texture.
            GUI.color = col5;
            if (!Target.cloudTexture)
                GUI.color = col6;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Cloud Texture", GUILayout.Width(labelWidth));
            Target.cloudTexture = (Texture)EditorGUILayout.ObjectField(Target.cloudTexture, typeof(Texture), true);
            EditorGUILayout.EndHorizontal();
            GUI.color = col1;

            // Sky Material.
            GUI.color = col5;
			if(!Target.skyMaterial)
				GUI.color = col6;
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label("Sky Material", GUILayout.Width(labelWidth));
			Target.skyMaterial =  (Material)EditorGUILayout.ObjectField (Target.skyMaterial, typeof(Material), true);
			EditorGUILayout.EndHorizontal ();
			GUI.color = col1;

			GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 2).x, GUILayoutUtility.GetRect (bgRect.width, 0).y-1, bgRect.width, 2), tab);
			EditorGUILayout.Space ();
		}
		EditorGUILayout.Space ();
		#endregion

		#region Scattering Tab
		// Scattering Tab.
		//-------------------------------------------------------------------------------------------------------
		GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 1).x, GUILayoutUtility.GetRect (bgRect.width, -4).y-5, bgRect.width, 2), tab);
		bgRect = EditorGUILayout.GetControlRect ();
		GUI.color = col2;
		if (GUI.Button(new Rect(bgRect.x, bgRect.y, bgRect.width, 15),"")) Target.showScatteringTab = !Target.showScatteringTab;
		GUI.color = col1;
		Target.showScatteringTab = EditorGUI.Foldout(new Rect (bgRect.width+15, bgRect.y, bgRect.width, 15), Target.showScatteringTab, "");
		GUI.Label (new Rect (bgRect.x, bgRect.y, bgRect.width, 15), "SCATTERING", EditorStyles.boldLabel);
		GUI.Label (new Rect (bgRect.width-40, bgRect.y, bgRect.width, 15), ShowHideScattering);
		GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 1).x, GUILayoutUtility.GetRect (bgRect.width, -4).y-4, bgRect.width, 2), tab);
		EditorGUILayout.Space ();

		if (Target.showScatteringTab)
		{
			// Rayleigh.
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Rayleigh", GUILayout.Width (labelWidth - 23));
		    if (GUILayout.Button ("R", GUILayout.Width (18), GUILayout.Height (15))) { Target.rayleighCurve = AnimationCurve.Linear (0.0f, 1.0f, 24.0f, 1.0f); }
		    Target.rayleighCurve = EditorGUILayout.CurveField (Target.rayleighCurve, curveColor, new Rect (0.0f, 0.0f, 24.0f, 5.0f));
			GUILayout.TextField (Target.rayleigh.ToString (), GUILayout.Width (curveValueWidth));
			EditorGUILayout.EndHorizontal ();

			// Mie.
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Mie", GUILayout.Width (labelWidth - 23));
			if (GUILayout.Button ("R", GUILayout.Width (18), GUILayout.Height (15))) { Target.mieCurve = AnimationCurve.Linear (0.0f, 1.0f, 24.0f, 1.0f); }
			Target.mieCurve = EditorGUILayout.CurveField (Target.mieCurve, curveColor, new Rect (0.0f, 0.0f, 24.0f, 30.0f));
			GUILayout.TextField (Target.mie.ToString (), GUILayout.Width (curveValueWidth));
			EditorGUILayout.EndHorizontal ();

			// Kr.
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Kr", GUILayout.Width (labelWidth - 23));
			if (GUILayout.Button ("R", GUILayout.Width (18), GUILayout.Height (15))) { Target.krCurve = AnimationCurve.Linear (0.0f, 8.4f, 24.0f, 8.4f); }
			Target.krCurve = EditorGUILayout.CurveField (Target.krCurve, curveColor, new Rect (0.0f, 1.0f, 24.0f, 29.0f));
			GUILayout.TextField (Target.kr.ToString (), GUILayout.Width (curveValueWidth));
			EditorGUILayout.EndHorizontal ();

			// Km.
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Km", GUILayout.Width(labelWidth-23));
			if (GUILayout.Button ("R", GUILayout.Width (18), GUILayout.Height (15))) { Target.kmCurve = AnimationCurve.Linear (0.0f, 1.25f, 24.0f, 1.25f); }
			Target.kmCurve = EditorGUILayout.CurveField (Target.kmCurve, curveColor, new Rect (0.0f, 1.0f, 24.0f, 29.0f));
			GUILayout.TextField (Target.km.ToString (), GUILayout.Width (curveValueWidth));
			EditorGUILayout.EndHorizontal ();

            // Scattering.
            EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Scattering", GUILayout.Width (labelWidth - 23));
			if (GUILayout.Button ("R", GUILayout.Width (18), GUILayout.Height (15))) { Target.scatteringCurve = AnimationCurve.Linear (0.0f, 15.0f, 24.0f, 15.0f); }
			Target.scatteringCurve = EditorGUILayout.CurveField (Target.scatteringCurve, curveColor, new Rect (0.0f, 0.0f, 24.0f, 75.0f));
			GUILayout.TextField (Target.scattering.ToString (), GUILayout.Width (curveValueWidth));
			EditorGUILayout.EndHorizontal ();

            // Sun Intensity.
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Sun Intensity", GUILayout.Width (labelWidth - 23));
			if (GUILayout.Button ("R", GUILayout.Width (18), GUILayout.Height (15))) { Target.sunIntensityCurve = AnimationCurve.Linear (0.0f, 3.0f, 24.0f, 3.0f); }
			Target.sunIntensityCurve = EditorGUILayout.CurveField (Target.sunIntensityCurve, curveColor, new Rect (0.0f, 0.0f, 24.0f, 5.0f));
			GUILayout.TextField (Target.sunIntensity.ToString (), GUILayout.Width (curveValueWidth));
			EditorGUILayout.EndHorizontal ();

            // Night Intensity.
            EditorGUILayout.BeginHorizontal ();
            GUILayout.Label("Night Intensity", GUILayout.Width (labelWidth - 23));
            if (GUILayout.Button("R", GUILayout.Width (18), GUILayout.Height (15))) { Target.nightIntensityCurve = AnimationCurve.Linear (0.0f, 0.5f, 24.0f, 0.5f); }
            Target.nightIntensityCurve = EditorGUILayout.CurveField (Target.nightIntensityCurve, curveColor, new Rect (0.0f, 0.0f, 24.0f, 5.0f));
            GUILayout.TextField (Target.nightIntensity.ToString (), GUILayout.Width (curveValueWidth));
            EditorGUILayout.EndHorizontal ();

            // Exposure.
            EditorGUILayout.BeginHorizontal ();
            GUILayout.Label("Exposure", GUILayout.Width (labelWidth - 23));
            if (GUILayout.Button("R", GUILayout.Width (18), GUILayout.Height (15))) { Target.exposureCurve = AnimationCurve.Linear (0.0f, 1.75f, 24.0f, 1.75f); }
            Target.exposureCurve = EditorGUILayout.CurveField (Target.exposureCurve, curveColor, new Rect (0.0f, 0.0f, 24.0f, 8.0f));
            GUILayout.TextField (Target.exposure.ToString (), GUILayout.Width (curveValueWidth));
            EditorGUILayout.EndHorizontal ();

            // Rayleigh Color.
            EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Rayleigh Color", GUILayout.Width(labelWidth));
			EditorGUILayout.PropertyField(RayleighSunColor, GUIContent.none);
			EditorGUILayout.EndHorizontal ();

			// Mie Color.
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Mie Color", GUILayout.Width(labelWidth));
			EditorGUILayout.PropertyField(MieSunColor, GUIContent.none);
			EditorGUILayout.EndHorizontal ();
		}
		EditorGUILayout.Space();
		#endregion

		#region Deep Space Tab
		// Deep Space Tab.
		//-------------------------------------------------------------------------------------------------------
		GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 1).x, GUILayoutUtility.GetRect (bgRect.width, -4).y-5, bgRect.width, 2), tab);
		bgRect = EditorGUILayout.GetControlRect ();
		GUI.color = col2;
		if (GUI.Button(new Rect(bgRect.x, bgRect.y, bgRect.width, 15),"")) Target.showNightSkyTab = !Target.showNightSkyTab;
		GUI.color = col1;
		Target.showNightSkyTab = EditorGUI.Foldout(new Rect (bgRect.width+15, bgRect.y, bgRect.width, 15), Target.showNightSkyTab, "");
		GUI.Label (new Rect (bgRect.x, bgRect.y, bgRect.width, 15), "DEEP SPACE", EditorStyles.boldLabel);
		GUI.Label (new Rect (bgRect.width-40, bgRect.y, bgRect.width, 15), ShowHideNightSky);
		GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 1).x, GUILayoutUtility.GetRect (bgRect.width, -4).y-4, bgRect.width, 2), tab);
		EditorGUILayout.Space ();

		if(Target.showNightSkyTab)
		{
            /// MOON ///
            GUILayout.Label("Moon:");
            // Moon Disk Color.
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Disk Color", GUILayout.Width(labelWidth));
            EditorGUILayout.PropertyField(MoonDiskColor, GUIContent.none);
            EditorGUILayout.EndHorizontal();

            // Moon Bright Color.
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Bright Color", GUILayout.Width(labelWidth));
            EditorGUILayout.PropertyField(MoonBrightColor, GUIContent.none);
            EditorGUILayout.EndHorizontal();

            // Moon Bright Range.
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Bright Range", GUILayout.Width(labelWidth - 23));
            if (GUILayout.Button("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.moonBrightRangeCurve = AnimationCurve.Linear(0.0f, 0.9f, 24.0f, 0.9f); }
            Target.moonBrightRangeCurve = EditorGUILayout.CurveField(Target.moonBrightRangeCurve, curveColor, new Rect(0.0f, 0.0f, 24.0f, 1.0f));
            GUILayout.TextField(Target.moonBrightRange.ToString(), GUILayout.Width(curveValueWidth));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            /// STARFIELD. ///
            GUILayout.Label("Starfield:");
            // Stars Intensity.
            EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Stars", GUILayout.Width (labelWidth-23));
			if (GUILayout.Button ("R", GUILayout.Width (18), GUILayout.Height (15))) { Target.starfieldIntensityCurve = AnimationCurve.Linear (0.0f, 0.0f, 24.0f, 0.0f); }
			Target.starfieldIntensityCurve = EditorGUILayout.CurveField (Target.starfieldIntensityCurve, curveColor, new Rect (0.0f, 0.0f, 24.0f, 10.0f));
			GUILayout.TextField (Target.starfieldIntensity.ToString (), GUILayout.Width (curveValueWidth));
			EditorGUILayout.EndHorizontal ();

			// Milky Way Intensity.
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Milky Way", GUILayout.Width (labelWidth-23));
			if (GUILayout.Button ("R", GUILayout.Width (18), GUILayout.Height (15))) { Target.milkyWayIntensityCurve = AnimationCurve.Linear (0.0f, 0.0f, 24.0f, 0.0f); }
			Target.milkyWayIntensityCurve = EditorGUILayout.CurveField (Target.milkyWayIntensityCurve, curveColor, new Rect (0.0f, 0.0f, 24.0f, 1.0f));
			GUILayout.TextField (Target.milkyWayIntensity.ToString (), GUILayout.Width (curveValueWidth));
			EditorGUILayout.EndHorizontal ();

			// Color Balance.
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Color Balance R", GUILayout.Width( labelWidth));
			Target.starfieldColorBalance.x = EditorGUILayout.Slider (Target.starfieldColorBalance.x, 1.0f, 2.0f);
			EditorGUILayout.EndHorizontal ();
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Color Balance G", GUILayout.Width (labelWidth));
			Target.starfieldColorBalance.y = EditorGUILayout.Slider (Target.starfieldColorBalance.y, 1.0f, 2.0f);
			EditorGUILayout.EndHorizontal ();
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Color Balance B", GUILayout.Width (labelWidth));
			Target.starfieldColorBalance.z = EditorGUILayout.Slider (Target.starfieldColorBalance.z, 1.0f, 2.0f);
			EditorGUILayout.EndHorizontal ();

            // Posiotion.
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Position X", GUILayout.Width (labelWidth));
			Target.starfieldPosition.x = EditorGUILayout.Slider (Target.starfieldPosition.x, 0.0f, 360.0f);
			EditorGUILayout.EndHorizontal ();
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Position Y", GUILayout.Width (labelWidth));
			Target.starfieldPosition.y = EditorGUILayout.Slider (Target.starfieldPosition.y, 0.0f, 360.0f);
			EditorGUILayout.EndHorizontal ();
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Position Z", GUILayout.Width (labelWidth));
			Target.starfieldPosition.z = EditorGUILayout.Slider (Target.starfieldPosition.z, 0.0f, 360.0f);
			EditorGUILayout.EndHorizontal ();
			EditorGUILayout.Space ();

			GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 2).x, GUILayoutUtility.GetRect (bgRect.width, 0).y-1, bgRect.width, 2), tab);
			EditorGUILayout.Space ();
		}
		EditorGUILayout.Space ();
		#endregion

		#region Cloud Tab
		// Cloud Tab.
		//-------------------------------------------------------------------------------------------------------
		GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 1).x, GUILayoutUtility.GetRect (bgRect.width, -4).y-5, bgRect.width, 2), tab);
		bgRect = EditorGUILayout.GetControlRect ();
		GUI.color = col2;
		if (GUI.Button(new Rect(bgRect.x, bgRect.y, bgRect.width, 15),"")) Target.showCloudTab = !Target.showCloudTab;
		GUI.color = col1;
		Target.showCloudTab = EditorGUI.Foldout(new Rect (bgRect.width+15, bgRect.y, bgRect.width, 15), Target.showCloudTab, "");
		GUI.Label (new Rect (bgRect.x, bgRect.y, bgRect.width, 15), "CLOUD", EditorStyles.boldLabel);
		GUI.Label (new Rect (bgRect.width-40, bgRect.y, bgRect.width, 15), ShowHideCloud);
		GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 1).x, GUILayoutUtility.GetRect (bgRect.width, -4).y-4, bgRect.width, 2), tab);
		EditorGUILayout.Space ();

		if(Target.showCloudTab)
		{
            // Cloud Color.
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Cloud Color", GUILayout.Width(labelWidth));
            EditorGUILayout.PropertyField(CloudColor, GUIContent.none);
            EditorGUILayout.EndHorizontal();

            // Cloud Scattering.
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Scattering", GUILayout.Width(labelWidth - 23));
            if (GUILayout.Button("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.cloudScatteringCurve = AnimationCurve.Linear(0.0f, 1.0f, 24.0f, 1.0f); }
            Target.cloudScatteringCurve = EditorGUILayout.CurveField(Target.cloudScatteringCurve, curveColor, new Rect(0.0f, 0.0f, 24.0f, 1.5f));
            GUILayout.TextField(Target.cloudScattering.ToString(), GUILayout.Width(curveValueWidth));
            EditorGUILayout.EndHorizontal();

            // Cloud Extinction.
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Extinction", GUILayout.Width(labelWidth - 23));
            if (GUILayout.Button("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.cloudExtinctionCurve = AnimationCurve.Linear(0.0f, 0.25f, 24.0f, 0.25f); }
            Target.cloudExtinctionCurve = EditorGUILayout.CurveField(Target.cloudExtinctionCurve, curveColor, new Rect(0.0f, 0.0f, 24.0f, 1.0f));
            GUILayout.TextField(Target.cloudExtinction.ToString(), GUILayout.Width(curveValueWidth));
            EditorGUILayout.EndHorizontal();

            // Cloud Power.
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Power", GUILayout.Width(labelWidth - 23));
            if (GUILayout.Button("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.cloudPowerCurve = AnimationCurve.Linear(0.0f, 2.2f, 24.0f, 2.2f); }
            Target.cloudPowerCurve = EditorGUILayout.CurveField(Target.cloudPowerCurve, curveColor, new Rect(0.0f, 1.8f, 24.0f, 2.4f));
            GUILayout.TextField(Target.cloudPower.ToString(), GUILayout.Width(curveValueWidth));
            EditorGUILayout.EndHorizontal();

            // Cloud Intensity.
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Intensity", GUILayout.Width(labelWidth - 23));
            if (GUILayout.Button("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.cloudIntensityCurve = AnimationCurve.Linear(0.0f, 1.0f, 24.0f, 1.0f); }
            Target.cloudIntensityCurve = EditorGUILayout.CurveField(Target.cloudIntensityCurve, curveColor, new Rect(0.0f, 0.0f, 24.0f, 2.0f));
            GUILayout.TextField(Target.cloudIntensity.ToString(), GUILayout.Width(curveValueWidth));
            EditorGUILayout.EndHorizontal();

            // Rotation Speed.
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Rotation Speed", GUILayout.Width(labelWidth));
            Target.cloudRotationSpeed = EditorGUILayout.Slider(Target.cloudRotationSpeed, -0.01f, 0.01f);
            EditorGUILayout.EndHorizontal();
        }
		EditorGUILayout.Space ();
		#endregion

		#region Fog Tab
		// Fog Tab.
		//-------------------------------------------------------------------------------------------------------
		GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 1).x, GUILayoutUtility.GetRect (bgRect.width, -4).y-5, bgRect.width, 2), tab);
		bgRect = EditorGUILayout.GetControlRect ();
		GUI.color = col2;
		if (GUI.Button(new Rect(bgRect.x, bgRect.y, bgRect.width, 15),"")) Target.showFogTab = !Target.showFogTab;
		GUI.color = col1;
		Target.showFogTab = EditorGUI.Foldout(new Rect (bgRect.width+15, bgRect.y, bgRect.width, 15), Target.showFogTab, "");
		GUI.Label (new Rect (bgRect.x, bgRect.y, bgRect.width, 15), "FOG", EditorStyles.boldLabel);
		GUI.Label (new Rect (bgRect.width-40, bgRect.y, bgRect.width, 15), ShowHideFog);
		GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 1).x, GUILayoutUtility.GetRect (bgRect.width, -4).y-4, bgRect.width, 2), tab);
		EditorGUILayout.Space ();

		if(Target.showFogTab)
		{
            // Fog Distance.
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Distance", GUILayout.Width(labelWidth - 23));
            if (GUILayout.Button("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.fogDistanceCurve = AnimationCurve.Linear(0.0f, 3500.0f, 24.0f, 3500.0f); }
            Target.fogDistanceCurve = EditorGUILayout.CurveField(Target.fogDistanceCurve, curveColor, new Rect(0.0f, 0.0f, 24.0f, 20000.0f));
            GUILayout.TextField(Target.fogDistance.ToString(), GUILayout.Width(curveValueWidth));
            EditorGUILayout.EndHorizontal();

            // Fog Blend.
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Blend", GUILayout.Width(labelWidth));
            Target.fogBlend = EditorGUILayout.Slider(Target.fogBlend, 0.0f, 1.0f);
            EditorGUILayout.EndHorizontal();

            // Mie Distance.
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Mie Distance", GUILayout.Width(labelWidth));
            Target.mieDistance = EditorGUILayout.Slider(Target.mieDistance, 0.0f, 1.0f);
            EditorGUILayout.EndHorizontal();
        }
		EditorGUILayout.Space ();
		#endregion

		#region Lighting Tab
		// Lighting Tab.
		//-------------------------------------------------------------------------------------------------------
		GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 1).x, GUILayoutUtility.GetRect (bgRect.width, -4).y-5, bgRect.width, 2), tab);
		bgRect = EditorGUILayout.GetControlRect ();
		GUI.color = col2;
		if (GUI.Button(new Rect(bgRect.x, bgRect.y, bgRect.width, 15),"")) Target.showLightingTab = !Target.showLightingTab;
		GUI.color = col1;
		Target.showLightingTab = EditorGUI.Foldout(new Rect (bgRect.width+15, bgRect.y, bgRect.width, 15), Target.showLightingTab, "");
		GUI.Label (new Rect (bgRect.x, bgRect.y, bgRect.width, 15), "LIGHTING", EditorStyles.boldLabel);
		GUI.Label (new Rect (bgRect.width-40, bgRect.y, bgRect.width, 15), ShowHideLighting);
		GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 1).x, GUILayoutUtility.GetRect (bgRect.width, -4).y-4, bgRect.width, 2), tab);
		EditorGUILayout.Space ();

		if(Target.showLightingTab)
		{
            GUILayout.Label("Light:");
            // Light Intensity.
            EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Intensity", GUILayout.Width (labelWidth-23));
			if (GUILayout.Button ("R", GUILayout.Width (18), GUILayout.Height (15))) { Target.lightIntensityCurve = AnimationCurve.Linear (0.0f, 0.0f, 24.0f, 0.0f); }
			Target.lightIntensityCurve = EditorGUILayout.CurveField (Target.lightIntensityCurve, curveColor, new Rect (0.0f, 0.0f, 24.0f, 8.0f));
			GUILayout.TextField (Target.lightIntensity.ToString (), GUILayout.Width (curveValueWidth));
			EditorGUILayout.EndHorizontal ();

			// Light Color.
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Color", GUILayout.Width (labelWidth));
			EditorGUILayout.PropertyField (LightColor, GUIContent.none);
			EditorGUILayout.EndHorizontal ();
			EditorGUILayout.Space ();
			GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 2).x, GUILayoutUtility.GetRect (bgRect.width, -2).y-3, bgRect.width, tabSize), tab);

            GUILayout.Label("Ambient:");
            EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Intensity", GUILayout.Width(labelWidth-23));
			if (GUILayout.Button ("R", GUILayout.Width(18), GUILayout.Height(15))) { Target.ambientIntensityCurve = AnimationCurve.Linear(0.0f, 1.0f, 24.0f, 1.0f); }
			Target.ambientIntensityCurve = EditorGUILayout.CurveField (Target.ambientIntensityCurve, curveColor, new Rect(0.0f, 0.0f, 24.0f, 8.0f));
			GUILayout.TextField (Target.ambientIntensity.ToString(), GUILayout.Width (curveValueWidth));
			EditorGUILayout.EndHorizontal ();

			// Ambient Color.
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Ambient Color", GUILayout.Width(labelWidth));
			EditorGUILayout.PropertyField(AmbientSkyColor, GUIContent.none);
			EditorGUILayout.EndHorizontal ();

			// Equator Color.
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Equator Color", GUILayout.Width(labelWidth));
			EditorGUILayout.PropertyField(EquatorSkyColor, GUIContent.none);
			EditorGUILayout.EndHorizontal ();

			// Ground Color.
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Ground Color", GUILayout.Width(labelWidth));
			EditorGUILayout.PropertyField(GroundSkyColor, GUIContent.none);
			EditorGUILayout.EndHorizontal ();

			GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 2).x, GUILayoutUtility.GetRect (bgRect.width, 0).y-1, bgRect.width, 2), tab);
			EditorGUILayout.Space ();
		}
		EditorGUILayout.Space ();
		#endregion

		#region Options Tab
		// Options Tab.
		//-------------------------------------------------------------------------------------------------------
		GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 1).x, GUILayoutUtility.GetRect (bgRect.width, -4).y-5, bgRect.width, 2), tab);
		bgRect = EditorGUILayout.GetControlRect ();
		GUI.color = col2;
		if (GUI.Button(new Rect(bgRect.x, bgRect.y, bgRect.width, 15),"")) Target.showOptionsTab = !Target.showOptionsTab;
		GUI.color = col1;
		Target.showOptionsTab = EditorGUI.Foldout(new Rect (bgRect.width+15, bgRect.y, bgRect.width, 15), Target.showOptionsTab, "");
		GUI.Label (new Rect (bgRect.x, bgRect.y, bgRect.width, 15), "OPTIONS", EditorStyles.boldLabel);
		GUI.Label (new Rect (bgRect.width-40, bgRect.y, bgRect.width, 15), ShowHideOptions);
		GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 1).x, GUILayoutUtility.GetRect (bgRect.width, -4).y-4, bgRect.width, 2), tab);
		EditorGUILayout.Space ();

		if(Target.showOptionsTab)
		{
			// Follow Main Camera?
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Follow Active Main Camera?");
			Target.followActiveMainCamera = EditorGUILayout.Toggle (Target.followActiveMainCamera, GUILayout.Width(15));
			EditorGUILayout.EndHorizontal ();

            // Sun Size.
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Sun Disk Size", GUILayout.Width(labelWidth));
            Target.sunDiskSize = EditorGUILayout.Slider(Target.sunDiskSize, 0.0f, 1.0f);
            EditorGUILayout.EndHorizontal();

            // Moon Size.
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Moon Disk Size", GUILayout.Width(labelWidth));
            Target.moonDiskSize = EditorGUILayout.Slider(Target.moonDiskSize, 0.0f, 1.0f);
            EditorGUILayout.EndHorizontal();

            // Cloud Mode.
            // 0 = No Clouds;
            // 1 = Static Clouds;
            checkSettingChange = Target.cloudMode;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Cloud Mode", GUILayout.Width(labelWidth));
            Target.cloudMode = EditorGUILayout.Popup(Target.cloudMode, CloudMode);
            EditorGUILayout.EndHorizontal();
            if(checkSettingChange != Target.cloudMode)
            {
                Target.UpdateSkySettings();
                //Debug.Log("Cloud Mode has changed to: " + Target.cloudMode.ToString());
            }

            // Sky Mode.
            // 0 = Skydome;
            // 1 = Skybox;
            checkSettingChange = Target.skyMode;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Sky Mode", GUILayout.Width(labelWidth));
            Target.skyMode = EditorGUILayout.Popup(Target.skyMode, SkyMode);
            EditorGUILayout.EndHorizontal();
            if (checkSettingChange != Target.skyMode)
            {
                Target.UpdateSkySettings();
                //Debug.Log("Sky Mode has changed to: " + Target.skyMode.ToString());
            }

            // Help Box.
            if (Target.skyMode == 0)
            {
                EditorGUILayout.HelpBox("Using vertex shader to achieve the best performance.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Using pixel shader to achieve the best quality.", MessageType.Info);
            }
        }
		EditorGUILayout.Space ();
		#endregion

		#region Outputs Tab
		// Outputs Tab.
		//-------------------------------------------------------------------------------------------------------
		GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 1).x, GUILayoutUtility.GetRect (bgRect.width, -4).y-5, bgRect.width, 2), tab);
		bgRect = EditorGUILayout.GetControlRect ();
		GUI.color = col2;
		if (GUI.Button(new Rect(bgRect.x, bgRect.y, bgRect.width, 15),"")) Target.showOutputsTab = !Target.showOutputsTab;
		GUI.color = col1;
		Target.showOutputsTab = EditorGUI.Foldout(new Rect (bgRect.width+15, bgRect.y, bgRect.width, 15), Target.showOutputsTab, "");
		GUI.Label (new Rect (bgRect.x, bgRect.y, bgRect.width, 15), "OUTPUTS", EditorStyles.boldLabel);
		GUI.Label (new Rect (bgRect.width-40, bgRect.y, bgRect.width, 15), ShowHideOutputs);
		GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 1).x, GUILayoutUtility.GetRect (bgRect.width, -4).y-4, bgRect.width, 2), tab);
		EditorGUILayout.Space ();

		if(Target.showOutputsTab)
		{
			EditorGUILayout.Space();
			reorderableCurveList.DoLayoutList();
			EditorGUILayout.Space();
			reorderableGradientList.DoLayoutList();
			EditorGUILayout.Space ();
			GUI.DrawTexture (new Rect (GUILayoutUtility.GetRect (bgRect.width, 1).x, GUILayoutUtility.GetRect (bgRect.width, -4).y-4, bgRect.width, 2), tab);
		}
		EditorGUILayout.Space ();
        #endregion

        // Refresh the Inspector.
        //-------------------------------------------------------------------------------------------------------
        serializedObject.ApplyModifiedProperties();
		EditorUtility.SetDirty(target);
	}
}