using UnityEngine;

namespace UtilityAI
{
    [CreateAssetMenu(menuName = "Utility AI/Consideration/Distance To Target", fileName = "DistanceToTarget")]
    public class DistanceToTargetConsideration : ConsiderationBase
    {
        [Tooltip("Distances greater than this will evaluate near 0, shorter near 1 (before curve/weight).")]
        public float maxUsefulDistance = 20f;

        protected override float Score(AIContext context)
        {
            if (context == null || context.self == null || context.target == null || maxUsefulDistance <= 0f)
                return 0f;

            float d = Vector3.Distance(context.self.position, context.target.position);
            float t = 1f - Mathf.Clamp01(d / maxUsefulDistance); // closer = higher
            return t;
        }
    }
}
