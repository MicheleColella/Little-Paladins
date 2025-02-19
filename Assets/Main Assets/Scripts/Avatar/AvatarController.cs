using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events; // Aggiunto per gli UnityEvent

public enum MovementType { Keyboard, PointAndClick }
public enum AvatarType { Player, NPC }
public enum MovementState { Idle, Walking, Jumping }

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(NavMeshAgent))]
public class AvatarController : MonoBehaviour
{
    [Header("Eventi")]
    public UnityEvent OnJump;  // Evento invocato al momento del salto
    public UnityEvent OnLand;  // Evento invocato quando l'avatar tocca il terreno

    [Header("Modalità di Movimento")]
    [SerializeField] private MovementType movementType = MovementType.Keyboard;
    public MovementType MovementMode {
        get => movementType;
        set {
            // Se è un NPC (ovvero se è presente il componente NPCBehaviour) si forza il movimento in PointAndClick
            if (AvatarType == AvatarType.NPC)
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
    [Tooltip("Velocità angolare (gradi/secondo) per la rotazione")]
    [SerializeField] private float angularSpeed = 360f;
    [Tooltip("Fattore di controllo in aria (0 = nessun controllo, 1 = controllo uguale a terra)")]
    [SerializeField] private float airControlFactor = 0.5f;
    private Vector3 _velocitySmoothDamp = Vector3.zero;
    [Tooltip("Moltiplicatore per il tempo di smoothing in aria rispetto a terra")]
    [SerializeField] private float inAirSmoothMultiplier = 3f;
    [SerializeField] private float baseSmoothTime = 0.05f;

    // Tempo per lo smoothing in modalità NavMesh
    [SerializeField] private float navmeshSmoothTime = 0.1f;
    private Vector3 _navmeshVelocitySmoothDamp = Vector3.zero;

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

    // Variabili per la gestione del salto (solo per il Player)
    private bool isAirborne = false;
    private Vector3 storedMoveDirection = Vector3.zero;
    private float jumpStartTime = 0f;

    // Stato dell'avatar
    private MovementState currentState = MovementState.Idle;

    // Componenti
    private Rigidbody rb;
    private NavMeshAgent navAgent;
    private Transform camTransform;

    // Variabili input (per il Player)
    private Vector2 moveInput;
    private bool jumpInput;
    private Vector3 targetPosition;

    /// <summary>
    /// Determina il tipo di avatar in base alla presenza del componente NPCBehaviour.
    /// Se è presente, l'avatar è NPC, altrimenti è Player.
    /// </summary>
    public AvatarType AvatarType {
        get { return GetComponent<NPCBehaviour>() != null ? AvatarType.NPC : AvatarType.Player; }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        // Blocca le rotazioni su X e Z;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        camTransform = Camera.main != null ? Camera.main.transform : null;
        navAgent = GetComponent<NavMeshAgent>();

        // Se è un NPC forziamo il movimento PointAndClick
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
                navAgent.updatePosition = false;
                navAgent.speed = navMeshSpeed;
                navAgent.acceleration = acceleration;
                navAgent.angularSpeed = angularSpeed;
                navAgent.stoppingDistance = 0.1f;
            }
        }
    }

    void Update()
    {
        // Gestione input solo per il Player
        if (AvatarType == AvatarType.Player)
        {
            if (movementType == MovementType.Keyboard)
                HandleKeyboardInput();
            else if (movementType == MovementType.PointAndClick)
                HandlePointAndClickInput();
        }
    }

    // Gestione della rotazione e il movimento NavMesh
    void LateUpdate()
    {
        if ((AvatarType == AvatarType.Player && movementType == MovementType.PointAndClick) || AvatarType == AvatarType.NPC)
        {
            if (navAgent != null && navAgent.enabled)
            {
                Vector3 agentTarget = navAgent.nextPosition;
                Vector3 currentPos = rb.position;
                Vector3 targetPos = new Vector3(agentTarget.x, currentPos.y, agentTarget.z);
                // Transizione fluida
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

        if (AvatarType == AvatarType.Player)
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
                    OnJump?.Invoke(); // Evento invocato al salto
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
                if (isAirborne && jumpInput)
                    jumpInput = false;
                if (!isAirborne && jumpInput && isGroundedDelayed)
                {
                    isAirborne = true;
                    jumpStartTime = Time.time;
                    rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                    OnJump?.Invoke(); // Evento invocato al salto
                    jumpInput = false;
                }
            }
        }
        
        rb.angularVelocity = Vector3.zero;
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

    #region Input per il Player
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
        if (AvatarType != AvatarType.Player) return;
        string buttonText = movementType == MovementType.Keyboard ? "Switch to PointAndClick" : "Switch to Keyboard";
        if (GUI.Button(new Rect(10, 10, 220, 40), buttonText))
        {
            MovementMode = movementType == MovementType.Keyboard ? MovementType.PointAndClick : MovementType.Keyboard;
        }
    }
    #endregion

    #region Metodi Pubblici per PlayerInputManager
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
    #endregion

    #region Gestione Collisioni
    private void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            if (isAirborne) // Se era in salto, invoca l'evento di "atterraggio"
            {
                OnLand?.Invoke();
            }
            isAirborne = false;
            storedMoveDirection = Vector3.zero;
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
                    }
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
