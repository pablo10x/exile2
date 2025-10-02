using UnityEngine;

namespace UtilityAI
{
    /// <summary>
    /// Simple finite state machine which runs ScriptableObject states.
    /// </summary>
    [DefaultExecutionOrder(11)]
    public class AIStateMachine : MonoBehaviour
    {
        [Tooltip("Shared AI context. If null, will try to GetComponent at runtime.")]
        public AIContext context;

        [Tooltip("Current active state (read-only at runtime).")]
        public AIState currentState;

        /// <summary>
        /// Switch to the given state. Calls OnExit on the previous state and OnEnter on the new one.
        /// </summary>
        public void SetState(AIState newState, AIContext forContext = null)
        {
            if (forContext != null)
                context = forContext;
            if (context == null)
                context = GetComponent<AIContext>();

            if (currentState == newState) return;

            if (currentState != null)
                currentState.OnExit(context);

            currentState = newState;

            if (currentState != null)
                currentState.OnEnter(context);
        }

        private void Reset()
        {
            context = GetComponent<AIContext>();
        }

        private void Update()
        {
            if (context == null)
                context = GetComponent<AIContext>();

            if (currentState != null)
                currentState.OnUpdate(context);
        }
    }
}
