using UnityEngine;
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;

#region Enums
public enum InfectionType {
    None,
    WoundInfection,      // From untreated cuts and wounds
    FoodPoisoning,       // From spoiled food
    Dysentery,           // Severe intestinal infection (realistic alternative to cholera)
    CommonCold,          // Respiratory infection (realistic alternative to influenza)
    GastricInfection,    // Stomach infection (realistic alternative to salmonella)
    Bacteremia,          // Bacteria in bloodstream (realistic alternative to blood infection)
    ToxicPoisoning,      // From contaminated water/food (realistic alternative to chemical)
    HeatStroke,          // Heat-related illness
    Frostbite            // Cold-related tissue damage
}

public enum InjuryType {
    None,
    Fracture,
    BulletWound,
    Cut,
    Bruise,
    Burn
}

public enum BloodType {
    ONegative,
    OPositive,
    ANegative,
    APositive,
    BNegative,
    BPositive,
    ABNegative,
    ABPositive
}

public enum ConsciousnessState {
    Conscious,
    Stunned,
    Unconscious,
    Dead
}

public enum TemperatureState {
    Hypothermia,
    Cold,
    Normal,
    Hot,
    Hyperthermia
}
#endregion

#region Status Structures
[Serializable]
public struct InfectionStatus {
    public InfectionType type;
    [Range(0, 100)] public float severity;
    public float tickDamage;
    public float duration;
    public float timeElapsed;
    public bool treatmentRequired;

    public bool IsActive => type != InfectionType.None && severity > 0;
}

[Serializable]
public struct InjuryStatus {
    public InjuryType type;
    public string bodyPart;
    [Range(0, 100)] public float severity;
    public bool isBandaged;
    public bool requiresSurgery;
    public float healTime;

    public bool IsActive => type != InjuryType.None && severity > 0;
}

[Serializable]
public struct BloodStatus {
    [Range(0, 5000)] public float volume; // ml
    public BloodType bloodType;
    public bool isRegenerating;
    public float regenRate;
}
#endregion

public class PlayerStatus : MonoBehaviour {
    
    #region Core Vitals
    [BoxGroup("Core Vitals")]
    [ProgressBar(0, 100, ColorGetter = "GetHealthBarColor")]
    [SerializeField] private float health = 100f;

    [BoxGroup("Core Vitals")]
    [ProgressBar(0, 5000, 0, 150, 0)]
    [SerializeField] private BloodStatus blood = new BloodStatus { volume = 5000f, bloodType = BloodType.OPositive, regenRate = 1f };

    [BoxGroup("Core Vitals")]
    [ProgressBar(0, 100, ColorGetter = "GetEnergyBarColor")]
    [SerializeField] private float energy = 100f;

    [BoxGroup("Core Vitals")]
    [ProgressBar(0, 100, ColorGetter = "GetHydrationBarColor")]
    [SerializeField] private float hydration = 100f;

    [BoxGroup("Core Vitals")]
    [ProgressBar(0, 100, 1, 0.5f, 0)]
    [SerializeField] private float stamina = 100f;

    [BoxGroup("Core Vitals")]
    [SerializeField, Range(20, 42)] private float bodyTemp = 36.6f; // Celsius
    #endregion

    #region Advanced Stats
    [BoxGroup("Advanced Stats")]
    [ProgressBar(0, 100)]
    [SerializeField] private float immunity = 100f;

    [BoxGroup("Advanced Stats")]
    [SerializeField, Range(0, 100)] private float shock = 0f; // Shock damage

    [BoxGroup("Advanced Stats")]
    [SerializeField, Range(0, 100)] private float pain = 0f;

    [BoxGroup("Advanced Stats")]
    [SerializeField] private ConsciousnessState consciousness = ConsciousnessState.Conscious;

    [BoxGroup("Advanced Stats")]
    [SerializeField, ReadOnly] private TemperatureState tempState = TemperatureState.Normal;

    

    [BoxGroup("Advanced Stats")]
    [SerializeField, ReadOnly] private bool isBleeding = false;

