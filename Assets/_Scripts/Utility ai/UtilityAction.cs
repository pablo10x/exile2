using UnityEngine;
using UnityEngine.Events;

namespace UtilityAI
{
    /// <summary>
    /// A simple concrete action that invokes a UnityEvent when executed.
    /// Useful for wiring animations, sounds, or method calls in the Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "Utility AI/Action/Utility Action", fileName = "UtilityAction")]
    public class UtilityAction : ActionBase
    {
        [TextArea]
        public string description;

        public UnityEvent<AIContext> onExecute;

        public override void Execute(AIContext context)
        {
            if (onExecute != null)
                onExecute.Invoke(context);
        }
    }
}
