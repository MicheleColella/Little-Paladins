using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

[RequireComponent(typeof(AvatarController))]
[RequireComponent(typeof(NavMeshAgent))]
public class NPCBehaviour : MonoBehaviour
{
    [Header("Patrol Settings")]
    [SerializeField] private float patrolRadius = 10f;
    // Tempo di attesa al punto di patrolling random tra i due valori
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

    // Variabili per il focus
    private int currentFocusCounter = 0;
    private bool isFocused = false;
    private Transform playerTransform;

    void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        // Se siamo in focus, controlla la distanza dal player
        if (isFocused)
        {
            // Trova il player tramite tag se non ancora trovato
            if (playerTransform == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                    playerTransform = playerObj.transform;
            }
            // Se il player è troppo lontano, esci dal focus
            if (playerTransform != null)
            {
                float distance = Vector3.Distance(transform.position, playerTransform.position);
                if (distance > maxFocusDistance)
                {
                    ExitFocus();
                    return;
                }
            }
            
            // Ferma il NavMeshAgent e ruota verso il player
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
            // Durante il focus il NPC non esegue il patrolling
            return;
        }

        // Comportamento di patrolling
        if (navAgent == null) return;

        if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance && navAgent.desiredVelocity.sqrMagnitude < 0.01f)
        {
            if (!waiting)
            {
                waiting = true;
                waitTimer = 0f;
                // Imposta un tempo di attesa casuale compreso tra i due valori configurabili
                currentWaitTime = Random.Range(minWaitTimeAtPatrolPoint, maxWaitTimeAtPatrolPoint);
            }
            else
            {
                waitTimer += Time.deltaTime;
                if (waitTimer >= currentWaitTime)
                {
                    waiting = false;
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
                SetNewPatrolDestination();
            }
            waiting = false;
        }
    }

    private void SetNewPatrolDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += transform.position;
        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
        {
            navAgent.SetDestination(hit.position);
            navAgent.isStopped = false;
        }
    }

    /// <summary>
    /// Funzione da chiamare per attivare il focus sul player.
    /// Ad ogni chiamata viene invocato l'evento OnFocus.
    /// Quando il contatore interno supera il focusThreshold il NPC esce dal focus e riprende il patrolling.
    /// </summary>
    public void TriggerFocus()
    {
        // Incrementa il contatore e invoca l'evento di focus
        currentFocusCounter++;
        OnFocus?.Invoke();

        // Attiva il focus: blocca il patrolling e ruota verso il player
        isFocused = true;
        if (navAgent != null)
            navAgent.isStopped = true;

        // Se il contatore ha superato la soglia, esce dal focus
        if (currentFocusCounter > focusThreshold)
        {
            ExitFocus();
        }
    }

    /// <summary>
    /// Metodo per uscire dal focus, invoca l'evento di uscita e resetta il contatore.
    /// </summary>
    private void ExitFocus()
    {
        currentFocusCounter = 0;
        isFocused = false;
        if (navAgent != null)
            navAgent.isStopped = false;
        SetNewPatrolDestination();
        OnExitFocus?.Invoke();
    }

    // Metodo pubblico per il FocusManager per forzare l'uscita dal focus
    public void ForceExitFocus()
    {
        if (isFocused)
            ExitFocus();
    }

    // Proprietà pubblica per leggere lo stato di focus
    public bool IsFocused => isFocused;

    private void OnDrawGizmosSelected()
    {
        // Disegna il raggio entro il quale il focus è attivo
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxFocusDistance);
    }
}
