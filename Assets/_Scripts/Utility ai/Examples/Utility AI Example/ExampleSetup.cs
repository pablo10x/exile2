using System.Collections.Generic;
using UnityEngine;
using UtilityAI;
using UtilityAI.Examples;

namespace UtilityAI.Examples
{
    /// <summary>
    /// Plug-and-play example that wires a UtilityAIController with two actions at runtime:
    /// - MoveTowardsTarget when far away from target.
    /// - Eat when close to target and hungry.
    /// Attach this script to a GameObject, set the Target in the inspector, and press Play.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UtilityAIController))]
    [RequireComponent(typeof(AIContext))]
    public class ExampleSetup : MonoBehaviour
    {
        [Header("Scene References")]
        public Transform Target;

        [Header("Agent Settings")] 
        [Tooltip("Initial normalized hunger (0 = full, 1 = starving)")]
        [Range(0f,1f)] public float initialHunger = 0.25f;
        [Tooltip("How fast hunger grows over time when not eating")]
        public float hungerIncreasePerSecond01 = 0.05f;

        private AIContext _context;
        private UtilityAIController _controller;

        // Created at runtime for demo purposes. You can also make assets in the Project window.
        private HungerConsideration _hungry;
        private DistanceToTargetConsideration _distance;
        private MoveTowardsTargetAction _move;
        private EatAction _eat;

        private void Awake()
        {
            _context = GetComponent<AIContext>();
            _controller = GetComponent<UtilityAIController>();
            if (_context.self == null) _context.self = transform;
            _context.target = Target;
            _context.hunger01 = initialHunger;

            // Create considerations
            _hungry = ScriptableObject.CreateInstance<HungerConsideration>();
            _hungry.name = "Hungry (High When Hungry)";
            _hungry.hungryIsHigh = true;
            _hungry.weight = 1f;
            _hungry.useCurve = true;
            _hungry.curve = AnimationCurve.Linear(0, 0, 1, 1); // pass-through

            _distance = ScriptableObject.CreateInstance<DistanceToTargetConsideration>();
            _distance.name = "Distance To Target";
            _distance.maxUsefulDistance = 10f;
            _distance.weight = 1f;
            _distance.useCurve = true; // We'll override curve per-action by duplicating instances

            // Create actions
            _move = ScriptableObject.CreateInstance<MoveTowardsTargetAction>();
            _move.name = "Move Towards Target (Far)";
            _move.speed = 3.5f;
            _move.stopDistance = 0.75f;
            _move.useCurve = false; // action-level curve off
            _move.weight = 1f;

            _eat = ScriptableObject.CreateInstance<EatAction>();
            _eat.name = "Eat (Close)";
            _eat.eatRange = 1.25f;
            _eat.eatPerSecond01 = 0.35f;
            _eat.useCurve = false;
            _eat.weight = 1f;

            // We want Move when far: use an inverted distance curve (far -> high score)
            var farDistance = ScriptableObject.CreateInstance<DistanceToTargetConsideration>();
            farDistance.name = "Distance (Far Is High)";
            farDistance.maxUsefulDistance = _distance.maxUsefulDistance;
            farDistance.weight = 1f;
            farDistance.useCurve = true;
            farDistance.curve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f)); // invert

            // We want Eat when close: direct distance curve (close -> high)
            var closeDistance = ScriptableObject.CreateInstance<DistanceToTargetConsideration>();
            closeDistance.name = "Distance (Close Is High)";
            closeDistance.maxUsefulDistance = _distance.maxUsefulDistance;
            closeDistance.weight = 1f;
            closeDistance.useCurve = true;
            closeDistance.curve = AnimationCurve.Linear(0, 1, 1, 0); // Also inverted? For close high, raw already maps close->1, so pass-through
            closeDistance.curve = AnimationCurve.Linear(0, 0, 1, 1); // correct pass-through

            // Setup considerations for each action
            _move.considerations = new List<ConsiderationBase> { farDistance };
            _eat.considerations = new List<ConsiderationBase> { _hungry, closeDistance };

            // Assign actions to controller
            _controller.actions = new List<ActionBase> { _eat, _move };
        }

        private void Update()
        {
            // Simulate hunger increasing over time when not eating
            // If EatAction executes (close to target), it will reduce hunger instead
            _context.hunger01 = Mathf.Clamp01(_context.hunger01 + hungerIncreasePerSecond01 * Time.deltaTime);
        }
    }
}
