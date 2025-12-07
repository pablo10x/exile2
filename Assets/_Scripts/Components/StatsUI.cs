using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Generic Stat UI Controller.
/// Can be used for temperature, health, stamina, hunger, etc.
/// Updates an icon and a fill image with color and fill amount.
/// </summary>
public class StatsUI : MonoBehaviour {
    [BoxGroup("UI References")] [SerializeField] private Image iconImage; // Icon or outline
    [BoxGroup("UI References")] [SerializeField] private Image fillImage; // Fill sprite (set type to Filled)

    [BoxGroup("Stat Settings")] [SerializeField]         private float    minValue          = 0f;   // Minimum stat
    [BoxGroup("Stat Settings")] [SerializeField]         private float    maxValue          = 100f; // Maximum stat
    [BoxGroup("Color Change")] [SerializeField]          private bool     ChangeFillerColor = false;
    [BoxGroup("Color Change")] [SerializeField]          private bool     shouldchangeBasecolor = false;
    [BoxGroup("Color Change")]  [SerializeField] private Gradient colorGradient; // Color from empty to full

    [FoldoutGroup("Tween Settings")] [SerializeField] private float tweenDuration = 0.3f;          // seconds for smooth fill
  //  [FoldoutGroup("Tween Settings")] [SerializeField] private Ease  tweenEase     = Ease.Linear; // easing type
    private                                                                                      float currentValue;

    /// <summary>
    /// Updates the stat value and refreshes UI.
    /// </summary>
    public void SetValue(float value) {
      
        currentValue = Mathf.Clamp(value, minValue, maxValue);
        UpdateUI();
    }

    /// <summary>
    /// Sets the min and max values (optional).
    /// </summary>
    public void SetRange(float min, float max) {
        minValue = min;
        maxValue = max;
        SetValue(currentValue); // re-apply clamp
    }

    /// <summary>
    /// Updates the UI elements (fill + color).
    /// </summary>
    private void UpdateUI() {
        float normalized = Mathf.InverseLerp(minValue, maxValue, currentValue);
        // Gradient color
        Color color = colorGradient.Evaluate(normalized);
        // Fill amount
        if (fillImage != null)
            fillImage.DOFillAmount(normalized, tweenDuration)
                     .onComplete += () => { fillImage.fillAmount = normalized; };

       
            if (fillImage != null && ChangeFillerColor)
                fillImage.color = color;

            if (iconImage != null && shouldchangeBasecolor)
                iconImage.color = color;
        
    }
}