using System.Collections.Generic;
using UnityEngine;

namespace UtilityAI
{
    /// <summary>
    /// Evaluates a set of actions and executes the highest-scoring one.
    /// Attach this to your agent GameObject alongside an AIContext.
    /// </summary>
    [DefaultExecutionOrder(10)]
    public class UtilityAIController : MonoBehaviour
    {
        [Tooltip("The context shared with actions and considerations.")]
        public AIContext context;

        [Tooltip("ScriptableObject actions to evaluate.")]
        public List<ActionBase> actions = new List<ActionBase>();

        [Header("Evaluation")]
        public bool evaluateEveryUpdate = true;
        [Min(0f)] public float evaluateInterval = 0.2f; // used if evaluateEveryUpdate == false

        private float _nextEvaluationTime;
        public ActionBase lastBestAction { get; private set; }

        [Header("State Machine (optional)")]
        [Tooltip("If present, selected actions can drive state transitions via ActionBase.stateOnExecute.")]
        public AIStateMachine stateMachine;

        private void Reset()
        {
            context = GetComponent<AIContext>();
            stateMachine = GetComponent<AIStateMachine>();
        }

        private void Update()
        {
            if (evaluateEveryUpdate)
            {
                Tick();
            }
            else if (Time.time >= _nextEvaluationTime)
            {
                _nextEvaluationTime = Time.time + evaluateInterval;
                Tick();
            }
        }

        /// <summary>
        /// Manually evaluates and executes the best action.
        /// </summary>
        public void Tick()
        {
            if (context == null) context = GetComponent<AIContext>();
            if (stateMachine == null) stateMachine = GetComponent<AIStateMachine>();
            if (actions == null || actions.Count == 0 || context == null) return;

            ActionBase best = null;
            float bestScore = 0f;

            for (int i = 0; i < actions.Count; i++)
            {
                var act = actions[i];
                if (act == null) continue;
                var score = act.CalculateScore(context);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = act;
                }
            }

            lastBestAction = best;
            if (best != null && bestScore > 0f)
            {
                // Transition to the action's state if provided and a state machine is available
                if (stateMachine != null && best.stateOnExecute != null)
                {
                    stateMachine.SetState(best.stateOnExecute, context);
                }
                
                // Execute the action's behavior this tick
                best.Execute(context);
            }
        }
    }
}
