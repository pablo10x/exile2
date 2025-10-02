using System.Collections.Generic;
using UnityEngine;

namespace UtilityAI.Examples
{
    /// <summary>
    /// Demonstrates integrating the UtilityAIController with the AIStateMachine.
    /// Creates two actions at runtime (MoveTowardsTarget and Eat) and assigns stateOnExecute
    /// so the FSM transitions into Idle / Eating states when those actions are chosen.
    /// Attach to a GameObject with AIContext; this script will ensure the controller and state machine exist.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AIContext))]
    public class ExampleSetup_FSM : MonoBehaviour
    {
        [Header("Scene References")] public Transform Target;

        [Header("Agent Settings")] 
        [Range(0,1)] public float initialHunger = 0.4f;
        public float hungerIncreasePerSecond01 = 0.05f;

        private AIContext _context;
        private UtilityAIController _controller;
        private AIStateMachine _stateMachine;

        // runtime-created assets for demonstration
        private AIState _idleState;
        private AIState _eatingState;

        private MoveTowardsTargetAction _move;
        private EatAction _eat;

        private void Awake()
        {
            _context = GetComponent<AIContext>();
            if (_context.self == null) _context.self = transform;
            _context.target = Target;
            _context.hunger01 = initialHunger;

            _controller = gameObject.GetComponent<UtilityAIController>();
            if (_controller == null) _controller = gameObject.AddComponent<UtilityAIController>();

            _stateMachine = gameObject.GetComponent<AIStateMachine>();
            if (_stateMachine == null) _stateMachine = gameObject.AddComponent<AIStateMachine>();

            // Create simple states (use base AIState for minimal example)
            _idleState = ScriptableObject.CreateInstance<AIState>();
            _idleState.name = "Idle (Runtime)";
            _eatingState = ScriptableObject.CreateInstance<AIState>();
            _eatingState.name = "Eating (Runtime)";

            // Create actions
            _move = ScriptableObject.CreateInstance<MoveTowardsTargetAction>();
            _move.name = "Move Towards Target";
            _move.speed = 3.5f;
            _move.stopDistance = 1.0f;
            _move.stateOnExecute = _idleState; // When moving, be in Idle-like locomotion state

            _eat = ScriptableObject.CreateInstance<EatAction>();
            _eat.name = "Eat";
            _eat.eatRange = 1.25f;
            _eat.eatPerSecond01 = 0.35f;
            _eat.stateOnExecute = _eatingState; // When eating, switch to Eating state

            // Considerations
            var farDistance = ScriptableObject.CreateInstance<DistanceToTargetConsideration>();
            farDistance.name = "Distance (Far Is High)";
            farDistance.maxUsefulDistance = 10f;
            farDistance.useCurve = true;
            farDistance.curve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));

            var closeDistance = ScriptableObject.CreateInstance<DistanceToTargetConsideration>();
            closeDistance.name = "Distance (Close Is High)";
            closeDistance.maxUsefulDistance = 10f;
            closeDistance.useCurve = true;
            closeDistance.curve = AnimationCurve.Linear(0, 0, 1, 1);

            var hungry = ScriptableObject.CreateInstance<HungerConsideration>();
            hungry.name = "Hungry";
            hungry.hungryIsHigh = true;
            hungry.useCurve = true;
            hungry.curve = AnimationCurve.Linear(0, 0, 1, 1);

            _move.considerations = new List<ConsiderationBase> { farDistance };
            _eat.considerations = new List<ConsiderationBase> { hungry, closeDistance };

            _controller.context = _context;
            _controller.stateMachine = _stateMachine;
            _controller.actions = new List<ActionBase> { _eat, _move };
        }

        private void Update()
        {
            // Simulate hunger rising over time
            _context.hunger01 = Mathf.Clamp01(_context.hunger01 + hungerIncreasePerSecond01 * Time.deltaTime);
        }
    }
}
