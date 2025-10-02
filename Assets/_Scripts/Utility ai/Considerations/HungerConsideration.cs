using UnityEngine;

namespace UtilityAI
{
    [CreateAssetMenu(menuName = "Utility AI/Consideration/Hunger", fileName = "HungerConsideration")]
    public class HungerConsideration : ConsiderationBase
    {
        [Tooltip("If true, returns 1 when starving and 0 when full. If false, inverted.")]
        public bool hungryIsHigh = true;

        protected override float Score(AIContext context)
        {
            if (context == null) return 0f;
            return hungryIsHigh ? Mathf.Clamp01(context.hunger01) : 1f - Mathf.Clamp01(context.hunger01);
        }
    }
}
