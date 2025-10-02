using System;
using UnityEngine;
using Sirenix.OdinInspector;

namespace UtilityAI
{
    /// <summary>
    /// Base consideration with an optional response curve and weight.
    /// </summary>
    [Serializable]
    public abstract class ConsiderationBase : ScriptableObject, IConsideration
    {
        [FoldoutGroup("Consideration/General", Expanded = true)]
        [LabelText("Display Name")]
        [SerializeField] private string _name = "Consideration";

        [FoldoutGroup("Consideration/General")]
        [Tooltip("Multiply the contribution of this consideration.")]
        [Range(0f, 2f)] public float weight = 1f;

        [FoldoutGroup("Consideration/Response Curve", Expanded = true)]
        [LabelText("Use Curve")] public bool useCurve = true;
        [FoldoutGroup("Consideration/Response Curve")] public AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);

        public string Name => _name;
        public float Weight => weight;

        /// <summary>
        /// Implement to return an unclamped raw value typically in [0,1].
        /// </summary>
        protected abstract float Score(AIContext context);

        public float Evaluate(AIContext context)
        {
            var raw = Mathf.Clamp01(Score(context));
            var shaped = useCurve && curve != null ? Mathf.Clamp01(curve.Evaluate(raw)) : raw;
            return Mathf.Clamp01(shaped) * Mathf.Max(0f, weight);
        }
    }
}
