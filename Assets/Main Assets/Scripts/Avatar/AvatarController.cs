using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public enum MovementType { Keyboard, PointAndClick }
public enum AvatarType { Player, NPC }
public enum MovementState { Idle, Walking, Jumping }

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(NavMeshAgent))]
public class AvatarController : MonoBehaviour
{
    [Header("Eventi")]
    public UnityEvent OnJump;
    public UnityEvent OnLand;

    [Header("Modalità di Movimento")]
    [SerializeField] private MovementType movementType = MovementType.Keyboard;
    public MovementType MovementMode {
        get => movementType;
        set {
            if (AvatarType == AvatarType.NPC)
                movementType = MovementType.PointAndClick;
            else {
                // resettare targetPosition alla posizione attuale
                if (movementType != value && value == MovementType.PointAndClick)
                {
                    targetPosition = transform.position;
                    if (navAgent != null && navAgent.enabled)
                    {
                        navAgent.SetDestination(targetPosition);
                    }
                }
                movementType = value;
                UpdateMovementMode();
            }
        }
    }

    [Header("Parametri Movimento")]
    [SerializeField] private float keyboardSpeed = 6f;
    [SerializeField] private float navMeshSpeed = 6f;
    [SerializeField] private float jumpForce = 5f;
    public float rotationOffset = 0f;

    [Header("Keyboard Movement Settings")]
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float angularSpeed = 360f;
    [SerializeField] private float airControlFactor = 0.5f;
    private Vector3 _velocitySmoothDamp = Vector3.zero;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckOffset = 0.1f;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Ground Check Delay")]
    [SerializeField] private float groundedToFalseDelay = 0.5f;
    [SerializeField] private float falseToGroundedDelay = 0.1f;
    private bool isGroundedDelayed = true;
    private float groundedStateTimer = 0f;

    private bool isAirborne = false;
    private Vector3 storedMoveDirection = Vector3.zero;
    private float jumpStartTime = 0f;
    public MovementState currentState = MovementState.Idle;

    private Rigidbody rb;
    private NavMeshAgent navAgent;
    private Transform camTransform;

    private Vector2 moveInput;
    private bool jumpInput;
    private Vector3 targetPosition;

    public AvatarType AvatarType {
        get { return GetComponent<NPCBehaviour>() != null ? AvatarType.NPC : AvatarType.Player; }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        camTransform = Camera.main != null ? Camera.main.transform : null;
        navAgent = GetComponent<NavMeshAgent>();

        if (AvatarType == AvatarType.NPC)
            movementType = MovementType.PointAndClick;

        UpdateMovementMode();
    }

    private void UpdateMovementMode()
    {
        if (navAgent != null)
        {
            if (movementType == MovementType.Keyboard && AvatarType == AvatarType.Player)
            {
                navAgent.enabled = false;
            }
            else
            {
                navAgent.enabled = true;
                navAgent.updatePosition = true;
                navAgent.speed = navMeshSpeed;
                navAgent.acceleration = acceleration;
                navAgent.angularSpeed = angularSpeed;
                navAgent.stoppingDistance = 0.1f;
            }
        }
    }

    void Update()
    {
        if (AvatarType == AvatarType.Player)
        {
            if (movementType == MovementType.Keyboard)
            {
                HandleKeyboardInput();
                // In modalità Keyboard non viene impostata la destinazione
            }
            else if (movementType == MovementType.PointAndClick)
            {
                HandlePointAndClickInput();
                navAgent.SetDestination(targetPosition);
                RotateNavmesh();
            }
        }
    }

    void FixedUpdate()
    {
        UpdateGroundedDelayedState();

        if (AvatarType == AvatarType.Player && movementType == MovementType.Keyboard)
        {
            Vector3 inputDir = new Vector3(-moveInput.y, 0, moveInput.x).normalized;

            if (isAirborne && jumpInput)
                jumpInput = false;

            if (!isAirborne && jumpInput && isGroundedDelayed)
            {
                storedMoveDirection = inputDir;
                isAirborne = true;
                jumpStartTime = Time.time;
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                OnJump?.Invoke();
                jumpInput = false;
            }

            if (isAirborne)
                inputDir = storedMoveDirection;

            float effectiveSpeed = isAirborne ? keyboardSpeed * airControlFactor : keyboardSpeed;
            Vector3 targetVelocity = inputDir * effectiveSpeed;

            Vector3 currentHorizontal = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            Vector3 newHorizontal = Vector3.MoveTowards(currentHorizontal, targetVelocity, acceleration * Time.fixedDeltaTime);

            Vector3 groundNormal = GetGroundNormal();
            Vector3 adjustedVelocity = Vector3.ProjectOnPlane(newHorizontal, groundNormal);

            rb.velocity = new Vector3(adjustedVelocity.x, rb.velocity.y, adjustedVelocity.z);

            RotateKeyboard();
        }
        
        rb.angularVelocity = Vector3.zero;
    }

