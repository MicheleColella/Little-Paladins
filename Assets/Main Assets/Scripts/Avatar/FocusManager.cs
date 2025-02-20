using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class FocusManager : MonoBehaviour
{
    [Header("NPC Focus List (visualizzazione in Inspector)")]
    [SerializeField] private List<NPCBehaviour> npcList = new List<NPCBehaviour>();

    [Header("Cinemachine Settings")]
    [SerializeField] private CinemachineVirtualCamera virtualCamera;

    // Il player viene trovato direttamente dal tag "Player"
    private Transform playerTransform;

    void Awake()
    {
        // Popola la lista con tutti gli NPCBehaviour presenti nella scena
        npcList = new List<NPCBehaviour>(FindObjectsOfType<NPCBehaviour>());

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            playerTransform = playerObj.transform;
    }

    void Update()
    {
        // Aggiorna la lista ogni frame per eventuali cambi dinamici (se necessario)
        npcList = new List<NPCBehaviour>(FindObjectsOfType<NPCBehaviour>());

        // Controlla quanti NPC hanno il focus attivo
        NPCBehaviour focusedNPC = null;
        foreach (var npc in npcList)
        {
            if (npc.IsFocused)
            {
                if (focusedNPC == null)
                {
                    // Primo NPC trovato in focus
                    focusedNPC = npc;
                }
                else
                {
                    // Se ce n'è già uno in focus, forziamo l'uscita dal focus su questo NPC
                    npc.ForceExitFocus();
                }
            }
        }

        // Imposta il LookAt della virtual camera:
        // Se c'è un NPC in focus, la camera lo guarda, altrimenti guarda il player
        if (virtualCamera != null)
        {
            virtualCamera.LookAt = focusedNPC != null ? focusedNPC.transform : playerTransform;
        }
    }

    void OnGUI()
    {
        // Mostra in GUI l'elenco degli NPC e il loro stato di focus
        string guiText = "NPC Focus List:\n";
        foreach (var npc in npcList)
        {
            guiText += $"{npc.gameObject.name} - Focus: {npc.IsFocused}\n";
        }
        GUI.Label(new Rect(10, 60, 300, 150), guiText);
    }
}
