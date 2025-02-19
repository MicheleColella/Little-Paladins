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

    // Impostazioni per il movimento con tastiera
    [Header("Keyboard Movement Settings")]
    [Tooltip("Velocità di accelerazione (unità/secondo)")]
    [SerializeField] private float acceleration = 10f;
    [Tooltip("Velocità di decelerazione (unità/secondo)")]
    [SerializeField] private float deceleration = 10f;
    [Tooltip("Velocità angolare (gradi/secondo) per la rotazione")]
    [SerializeField] private float angularSpeed = 360f;
    [Tooltip("Fattore di controllo in aria (0 = nessun controllo, 1 = controllo uguale a terra)")]
    [SerializeField] private float airControlFactor = 0.5f;
    private Vector3 _velocitySmoothDamp = Vector3.zero;
    [Tooltip("Moltiplicatore per il tempo di smoothing in aria rispetto a terra")]
    [SerializeField] private float inAirSmoothMultiplier = 3f;
    [SerializeField] private float baseSmoothTime = 0.05f;

    // Tempo per lo smoothing in modalità NavMesh – usato con SmoothDamp
    [SerializeField] private float navmeshSmoothTime = 0.1f;
    private Vector3 _navmeshVelocitySmoothDamp = Vector3.zero;

    [Header("Patrolling NPC")]
    [SerializeField] private float patrolRadius = 10f;
    [SerializeField] private float waitTimeAtPatrolPoint = 2f;
    [SerializeField] private float patrolTimeout = 5f;
    private float patrolTimer = 0f;

    // Stato dell'avatar
    private MovementState currentState = MovementState.Idle;

    // Componenti
    private Rigidbody rb;
    private NavMeshAgent navAgent;
    private Transform camTransform;

    // Variabili input (per il Player)
    private Vector2 moveInput;
    private bool jumpInput;
    private Vector3 targetPosition; // destinazione impostata dal raycast

    // Variabili per il patrolling NPC
    private bool waiting = false;
    private float waitTimer = 0f;

    // Parametri per il Ground Check
    [Header("Ground Check")]
    [SerializeField] private float groundCheckOffset = 0.1f;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    // Ritardo per il Ground Check
    [Header("Ground Check Delay")]
    [Tooltip("Delay (in secondi) per passare da grounded (true) a non grounded (false)")]
    [SerializeField] private float groundedToFalseDelay = 0.5f;
    [Tooltip("Delay (in secondi) per passare da non grounded (false) a grounded (true)")]
    [SerializeField] private float falseToGroundedDelay = 0.1f;
    private bool isGroundedDelayed = true;
    private float groundedStateTimer = 0f;

    // Variabili per la gestione del salto "inertia" (per il Player)
    private bool isAirborne = false;
    private Vector3 storedMoveDirection = Vector3.zero;
    [SerializeField] private float minAirborneTime = 0.2f;
    private float jumpStartTime = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

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
                navAgent.acceleration = acceleration;
                navAgent.angularSpeed = angularSpeed;
                navAgent.stoppingDistance = 0.1f;
                Debug.Log("Modalità PointAndClick: NavMeshAgent abilitato (updatePosition=false)");
            }
        }
    }

    void Update()
    {
        if (avatarType == AvatarType.Player)
        {
            if (movementType == MovementType.Keyboard)
                HandleKeyboardInput();
            else if (movementType == MovementType.PointAndClick)
                HandlePointAndClickInput();
        }
        else
        {
            HandleNPCPatrol();
        }
    }

    // In LateUpdate gestiamo sia la rotazione che il movimento NavMesh con SmoothDamp
    void LateUpdate()
    {
        if ((avatarType == AvatarType.Player && movementType == MovementType.PointAndClick) || avatarType == AvatarType.NPC)
        {
            if (navAgent != null && navAgent.enabled)
            {
                Vector3 agentTarget = navAgent.nextPosition;
                Vector3 currentPos = rb.position;
                // Manteniamo la componente verticale corrente
                Vector3 targetPos = new Vector3(agentTarget.x, currentPos.y, agentTarget.z);
                // Utilizziamo SmoothDamp per ottenere una transizione fluida
                Vector3 smoothPos = Vector3.SmoothDamp(currentPos, targetPos, ref _navmeshVelocitySmoothDamp, navmeshSmoothTime);
                rb.MovePosition(smoothPos);
            }

            RotateNavmesh();
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

    void FixedUpdate()
    {
        UpdateGroundedDelayedState();

        if (avatarType == AvatarType.Player)
        {
            if (movementType == MovementType.Keyboard)
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
                    jumpInput = false;
                }

                if (isAirborne)
                    inputDir = storedMoveDirection;

                float effectiveSpeed = isAirborne ? keyboardSpeed * airControlFactor : keyboardSpeed;
                Vector3 targetVelocity = inputDir * effectiveSpeed;
                float smoothTime = isAirborne ? baseSmoothTime * inAirSmoothMultiplier : baseSmoothTime;
                Vector3 currentHorizontal = new Vector3(rb.velocity.x, 0, rb.velocity.z);
                Vector3 newHorizontal = Vector3.SmoothDamp(currentHorizontal, targetVelocity, ref _velocitySmoothDamp, smoothTime);

                Vector3 groundNormal = GetGroundNormal();
                Vector3 adjustedVelocity = Vector3.ProjectOnPlane(newHorizontal, groundNormal);

                rb.velocity = new Vector3(adjustedVelocity.x, rb.velocity.y, adjustedVelocity.z);

                RotateKeyboard();
            }
            else if (movementType == MovementType.PointAndClick)
            {
                // In modalità NavMesh il salto viene gestito qui; il movimento orizzontale è in LateUpdate
                if (isAirborne && jumpInput)
                    jumpInput = false;
                if (!isAirborne && jumpInput && isGroundedDelayed)
                {
                    isAirborne = true;
                    jumpStartTime = Time.time;
                    rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                    jumpInput = false;
                }
            }
        }
        else
        {
            // NPC: eventuale salto o altre logiche in FixedUpdate (se necessario)
        }
    }

    #region Rotazione
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
        if (navAgent != null && navAgent.enabled)
        {
            Vector3 dir = navAgent.nextPosition - rb.position;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(dir.normalized) * Quaternion.Euler(0, rotationOffset, 0);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, angularSpeed * Time.deltaTime);
            }
        }
    }
    #endregion

    #region Input e Stato per il Player
    private void HandleKeyboardInput()
    {
        currentState = moveInput.sqrMagnitude > 0.01f ? MovementState.Walking : MovementState.Idle;
    }

    private void HandlePointAndClickInput()
    {
        if (!navAgent.pathPending &&
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
    #endregion

    #region Patrolling NPC
    private void HandleNPCPatrol()
    {
        if (navAgent == null) return;

        if (!navAgent.pathPending && navAgent.remainingDistance > navAgent.stoppingDistance && navAgent.desiredVelocity.sqrMagnitude < 0.01f)
        {
            patrolTimer += Time.deltaTime;
            if (patrolTimer >= patrolTimeout)
            {
                patrolTimer = 0f;
                SetNewPatrolDestination();
            }
        }
        else
        {
            patrolTimer = 0f;
        }

        if (!navAgent.pathPending &&
            navAgent.remainingDistance <= navAgent.stoppingDistance &&
            navAgent.desiredVelocity.sqrMagnitude < 0.01f)
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
                    SetNewPatrolDestination();
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
                        navAgent.desiredVelocity.sqrMagnitude < 0.01f)
                        ? MovementState.Idle : MovementState.Walking;
    }

    private void SetNewPatrolDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += transform.position;
        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
        {
            navAgent.SetDestination(hit.position);
            Debug.Log("NPC nuova destinazione: " + hit.position);
        }
    }
    #endregion

    #region Salto e Controllo del Terreno (Ground Check)
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

    #region Gestione Collisioni
    private void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            isAirborne = false;
            storedMoveDirection = Vector3.zero;
        }
        else
        {
            foreach (ContactPoint contact in collision.contacts)
            {
                if (Vector3.Dot(contact.normal, Vector3.up) < 0.5f)
                {
                    isAirborne = false;
                    storedMoveDirection = Vector3.zero;
                    break;
                }
            }
        }
    }
    #endregion

    #region Gizmos
    private void OnDrawGizmosSelected()
    {
        Vector3 sphereCenter = transform.position + Vector3.down * groundCheckOffset;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(sphereCenter, groundCheckRadius);
    }
    #endregion
}
