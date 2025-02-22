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
                // Resetta targetPosition alla posizione attuale se cambio modalità
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

    // Il salto è attivato volontariamente
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

    // Per la gestione dell'ultima direzione valida (hysteresis)
    private Vector3 lastNonZeroDirection = Vector3.zero;

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

        // Disabilita la rotazione automatica del NavMeshAgent
        navAgent.updateRotation = false;

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
        // Gestione per il player
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
                // Imposta la destinazione solo se il target è cambiato significativamente
                if (Vector3.Distance(navAgent.destination, targetPosition) > 0.1f)
                {
                    navAgent.SetDestination(targetPosition);
                }
                RotateNavmesh();
            }
        }
        // Gestione per gli NPC
        else if (AvatarType == AvatarType.NPC)
        {
            // Se è in focus, il NPCBehaviour gestirà la rotazione verso il player;
            // altrimenti, usiamo la rotazione basata sulla velocità del NavMeshAgent.
            NPCBehaviour npcBehavior = GetComponent<NPCBehaviour>();
            if (npcBehavior != null && npcBehavior.IsFocused)
            {
                // Non interveniamo: NPCBehaviour aggiorna la rotazione
            }
            else
            {
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

            // Se il player non è a terra e non ha saltato volontariamente, non permettiamo il controllo in volo
            if (!isGroundedDelayed && !isAirborne)
            {
                inputDir = Vector3.zero;
            }

            if (isAirborne && jumpInput)
                jumpInput = false;

            // Attiva il salto solo se a terra
            if (!isAirborne && jumpInput && isGroundedDelayed)
            {
                storedMoveDirection = inputDir;
                isAirborne = true;
                jumpStartTime = Time.time;
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                OnJump?.Invoke();
                jumpInput = false;
            }

            // Durante il salto, usa la direzione memorizzata
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

    // Metodo di rotazione modificato per usare l'ultima direzione valida (hysteresis)
    private void RotateNavmesh()
    {
        // Blocca l'aggiornamento se il percorso è in attesa o se l'agente è praticamente fermo
        if (navAgent.pathPending ||
           (navAgent.remainingDistance <= navAgent.stoppingDistance + 0.05f && navAgent.velocity.sqrMagnitude < 0.001f))
        {
            return;
        }

        // Usa la velocità reale dell'agente e azzera la componente verticale
        Vector3 moveDirection = navAgent.velocity;
        moveDirection.y = 0;

        // Se la velocità è troppo bassa, usa l'ultima direzione valida per evitare oscillazioni
        if (moveDirection.sqrMagnitude < 0.01f)
        {
            if (lastNonZeroDirection.sqrMagnitude < 0.01f)
                return;
            moveDirection = lastNonZeroDirection;
        }
        else
        {
            lastNonZeroDirection = moveDirection;
        }

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection.normalized) * Quaternion.Euler(0, rotationOffset, 0);
        
        // Aggiorna la rotazione solo se la differenza angolare è superiore a 1 grado
        if (Quaternion.Angle(transform.rotation, targetRotation) < 1f)
            return;
        
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, angularSpeed * Time.deltaTime);
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

    private void UpdateGroundedDelayedState()
    {
        bool actualGrounded = IsTouchingGround();
        if (actualGrounded != isGroundedDelayed)
        {
            float requiredDelay = isGroundedDelayed ? groundedToFalseDelay : falseToGroundedDelay;
            groundedStateTimer += Time.fixedDeltaTime;
            if (groundedStateTimer >= requiredDelay)
            {
                bool previousGrounded = isGroundedDelayed;
                isGroundedDelayed = actualGrounded;
                groundedStateTimer = 0f;
                if (!previousGrounded && isGroundedDelayed)
                {
                    OnLand?.Invoke();
                    isAirborne = false;
                }
            }
        }
        else
        {
            groundedStateTimer = 0f;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            if (moveInput.sqrMagnitude < 0.01f)
            {
                rb.velocity = new Vector3(0, rb.velocity.y, 0);
            }
            isAirborne = false;
        }
        else
        {
            foreach (ContactPoint contact in collision.contacts)
            {
                if (Vector3.Dot(contact.normal, Vector3.up) < 0.5f)
                {
                    if (moveInput.sqrMagnitude < 0.01f)
                    {
                        rb.velocity = new Vector3(0, rb.velocity.y, 0);
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
}
