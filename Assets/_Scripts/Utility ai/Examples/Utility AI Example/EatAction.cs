using UnityEngine;

namespace UtilityAI.Examples
{
    /// <summary>
    /// Reduces hunger when the agent is close enough to the target (e.g., food).
    /// </summary>
    [CreateAssetMenu(menuName = "Utility AI/Action/Examples/Eat", fileName = "EatAction")]
    public class EatAction : ActionBase
    {
        public float eatRange = 1.25f;
        public float eatPerSecond01 = 0.25f; // normalized hunger reduction per second

        public override bool IsValid(AIContext context)
        {
            return context != null && context.self != null && context.target != null;
        }

        public override void Execute(AIContext context)
        {
            if (context == null || context.self == null || context.target == null) return;

            float d = Vector3.Distance(context.self.position, context.target.position);
            if (d <= eatRange)
            {
                context.hunger01 = Mathf.Clamp01(context.hunger01 - eatPerSecond01 * Time.deltaTime);
            }
        }
    }
}
