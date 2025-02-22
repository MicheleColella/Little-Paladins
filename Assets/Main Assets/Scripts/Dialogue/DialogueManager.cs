using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;

[System.Serializable]
public class DialogueData
{
    [TextArea]
    public string dialogueText;           // Testo del dialogo
    public UnityEvent onDialogueAssigned; // Evento da invocare quando il dialogo viene assegnato
}
 
public class DialogueManager : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("Contenitore che ospita il TextMeshPro")]
    public GameObject dialogueContainer;
    [Tooltip("Componente TextMeshProUGUI dove verrà visualizzato il dialogo")]
    public TextMeshProUGUI dialogueText;

    [Header("Dialoghi")]
    [Tooltip("Lista dei dialoghi (la numerazione parte da 1)")]
    public List<DialogueData> dialogues = new List<DialogueData>();
    private int currentDialogueIndex = 0;
    public void OnFocusTriggered()
    {
        if (currentDialogueIndex == 0)
        {
            currentDialogueIndex = 1;
        }
        else
        {
            currentDialogueIndex++;
        }

        if (currentDialogueIndex > dialogues.Count)
        {
            currentDialogueIndex = dialogues.Count;
        }

        // Visualizza il dialogo corrispondente
        if (currentDialogueIndex > 0)
        {
            dialogueContainer.SetActive(true);
            DialogueData data = dialogues[currentDialogueIndex - 1];
            dialogueText.text = data.dialogueText;
            // Invoca l'evento specifico del dialogo
            data.onDialogueAssigned?.Invoke();
        }
    }

    public void OnExitFocusTriggered()
    {
        currentDialogueIndex = 0;
        dialogueContainer.SetActive(false);
    }
}
