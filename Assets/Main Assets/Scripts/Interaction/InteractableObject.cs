using UnityEngine;
using UnityEngine.Events;

public class InteractableObject : MonoBehaviour
{
    public float interactionRange = 3f;
    public GameObject uIBillBoard;
    // Se true, inizialmente il comportamento in range è normale; 
    // ma, al click, si innesca onBillboardInteract e non si possono più fare ulteriori interazioni.
    public bool disactiveBillBoard = false;

    [Header("Eventi Interazione")]
    public UnityEvent onAppear;
    public UnityEvent onDisappear;
    public UnityEvent onInteract;
    public UnityEvent onBillboardInteract;

    // Stato corrente per il controllo di apparizione/scomparsa
    private bool currentActiveState = false;
    // Flag che indica se è già avvenuta l'interazione (per oggetti con disactiveBillBoard true)
    private bool hasInteracted = false;

    private void Start()
    {
        // Disattiva il billboard all'inizio (se presente)
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

            // Al primo click, invoca onBillboardInteract e setta il flag per impedire ulteriori interazioni
            onBillboardInteract?.Invoke();
            hasInteracted = true;
        }
        else
        {
            // Per gli oggetti non togglable, invoca sempre onInteract
            onInteract?.Invoke();
        }
    }

    /// <summary>
    /// Gestisce l'attivazione/disattivazione del billboard tramite eventi,
    /// chiamandoli solo se lo stato cambia.
    /// Se disactiveBillBoard è true e l'oggetto ha già subito un'interazione, 
    /// onAppear/onDisappear non vengono più attivati.
    /// </summary>
    /// <param name="isActive">True per attivare (onAppear), false per disattivare (onDisappear).</param>
    public void SetChildActive(bool isActive)
    {
        // Per oggetti con disactiveBillBoard true: se l'interazione è già avvenuta, non attivare onAppear/onDisappear
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
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
