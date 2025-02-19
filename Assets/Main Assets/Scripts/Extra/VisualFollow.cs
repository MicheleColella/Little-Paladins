using UnityEngine;

public class VisualFollow : MonoBehaviour
{
    public Transform parentTransform;
    [Tooltip("Tempo di smoothing (in secondi) per raggiungere la posizione target")]
    public float smoothTime = 0.1f;
    [Tooltip("Distanza oltre la quale si effettua uno snap immediato")]
    public float snapThreshold = 0.5f;
    
    private Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {
        // Calcola la distanza corrente tra il visual e il parent
        float distance = Vector3.Distance(transform.position, parentTransform.position);

        if(distance > snapThreshold)
        {
            // Se la distanza è troppo grande, imposta direttamente la posizione target
            transform.position = parentTransform.position;
            velocity = Vector3.zero;
        }
        else
        {
            // Altrimenti, usa SmoothDamp per un movimento fluido
            transform.position = Vector3.SmoothDamp(transform.position, parentTransform.position, ref velocity, smoothTime);
        }
        
        // Per la rotazione, puoi usare Slerp (o eventualmente SmoothDampAngle se vuoi più controllo)
        transform.rotation = Quaternion.Slerp(transform.rotation, parentTransform.rotation, Time.deltaTime / smoothTime);
    }
}
