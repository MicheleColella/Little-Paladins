using UnityEngine;
using UnityEngine.Events;

public class InteractableObject : MonoBehaviour
{
    public float interactionRange = 3f;
    public GameObject uIBillBoard;
    public bool disactiveBillBoard = false;

    [Header("Eventi Interazione")]
    public UnityEvent onAppear;
    public UnityEvent onDisappear;
    public UnityEvent onInteract;
    public UnityEvent onBillboardInteract;
    private bool currentActiveState = false;
    private bool hasInteracted = false;

    private void Start()
    {
        // Disattiva il billboard all'inizio
        if(uIBillBoard != null)
            uIBillBoard.SetActive(false);

        InteractionManager.Instance.RegisterInteractable(this);
    }

    private void OnDestroy()
    {
        if (InteractionManager.Instance != null)
            InteractionManager.Instance.UnregisterInteractable(this);
    }

    public void Interact()
    {
        if (disactiveBillBoard)
        {
            // Se l'interazione è già avvenuta, non fare nulla
            if (hasInteracted)
                return;

            onBillboardInteract?.Invoke();
            hasInteracted = true;
        }
        else
        {
            onInteract?.Invoke();
        }
    }

    public void SetChildActive(bool isActive)
    {
        if(uIBillBoard != null && (!disactiveBillBoard || (disactiveBillBoard && !hasInteracted)) && currentActiveState != isActive)
        {
            currentActiveState = isActive;
            if(isActive)
            {
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
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