    public float GetMaxSpeed()
    {
        return (MovementMode == MovementType.Keyboard) ? keyboardSpeed : navMeshSpeed;
    }

    private void RotateKeyboard()
    {
        Vector3 direction = isAirborne ? storedMoveDirection : new Vector3(rb.velocity.x, 0, rb.velocity.z);
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized) * Quaternion.Euler(0, rotationOffset, 0);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, angularSpeed * Time.fixedDeltaTime);
        }
    }

    private void RotateNavmesh()
    {
        Vector3 dir = targetPosition - transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(dir.normalized) * Quaternion.Euler(0, rotationOffset, 0);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, angularSpeed * Time.deltaTime);
        }
    }

    private void HandleKeyboardInput()
    {
        if (!IsTouchingGround())
            currentState = MovementState.Jumping;
        else
            currentState = moveInput.sqrMagnitude > 0.01f ? MovementState.Walking : MovementState.Idle;
    }

    private void HandlePointAndClickInput()
    {
        if (!IsTouchingGround())
            currentState = MovementState.Jumping;
        else if (!navAgent.pathPending &&
                 navAgent.remainingDistance <= navAgent.stoppingDistance &&
                 navAgent.desiredVelocity.sqrMagnitude < 0.01f)
        {
            currentState = MovementState.Idle;
        }
        else
        {
            currentState = MovementState.Walking;
        }
    }

    public bool IsTouchingGround()
    {
        Vector3 sphereCenter = transform.position + Vector3.down * groundCheckOffset;
        Debug.DrawRay(sphereCenter, Vector3.up * 0.1f, Color.red);
        return Physics.CheckSphere(sphereCenter, groundCheckRadius, groundLayer);
    }

    private Vector3 GetGroundNormal()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, groundCheckOffset + groundCheckRadius + 0.2f, groundLayer))
            return hit.normal;
        return Vector3.up;
    }

    private void OnGUI()
    {
        if (!DebugManager.DebugState) return;
        if (AvatarType != AvatarType.Player) return;
        
        string buttonText = movementType == MovementType.Keyboard ? "Switch to PointAndClick" : "Switch to Keyboard";
        if (GUI.Button(new Rect(10, 10, 220, 40), buttonText))
        {
            MovementMode = movementType == MovementType.Keyboard ? MovementType.PointAndClick : MovementType.Keyboard;
        }
    }

    public void SetMoveInput(Vector2 input)
    {
        if (AvatarType != AvatarType.Player) return;
        moveInput = input;
    }

    public void SetJumpInput(bool jump)
    {
        if (AvatarType != AvatarType.Player) return;
        jumpInput = jump;
    }

    public void SetTargetPosition(Vector3 pos)
    {
        if (AvatarType != AvatarType.Player) return;
        targetPosition = pos;
        if (navAgent != null && navAgent.enabled)
        {
            navAgent.SetDestination(targetPosition);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            if (isAirborne)
            {
                OnLand?.Invoke();
                Vector3 effectiveDirection = new Vector3(-moveInput.y, 0, moveInput.x).normalized;
                if (effectiveDirection.sqrMagnitude < 0.01f)
                    effectiveDirection = storedMoveDirection;
                rb.velocity = new Vector3(effectiveDirection.x * keyboardSpeed, rb.velocity.y, effectiveDirection.z * keyboardSpeed);
            }
            isAirborne = false;
        }
        else
        {
            foreach (ContactPoint contact in collision.contacts)
            {
                if (Vector3.Dot(contact.normal, Vector3.up) < 0.5f)
                {
                    if (isAirborne)
                    {
                        OnLand?.Invoke();
                        Vector3 effectiveDirection = new Vector3(-moveInput.y, 0, moveInput.x).normalized;
                        if (effectiveDirection.sqrMagnitude < 0.01f)
                            effectiveDirection = storedMoveDirection;
                        rb.velocity = new Vector3(effectiveDirection.x * keyboardSpeed, rb.velocity.y, effectiveDirection.z * keyboardSpeed);
                    }
                    isAirborne = false;
                    break;
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 sphereCenter = transform.position + Vector3.down * groundCheckOffset;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(sphereCenter, groundCheckRadius);
    }

    public void StopMovement()
    {
        if (AvatarType != AvatarType.Player) return;
        if (MovementMode == MovementType.PointAndClick && navAgent != null && navAgent.enabled)
        {
            navAgent.ResetPath();
        }
        SetMoveInput(Vector2.zero);
        if (rb != null)
        {
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
        }
    }

    private void UpdateGroundedDelayedState()
    {
        bool currentCheck = IsTouchingGround();
        if (currentCheck != isGroundedDelayed)
        {
            float requiredDelay = isGroundedDelayed ? groundedToFalseDelay : falseToGroundedDelay;
            groundedStateTimer += Time.fixedDeltaTime;
            if (groundedStateTimer >= requiredDelay)
            {
                isGroundedDelayed = currentCheck;
                groundedStateTimer = 0f;
            }
        }
        else
        {
            groundedStateTimer = 0f;
        }
    }
}
