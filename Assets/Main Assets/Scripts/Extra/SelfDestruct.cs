using UnityEngine;

public class SelfDestruct : MonoBehaviour
{
    [SerializeField] private float lifetime = 5f; // Tempo prima della distruzione

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }
}
