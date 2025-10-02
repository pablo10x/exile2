using UnityEngine;

namespace UtilityAI.Examples
{
    [CreateAssetMenu(menuName = "Utility AI/State Examples/Eating", fileName = "EatingState")]
    public class EatingStateSO : UtilityAI.AIState
    {
        public override void OnEnter(UtilityAI.AIContext context)
        {
            Debug.Log("[UtilityAI] Enter EatingState");
        }

        public override void OnUpdate(UtilityAI.AIContext context)
        {
            // Could trigger eating animation / VFX here.
        }

        public override void OnExit(UtilityAI.AIContext context)
        {
            Debug.Log("[UtilityAI] Exit EatingState");
        }
    }
}