    [BoxGroup("Advanced Stats")]
    [SerializeField, ReadOnly] private int bleedingWounds = 0;
    #endregion

    #region Status Effects
    [BoxGroup("Status")]
    [ListDrawerSettings(ShowFoldout = true)]
    [SerializeField] private List<InfectionStatus> activeInfections = new List<InfectionStatus>();

    [BoxGroup("Status")]
    [ListDrawerSettings(ShowFoldout = true)]
    [SerializeField] private List<InjuryStatus> activeInjuries = new List<InjuryStatus>();

   

   

    [BoxGroup("Effects")]
    [SerializeField] private bool isVomiting = false;

    [BoxGroup("Effects")]
    [SerializeField] private bool isCoughing = false;

    [BoxGroup("Effects")]
    [SerializeField] private bool isSneaking = false;

    [BoxGroup("Effects")]
    [SerializeField] private bool isRunning = false;
    #endregion

    #region Decay Rates
    [FoldoutGroup("Decay Rates")]
    [SerializeField] private float energyDecayIdle = 0.05f;

    [FoldoutGroup("Decay Rates")]
    [SerializeField] private float energyDecayWalking = 0.1f;

    [FoldoutGroup("Decay Rates")]
    [SerializeField] private float energyDecayRunning = 0.3f;

    [FoldoutGroup("Decay Rates")]
    [SerializeField] private float hydrationDecayIdle = 0.08f;

    [FoldoutGroup("Decay Rates")]
    [SerializeField] private float hydrationDecayWalking = 0.15f;

    [FoldoutGroup("Decay Rates")]
    [SerializeField] private float hydrationDecayRunning = 0.4f;

    [FoldoutGroup("Decay Rates")]
    [SerializeField] private float staminaRegenIdle = 8f;

    [FoldoutGroup("Decay Rates")]
    [SerializeField] private float staminaRegenWalking = 3f;

    [FoldoutGroup("Decay Rates")]
    [SerializeField] private float staminaDrainRunning = 15f;

    [FoldoutGroup("Decay Rates")]
    [SerializeField] private float bloodRegenRate = 0.5f; // ml per second when healthy

    [FoldoutGroup("Decay Rates")]
    [SerializeField] private float bleedRatePerWound = 5f; // ml per second per wound
    #endregion

    #region Thresholds
    [FoldoutGroup("Thresholds")]
    [SerializeField] private float unconsciousHealthThreshold = 10f;

    [FoldoutGroup("Thresholds")]
    [SerializeField] private float unconsciousBloodThreshold = 2000f;

    [FoldoutGroup("Thresholds")]
    [SerializeField] private float criticalBloodLevel = 1500f;

    [FoldoutGroup("Thresholds")]
    [SerializeField] private float starvationHealthDrain = 2f;

    [FoldoutGroup("Thresholds")]
    [SerializeField] private float dehydrationHealthDrain = 4f;

    [FoldoutGroup("Thresholds")]
    [SerializeField] private float hypothermiaDamage = 1f;

    [FoldoutGroup("Thresholds")]
    [SerializeField] private float hyperthermiaDamage = 1.5f;
    #endregion

    #region Events
    public event Action OnDeath;
    public event Action OnUnconscious;
    public event Action OnRegainConsciousness;
    public event Action<float> OnHealthChanged;
    public event Action<float> OnBloodChanged;
    public event Action<float> OnEnergyChanged;
    public event Action<float> OnHydrationChanged;
    public event Action<float> OnStaminaChanged;
    public event Action<float> OnPainChanged;
    public event Action<ConsciousnessState> OnConsciousnessChanged;
    public event Action OnStartBleeding;
    public event Action OnStopBleeding;
    public event Action<InfectionType> OnInfectionAdded;
    public event Action<InjuryType> OnInjuryAdded;
    #endregion

    #region Private Variables
    [SerializeField] private float updateInterval = 0.5f;
    private float updateTimer;
    private float vomitTimer;
    private float coughTimer;
    private float unconsciousTimer;
    #endregion

    #region Unity Lifecycle
    private void Start() {
        InitializeStats();
    }

