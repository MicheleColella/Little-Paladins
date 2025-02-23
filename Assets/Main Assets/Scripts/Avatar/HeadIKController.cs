using UnityEngine;

public class HeadIKController : MonoBehaviour
{
    [Header("Riferimenti")]
    public Animator animator;
    public Transform headIKTarget;  // Target IK per la testa

    [Header("Impostazioni Target")]
    public LayerMask targetLayer; // Layer dei target validi
    public float searchRadius = 10f;    // Raggio massimo di ricerca
    public float minSearchRadius = 0.5f;  // Raggio minimo di ricerca
    public float fieldOfView = 90f; // Angolo di visione

    [Header("Impostazioni Movimento")]
    public float targetSmoothSpeed = 5f;    // Velocità quando c'è un target da guardare
    public float defaultSmoothSpeed = 2f;   // Velocità per il ritorno alla posizione di default

    private Transform currentTarget;
    private Vector3 currentIKPosition;
    private Vector3 defaultLocalPosition;

    void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (headIKTarget != null)
        {
            defaultLocalPosition = headIKTarget.localPosition;
            currentIKPosition = headIKTarget.position;
        }
    }

    void Update()
    {
        if (headIKTarget == null)
            return;

        // Cerca il target valido
        FindClosestTarget();

        if (currentTarget != null)
        {
            Vector3 desiredPosition = currentTarget.position;
            currentIKPosition = Vector3.Lerp(currentIKPosition, desiredPosition, Time.deltaTime * targetSmoothSpeed);
        }
        else
        {
            Vector3 defaultWorldPosition = transform.TransformPoint(defaultLocalPosition);
            currentIKPosition = Vector3.Lerp(currentIKPosition, defaultWorldPosition, Time.deltaTime * defaultSmoothSpeed);
        }

        headIKTarget.position = currentIKPosition;
    }

    // Cerca il target più vicino entro i range
    void FindClosestTarget()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, searchRadius, targetLayer);
        Transform nearestTarget = null;
        float minDistance = Mathf.Infinity;
        Vector3 forward = transform.forward;

        foreach (Collider col in colliders)
        {
            // Esclude l'oggetto stesso
            if (col.gameObject == gameObject)
                continue;

            Vector3 directionToTarget = col.transform.position - transform.position;
            float angle = Vector3.Angle(forward, directionToTarget);
            if (angle < fieldOfView * 0.5f)
            {
                float distance = directionToTarget.magnitude;
                if (distance < minSearchRadius)
                    continue;

                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestTarget = col.transform;
                }
            }
        }

        currentTarget = nearestTarget;
    }

    void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position;
        float halfFOV = fieldOfView * 0.5f;
        int segments = 20;

        // Gizmo per il raggio massimo (searchRadius) - semiarco in verde
        Gizmos.color = Color.green;
        Vector3 previousPoint = origin + (Quaternion.Euler(0, -halfFOV, 0) * transform.forward) * searchRadius;
        for (int i = 1; i <= segments; i++)
        {
            float angle = Mathf.Lerp(-halfFOV, halfFOV, i / (float)segments);
            Vector3 nextPoint = origin + (Quaternion.Euler(0, angle, 0) * transform.forward) * searchRadius;
            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
        }
        Vector3 rightBoundary = origin + (Quaternion.Euler(0, halfFOV, 0) * transform.forward) * searchRadius;
        Vector3 leftBoundary = origin + (Quaternion.Euler(0, -halfFOV, 0) * transform.forward) * searchRadius;
        Gizmos.DrawLine(origin, rightBoundary);
        Gizmos.DrawLine(origin, leftBoundary);

        // Gizmo per il raggio minimo (minSearchRadius) - semiarco in rosso
        Gizmos.color = Color.red;
        Vector3 previousMinPoint = origin + (Quaternion.Euler(0, -halfFOV, 0) * transform.forward) * minSearchRadius;
        for (int i = 1; i <= segments; i++)
        {
            float angle = Mathf.Lerp(-halfFOV, halfFOV, i / (float)segments);
            Vector3 nextMinPoint = origin + (Quaternion.Euler(0, angle, 0) * transform.forward) * minSearchRadius;
            Gizmos.DrawLine(previousMinPoint, nextMinPoint);
            previousMinPoint = nextMinPoint;
        }
        Vector3 rightMinBoundary = origin + (Quaternion.Euler(0, halfFOV, 0) * transform.forward) * minSearchRadius;
        Vector3 leftMinBoundary = origin + (Quaternion.Euler(0, -halfFOV, 0) * transform.forward) * minSearchRadius;
        Gizmos.DrawLine(origin, rightMinBoundary);
        Gizmos.DrawLine(origin, leftMinBoundary);
    }

}
