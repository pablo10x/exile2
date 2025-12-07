using UnityEngine;

namespace UtilityAI.Examples
{
    /// <summary>
    /// Simple action that moves the agent's transform toward the target each frame when executed.
    /// </summary>
    [CreateAssetMenu(menuName = "Utility AI/Action/Examples/Move Towards Target", fileName = "MoveTowardsTargetAction")]
    public class MoveTowardsTargetAction : ActionBase
    {
        [Header("Movement")]
        public float speed = 3f;
        public float stopDistance = 0.5f;

        public override bool IsValid(AIContext context)
        {
            return context != null && context.self != null && context.target != null;
        }

        public override void Execute(AIContext context)
        {
            if (context == null || context.self == null || context.target == null) return;

            Vector3 toTarget = context.target.position - context.self.position;
            float dist = toTarget.magnitude;
            if (dist <= stopDistance) return;

            Vector3 dir = toTarget.normalized;
            context.self.position += dir * speed * Time.deltaTime;
        }
    }
}
