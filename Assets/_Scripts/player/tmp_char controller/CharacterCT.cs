using UnityEngine;

/// <summary>
/// Lightweight, deterministic kinematic character controller.
/// Works with a CapsuleCollider, no Rigidbody required.
/// Suitable for singleplayer or predicted multiplayer.
/// </summary>
[RequireComponent(typeof(CapsuleCollider))]
public class CharacterCT : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float sprintMultiplier = 1.5f;
    public float airControl = 0.4f;

    [Header("Jumping")]
    public float jumpHeight = 1.5f;
    public float gravity = -20f;

    [Header("Grounding")]
    public float slopeLimit = 45f;
    public float groundCheckDistance = 0.1f;
    public LayerMask groundMask;

    [Header("Step Offset")]
    public float stepOffset = 0.3f;
    public float stepCheckDistance = 0.2f;

    private CapsuleCollider capsule;
    private Vector3 velocity;
    private Vector3 groundNormal;
    private bool isGrounded;

    private float CapsuleHeight => capsule.height;
    private float CapsuleRadius => capsule.radius;
    private Vector3 CapsuleBottom => transform.position + capsule.center - new Vector3(0, CapsuleHeight / 2f, 0);
    private Vector3 CapsuleTop => transform.position + capsule.center + new Vector3(0, CapsuleHeight / 2f, 0);

    void Awake()
    {
        capsule = GetComponent<CapsuleCollider>();

        // Disable physics interactions
        capsule.direction = 1; // Y axis
    }

    void Update()
    {
        // Example input wrapper — you can replace this
        Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        bool jumping = Input.GetKeyDown(KeyCode.Space);
        bool sprinting = Input.GetKey(KeyCode.LeftShift);

        MoveController(input, jumping, sprinting, Time.deltaTime);
    }

    /// <summary>
    /// Main movement function. Deterministic.
    /// </summary>
    public void MoveController(Vector2 input, bool jump, bool sprint, float dt)
    {
        CheckGround();

        // Horizontal movement direction
        Vector3 moveDir = (transform.forward * input.y + transform.right * input.x).normalized;

        // Apply slope-normal correction
        moveDir = Vector3.ProjectOnPlane(moveDir, groundNormal).normalized;

        // Calculate speed
        float targetSpeed = sprint ? moveSpeed * sprintMultiplier : moveSpeed;

        if (isGrounded)
        {
            // Grounded horizontal movement
            Vector3 desiredVelocity = moveDir * targetSpeed;
            velocity.x = desiredVelocity.x;
            velocity.z = desiredVelocity.z;

            // Jump
            if (jump)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                isGrounded = false;
            }
            else
            {
                // Stick to ground on slopes
                velocity.y = -2f;
            }
        }
        else
        {
            // Air control
            Vector3 airVel = moveDir * targetSpeed * airControl;
            velocity.x = Mathf.Lerp(velocity.x, airVel.x, dt * airControl * 5f);
            velocity.z = Mathf.Lerp(velocity.z, airVel.z, dt * airControl * 5f);

            // Gravity
            velocity.y += gravity * dt;
        }

        // Step offset handling
        Vector3 move = velocity * dt;
        if (isGrounded && input != Vector2.zero)
        {
            if (TryStep(ref move))
            {
                transform.position += move;
                return;
            }
        }

        // Regular movement with capsule cast
        MoveWithCollisions(move);
    }

    /// <summary>
    /// Checks if the character is on the ground using capsule cast.
    /// </summary>
    private void CheckGround()
    {
        isGrounded = false;
        groundNormal = Vector3.up;

        if (Physics.CapsuleCast(CapsuleTop, CapsuleBottom, CapsuleRadius, Vector3.down,
            out RaycastHit hit, groundCheckDistance, groundMask))
        {
            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            if (slopeAngle <= slopeLimit)
            {
                isGrounded = true;
                groundNormal = hit.normal;
            }
        }
    }

    /// <summary>
    /// Moves the capsule with collision resolution.
    /// </summary>
    private void MoveWithCollisions(Vector3 displacement)
    {
        if (Physics.CapsuleCast(CapsuleTop, CapsuleBottom, CapsuleRadius, displacement.normalized,
            out RaycastHit hit, displacement.magnitude, groundMask))
        {
            Vector3 normal = hit.normal;
            Vector3 slide = Vector3.ProjectOnPlane(displacement, normal);
            transform.position += slide;
        }
        else
        {
            transform.position += displacement;
        }
    }

    /// <summary>
    /// Attempt to step over small obstacles.
    /// </summary>
    private bool TryStep(ref Vector3 move)
    {
        Vector3 stepUp = Vector3.up * stepOffset;

        // Check if there is an obstacle in front
        if (Physics.CapsuleCast(CapsuleTop, CapsuleBottom, CapsuleRadius, move.normalized,
            out RaycastHit frontHit, stepCheckDistance, groundMask))
        {
            // Try stepping up
            Vector3 newPos = transform.position + stepUp + move;

            if (!Physics.CheckCapsule(
                newPos + capsule.center + Vector3.up * (CapsuleHeight / 2f),
                newPos + capsule.center - Vector3.up * (CapsuleHeight / 2f),
                CapsuleRadius, groundMask))
            {
                // Successful step
                move += stepUp;
                return true;
            }
        }
        return false;
    }
}
