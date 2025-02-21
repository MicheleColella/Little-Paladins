using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DebugManager : MonoBehaviour
{
    public static bool DebugState = false;

    [SerializeField] private Toggle debugToggle;
    [SerializeField] private TMP_Dropdown debugDropdown; // Dropdown per selezionare il metodo di movimento

    void Start()
    {
        if (debugToggle != null)
        {
            debugToggle.onValueChanged.AddListener(OnDebugToggleChanged);
            DebugState = debugToggle.isOn;
        }
        if (debugDropdown != null)
        {
            debugDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        }
    }

    private void OnDebugToggleChanged(bool isOn)
    {
        DebugState = isOn;
    }

    private void OnDropdownValueChanged(int index)
    {
        // 0 -> Keyboard, 1 -> PointAndClick
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            AvatarController avatarController = player.GetComponent<AvatarController>();
            if (avatarController != null)
            {
                avatarController.MovementMode = (index == 0) ? MovementType.Keyboard : MovementType.PointAndClick;
            }
        }
    }
}
