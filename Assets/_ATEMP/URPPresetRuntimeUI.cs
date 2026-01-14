using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class URPPresetRuntimeUI : MonoBehaviour
{
    [Header("Presets (assign in Inspector)")]
    public List<UniversalRenderPipelineAsset> presets = new List<UniversalRenderPipelineAsset>();

    [Header("Optional: Bind existing UI (uGUI)")]
    public Dropdown presetDropdown;     // or TMP_Dropdown via adapter
    public Button applyButton;
    public Toggle foldoutPresetsToggle;
    public Toggle foldoutTweaksToggle;

    // Tweaks
    public Slider renderScaleSlider;
    public Text renderScaleValueLabel;
    public Dropdown msaaDropdown;
    public Toggle mainLightShadowsToggle;
    public Toggle softShadowsToggle;

    [Header("Containers (foldouts)")]
    public GameObject presetsFoldoutContent;
    public GameObject tweaksFoldoutContent;

    [Header("Auto UI")]
    public bool autoBuildMinimalUI = true;

    int _currentIndex = 0;

    // Cached reflection for flexible URP versions
    PropertyInfo _piRenderScale;
    PropertyInfo _piMsaaSampleCount;
    PropertyInfo _piMainLightShadows;
    PropertyInfo _piSoftShadows;

    // Quality API via reflection (for forward/back compat)
    MethodInfo _miSetQualityRP; // QualitySettings.SetRenderPipelineAssetAt(int, RenderPipelineAsset)
    MethodInfo _miGetQualityRP; // QualitySettings.GetRenderPipelineAssetAt(int)

    void Awake()
    {
        CacheURPProperties();
        CacheQualityAPI();
        EnsureMinimalUIIfNeeded();
        RefreshPresetListDropdown();
        BindUIEvents();
        SyncUIFromActiveAsset();
    }

    void CacheURPProperties()
    {
        var t = typeof(UniversalRenderPipelineAsset);
        _piRenderScale       = t.GetProperty("renderScale", BindingFlags.Public | BindingFlags.Instance);
        _piMsaaSampleCount   = t.GetProperty("msaaSampleCount", BindingFlags.Public | BindingFlags.Instance);
        _piMainLightShadows  = t.GetProperty("supportsMainLightShadows", BindingFlags.Public | BindingFlags.Instance);
        _piSoftShadows       = t.GetProperty("supportsSoftShadows", BindingFlags.Public | BindingFlags.Instance);
    }

    void CacheQualityAPI()
    {
        var t = typeof(QualitySettings);
        _miGetQualityRP = t.GetMethod("GetRenderPipelineAssetAt", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(int) }, null);
        _miSetQualityRP = t.GetMethod("SetRenderPipelineAssetAt", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(int), typeof(RenderPipelineAsset) }, null);
    }

    void EnsureMinimalUIIfNeeded()
    {
        if (!autoBuildMinimalUI) return;

        // Make sure we have a VerticalLayout root
        if (GetComponent<VerticalLayoutGroup>() == null)
            gameObject.AddComponent<VerticalLayoutGroup>();

        // Create components if not assigned
        if (foldoutPresetsToggle == null)
            foldoutPresetsToggle = CreateToggle("Presets ▼", true);
        if (presetsFoldoutContent == null)
            presetsFoldoutContent = CreateGroup("PresetsContent");

        if (presetDropdown == null)
            presetDropdown = CreateDropdown("Preset");

        if (applyButton == null)
            applyButton = CreateButton("Apply Preset");

        if (foldoutTweaksToggle == null)
            foldoutTweaksToggle = CreateToggle("Tweaks ▼", true);
        if (tweaksFoldoutContent == null)
            tweaksFoldoutContent = CreateGroup("TweaksContent");

        // Tweak controls
        if (renderScaleSlider == null)
        {
            var row = CreateGroup("RenderScaleRow", tweaksFoldoutContent.transform);
            CreateLabel("Render Scale", row.transform);
            renderScaleSlider = CreateSlider(0.5f, 2.0f, 1.0f, row.transform);
            renderScaleValueLabel = CreateLabel("1.00", row.transform);
        }

        if (msaaDropdown == null)
        {
            var row = CreateGroup("MSAARow", tweaksFoldoutContent.transform);
            CreateLabel("MSAA", row.transform);
            msaaDropdown = CreateDropdown("MSAA", row.transform);
            msaaDropdown.options = new List<Dropdown.OptionData>
            {
                new Dropdown.OptionData("Off"),
                new Dropdown.OptionData("2x"),
                new Dropdown.OptionData("4x"),
                new Dropdown.OptionData("8x"),
            };
        }

        if (mainLightShadowsToggle == null)
            mainLightShadowsToggle = CreateToggle("Main Light Shadows", parent: tweaksFoldoutContent.transform);

        if (softShadowsToggle == null)
            softShadowsToggle = CreateToggle("Soft Shadows", parent: tweaksFoldoutContent.transform);

        // Hook foldout content visibility
        foldoutPresetsToggle.onValueChanged.AddListener(v =>
        {
            if (presetsFoldoutContent) presetsFoldoutContent.SetActive(v);
            foldoutPresetsToggle.GetComponentInChildren<Text>().text = v ? "Presets ▼" : "Presets ▶";
        });
        foldoutTweaksToggle.onValueChanged.AddListener(v =>
        {
            if (tweaksFoldoutContent) tweaksFoldoutContent.SetActive(v);
            foldoutTweaksToggle.GetComponentInChildren<Text>().text = v ? "Tweaks ▼" : "Tweaks ▶";
        });
    }

    void BindUIEvents()
    {
        if (presetDropdown)
            presetDropdown.onValueChanged.AddListener(OnPresetSelected);

        if (applyButton)
            applyButton.onClick.AddListener(ApplySelectedPreset);

        if (renderScaleSlider)
            renderScaleSlider.onValueChanged.AddListener(v =>
            {
                var asset = GetActiveEditableAsset();
                if (asset == null || _piRenderScale == null) return;
                SafeSetFloat(_piRenderScale, asset, v);
                if (renderScaleValueLabel) renderScaleValueLabel.text = v.ToString("0.00");
            });

        if (msaaDropdown)
            msaaDropdown.onValueChanged.AddListener(idx =>
            {
                var asset = GetActiveEditableAsset();
                if (asset == null || _piMsaaSampleCount == null) return;
                int count = IndexToMSAA(idx);
                SafeSetInt(_piMsaaSampleCount, asset, count);
            });

        if (mainLightShadowsToggle)
            mainLightShadowsToggle.onValueChanged.AddListener(v =>
            {
                var asset = GetActiveEditableAsset();
                if (asset == null || _piMainLightShadows == null) return;
                SafeSetBool(_piMainLightShadows, asset, v);
            });

        if (softShadowsToggle)
            softShadowsToggle.onValueChanged.AddListener(v =>
            {
                var asset = GetActiveEditableAsset();
                if (asset == null || _piSoftShadows == null) return;
                SafeSetBool(_piSoftShadows, asset, v);
            });
    }

    void RefreshPresetListDropdown()
    {
        if (!presetDropdown) return;

        presetDropdown.ClearOptions();
        var opts = new List<Dropdown.OptionData>();
        foreach (var p in presets)
        {
            var nm = p ? p.name : "<null>";
            opts.Add(new Dropdown.OptionData(nm));
        }
        if (opts.Count == 0)
            opts.Add(new Dropdown.OptionData("(no presets assigned)"));
        presetDropdown.AddOptions(opts);

        // Select current active if it matches one from list
        var active = GetCurrentRP();
        _currentIndex = Mathf.Max(0, presets.FindIndex(a => a == active));
        presetDropdown.SetValueWithoutNotify(_currentIndex);
    }

    void OnPresetSelected(int index)
    {
        _currentIndex = Mathf.Clamp(index, 0, presets.Count - 1);
        SyncUIFromAsset(presets.Count > 0 ? presets[_currentIndex] : null);
    }

    void ApplySelectedPreset()
    {
        var asset = (_currentIndex >= 0 && _currentIndex < presets.Count) ? presets[_currentIndex] : null;
        if (asset == null) return;

        bool applied = TrySetCurrentQualityRP(asset);
        if (!applied)
        {
            GraphicsSettings.defaultRenderPipeline = asset; // fallback
        }
        SyncUIFromAsset(asset);
    }

    // UI sync to reflect current asset values
    void SyncUIFromActiveAsset()
    {
        SyncUIFromAsset(GetCurrentRP() as UniversalRenderPipelineAsset);
    }

    UniversalRenderPipelineAsset GetActiveEditableAsset()
    {
        // The "active" runtime asset is usually the current quality's RP asset
        var rp = GetCurrentRP();
        return rp as UniversalRenderPipelineAsset;
    }

    void SyncUIFromAsset(UniversalRenderPipelineAsset asset)
    {
        // Toggle visibility of controls based on property existence and asset availability
        SetInteractable(renderScaleSlider, asset != null && _piRenderScale != null);
        SetInteractable(msaaDropdown, asset != null && _piMsaaSampleCount != null);
        SetInteractable(mainLightShadowsToggle, asset != null && _piMainLightShadows != null);
        SetInteractable(softShadowsToggle, asset != null && _piSoftShadows != null);

        if (asset == null) return;

        if (_piRenderScale != null && renderScaleSlider)
        {
            float v = SafeGetFloat(_piRenderScale, asset, 1.0f);
            renderScaleSlider.SetValueWithoutNotify(Mathf.Clamp(v, renderScaleSlider.minValue, renderScaleSlider.maxValue));
            if (renderScaleValueLabel) renderScaleValueLabel.text = v.ToString("0.00");
        }

        if (_piMsaaSampleCount != null && msaaDropdown)
        {
            int count = SafeGetInt(_piMsaaSampleCount, asset, 1);
            msaaDropdown.SetValueWithoutNotify(MSAAtoIndex(count));
        }

        if (_piMainLightShadows != null && mainLightShadowsToggle)
        {
            bool v = SafeGetBool(_piMainLightShadows, asset, true);
            mainLightShadowsToggle.SetIsOnWithoutNotify(v);
        }

        if (_piSoftShadows != null && softShadowsToggle)
        {
            bool v = SafeGetBool(_piSoftShadows, asset, true);
            softShadowsToggle.SetIsOnWithoutNotify(v);
        }
    }

    // Helpers: current RP, quality-aware set
    RenderPipelineAsset GetCurrentRP()
    {
        // Prefer per-quality if available
        try
        {
            if (_miGetQualityRP != null)
            {
                int level = QualitySettings.GetQualityLevel();
                var rp = _miGetQualityRP.Invoke(null, new object[] { level }) as RenderPipelineAsset;
                if (rp != null) return rp;
            }
        }
        catch { /* ignore */ }

        return GraphicsSettings.defaultRenderPipeline;
    }

    bool TrySetCurrentQualityRP(RenderPipelineAsset asset)
    {
        try
        {
            if (_miSetQualityRP != null)
            {
                int level = QualitySettings.GetQualityLevel();
                _miSetQualityRP.Invoke(null, new object[] { level, asset });
                return true;
            }
        }
        catch { /* ignore */ }
        return false;
    }

    // Reflection-safe getters/setters
    static void SafeSetFloat(PropertyInfo pi, object obj, float value)
    {
        try { pi.SetValue(obj, value, null); } catch { }
    }
    static float SafeGetFloat(PropertyInfo pi, object obj, float def)
    {
        try { var v = pi.GetValue(obj, null); if (v is float f) return f; } catch { }
        return def;
    }
    static void SafeSetInt(PropertyInfo pi, object obj, int value)
    {
        try { pi.SetValue(obj, value, null); } catch { }
    }
    static int SafeGetInt(PropertyInfo pi, object obj, int def)
    {
        try { var v = pi.GetValue(obj, null); if (v is int i) return i; } catch { }
        return def;
    }
    static void SafeSetBool(PropertyInfo pi, object obj, bool value)
    {
        try { pi.SetValue(obj, value, null); } catch { }
    }
    static bool SafeGetBool(PropertyInfo pi, object obj, bool def)
    {
        try { var v = pi.GetValue(obj, null); if (v is bool b) return b; } catch { }
        return def;
    }

    static int IndexToMSAA(int idx)
    {
        switch (idx)
        {
            default:
            case 0: return 1; // Off
            case 1: return 2;
            case 2: return 4;
            case 3: return 8;
        }
    }
    static int MSAAtoIndex(int samples)
    {
        switch (samples)
        {
            default:
            case 1: return 0;
            case 2: return 1;
            case 4: return 2;
            case 8: return 3;
        }
    }

    // uGUI creation helpers
    GameObject CreateGroup(string name, Transform parent = null)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
        go.transform.SetParent(parent ? parent : transform, false);
        var vg = go.GetComponent<VerticalLayoutGroup>();
        vg.childControlHeight = true;
        vg.childControlWidth = true;
        vg.childForceExpandHeight = false;
        vg.childForceExpandWidth = true;
        return go;
    }

    Toggle CreateToggle(string label, bool defaultOn = true, Transform parent = null)
    {
        var go = new GameObject(label, typeof(RectTransform), typeof(Toggle));
        go.transform.SetParent(parent ? parent : transform, false);

        var toggle = go.GetComponent<Toggle>();
        var bg = new GameObject("Background", typeof(Image));
        bg.transform.SetParent(go.transform, false);
        var check = new GameObject("Checkmark", typeof(Image));
        check.transform.SetParent(bg.transform, false);
        toggle.targetGraphic = bg.GetComponent<Image>();
        toggle.graphic = check.GetComponent<Image>();
        toggle.isOn = defaultOn;

        var txt = CreateLabel(label, go.transform);
        return toggle;
    }

    Button CreateButton(string label, Transform parent = null)
    {
        var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent ? parent : transform, false);
        var btn = go.GetComponent<Button>();
        CreateLabel(label, go.transform);
        return btn;
    }

    Text CreateLabel(string label, Transform parent = null)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent ? parent : transform, false);
        var txt = go.GetComponent<Text>();
        txt.text = label;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleLeft;
        return txt;
    }

    Dropdown CreateDropdown(string name, Transform parent = null)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Dropdown));
        go.transform.SetParent(parent ? parent : transform, false);
        var dd = go.GetComponent<Dropdown>();
        dd.options = new List<Dropdown.OptionData> { new Dropdown.OptionData("(empty)") };
        return dd;
    }

    Slider CreateSlider(float min, float max, float value, Transform parent = null)
    {
        var go = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        go.transform.SetParent(parent ? parent : transform, false);
        var s = go.GetComponent<Slider>();
        s.minValue = min; s.maxValue = max; s.value = value;
        return s;
    }

    static void SetInteractable(Selectable s, bool interactable)
    {
        if (!s) return;
        s.interactable = interactable;
        var cg = s.GetComponentInParent<CanvasGroup>();
        if (cg == null)
        {
            cg = s.gameObject.AddComponent<CanvasGroup>();
        }
        cg.alpha = interactable ? 1f : 0.5f;
    }
}