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
            StartCoroutine(SimulatePlayerInput());
        }
    
        private IEnumerator SimulatePlayerInput()
        {
            while (true)
            {
                // Generate random joystick direction
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float magnitude = Random.Range(0.2f, 1f);
                simulatedJoystickDirection = new Vector2(
                    Mathf.Cos(angle) * magnitude,
                    Mathf.Sin(angle) * magnitude
                );
    
                // Choose movement type based on magnitude (similar to player joystick behavior)
                if (simulatedJoystickDirection.magnitude < 0.1f)
                {
                    // Idle
                    character.StopMoving();
                }
                else if (simulatedJoystickDirection.magnitude <= 0.8f || simulatedJoystickDirection.y < 0.8f)
                {
                    // Strafe
                    if (allowStrafe)
                    {
                        Vector3 targetPosition = transform.position + new Vector3(
                            simulatedJoystickDirection.x,
                            0,
                            simulatedJoystickDirection.y
                        ) * 5f;
                        
                        // Keep within movement radius
                        if (Vector3.Distance(targetPosition, startPosition) > movementRadius)
                        {
                            targetPosition = startPosition + (targetPosition - startPosition).normalized * movementRadius;
                        }
                        
                        character.SetDestination(targetPosition, Character.MovementSpeed.Walk);
                    }
                }
                else if (simulatedJoystickDirection.y > 0.8f && allowRunning)
                {
                    // Run forward
                    Vector3 targetPosition = transform.position + transform.forward * 10f;
                    
                    // Keep within movement radius
                    if (Vector3.Distance(targetPosition, startPosition) > movementRadius)
                    {
                        targetPosition = startPosition + (targetPosition - startPosition).normalized * movementRadius;
                    }
                    
                    character.SetDestination(targetPosition, Character.MovementSpeed.Run);
                }
    
                // Wait for random duration before next input change
                yield return new WaitForSeconds(Random.Range(minActionDuration, maxActionDuration));
            }
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
            character.StopMoving();
        }
    }