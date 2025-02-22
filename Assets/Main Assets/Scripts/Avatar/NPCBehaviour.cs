using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

[RequireComponent(typeof(AvatarController))]
[RequireComponent(typeof(NavMeshAgent))]
public class NPCBehaviour : MonoBehaviour
{
    [Header("Patrol Settings")]
    [SerializeField] private float patrolMinRadius = 5f;    // Raggio minimo per il patrol
    [SerializeField] private float patrolMaxRadius = 10f;   // Raggio massimo per il patrol
    [SerializeField] private float minWaitTimeAtPatrolPoint = 2f;
    [SerializeField] private float maxWaitTimeAtPatrolPoint = 4f;
    [SerializeField] private float patrolTimeout = 5f;

    [Header("Focus Settings")]
    [Tooltip("Numero di chiamate per mantenere il focus prima di uscire dal focus")]
    [SerializeField] private int focusThreshold = 1;
    [Tooltip("Distanza massima dal player per mantenere il focus")]
    [SerializeField] private float maxFocusDistance = 15f;
    [Tooltip("Evento invocato ad ogni attivazione del focus (anche durante il focus)")]
    public UnityEvent OnFocus;
    [Tooltip("Evento invocato quando il NPC esce dal focus (sia per superamento soglia che per distanza)")]
    public UnityEvent OnExitFocus;

    private NavMeshAgent navAgent;
    private float patrolTimer = 0f;
    private bool waiting = false;
    private float waitTimer = 0f;
    private float currentWaitTime = 0f;

    private int currentFocusCounter = 0;
    private bool isFocused = false;
    private Transform playerTransform;

    void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (isFocused)
        {
            if (playerTransform == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                    playerTransform = playerObj.transform;
            }
            if (playerTransform != null)
            {
                float distance = Vector3.Distance(transform.position, playerTransform.position);
                if (distance > maxFocusDistance)
                {
                    ExitFocus();
                    return;
                }
            }
            
            if (navAgent != null)
                navAgent.isStopped = true;

            if (playerTransform != null)
            {
                Vector3 direction = (playerTransform.position - transform.position).normalized;
                direction.y = 0;
                if (direction.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, navAgent.angularSpeed * Time.deltaTime);
                }
            }
            return;
        }

        if (navAgent == null) return;

        // Se l'NPC ha raggiunto la destinazione, inizia la fase di attesa
        if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance && navAgent.desiredVelocity.sqrMagnitude < 0.01f)
        {
            if (!waiting)
            {
                waiting = true;
                waitTimer = 0f;
                currentWaitTime = Random.Range(minWaitTimeAtPatrolPoint, maxWaitTimeAtPatrolPoint);
            }
            else
            {
                waitTimer += Time.deltaTime;
                if (waitTimer >= currentWaitTime)
                {
                    waiting = false;
                    // Se l'agente sembra bloccato, resetta il path prima di impostare una nuova destinazione
                    navAgent.ResetPath();
                    SetNewPatrolDestination();
                }
            }
        }
        else
        {
            patrolTimer += Time.deltaTime;
            if (patrolTimer >= patrolTimeout)
            {
                patrolTimer = 0f;
                waiting = false;
                navAgent.ResetPath();
                SetNewPatrolDestination();
            }
        }
    }

    /// <summary>
    /// Imposta una nuova destinazione di patrol scegliendo una posizione casuale ad una distanza compresa tra patrolMinRadius e patrolMaxRadius.
    /// </summary>
    private void SetNewPatrolDestination()
    {
        // Seleziona un raggio casuale tra il minimo e il massimo
        float randomRadius = Random.Range(patrolMinRadius, patrolMaxRadius);
        // Genera una direzione casuale sul piano orizzontale
        Vector3 randomDirection = Random.insideUnitSphere;
        randomDirection.y = 0;
        randomDirection = randomDirection.normalized * randomRadius;
        Vector3 destination = transform.position + randomDirection;

        // Verifica che la posizione sia raggiungibile sulla NavMesh
        if (NavMesh.SamplePosition(destination, out NavMeshHit hit, patrolMaxRadius, NavMesh.AllAreas))
        {
            navAgent.SetDestination(hit.position);
            navAgent.isStopped = false;
        }
    }

    public void TriggerFocus()
    {
        currentFocusCounter++;
        OnFocus?.Invoke();
        isFocused = true;
        if (navAgent != null)
            navAgent.isStopped = true;
        if (currentFocusCounter > focusThreshold)
        {
            ExitFocus();
        }
    }

    private void ExitFocus()
    {
        currentFocusCounter = 0;
        isFocused = false;
        if (navAgent != null)
            navAgent.isStopped = false;
        SetNewPatrolDestination();
        OnExitFocus?.Invoke();
    }

    public void ForceExitFocus()
    {
        if (isFocused)
            ExitFocus();
    }

    public bool IsFocused => isFocused;

    private void OnDrawGizmos()
    {
        if (navAgent != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(navAgent.destination, 0.5f);
            Gizmos.DrawLine(transform.position, navAgent.destination);
        }

        // Visualizza il raggio minimo in verde
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, patrolMinRadius);
        
        // Visualizza il raggio massimo in rosso
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, patrolMaxRadius);
    }
}
