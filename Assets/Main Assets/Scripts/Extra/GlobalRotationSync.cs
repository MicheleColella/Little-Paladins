using UnityEngine;

public class GlobalRotationSync : MonoBehaviour
{
    public Transform target;

    void Update()
    {
        if (target != null)
        {
            transform.rotation = target.rotation;
        }
    }
}
