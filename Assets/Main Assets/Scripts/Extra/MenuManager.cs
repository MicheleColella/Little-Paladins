using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class MenuManager : MonoBehaviour
{
    [Tooltip("Lista dei due GameObject: [0] pulsante per aprire il menu, [1] schermata del menu con il pulsante per chiudere.")]
    [SerializeField] private List<GameObject> menuObjects;

    [Tooltip("Riferimento al PlayerInputManager, da assegnare via Inspector.")]
    [SerializeField] private PlayerInputManager playerInputManager;
    public UnityEvent onEscapeStateChanged;

    void Update()
    {
        if (playerInputManager != null && playerInputManager.InputActions.Player.Escape.triggered)
        {
            onEscapeStateChanged?.Invoke();
            ToggleMenu();
        }
    }

    // Funzione per effettuare il toggle tra i due GameObject del menu.
    public void ToggleMenu()
    {
        if (menuObjects == null || menuObjects.Count != 2)
        {
            Debug.LogError("MenuManager: La lista deve contenere esattamente due GameObject.");
            return;
        }

        if (menuObjects[0].activeSelf)
        {
            menuObjects[0].SetActive(false);
            menuObjects[1].SetActive(true);
        }
        else if (menuObjects[1].activeSelf)
        {
            menuObjects[1].SetActive(false);
            menuObjects[0].SetActive(true);
        }
        else
        {
            menuObjects[0].SetActive(true);
        }

        // Aggiorna i flag degli input in base allo stato del menu.
        bool menuActive = menuObjects[1].activeSelf;
        if (playerInputManager != null)
        {
            playerInputManager.CanMove = !menuActive;
            playerInputManager.CanJump = !menuActive;
            playerInputManager.CanInteract = !menuActive;
            if (!playerInputManager.CanMove)
            {
                playerInputManager.StopMovement();
            }
        }
    }
}