    private void Update() {
        updateTimer -= Time.deltaTime;

        if (updateTimer <= 0f) {
            updateTimer = updateInterval;
            TickUpdate(updateInterval);
        }

        HandleStaminaDrain(Time.deltaTime);
        HandleSymptoms(Time.deltaTime);
    }
    #endregion

    #region Initialization
    private void InitializeStats() {
        health = 100f;
        energy = 100f;
        hydration = 100f;
        stamina = 100f;
        blood.volume = 5000f;
        blood.isRegenerating = true;
        immunity = 100f;
        bodyTemp = 36.6f;
        consciousness = ConsciousnessState.Conscious;
        updateTimer = updateInterval;
    }
    #endregion

    #region Main Update Logic
    private void TickUpdate(float delta) {
        if (consciousness == ConsciousnessState.Dead) return;

        UpdateMetabolism(delta);
        UpdateBlood(delta);
        UpdateInfections(delta);
        UpdateInjuries(delta);
        UpdateTemperature(delta);
        UpdateShockAndPain(delta);
        UpdateConsciousness(delta);
        CheckDeathConditions();
    }

    private void UpdateMetabolism(float delta) {
        // Energy decay based on activity
        float energyDecay = isRunning ? energyDecayRunning : 
                           isSneaking ? energyDecayIdle : energyDecayWalking;
        
        energy = Mathf.Max(0, energy - energyDecay * delta);

        // Hydration decay
        float hydrationDecay = isRunning ? hydrationDecayRunning : 
                              isSneaking ? hydrationDecayIdle : hydrationDecayWalking;
        
        hydration = Mathf.Max(0, hydration - hydrationDecay * delta);

        // Starvation and dehydration damage
        if (energy <= 0) {
            ModifyHealth(-starvationHealthDrain * delta);
        }

        if (hydration <= 0) {
            ModifyHealth(-dehydrationHealthDrain * delta);
        }

        OnEnergyChanged?.Invoke(energy);
        OnHydrationChanged?.Invoke(hydration);
    }

    private void UpdateBlood(float delta) {
        // Blood loss from wounds
        if (isBleeding && bleedingWounds > 0) {
            float bloodLoss = bleedRatePerWound * bleedingWounds * delta;
            blood.volume = Mathf.Max(0, blood.volume - bloodLoss);
        }

        // Natural blood regeneration (requires energy and hydration)
        if (blood.volume < 5000f && energy > 30 && hydration > 30 && !isBleeding) {
            blood.isRegenerating = true;
            float regenAmount = bloodRegenRate * delta * (energy / 100f);
            blood.volume = Mathf.Min(5000f, blood.volume + regenAmount);
        } else if (isBleeding) {
            blood.isRegenerating = false;
        }

        // Critical blood loss effects
        if (blood.volume < criticalBloodLevel) {
            ModifyHealth(-0.5f * delta);
            shock = Mathf.Min(100, shock + 10f * delta);
        }

        OnBloodChanged?.Invoke(blood.volume);
    }

    private void UpdateInfections(float delta) {
        for (int i = activeInfections.Count - 1; i >= 0; i--) {
            InfectionStatus infection = activeInfections[i];
            infection.timeElapsed += delta;

            // Infection worsens over time without treatment
            if (!infection.treatmentRequired || infection.severity > 80) {
                infection.severity = Mathf.Min(100, infection.severity + 0.5f * delta);
            }

            // Apply damage
            ModifyHealth(-infection.tickDamage * delta);
            
            // Reduce immunity
            immunity = Mathf.Max(0, immunity - 0.1f * delta);

            // Infection-specific effects
            ApplyInfectionEffects(infection, delta);

            // Remove if duration expired or cured
            if (infection.duration > 0) {
                infection.duration -= delta;
                if (infection.duration <= 0) {
                    activeInfections.RemoveAt(i);
                    continue;
                }
            }

            activeInfections[i] = infection;
        }
    }

