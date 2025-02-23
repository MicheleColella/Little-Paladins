using UnityEngine;

public class BillboardUI : MonoBehaviour
{
    private Transform _transform;
    private Transform _camTransform;

    void Start()
    {
        _transform = transform;

        if (Camera.main != null)
        {
            _camTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogWarning("Nessuna Camera trovata.");
        }
    }

    void LateUpdate()
    {
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
        
        // Aggiorna l'orientamento dell'oggetto verso la camera.
        _transform.LookAt(_transform.position + _camTransform.rotation * Vector3.forward, _camTransform.rotation * Vector3.up);
    }
}
