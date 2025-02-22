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
    
    [Header("Speaker Settings")]
    [Tooltip("Nome del parlante, da impostare in Inspector")]
    public string speakerName;
    [Tooltip("TextMeshProUGUI per visualizzare il nome del parlante")]
    public TextMeshProUGUI speakerNameText;

    [Header("Dialoghi")]
    [Tooltip("Lista dei dialoghi (la numerazione parte da 1)")]
    public List<DialogueData> dialogues = new List<DialogueData>();
    
    // Contatore interno del dialogo corrente: 0 = nessun dialogo attivo, altrimenti parte da 1
    private int currentDialogueIndex = 0;
    
    public void OnFocusTriggered()
    {
        if (currentDialogueIndex == 0)
        {
            currentDialogueIndex = 1;
            // Assegna il nome del parlante quando inizia il primo dialogo
            if (speakerNameText != null)
                speakerNameText.text = speakerName;
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
        // Resetta il nome del parlante, se necessario
        if (speakerNameText != null)
            speakerNameText.text = "";
    }
}
