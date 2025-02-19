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

    // Impostazioni per il movimento con tastiera (utilizzate anche per il NavMeshAgent)
    [Header("Keyboard Movement Settings")]
    [Tooltip("Velocità di accelerazione (unità/secondo)")]
    [SerializeField] private float acceleration = 10f;
    [Tooltip("Velocità di decelerazione (unità/secondo)")]
    [SerializeField] private float deceleration = 10f;
    [Tooltip("Velocità angolare (gradi/secondo) per la rotazione")]
    [SerializeField] private float angularSpeed = 360f;
    [Tooltip("Fattore di controllo in aria (0 = nessun controllo, 1 = controllo uguale a terra)")]
    [SerializeField] private float airControlFactor = 0.5f;
    // Variabile per lo smooth della velocità (modalità tastiera)
    private Vector3 _velocitySmoothDamp = Vector3.zero;
    [Tooltip("Moltiplicatore per il tempo di smoothing in aria rispetto a terra")]
    [SerializeField] private float inAirSmoothMultiplier = 3f;
    // Tempo base per lo smooth (modalità tastiera)
    [SerializeField] private float baseSmoothTime = 0.05f;

    // Parametro di smoothing per la modalità NavMesh (PointAndClick / NPC)
    [SerializeField] private float navmeshSmoothTime = 0.1f;
    private Vector3 _navmeshVelocitySmoothDamp = Vector3.zero;

    [Header("Patrolling NPC")]
    [SerializeField] private float patrolRadius = 10f;
    [SerializeField] private float waitTimeAtPatrolPoint = 2f;
    [SerializeField] private float patrolTimeout = 5f; // Timeout per il patrol
    private float patrolTimer = 0f;

    // Stato dell'avatar
    private MovementState currentState = MovementState.Idle;

    // Componenti
    private Rigidbody rb;
    private NavMeshAgent navAgent;
    private Transform camTransform;

    // Variabili input (per il Player)
    private Vector2 moveInput; // aggiornato anche durante il salto
    private bool jumpInput;
    private Vector3 targetPosition; // per PointAndClick

    // Variabili patrolling (per NPC)
    private bool waiting = false;
    private float waitTimer = 0f;

    // Parametri per il Ground Check
    [Header("Ground Check")]
    [SerializeField] private float groundCheckOffset = 0.1f;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    // Parametri per il ritardo del Ground Check
    [Header("Ground Check Delay")]
    [Tooltip("Delay (in secondi) per passare da grounded (true) a non grounded (false)")]
    [SerializeField] private float groundedToFalseDelay = 0.5f;
    [Tooltip("Delay (in secondi) per passare da non grounded (false) a grounded (true)")]
    [SerializeField] private float falseToGroundedDelay = 0.1f;
    private bool isGroundedDelayed = true; // inizialmente considerato a terra
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
        // Gestione degli input
        if (avatarType == AvatarType.Player)
        {
            if (movementType == MovementType.Keyboard)
                HandleKeyboardInput();
            else if (movementType == MovementType.PointAndClick)
                HandlePointAndClickInput();
        }
        else // NPC
        {
            HandleNPCPatrol();
        }
    }

    // Per la modalità NavMesh (e NPC) la rotazione viene aggiornata in LateUpdate per stare in sincronia col ciclo della camera
    private void LateUpdate()
    {
        if ((avatarType == AvatarType.Player && movementType == MovementType.PointAndClick) || avatarType == AvatarType.NPC)
        {
            RotateNavmesh();
        }
    }

    // Aggiorna lo stato "grounded" con un ritardo
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

    // MOVIMENTO: utilizzo di rb.velocity per sfruttare l'interpolazione
    void FixedUpdate()
    {
        UpdateGroundedDelayedState();

        if (avatarType == AvatarType.Player)
        {
            if (movementType == MovementType.Keyboard)
            {
                // Calcola la direzione dall'input (orizzontale)
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

                // Proiezione sul piano definito dalla normale del terreno
                Vector3 groundNormal = GetGroundNormal();
                Vector3 adjustedVelocity = Vector3.ProjectOnPlane(newHorizontal, groundNormal);

                // Imposta la velocità mantenendo la componente verticale
                rb.velocity = new Vector3(adjustedVelocity.x, rb.velocity.y, adjustedVelocity.z);

                // ROTAZIONE per la modalità Keyboard aggiornata in FixedUpdate
                RotateKeyboard();
            }
            else if (movementType == MovementType.PointAndClick)
            {
                if (navAgent != null && navAgent.enabled)
                {
                    // Gestione del salto in modalità PointAndClick
                    if (isAirborne && jumpInput)
                        jumpInput = false;
                    if (!isAirborne && jumpInput && isGroundedDelayed)
                    {
                        isAirborne = true;
                        jumpStartTime = Time.time;
                        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                        jumpInput = false;
                    }

                    // Calcola la direzione verso il prossimo punto della navmesh
                    Vector3 toTarget = navAgent.nextPosition - rb.position;
                    Vector3 desiredDir = toTarget.normalized;
                    float distance = toTarget.magnitude;
                    float factor = Mathf.Clamp01(distance);
                    Vector3 targetNavVelocity = desiredDir * navMeshSpeed * factor;

                    // Smooth per la velocità in modalità NavMesh
                    Vector3 currentNavVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
                    Vector3 newNavVelocity = Vector3.SmoothDamp(currentNavVelocity, targetNavVelocity, ref _navmeshVelocitySmoothDamp, navmeshSmoothTime);

                    rb.velocity = new Vector3(newNavVelocity.x, rb.velocity.y, newNavVelocity.z);
                }
            }
        }
        else // NPC (modalità NavMesh)
        {
            if (navAgent != null && navAgent.enabled)
            {
                Vector3 toTarget = navAgent.nextPosition - rb.position;
                Vector3 desiredDir = toTarget.normalized;
                float distance = toTarget.magnitude;
                float factor = Mathf.Clamp01(distance);
                Vector3 targetNavVelocity = desiredDir * navMeshSpeed * factor;

                Vector3 currentNavVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
                Vector3 newNavVelocity = Vector3.SmoothDamp(currentNavVelocity, targetNavVelocity, ref _navmeshVelocitySmoothDamp, navmeshSmoothTime);

                rb.velocity = new Vector3(newNavVelocity.x, rb.velocity.y, newNavVelocity.z);
            }
        }

        rb.angularVelocity = Vector3.zero;
    }

    #region Rotazione
    // Rotazione per modalità Keyboard, aggiornata in FixedUpdate
    private void RotateKeyboard()
    {
        // Per la tastiera usiamo la direzione in base alla velocità orizzontale (o la stored direction se in salto)
        Vector3 direction = (isAirborne) ? storedMoveDirection : new Vector3(rb.velocity.x, 0, rb.velocity.z);
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized) * Quaternion.Euler(0, rotationOffset, 0);
            // Usa Time.fixedDeltaTime per la rotazione in fisica
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, angularSpeed * Time.fixedDeltaTime);
        }
    }

    // Rotazione per modalità NavMesh e per NPC, aggiornata in LateUpdate
    private void RotateNavmesh()
    {
        Vector3 direction = Vector3.zero;
        if (avatarType == AvatarType.Player)
        {
            if (movementType == MovementType.PointAndClick && navAgent != null)
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
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized) * Quaternion.Euler(0, rotationOffset, 0);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, angularSpeed * Time.deltaTime);
        }
    }
    #endregion

    #region Input e Stato per il Player (Keyboard / PointAndClick)
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

        if (!navAgent.pathPending && navAgent.remainingDistance > navAgent.stoppingDistance && navAgent.velocity.sqrMagnitude < 0.01f)
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
                        navAgent.velocity.sqrMagnitude < 0.01f)
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