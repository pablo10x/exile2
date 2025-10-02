using UnityEngine;
using System;
using Sirenix.OdinInspector; // Odin Inspector


public enum InfectionType {
    None,
    WoundInfection,
    FoodPoisoning,
    Sepsis,
    Respiratory,
    Parasites
}

[Serializable]
public struct InfectionStatus {
    [LabelWidth(100)]                public InfectionType type;
    [Range(0, 100), LabelWidth(100)] public float         severity;
    [LabelWidth(100)]                public float         tickDamage;
    [LabelWidth(100)]                public float         duration;

    [ShowInInspector, ReadOnly] public bool IsActive => type != InfectionType.None && severity > 0;
}
public class PlayerStatus : MonoBehaviour
{
    // 🔹 Core Vital Stats
    [FoldoutGroup("Vitals"), Range(0, 100)]
    public float health = 100f;

    [FoldoutGroup("Vitals"), Range(0, 100)]
    public float hunger = 100f;

    [FoldoutGroup("Vitals"), Range(0, 100)]
    public float thirst = 100f;

    [FoldoutGroup("Vitals"), Range(0, 100)]
    public float stamina = 100f;

    // 🔹 Status Effects
    [FoldoutGroup("Status Effects")]
    [ToggleLeft] public bool isBleeding = false;

    [FoldoutGroup("Status Effects")]
    [ToggleLeft] public bool isInPain = false;

    [FoldoutGroup("Infection"), HideLabel]
    [InlineProperty, BoxGroup("Infection/Details", ShowLabel = false)]
    public InfectionStatus infection;

    // 🔹 Rates
    [FoldoutGroup("Rates"), SerializeField, LabelText("Bleed Damage/sec")]
    private float bleedRate = 1f;

    [FoldoutGroup("Rates"), SerializeField, LabelText("Hunger Decay/sec")]
    private float hungerDecay = 0.1f;

    [FoldoutGroup("Rates"), SerializeField, LabelText("Thirst Decay/sec")]
    private float thirstDecay = 0.2f;

    [FoldoutGroup("Rates"), SerializeField, LabelText("Stamina Regen/sec")]
    private float staminaRegen = 5f;

    // 🔹 Events
    public event Action OnDeath;
    public event Action OnHealthChanged;
    public event Action OnHungerChanged;
    public event Action OnThirstChanged;
    public event Action OnStaminaChanged;
    public event Action OnStatusEffectChanged;

    // 🔹 Unity Update Hook
    private void Update()
    {
        Tick(Time.deltaTime);
    }

    // 🔹 Tick Update Logic
    private void Tick(float deltaTime)
    {
        // Hunger/Thirst decay
        hunger = Mathf.Max(0, hunger - hungerDecay * deltaTime);
        thirst = Mathf.Max(0, thirst - thirstDecay * deltaTime);

        if (hunger <= 0) ModifyHealth(-2f * deltaTime);
        if (thirst <= 0) ModifyHealth(-4f * deltaTime);

        // Bleeding
        if (isBleeding) ModifyHealth(-bleedRate * deltaTime);

        // Infection
        if (infection.IsActive)
        {
            infection.duration -= deltaTime;
            ModifyHealth(-infection.tickDamage * deltaTime);

            if (infection.duration <= 0)
                ClearInfection();
        }

        // Stamina regen
        stamina = Mathf.Min(100f, stamina + staminaRegen * deltaTime);
    }

    // 🔹 Infection Controls
    [Button(ButtonSizes.Medium), GUIColor(1f, 0.6f, 0.6f)]
    public void ApplyInfection(InfectionType type, float severity, float duration)
    {
        infection.type = type;
        infection.severity = severity;
        infection.duration = duration;
        infection.tickDamage = Mathf.Lerp(0.1f, 5f, severity / 100f);

        OnStatusEffectChanged?.Invoke();
    }

    [Button(ButtonSizes.Medium), GUIColor(0.6f, 1f, 0.6f)]
    public void ClearInfection()
    {
        infection.type = InfectionType.None;
        infection.severity = 0;
        infection.tickDamage = 0;
        infection.duration = 0;

        OnStatusEffectChanged?.Invoke();
    }

    // 🔹 Core Modifiers
    public void ModifyHealth(float amount)
    {
        health = Mathf.Clamp(health + amount, 0, 100);
        OnHealthChanged?.Invoke();

        if (health <= 0) OnDeath?.Invoke();
    }

    public void ModifyHunger(float amount) {
    
        hunger = Mathf.Clamp(hunger + amount, 0, 100);
        OnHungerChanged?.Invoke();
    }
        
    public void ModifyThirst(float amount) {
        thirst = Mathf.Clamp(thirst + amount, 0, 100);
        OnThirstChanged?.Invoke();
    }

    public void ModifyStamina(float amount)
    {
        stamina = Mathf.Clamp(stamina + amount, 0, 100);
        OnStaminaChanged?.Invoke();
    }
}
