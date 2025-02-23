using UnityEngine;
using System.Collections.Generic;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance { get; private set; }

    private Transform playerTransform;
    private List<InteractableObject> interactableObjects = new List<InteractableObject>();
    private InteractableObject nearestObject;
    private InteractableObject lastNearestObject;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
    }

    private void Update()
    {
        if (playerTransform == null)
            return;

        UpdateNearestObject();
    }

    public void RegisterInteractable(InteractableObject interactable)
    {
        if (!interactableObjects.Contains(interactable))
            interactableObjects.Add(interactable);
    }

    public void UnregisterInteractable(InteractableObject interactable)
    {
        interactableObjects.Remove(interactable);
    }

    private void UpdateNearestObject()
    {
        float minSqrDistance = float.MaxValue;
        nearestObject = null;

        foreach (var obj in interactableObjects)
        {
            if (obj == null)
                continue;

            float sqrDistance = (playerTransform.position - obj.transform.position).sqrMagnitude;
            float interactionRangeSqr = obj.interactionRange * obj.interactionRange;
            if (sqrDistance < minSqrDistance && sqrDistance <= interactionRangeSqr)
            {
                minSqrDistance = sqrDistance;
                nearestObject = obj;
            }
        }

        if (nearestObject != lastNearestObject)
        {
            if (lastNearestObject != null)
                lastNearestObject.SetChildActive(false);
            if (nearestObject != null)
                nearestObject.SetChildActive(true);
            lastNearestObject = nearestObject;
        }
    }

    public void InteractWithNearestObject()
    {
        if (nearestObject != null)
            nearestObject.Interact();
    }
}
