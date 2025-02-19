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

    // Impostazioni per il movimento con tastiera (che verranno usate anche per il NavMeshAgent)
    [Header("Keyboard Movement Settings")]
    [Tooltip("Velocità di accelerazione (unità/secondo)")]
    [SerializeField] private float acceleration = 10f;
    [Tooltip("Velocità di decelerazione (unità/secondo)")]
    [SerializeField] private float deceleration = 10f;
    [Tooltip("Velocità angolare (gradi/secondo) per la rotazione")]
    [SerializeField] private float angularSpeed = 360f;
    // Nuovo parametro: fattore di controllo in aria (tra 0 e 1, dove 1 significa controllo completo a terra)
    [Tooltip("Fattore di controllo in aria (0 = nessun controllo, 1 = controllo uguale a terra)")]
    [SerializeField] private float airControlFactor = 0.5f;
    // Variabile per gestire la velocità attuale (solo componente orizzontale)
    private Vector3 currentHorizontalVelocity = Vector3.zero;
    // Variabile per il SmoothDamp (richiesta dal SmoothDamp)
    private Vector3 _velocitySmoothDamp = Vector3.zero;
    // Moltiplicatore per il tempo di smoothing in aria (ad es. se è 1.0 il tempo di smoothing è baseSmoothTime, se è 2.0 si raddoppia)
    [Tooltip("Moltiplicatore per il tempo di smoothing in aria rispetto a terra")]
    [SerializeField] private float inAirSmoothMultiplier = 3f;

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
    private Vector2 moveInput; // viene aggiornato anche durante il salto
    private bool jumpInput;
    private Vector3 targetPosition; // per PointAndClick

    // Variabili patrolling (per NPC)
    private bool waiting = false;
    private float waitTimer = 0f;

    [SerializeField] private float smoothFactorNavMesh = 10f;

    // Parametri per il controllo delle collisioni durante il movimento
    [SerializeField] private float safetyMargin = 0.1f;
    [SerializeField] private float skinWidth = 0.05f;
    [SerializeField] private float sphereCastRadius = 0.5f;

    // Parametri per il Ground Check
    [Header("Ground Check")]
    [SerializeField] private float groundCheckOffset = 0.1f;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    // *** Nuovi parametri per il ritardo del Ground Check ***
    [Header("Ground Check Delay")]
    [Tooltip("Delay (in secondi) per passare da grounded (true) a non grounded (false)")]
    [SerializeField] private float groundedToFalseDelay = 0.5f;
    [Tooltip("Delay (in secondi) per passare da non grounded (false) a grounded (true)")]
    [SerializeField] private float falseToGroundedDelay = 0.1f;
    // Stato ritardato del ground check e timer per la transizione
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
                // Assegna i valori configurati nell'Inspector:
                navAgent.speed = navMeshSpeed;
                // Usiamo i valori "acceleration" e "angularSpeed" definiti per il movimento con tastiera
                navAgent.acceleration = acceleration;
                navAgent.angularSpeed = angularSpeed;
                // Lasciamo invariata la distanza di arresto (si può rendere configurabile se necessario)
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
            {
                HandleKeyboardInput();
            }
            else if (movementType == MovementType.PointAndClick)
            {
                HandlePointAndClickInput();
            }
        }
        else // NPC
        {
            HandleNPCPatrol();
        }

        RotateInMovementDirection();
    }

    // Aggiorna lo stato "grounded" con ritardo (da false a true e viceversa)
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

    // Movimento fisico
    void FixedUpdate()
    {
        // Aggiorno lo stato ritardato del Ground Check
        UpdateGroundedDelayedState();

        if (avatarType == AvatarType.Player)
        {
            if (movementType == MovementType.Keyboard)
            {
                // Calcola la direzione corrente dall'input (solo componente orizzontale)
                Vector3 inputDir = new Vector3(-moveInput.y, 0, moveInput.x).normalized;

                // Se il personaggio è già in aria, cancelliamo eventuali input di salto buffered
                if (isAirborne && jumpInput)
                    jumpInput = false;

                // Se siamo a terra (usando lo stato ritardato) e si è premuto il tasto di salto, eseguiamo il salto
                if (!isAirborne && jumpInput && isGroundedDelayed)
                {
                    storedMoveDirection = inputDir;
                    isAirborne = true;
                    jumpStartTime = Time.time;
                    rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                    jumpInput = false;
                }

                // Durante il salto, usiamo la direzione memorizzata per controllare il movimento orizzontale
                if (isAirborne)
                {
                    inputDir = storedMoveDirection;
                }

                // --- SISTEMA DI ACCELERAZIONE / DECELERAZIONE CON SMOOTHDAMP ---
                // Se in aria, riduciamo il controllo orizzontale usando airControlFactor
                float effectiveSpeed = isAirborne ? keyboardSpeed * airControlFactor : keyboardSpeed;
                Vector3 desiredVelocity = inputDir * effectiveSpeed;
                float baseSmoothTime = 0.05f;
                float smoothTime = isAirborne ? baseSmoothTime * inAirSmoothMultiplier : baseSmoothTime;
                
                // Applica il SmoothDamp alla componente orizzontale
                Vector3 currentHorizontal = new Vector3(currentHorizontalVelocity.x, 0, currentHorizontalVelocity.z);
                Vector3 targetHorizontal = new Vector3(desiredVelocity.x, 0, desiredVelocity.z);
                currentHorizontal = Vector3.SmoothDamp(currentHorizontal, targetHorizontal, ref _velocitySmoothDamp, smoothTime);
                currentHorizontalVelocity = currentHorizontal;  // aggiorna la velocità orizzontale

                // Proietta la velocità sul piano definito dalla normale del terreno
                Vector3 groundNormal = GetGroundNormal();
                Vector3 adjustedVelocity = Vector3.ProjectOnPlane(currentHorizontalVelocity, groundNormal);

                // Calcola lo spostamento per questo frame
                Vector3 displacement = adjustedVelocity * Time.fixedDeltaTime;

                // Verifica se il percorso è bloccato da un ostacolo (con SphereCast)
                if (displacement.sqrMagnitude > 0.0001f)
                {
                    Ray sphereCastRay = new Ray(rb.position, displacement.normalized);
                    float distance = displacement.magnitude;
                    if (Physics.SphereCast(sphereCastRay, sphereCastRadius, out RaycastHit hit, distance + safetyMargin + skinWidth))
                    {
                        displacement = displacement.normalized * Mathf.Max(0, hit.distance - safetyMargin - skinWidth);
                    }
                }
                
                rb.MovePosition(rb.position + displacement);
            }
            else if (movementType == MovementType.PointAndClick)
            {
                if (navAgent != null && navAgent.enabled)
                {
                    // Logica simile per il salto in modalità PointAndClick
                    if (isAirborne && jumpInput)
                        jumpInput = false;
                    if (!isAirborne && jumpInput && isGroundedDelayed)
                    {
                        isAirborne = true;
                        jumpStartTime = Time.time;
                        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                        jumpInput = false;
                    }
                    
                    Vector3 targetPos = Vector3.Lerp(rb.position, navAgent.nextPosition, smoothFactorNavMesh * Time.fixedDeltaTime);
                    rb.MovePosition(targetPos);
                }
            }
        }
        else // NPC
        {
            if (navAgent != null && navAgent.enabled)
            {
                Vector3 targetPos = Vector3.Lerp(rb.position, navAgent.nextPosition, smoothFactorNavMesh * Time.fixedDeltaTime);
                rb.MovePosition(targetPos);
            }
        }

        rb.angularVelocity = Vector3.zero;
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
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, NavMesh.AllAreas))
        {
            navAgent.SetDestination(hit.position);
            Debug.Log("NPC nuova destinazione: " + hit.position);
        }
    }
    #endregion

    #region Salto e Controllo del Terreno (Ground Check)
    // Restituisce true se il controllo sfera (senza delay) rileva il terreno.
    public bool IsTouchingGround()
    {
        Vector3 sphereCenter = transform.position + Vector3.down * groundCheckOffset;
        Debug.DrawRay(sphereCenter, Vector3.up * 0.1f, Color.red);
        return Physics.CheckSphere(sphereCenter, groundCheckRadius, groundLayer);
    }

    private Vector3 GetGroundNormal()
    {
        RaycastHit hit;
        float rayDistance = groundCheckOffset + groundCheckRadius + 0.2f;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, rayDistance, groundLayer))
        {
            return hit.normal;
        }
        return Vector3.up;
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
                // Se siamo in movimento via tastiera, usiamo la velocità orizzontale attuale (oppure la stored direction se in salto)
                direction = isAirborne ? storedMoveDirection : currentHorizontalVelocity;
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

        if (direction.sqrMagnitude > 0.01f)
        {
            // Calcola la rotazione target, applicando l'offset
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized) * Quaternion.Euler(0, rotationOffset, 0);
            // Ruota usando la velocità angolare configurata
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, angularSpeed * Time.deltaTime);
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

    #region Gestione Collisioni
    private void OnCollisionEnter(Collision collision)
    {
        // Se la collisione è con il terreno o è una collisione laterale, resettare l'inertia.
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
