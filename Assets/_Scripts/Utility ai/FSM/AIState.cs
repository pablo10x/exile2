using UnityEngine;

namespace UtilityAI
{
    /// <summary>
    /// Base AI State as a ScriptableObject so it can be authored and reused in assets.
    /// </summary>
    [CreateAssetMenu(menuName = "Utility AI/State", fileName = "AIState")]
    public class AIState : ScriptableObject
    {
        [SerializeField] private string _displayName = "State";
        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;

        /// <summary>
        /// Called when the state becomes active.
        /// </summary>
        public virtual void OnEnter(AIContext context) { }

        /// <summary>
        /// Called every Update while active.
        /// </summary>
        public virtual void OnUpdate(AIContext context) { }

        /// <summary>
        /// Called when exiting this state.
        /// </summary>
        public virtual void OnExit(AIContext context) { }
    }
}
