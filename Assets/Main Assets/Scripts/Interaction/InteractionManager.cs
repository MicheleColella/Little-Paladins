using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance { get; private set; }

    private Transform playerTransform;
    private List<InteractableObject> interactableObjects = new List<InteractableObject>();
    private InteractableObject nearestObject;
    private InteractableObject lastNearestObject; // per tenere traccia del precedente nearest
    private PlayerInputActions inputActions;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            inputActions = new PlayerInputActions();
            inputActions.Player.Interact.performed += OnInteractPerformed;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void Update()
    {
        // Se il riferimento al player non è stato ancora trovato, lo cerchiamo tramite tag
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        UpdateNearestObject();
    }

    public void RegisterInteractable(InteractableObject interactable)
    {
        if (!interactableObjects.Contains(interactable))
        {
            interactableObjects.Add(interactable);
        }
    }

    public void UnregisterInteractable(InteractableObject interactable)
    {
        if (interactableObjects.Contains(interactable))
        {
            interactableObjects.Remove(interactable);
        }
    }

    private void UpdateNearestObject()
    {
        if (playerTransform == null)
            return;

        float minDistance = float.MaxValue;
        nearestObject = null;

        // Cerca l'oggetto più vicino entro il range
        foreach (var obj in interactableObjects)
        {
            if (obj == null)
                continue;

            float distance = Vector3.Distance(playerTransform.position, obj.transform.position);
            if (distance < minDistance && distance <= obj.interactionRange)
            {
                minDistance = distance;
                nearestObject = obj;
            }
        }

        // Se il nearest object è cambiato, invoca gli eventi solo per il nuovo (e per il precedente, se presente)
        if (nearestObject != lastNearestObject)
        {
            if (lastNearestObject != null)
            {
                lastNearestObject.SetChildActive(false);
            }
            if (nearestObject != null)
            {
                nearestObject.SetChildActive(true);
            }
            lastNearestObject = nearestObject;
        }
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        InteractWithNearestObject();
    }

    private void InteractWithNearestObject()
    {
        if (nearestObject != null)
        {
            nearestObject.Interact();
        }
    }
}
