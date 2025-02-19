using UnityEngine;
using UnityEngine.AI;

public enum MovementType { Keyboard, PointAndClick }
public enum AvatarType { Player, NPC }
public enum MovementState { Idle, Walking, Jumping }

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class AvatarController : MonoBehaviour
{
    [Header("Tipologia Avatar")]
    public AvatarType avatarType = AvatarType.Player;

    [Header("Modalità di Movimento")]
    [SerializeField] private MovementType movementType = MovementType.Keyboard;
    public MovementType MovementMode {
        get => movementType;
        set {
            if (avatarType == AvatarType.NPC)
                movementType = MovementType.PointAndClick;
            else {
                movementType = value;
                UpdateMovementMode();
            }
        }
    }

    [Header("Parametri Movimento")]
    [SerializeField] private float keyboardSpeed = 6f;
    [SerializeField] private float navMeshSpeed = 6f;
    [SerializeField] private float jumpForce = 5f;
    [Tooltip("Offset (in gradi) per correggere la direzione frontale del modello")]
    public float rotationOffset = 0f;

    [Header("Patrolling NPC")]
    [SerializeField] private float patrolRadius = 10f;
    [SerializeField] private float waitTimeAtPatrolPoint = 2f;

    // Stato dell'avatar
    private MovementState currentState = MovementState.Idle;

    // Componenti
    private Rigidbody rb;
    private NavMeshAgent navAgent;
    private Transform camTransform;

    // Variabili input
    private Vector2 moveInput;
    private bool jumpInput;
    private Vector3 targetPosition; // per PointAndClick

    // Variabili patrolling
    private bool waiting = false;
    private float waitTimer = 0f;

    private Vector3 lastNonZeroDirection = Vector3.forward;
    [SerializeField] private float smoothFactorNavMesh = 10f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        camTransform = Camera.main != null ? Camera.main.transform : null;
        navAgent = GetComponent<NavMeshAgent>();

        if (avatarType == AvatarType.NPC)
            movementType = MovementType.PointAndClick;

        UpdateMovementMode();
    }

    private void UpdateMovementMode()
    {
        if (navAgent != null)
        {
            if (movementType == MovementType.Keyboard && avatarType == AvatarType.Player)
            {
                navAgent.enabled = false;
                Debug.Log("Modalità Keyboard: NavMeshAgent disabilitato");
            }
            else
            {
                navAgent.enabled = true;
                navAgent.updatePosition = false;
                navAgent.speed = navMeshSpeed;
                Debug.Log("Modalità PointAndClick: NavMeshAgent abilitato (updatePosition=false)");
            }
        }
    }

    void Update()
    {
        if (avatarType == AvatarType.Player)
        {
            if (movementType == MovementType.Keyboard)
            {
                // Gestiamo lo stato (es. Idle/Walking) in base all'input
                HandleKeyboardInput();
            }
            else if (movementType == MovementType.PointAndClick)
            {
                HandlePointAndClickInput();
            }
        }
        else // NPC: esegue il patrolling
        {
            HandleNPCPatrol();
        }

        RotateInMovementDirection();
    }

    // Movimento fisico
    void FixedUpdate()
    {
        if (avatarType == AvatarType.Player)
        {
            if (movementType == MovementType.Keyboard)
            {
                // Calcola la direzione di movimento
                Vector3 moveDir = Vector3.zero;
                if (camTransform != null)
                {
                    Vector3 camForward = camTransform.forward; camForward.y = 0; camForward.Normalize();
                    Vector3 camRight = camTransform.right; camRight.y = 0; camRight.Normalize();
                    moveDir = (camRight * moveInput.x + camForward * moveInput.y).normalized;
                }
                else
                {
                    moveDir = new Vector3(moveInput.x, 0, moveInput.y).normalized;
                }
                
                Vector3 targetPos = rb.position + moveDir * keyboardSpeed * Time.fixedDeltaTime;
                rb.MovePosition(targetPos);

                
                if (jumpInput && IsGrounded())
                {
                    rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                    jumpInput = false;
                }
            }
            else if (movementType == MovementType.PointAndClick)
            {
                
                if (navAgent != null && navAgent.enabled)
                {
                    Vector3 targetPos = Vector3.Lerp(rb.position, navAgent.nextPosition, smoothFactorNavMesh * Time.fixedDeltaTime);
                    rb.MovePosition(targetPos);

                    if (jumpInput && IsGrounded())
                    {
                        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                        jumpInput = false;
                    }
                }
            }
        }
        else
        {
            if (navAgent != null && navAgent.enabled)
            {
                Vector3 targetPos = Vector3.Lerp(rb.position, navAgent.nextPosition, smoothFactorNavMesh * Time.fixedDeltaTime);
                rb.MovePosition(targetPos);
            }
        }
    }

    #region Input e Stato per il Player Keyboard/PointAndClick
    private void HandleKeyboardInput()
    {
        currentState = moveInput.sqrMagnitude > 0.01f ? MovementState.Walking : MovementState.Idle;
    }

    private void HandlePointAndClickInput()
    {
        if (!navAgent.pathPending &&
            navAgent.remainingDistance <= navAgent.stoppingDistance &&
            navAgent.velocity.sqrMagnitude < 0.01f)
        {
            currentState = MovementState.Idle;
        }
        else
        {
            currentState = MovementState.Walking;
        }
    }
    #endregion

    #region Patrolling NPC
    private void HandleNPCPatrol()
    {
        if (navAgent == null) return;
        if (!navAgent.pathPending &&
            navAgent.remainingDistance <= navAgent.stoppingDistance &&
            navAgent.velocity.sqrMagnitude < 0.01f)
        {
            if (!waiting)
            {
                waiting = true;
                waitTimer = 0f;
            }
            else
            {
                waitTimer += Time.deltaTime;
                if (waitTimer >= waitTimeAtPatrolPoint)
                {
                    waiting = false;
                    Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
                    randomDirection += transform.position;
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, NavMesh.AllAreas))
                    {
                        navAgent.SetDestination(hit.position);
                        Debug.Log("NPC nuova destinazione: " + hit.position);
                    }
                }
            }
        }
        else
        {
            waiting = false;
            waitTimer = 0f;
        }
        currentState = (!navAgent.pathPending &&
                        navAgent.remainingDistance <= navAgent.stoppingDistance &&
                        navAgent.velocity.sqrMagnitude < 0.01f)
                        ? MovementState.Idle : MovementState.Walking;
    }
    #endregion

    #region Salto e Controllo del Terreno
    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, 1.1f);
    }
    #endregion

    #region Rotazione dell'Avatar
    private void RotateInMovementDirection()
    {
        Vector3 direction = Vector3.zero;
        if (avatarType == AvatarType.Player)
        {
            if (movementType == MovementType.Keyboard)
            {
                if (camTransform != null)
                {
                    Vector3 camForward = camTransform.forward; camForward.y = 0; camForward.Normalize();
                    Vector3 camRight = camTransform.right; camRight.y = 0; camRight.Normalize();
                    direction = (camRight * moveInput.x + camForward * moveInput.y);
                }
                else
                {
                    direction = new Vector3(moveInput.x, 0, moveInput.y);
                }
            }
            else if (movementType == MovementType.PointAndClick && navAgent != null)
            {
                if (navAgent.velocity.sqrMagnitude > 0.1f)
                    direction = navAgent.velocity;
            }
        }
        else // NPC
        {
            if (navAgent != null && navAgent.velocity.sqrMagnitude > 0.1f)
                direction = navAgent.velocity;
        }
        if (direction.sqrMagnitude < 0.01f)
            direction = lastNonZeroDirection;
        else
            lastNonZeroDirection = direction;
        if (direction.sqrMagnitude > 0.001f)
        {
            float smoothFactor = 8f;
            Quaternion targetRotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, rotationOffset, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothFactor);
        }
    }
    #endregion

    #region GUI Toggle (solo per il Player)
    private void OnGUI()
    {
        if (avatarType != AvatarType.Player) return;
        string buttonText = movementType == MovementType.Keyboard ? "Switch to PointAndClick" : "Switch to Keyboard";
        if (GUI.Button(new Rect(10, 10, 220, 40), buttonText))
        {
            MovementMode = movementType == MovementType.Keyboard ? MovementType.PointAndClick : MovementType.Keyboard;
        }
    }
    #endregion

    #region Metodi Pubblici di Set (per PlayerInputManager)
    public void SetMoveInput(Vector2 input)
    {
        if (avatarType != AvatarType.Player) return;
        moveInput = input;
    }

    public void SetJumpInput(bool jump)
    {
        if (avatarType != AvatarType.Player) return;
        jumpInput = jump;
    }

    public void SetTargetPosition(Vector3 pos)
    {
        if (avatarType != AvatarType.Player) return;
        targetPosition = pos;
        if (navAgent != null && navAgent.enabled)
        {
            navAgent.SetDestination(targetPosition);
            Debug.Log("Destinazione impostata: " + targetPosition);
        }
    }
    #endregion
}