    private void ApplyInfectionEffects(InfectionStatus infection, float delta) {
        switch (infection.type) {
            case InfectionType.FoodPoisoning:
            case InfectionType.GastricInfection:
                isVomiting = true;
                if (UnityEngine.Random.value < 0.1f * delta) {
                    ModifyEnergy(-5f);
                    ModifyHydration(-5f);
                }
                break;

            case InfectionType.CommonCold:
                isCoughing = true;
                bodyTemp = Mathf.Min(39.5f, bodyTemp + 0.4f * delta);
                break;

            case InfectionType.Dysentery:
                ModifyHydration(-1.5f * delta);
                ModifyEnergy(-0.5f * delta);
                isVomiting = true;
                break;

            case InfectionType.WoundInfection:
            case InfectionType.Bacteremia:
                bodyTemp = Mathf.Min(40f, bodyTemp + 0.8f * delta);
                shock = Mathf.Min(100, shock + 5f * delta);
                break;

            case InfectionType.ToxicPoisoning:
                isVomiting = true;
                ModifyHealth(-0.8f * delta);
                shock = Mathf.Min(100, shock + 3f * delta);
                break;

            case InfectionType.HeatStroke:
                bodyTemp = Mathf.Min(41f, bodyTemp + 1f * delta);
                ModifyHydration(-2f * delta);
                shock = Mathf.Min(100, shock + 4f * delta);
                break;

            case InfectionType.Frostbite:
                // Localized damage, mainly affects extremities
                ModifyHealth(-0.3f * delta);
                pain = Mathf.Min(100, pain + 2f * delta);
                break;
        }
    }

    private void UpdateInjuries(float delta) {
        bleedingWounds = 0;

        for (int i = activeInjuries.Count - 1; i >= 0; i--) {
            InjuryStatus injury = activeInjuries[i];

            // Count bleeding wounds
            if (!injury.isBandaged && injury.type != InjuryType.Fracture && injury.type != InjuryType.Bruise) {
                bleedingWounds++;
            }

            // Natural healing (slower if not treated)
            if (injury.isBandaged || injury.type == InjuryType.Bruise) {
                injury.healTime -= delta;
                injury.severity = Mathf.Max(0, injury.severity - (5f * delta));
            }

            // Remove healed injuries
            if (injury.severity <= 0 || injury.healTime <= 0) {
                activeInjuries.RemoveAt(i);
                continue;
            }

            activeInjuries[i] = injury;
        }

        isBleeding = bleedingWounds > 0;
        if (!isBleeding && bleedingWounds == 0) {
            OnStopBleeding?.Invoke();
        }
    }

    private void UpdateTemperature(float delta) {
        // Natural temperature regulation toward 36.6°C
        float targetTemp = 36.6f;
        float tempDiff = targetTemp - bodyTemp;
        
        if (Mathf.Abs(tempDiff) > 0.1f) {
            bodyTemp += tempDiff * 0.1f * delta; // Slow regulation
        }

        // Update temperature state
        if (bodyTemp < 35f) {
            tempState = TemperatureState.Hypothermia;
            ModifyHealth(-hypothermiaDamage * delta);
            shock = Mathf.Min(100, shock + 2f * delta);
        } else if (bodyTemp < 36f) {
            tempState = TemperatureState.Cold;
        } else if (bodyTemp > 39f) {
            tempState = TemperatureState.Hyperthermia;
            ModifyHealth(-hyperthermiaDamage * delta);
            ModifyHydration(-0.5f * delta);
        } else if (bodyTemp > 37.5f) {
            tempState = TemperatureState.Hot;
        } else {
            tempState = TemperatureState.Normal;
        }
    }

    private void UpdateShockAndPain(float delta) {
        // Shock naturally decreases
        shock = Mathf.Max(0, shock - 5f * delta);

        // Pain naturally decreases
        pain = Mathf.Max(0, pain - 3f * delta);

       
        OnPainChanged?.Invoke(pain);
    }

