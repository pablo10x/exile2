using UnityEngine;
using System;
using Sirenix.OdinInspector;

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
                  public InfectionType type;
    [Range(0, 100)] public float         severity;
                  public float         tickDamage;
                  public float         duration;

    public bool IsActive => type != InfectionType.None && severity > 0;
}

public class PlayerStatus : MonoBehaviour {
    // 🔹 Core Vital Stats
    [FoldoutGroup("Vitals"), Range(0, 100)] public float health = 100f;

    [FoldoutGroup("Vitals"), Range(0, 100)] public float hunger = 100f;

    [FoldoutGroup("Vitals"), Range(0, 100)] public float thirst = 100f;

    [FoldoutGroup("Vitals"), Range(0, 100)] public float stamina = 100f;

    [FoldoutGroup("Vitals"), Range(-10, 80)] public float temp = 30;

    // 🔹 Status Effects
    [FoldoutGroup("Status Effects")]  public bool isBleeding = false;

    [FoldoutGroup("Status Effects")]  public bool isInPain = false;

    [FoldoutGroup("Infection")] [BoxGroup("Infection/Details")] public InfectionStatus infection;

    // 🔹 Rates
    [FoldoutGroup("Rates"), SerializeField, LabelText("Bleed Damage/sec")] private float bleedRate = 1f;

    [FoldoutGroup("Rates"), SerializeField, LabelText("Hunger Decay/sec")] private float hungerDecay = 0.1f;

    [FoldoutGroup("Rates"), SerializeField, LabelText("Thirst Decay/sec")] private float thirstDecay = 0.2f;

    [FoldoutGroup("Rates"), SerializeField, LabelText("Stamina Regen/sec")] private float staminaRegen = 5f;

    // 🔹 Events
    public event Action        OnDeath;
    public event Action        OnHealthChanged;
    public event Action        OnHungerChanged;
    public event Action OnThirstChanged;
    public event Action        OnStaminaChanged;
    public event Action        OnStatusEffectChanged;

    public event Action OntempChanged;

    // Interval in seconds
    [SerializeField] private float interval = 0.5f;

    private float _timer;

    private void Start() {
        thirst  = 100;
        hunger  = 100;
        stamina = 100;
        temp    = 30;
    }

    // 🔹 Unity Update Hook
    private void Update() {
        // Count down
        _timer -= Time.deltaTime;

        if (_timer <= 0f) {
            // Reset timer
            _timer = interval;

            // Action every 0.5s
            Tick(Time.deltaTime);
        }
    }

    /// <summary>
    /// Called every time the timer reaches 0
    /// </summary>
    

    // 🔹 Tick Update Logic
    private void Tick(float deltaTime) {
        // Hunger/Thirst decay
        hunger = Mathf.Max(0, hunger - hungerDecay * deltaTime);
        thirst = Mathf.Max(0, thirst - thirstDecay * deltaTime);

        if (hunger <= 0) ModifyHealth(-2f * deltaTime);
        if (thirst <= 0) ModifyHealth(-4f * deltaTime);

        // Bleeding
        if (isBleeding) ModifyHealth(-bleedRate * deltaTime);

        // Infection
        if (infection.IsActive) {
            infection.duration -= deltaTime;
            ModifyHealth(-infection.tickDamage * deltaTime);

            if (infection.duration <= 0)
                ClearInfection();
        }

        // Stamina regen
        stamina = Mathf.Min(100f, stamina + staminaRegen * deltaTime);
        
        OnHealthChanged?.Invoke();
        OnHungerChanged?.Invoke();
        OnThirstChanged?.Invoke();
        OnStaminaChanged?.Invoke();
        OntempChanged?.Invoke();
    }

    // 🔹 Infection Controls
    [Button("Apply Infection")]
    public void ApplyInfection(InfectionType type, float severity, float duration) {
        infection.type       = type;
        infection.severity   = severity;
        infection.duration   = duration;
        infection.tickDamage = Mathf.Lerp(0.1f, 5f, severity / 100f);

        OnStatusEffectChanged?.Invoke();
    }

    [Button("Clear Infection")]
    public void ClearInfection() {
        infection.type       = InfectionType.None;
        infection.severity   = 0;
        infection.tickDamage = 0;
        infection.duration   = 0;

        OnStatusEffectChanged?.Invoke();
    }

    // 🔹 Core Modifiers

    [Button("Modify Health")]
    public void ModifyHealth(float amount) {
        health = Mathf.Clamp(health + amount, 0, 100);
        OnHealthChanged?.Invoke();

        if (health <= 0) OnDeath?.Invoke();
    }

    [Button("Modify Hunger")]
    public void ModifyHunger(float amount) {
        hunger = Mathf.Clamp(hunger + amount, 0, 100);
        OnHungerChanged?.Invoke();
    }

    [Button("Modify Thirst")]
    public void ModifyThirst(float amount) {
        thirst = Mathf.Clamp(thirst + amount, 0, 100);
      
        OnThirstChanged?.Invoke();
    }

    [Button("Modify Stamina")]
    public void ModifyStamina(float amount) {
        stamina = Mathf.Clamp(stamina + amount, 0, 100);
        OnStaminaChanged?.Invoke();
    }

    [Button("Modify Temp")]
    public void ModifyTemp(float amount) {
        temp = Mathf.Clamp(temp + amount, 10, 80);
        OntempChanged?.Invoke();
    }
}