using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(AvatarController))]
[RequireComponent(typeof(NavMeshAgent))]
public class NPCBehaviour : MonoBehaviour
{
    [Header("Patrol Settings")]
    [SerializeField] private float patrolRadius = 10f;
    [SerializeField] private float waitTimeAtPatrolPoint = 2f;
    [SerializeField] private float patrolTimeout = 5f;

    private NavMeshAgent navAgent;
    private float patrolTimer = 0f;
    private bool waiting = false;
    private float waitTimer = 0f;

    void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (navAgent == null) return;

        if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance && navAgent.desiredVelocity.sqrMagnitude < 0.01f)
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
        }
    }
}
