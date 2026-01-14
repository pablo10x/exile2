using UnityEngine;
using UnityEngine.Events;

namespace KinematicCharacterController.Examples
{
    public class Teleporter : MonoBehaviour
    {
        public Transform TeleportTo;

        public UnityAction<ExampleCharacterController> OnCharacterTeleport;

        public bool isBeingTeleportedTo { get; set; }

        private void OnTriggerEnter(Collider other)
        {
            var cc = other.GetComponent<KinematicCharacterMotor>();
            if (cc) cc.SetPositionAndRotation(TeleportTo.transform.position, TeleportTo.transform.rotation);
        }
    }
}