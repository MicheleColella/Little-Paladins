using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;

[RequireComponent(typeof(AvatarController))]
public class PlayerInputManager : MonoBehaviour
{
    private PlayerInputActions inputActions;
    private AvatarController avatarController;

    [Tooltip("Layer mask delle superfici target per il raycast. Gli oggetti che non appartengono a questi layer saranno ignorati.")]
    [SerializeField] private LayerMask surfaceLayerMask;

    void Awake()
    {
        inputActions = new PlayerInputActions();
        avatarController = GetComponent<AvatarController>();
    }

    void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.Move.performed += OnMovePerformed;
        inputActions.Player.Move.canceled += OnMoveCanceled;
        inputActions.Player.Jump.performed += OnJumpPerformed;
        inputActions.Player.Click.performed += OnClickPerformed;
    }

    void OnDisable()
    {
        inputActions.Disable();
        inputActions.Player.Move.performed -= OnMovePerformed;
        inputActions.Player.Move.canceled -= OnMoveCanceled;
        inputActions.Player.Jump.performed -= OnJumpPerformed;
        inputActions.Player.Click.performed -= OnClickPerformed;
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        // Operiamo solo se l'avatar è Player
        if (avatarController.AvatarType != AvatarType.Player) return;
        Vector2 move = context.ReadValue<Vector2>();
        avatarController.SetMoveInput(move);
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        if (avatarController.AvatarType != AvatarType.Player) return;
        avatarController.SetMoveInput(Vector2.zero);
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if (avatarController.AvatarType != AvatarType.Player) return;
        avatarController.SetJumpInput(true);
    }

    private void OnClickPerformed(InputAction.CallbackContext context)
    {
        if (avatarController.AvatarType != AvatarType.Player) return;
        if (avatarController != null && avatarController.gameObject.activeInHierarchy)
        {
            if (avatarController.MovementMode == MovementType.PointAndClick)
            {
                Vector2 mousePos = Mouse.current.position.ReadValue();
                Ray ray = Camera.main.ScreenPointToRay(mousePos);
                Debug.DrawRay(ray.origin, ray.direction * 100f, Color.green, 2f);
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, surfaceLayerMask))
                {
                    if (NavMesh.SamplePosition(hit.point, out NavMeshHit hitInfo, 1.0f, NavMesh.AllAreas))
                    {
                        avatarController.SetTargetPosition(hitInfo.position);
                    }
                    else
                    {
                        Debug.Log("Il punto di click non è su una NavMesh valida.");
                    }
                }
                else
                {
                    Debug.Log("Raycast non ha colpito una superficie target.");
                }
            }
        }
    }
}
