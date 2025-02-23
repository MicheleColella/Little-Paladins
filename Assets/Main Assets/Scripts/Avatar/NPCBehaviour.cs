using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

[RequireComponent(typeof(AvatarController))]
[RequireComponent(typeof(NavMeshAgent))]
public class NPCBehaviour : MonoBehaviour
{
    [Header("Patrol Settings")]
    [SerializeField] private float patrolMinRadius = 5f;
    [SerializeField] private float patrolMaxRadius = 10f;
    [SerializeField] private float minWaitTimeAtPatrolPoint = 2f;
    [SerializeField] private float maxWaitTimeAtPatrolPoint = 4f;
    [SerializeField] private float patrolTimeout = 5f;

    [Header("Focus Settings")]
    [SerializeField, Tooltip("Numero di chiamate per mantenere il focus prima di uscire dal focus")]
    private int focusThreshold = 1;
    [SerializeField, Tooltip("Distanza massima dal player per mantenere il focus")]
    private float maxFocusDistance = 15f;
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
                Vector3 direction = (playerTransform.position - transform.position).normalized;
                direction.y = 0;
                if (direction.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, navAgent.angularSpeed * Time.deltaTime);
                }
            }
            if (navAgent != null)
                navAgent.isStopped = true;
            return;
        }

        if (navAgent == null)
            return;

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

    private void SetNewPatrolDestination()
    {
        float randomRadius = Random.Range(patrolMinRadius, patrolMaxRadius);
        Vector3 randomDirection = Random.insideUnitSphere;
        randomDirection.y = 0;
        randomDirection = randomDirection.normalized * randomRadius;
        Vector3 destination = transform.position + randomDirection;

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
            ExitFocus();
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

    private void OnDrawGizmosSelected()
    {
        if (navAgent != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(navAgent.destination, 0.5f);
            Gizmos.DrawLine(transform.position, navAgent.destination);
        }
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, patrolMinRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, patrolMaxRadius);
    }
}
