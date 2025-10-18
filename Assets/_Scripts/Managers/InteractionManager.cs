using System.Collections.Generic;
using UnityEngine;

namespace core.Managers {
    public class InteractionManager : Singleton<InteractionManager> {
        private readonly HashSet<IInteractable> _interactables = new();



        public void RegisterInteractable(IInteractable interactable) {
            _interactables.Add(interactable);
        }

        public void UnregisterInteractable(IInteractable interactable) {
            _interactables.Remove(interactable);
        }

        /// <summary>
        /// Finds the closest interactable to a given position within a range.
        /// </summary>
        public IInteractable GetClosestInteractable(Vector3 position, float range) {
            IInteractable closest       = null;
            float         closestDistSq = range * range;

            foreach (var interactable in _interactables) {
                float distSq = (interactable.GetPosition() - position).sqrMagnitude;
                if (distSq < closestDistSq) {
                    closest       = interactable;
                    closestDistSq = distSq;
                }
            }

            return closest;
        }
    }
}