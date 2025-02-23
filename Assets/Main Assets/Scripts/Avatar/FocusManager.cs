using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class FocusManager : MonoBehaviour
{
    public static FocusManager Instance { get; private set; }  // Singleton

    [Header("NPC Focus List (visualizzazione in Inspector)")]
    [SerializeField] private List<NPCBehaviour> npcList = new List<NPCBehaviour>();

    [Header("Cinemachine Settings")]
    [SerializeField] private CinemachineVirtualCamera virtualCamera;

    private Transform playerTransform;
    public NPCBehaviour CurrentFocusedNPC { get; private set; }  // NPC attualmente in focus

    void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;

        npcList = new List<NPCBehaviour>(FindObjectsOfType<NPCBehaviour>());

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            playerTransform = playerObj.transform;
    }

    void Update()
    {
        // Aggiorna la lista degli NPC
        npcList = new List<NPCBehaviour>(FindObjectsOfType<NPCBehaviour>());

        // Determina l’NPC in focus
        NPCBehaviour focusedNPC = null;
        foreach (var npc in npcList)
        {
            if (npc.IsFocused)
            {
                if (focusedNPC == null)
                    focusedNPC = npc;
                else
                    npc.ForceExitFocus();
            }
        }
        CurrentFocusedNPC = focusedNPC;

        // Imposta il LookAt della virtual camera: se c'è un NPC in focus, la camera lo guarda, altrimenti il player
        if (virtualCamera != null)
            virtualCamera.LookAt = (CurrentFocusedNPC != null) ? CurrentFocusedNPC.transform : playerTransform;
    }

    void OnGUI()
    {
        if (!DebugManager.DebugState) return;

        string guiText = "NPC Focus List:\n";
        foreach (var npc in npcList)
        {
            guiText += $"{npc.gameObject.name} - Focus: {npc.IsFocused}\n";
        }
        GUI.Label(new Rect(10, 60, 300, 150), guiText);
    }
}
