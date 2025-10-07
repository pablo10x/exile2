using UnityEngine;
    using System.Collections;
    
    public class AIBehaviorController : MonoBehaviour
    {
        private Character character;
        private float actionChangeInterval = 3f;
        private float movementRadius = 10f;
        private Vector3 startPosition;
        
        // Simulated joystick input
        private Vector2 simulatedJoystickDirection;
        
        [SerializeField] private bool allowStrafe = true;
        [SerializeField] private bool allowRunning = true;
        [SerializeField] private float minActionDuration = 1f;
        [SerializeField] private float maxActionDuration = 4f;
    
        private void Start()
        {
            character = GetComponent<Character>();
            startPosition = transform.position;
            
        }
    
     
    
        private void OnDrawGizmosSelected()
        {
            // Visualize movement radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(startPosition, movementRadius);
            
            // Visualize simulated joystick input
            if (Application.isPlaying)
            {
                Gizmos.color = Color.blue;
                Vector3 inputVisualization = transform.position + new Vector3(
                    simulatedJoystickDirection.x,
                    0,
                    simulatedJoystickDirection.y
                );
                Gizmos.DrawLine(transform.position, inputVisualization);
            }
        }
    
        public void StopAIBehavior()
        {
            StopAllCoroutines();
         
        }
    }