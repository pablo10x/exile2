using UnityEngine;

namespace UtilityAI.Examples
{
    [CreateAssetMenu(menuName = "Utility AI/State/Idle", fileName = "IdleState")]
    public class IdleStateSO : UtilityAI.AIState
    {
        public override void OnEnter(UtilityAI.AIContext context)
        {
            Debug.Log("[UtilityAI] Enter IdleState");
        }

        public override void OnUpdate(UtilityAI.AIContext context)
        {
            // Idle: you could play an idle animation, look around, etc.
        }

        public override void OnExit(UtilityAI.AIContext context)
        {
            Debug.Log("[UtilityAI] Exit IdleState");
        }
    }
}
