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
    // Manteniamo il riferimento alla camera per eventuali altre necessità
    private Transform camTransform;

    // Variabili input
    private Vector2 moveInput;
    private bool jumpInput;
    private Vector3 targetPosition; // per PointAndClick

    // Variabili patrolling
    private bool waiting = false;
    private float waitTimer = 0f;

    [SerializeField] private float smoothFactorNavMesh = 10f;

    // Parametri per il controllo delle collisioni durante il movimento
    [SerializeField] private float safetyMargin = 0.1f; // margine di sicurezza per non "incastrarsi"
    [SerializeField] private float skinWidth = 0.05f;   // tolleranza per il collider
    [SerializeField] private float sphereCastRadius = 0.5f; // raggio dello SphereCast (da regolare in base al collider)

    // Parametri per il Ground Check (controllo se il player tocca il terreno)
    [Header("Ground Check")]
    [SerializeField] private float groundCheckOffset = 0.1f; // distanza in basso rispetto al centro del player
    [SerializeField] private float groundCheckRadius = 0.2f; // raggio della sfera per il check
    [SerializeField] private LayerMask groundLayer;          // layer considerati come terreno

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        // Impostiamo la modalità di collisione continua per evitare tunneling
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
                // Calcola il vettore di input (mappato con -moveInput.y, moveInput.x)
                Vector3 moveDir = new Vector3(-moveInput.y, 0, moveInput.x).normalized;
                // Ottieni la normale della superficie (se presente) per allineare il movimento alla pendenza
                Vector3 groundNormal = GetGroundNormal();
                // Proietta il vettore di movimento sul piano definito dalla normale rilevata
                Vector3 adjustedMoveDir = Vector3.ProjectOnPlane(moveDir, groundNormal).normalized;

                float moveDistance = keyboardSpeed * Time.fixedDeltaTime;
                if (adjustedMoveDir.sqrMagnitude > 0)
                {
                    // Utilizziamo uno SphereCast per verificare se il percorso è bloccato
                    Ray sphereCastRay = new Ray(rb.position, adjustedMoveDir);
                    if (Physics.SphereCast(sphereCastRay, sphereCastRadius, out RaycastHit hit, moveDistance + safetyMargin + skinWidth))
                    {
                        // Se la distanza rilevata è minore della distanza prevista, riduciamo il movimento
                        moveDistance = Mathf.Max(0, hit.distance - safetyMargin - skinWidth);
                    }
                }
                
                Vector3 targetPos = rb.position + adjustedMoveDir * moveDistance;
                rb.MovePosition(targetPos);

                if (jumpInput && IsTouchingGround())
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

                    if (jumpInput && IsTouchingGround())
                    {
                        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                        jumpInput = false;
                    }
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

        // Azzera l'angular velocity per evitare rotazioni indesiderate (ad esempio, per collisioni)
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

    #region Salto e Controllo del Terreno (Ground Check)
    /// <summary>
    /// Ritorna true se il player sta toccando il terreno, basandosi su un controllo con una sfera.
    /// </summary>
    public bool IsTouchingGround()
    {
        // Calcola il centro della sfera di controllo in basso rispetto al player
        Vector3 sphereCenter = transform.position + Vector3.down * groundCheckOffset;
        // Disegna la sfera in modalità debug (opzionale)
        Debug.DrawRay(sphereCenter, Vector3.up * 0.1f, Color.red);
        return Physics.CheckSphere(sphereCenter, groundCheckRadius, groundLayer);
    }

    /// <summary>
    /// Esegue un raycast verso il basso per ottenere la normale della superficie.
    /// Se non viene colpito nulla, ritorna Vector3.up.
    /// </summary>
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

        // Calcolo della direzione in base al tipo di movimento
        if (avatarType == AvatarType.Player)
        {
            if (movementType == MovementType.Keyboard)
            {
                // Utilizziamo lo stesso mapping corretto per la rotazione
                direction = new Vector3(-moveInput.y, 0, moveInput.x);
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

        // Se c'è un input significativo, ruotiamo in quella direzione
        if (direction.sqrMagnitude > 0.01f)
        {
            Vector3 normalizedDirection = direction.normalized;
            float smoothFactor = 8f;
            Quaternion targetRotation = Quaternion.LookRotation(normalizedDirection) * Quaternion.Euler(0, rotationOffset, 0);
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

    #region Gizmos
    // Disegna il Gizmo per il Ground Check quando l'oggetto è selezionato in editor
    private void OnDrawGizmosSelected()
    {
        // Calcola il centro della sfera di controllo per il ground check
        Vector3 sphereCenter = transform.position + Vector3.down * groundCheckOffset;
        // Imposta il colore del Gizmo
        Gizmos.color = Color.green;
        // Disegna una sfera (wire) che rappresenta l'area del ground check
        Gizmos.DrawWireSphere(sphereCenter, groundCheckRadius);
    }
    #endregion
}
