using UnityEngine;

public class BillboardUI : MonoBehaviour
{
    // Cache della trasformata dell'oggetto per ottimizzazione
    private Transform _transform;
    // Riferimento alla camera da seguire
    private Transform _camTransform;

    void Start()
    {
        // Cache della trasformata dell'oggetto
        _transform = transform;
        // Se esiste una Camera principale, la usiamo come riferimento
        if (Camera.main != null)
        {
            _camTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogWarning("Nessuna Camera principale trovata. Assicurati di avere una camera con tag 'MainCamera'.");
        }
    }

    void LateUpdate()
    {
        // Verifica che _camTransform sia valido, nel caso in cui la camera venga assegnata in runtime
        if (_camTransform == null)
        {
            if (Camera.main != null)
            {
                _camTransform = Camera.main.transform;
            }
            else
            {
                return;
            }
        }
        
        // Aggiorna l'orientamento dell'oggetto per farlo puntare verso la camera.
        // Utilizziamo LateUpdate per assicurarci che il movimento della camera sia già stato processato.
        _transform.LookAt(_transform.position + _camTransform.rotation * Vector3.forward, _camTransform.rotation * Vector3.up);
    }
}