    private void UpdateConsciousness(float delta) {
        ConsciousnessState previousState = consciousness;

        // Check unconsciousness conditions
        if (consciousness == ConsciousnessState.Conscious) {
            if (health <= unconsciousHealthThreshold || 
                blood.volume <= unconsciousBloodThreshold || 
                shock >= 100f) {
                
                consciousness = ConsciousnessState.Unconscious;
                unconsciousTimer = 0f;
                OnUnconscious?.Invoke();
            }
        }

        // Recovery from unconsciousness
        if (consciousness == ConsciousnessState.Unconscious) {
            unconsciousTimer += delta;

            bool canRecover = health > unconsciousHealthThreshold + 10f &&
                            blood.volume > unconsciousBloodThreshold + 500f &&
                            shock < 50f;

            if (canRecover && unconsciousTimer > 10f) {
                consciousness = ConsciousnessState.Conscious;
                OnRegainConsciousness?.Invoke();
            }
        }

        if (previousState != consciousness) {
            OnConsciousnessChanged?.Invoke(consciousness);
        }
    }

    private void HandleStaminaDrain(float delta) {
        if (isRunning && stamina > 0) {
            stamina = Mathf.Max(0, stamina - staminaDrainRunning * delta);
        } else if (!isRunning) {
            float regenRate = isSneaking ? staminaRegenIdle : staminaRegenWalking;
            stamina = Mathf.Min(100f, stamina + regenRate * delta);
        }

        OnStaminaChanged?.Invoke(stamina);
    }

    private void HandleSymptoms(float delta) {
        // Vomiting
        if (isVomiting) {
            vomitTimer += delta;
            if (vomitTimer >= UnityEngine.Random.Range(30f, 60f)) {
                vomitTimer = 0f;
                // Trigger vomit effect
                Debug.Log("Player vomits");
            }
        }

        // Coughing
        if (isCoughing) {
            coughTimer += delta;
            if (coughTimer >= UnityEngine.Random.Range(10f, 20f)) {
                coughTimer = 0f;
                // Trigger cough effect
                Debug.Log("Player coughs");
            }
        }
    }

    private void CheckDeathConditions() {
        if (health <= 0 || blood.volume <= 0) {
            Die();
        }
    }
    #endregion

    #region Public Modifiers
    [Button("Modify Health"), ButtonGroup("Actions")]
    public void ModifyHealth(float amount) {
        health = Mathf.Clamp(health + amount, 0, 100);
        OnHealthChanged?.Invoke(health);

        if (health <= 0) Die();
    }

    [Button("Modify Energy"), ButtonGroup("Actions")]
    public void ModifyEnergy(float amount) {
        energy = Mathf.Clamp(energy + amount, 0, 100);
        OnEnergyChanged?.Invoke(energy);
    }

    [Button("Modify Hydration"), ButtonGroup("Actions")]
    public void ModifyHydration(float amount) {
        hydration = Mathf.Clamp(hydration + amount, 0, 100);
        OnHydrationChanged?.Invoke(hydration);
    }

    [Button("Modify Stamina"), ButtonGroup("Actions")]
    public void ModifyStamina(float amount) {
        stamina = Mathf.Clamp(stamina + amount, 0, 100);
        OnStaminaChanged?.Invoke(stamina);
    }

    [Button("Modify Blood"), ButtonGroup("Actions")]
    public void ModifyBlood(float amount) {
        blood.volume = Mathf.Clamp(blood.volume + amount, 0, 5000);
        OnBloodChanged?.Invoke(blood.volume);
    }

    [Button("Add Pain"), ButtonGroup("Actions")]
    public void AddPain(float amount) {
        pain = Mathf.Clamp(pain + amount, 0, 100);
        OnPainChanged?.Invoke(pain);
    }

    [Button("Add Shock"), ButtonGroup("Actions")]
    public void AddShock(float amount) {
        shock = Mathf.Clamp(shock + amount, 0, 100);
    }
    #endregion

    #region Infection System
    [Button("Add Infection"), ButtonGroup("Infections")]
    public void AddInfection(InfectionType type, float severity, float duration = -1f) {
        InfectionStatus newInfection = new InfectionStatus {
            type = type,
            severity = Mathf.Clamp(severity, 0, 100),
            duration = duration,
            timeElapsed = 0f,
            tickDamage = Mathf.Lerp(0.1f, 3f, severity / 100f),
            treatmentRequired = severity > 50f
        };

        activeInfections.Add(newInfection);
        OnInfectionAdded?.Invoke(type);
    }

