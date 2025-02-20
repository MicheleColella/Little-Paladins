using UnityEngine;
using UnityEngine.Events;

public class InteractableObject : MonoBehaviour
{
    public float interactionRange = 3f;
    public GameObject uIBillBoard;
    public bool disactiveBillBoard = false;
    private bool hasInteracted = false;

    // Eventi Unity configurabili
    [Header("Eventi Interazione")]
    public UnityEvent onAppear;
    public UnityEvent onDisappear;
    public UnityEvent onInteract;
    public UnityEvent onBillboardInteract;

    private void Start()
    {
        InteractionManager.Instance.RegisterInteractable(this);
    }

    private void OnDestroy()
    {
        InteractionManager.Instance.UnregisterInteractable(this);
    }

    public void Interact()
    {
        if (disactiveBillBoard && hasInteracted)
            return;

        if (disactiveBillBoard)
        {
            hasInteracted = true;
            onDisappear?.Invoke();
            onBillboardInteract?.Invoke();
        }
        else
        {
            onInteract?.Invoke();
        }

        // Esecuzione di eventuale logica aggiuntiva tramite handler
        var handler = GetComponent<IInteractionHandler>();
        if (handler != null)
        {
            handler.HandleInteraction(this);
        }
    }

    public void SetChildActive(bool isActive)
    {
        if (uIBillBoard != null)
        {
            if (isActive && (!hasInteracted || !disactiveBillBoard))
            {
                // Assicuriamo l'attivazione dell'oggetto billboard se non già attivo
                if (!uIBillBoard.activeSelf)
                    uIBillBoard.SetActive(true);
                onAppear?.Invoke();
            }
            else
            {
                onDisappear?.Invoke();
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
