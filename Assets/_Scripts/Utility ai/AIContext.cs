using UnityEngine;

namespace UtilityAI
{
    /// <summary>
    /// Shared context passed to considerations and actions.
    /// Extend this or derive a custom context for your agents.
    /// </summary>
    public class AIContext : MonoBehaviour
    {
        [Header("Common References")] 
        public Transform self;           // The agent root/transform
        public Transform target;         // Optional target (enemy, food, waypoint, etc.)

        [Header("Common State Values")] 
        [Range(0f, 1f)] public float energy01 = 1f;    // Normalized energy/stamina
        [Range(0f, 1f)] public float hunger01 = 0f;    // 0 = full, 1 = starving
        [Range(0f, 1f)] public float fear01 = 0f;      // 0 = calm, 1 = terrified

        private void Reset()
        {
            self = transform;
        }
    }
}