    [Button("Cure Infection"), ButtonGroup("Infections")]
    public void CureInfection(InfectionType type) {
        activeInfections.RemoveAll(inf => inf.type == type);
        
        // Reset symptoms if no infections remain
        if (activeInfections.Count == 0) {
            isVomiting = false;
            isCoughing = false;
        }
    }

    [Button("Clear All Infections"), ButtonGroup("Infections")]
    public void ClearAllInfections() {
        activeInfections.Clear();
        isVomiting = false;
        isCoughing = false;
    }
    #endregion

    #region Injury System
    [Button("Add Injury"), ButtonGroup("Injuries")]
    public void AddInjury(InjuryType type, string bodyPart, float severity, bool causeBleeding = true) {
        InjuryStatus newInjury = new InjuryStatus {
            type = type,
            bodyPart = bodyPart,
            severity = Mathf.Clamp(severity, 0, 100),
            isBandaged = false,
            requiresSurgery = type == InjuryType.BulletWound || type == InjuryType.Fracture,
            healTime = severity * 2f // 2 seconds per severity point
        };

        activeInjuries.Add(newInjury);
        
        if (causeBleeding && type != InjuryType.Fracture && type != InjuryType.Bruise) {
            if (!isBleeding) {
                isBleeding = true;
                OnStartBleeding?.Invoke();
            }
        }

        // Apply immediate effects
        AddPain(severity * 0.5f);
        AddShock(severity * 0.3f);

     

        OnInjuryAdded?.Invoke(type);
    }

    [Button("Bandage Wound"), ButtonGroup("Injuries")]
    public void BandageWound(int injuryIndex) {
        if (injuryIndex >= 0 && injuryIndex < activeInjuries.Count) {
            InjuryStatus injury = activeInjuries[injuryIndex];
            injury.isBandaged = true;
            activeInjuries[injuryIndex] = injury;
        }
    }

    [Button("Bandage All"), ButtonGroup("Injuries")]
    public void BandageAllWounds() {
        for (int i = 0; i < activeInjuries.Count; i++) {
            InjuryStatus injury = activeInjuries[i];
            injury.isBandaged = true;
            activeInjuries[i] = injury;
        }
    }
    #endregion

    #region Death System
    private void Die() {
        if (consciousness == ConsciousnessState.Dead) return;

        consciousness = ConsciousnessState.Dead;
        health = 0f;
        
        Debug.Log("Player has died.");
        OnDeath?.Invoke();
        OnConsciousnessChanged?.Invoke(consciousness);
    }

    [Button("Respawn"), ButtonGroup("Actions")]
    public void Respawn() {
        InitializeStats();
        activeInfections.Clear();
        activeInjuries.Clear();
        isBleeding = false;
        isVomiting = false;
        isCoughing = false;
      
      
        
        Debug.Log("Player respawned.");
    }
    #endregion

    #region Getters
    public float Health => health;
    public float Blood => blood.volume;
    public float Energy => energy;
    public float Hydration => hydration;
    public float Stamina => stamina;
    public float BodyTemp => bodyTemp;
    public float Pain => pain;
    public float Shock => shock;
    public float Immunity => immunity;
    public bool IsBleeding => isBleeding;
    public bool IsInPain => pain > 20f;
    public ConsciousnessState Consciousness => consciousness;
    public TemperatureState TempState => tempState;
    public int ActiveInfectionCount => activeInfections.Count;
    public int ActiveInjuryCount => activeInjuries.Count;
    #endregion

    #region Odin Inspector Helpers
    private Color GetHealthBarColor(float value) {
        if (value > 60f) return Color.green;
        if (value > 30f) return Color.yellow;
        return Color.red;
    }

    private Color GetEnergyBarColor(float value) {
        return Color.Lerp(Color.red, Color.yellow, value / 100f);
    }

    private Color GetHydrationBarColor(float value) {
        return Color.Lerp(Color.red, Color.cyan, value / 100f);
    }
    #endregion
}