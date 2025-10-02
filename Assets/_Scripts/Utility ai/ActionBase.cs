using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace UtilityAI
{
    /// <summary>
    /// Base ScriptableObject action with a list of considerations.
    /// Score is calculated as the product of all consideration scores (clamped 0..1),
    /// then optionally remapped by an action-level curve and weight.
    /// </summary>
    public abstract class ActionBase : ScriptableObject, IAction
    {
        [FoldoutGroup("Action/General")]
        [LabelText("Display Name")]
        [SerializeField] private string _name = "Action";

        [FoldoutGroup("Action/Considerations")]
        [Tooltip("Considerations that determine the utility of this action.")]
        [ListDrawerSettings(ShowFoldout = true, DraggableItems = true, ShowIndexLabels = false)]
        public List<ConsiderationBase> considerations = new List<ConsiderationBase>();

        [FoldoutGroup("Action/Scoring", Expanded = true)]
        [LabelText("Use Score Curve")] public bool useCurve = false;
        [FoldoutGroup("Action/Scoring")] public AnimationCurve scoreCurve = AnimationCurve.Linear(0, 0, 1, 1);
        [FoldoutGroup("Action/Scoring")] [Range(0f, 2f)] [LabelText("Weight")] public float weight = 1f;

        [FoldoutGroup("Action/State", Expanded = true)]
        [Tooltip("Optional: State machine state to enter when this action is selected.")]
        public AIState stateOnExecute;

        [System.NonSerialized] private float _score;
        public string Name => _name;
        public float Score => _score;

        public virtual bool IsValid(AIContext context) => context != null;

        public virtual float CalculateScore(AIContext context)
        {
            if (!IsValid(context))
            {
                _score = 0f;
                return _score;
            }

            float product = 1f;
            for (int i = 0; i < considerations.Count; i++)
            {
                var c = considerations[i];
                if (c == null) continue;
                product *= Mathf.Clamp01(c.Evaluate(context));
                if (product <= 0f) break;
            }

            float shaped = useCurve && scoreCurve != null ? Mathf.Clamp01(scoreCurve.Evaluate(product)) : product;
            _score = Mathf.Clamp01(shaped) * Mathf.Max(0f, weight);
            return _score;
        }

        /// <summary>
        /// Perform the action. The implementation is up to you.
        /// For example: move agent, play animation, set NavMesh destination, etc.
        /// </summary>
        public abstract void Execute(AIContext context);
    }
}
