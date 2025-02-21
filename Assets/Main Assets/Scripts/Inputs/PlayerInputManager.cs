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

    public bool CanMove = true;     // Controlla Move e Click.
    public bool CanInteract = true; // Controlla Interact.
    public bool CanJump = true;     // Controlla Jump.

    void Awake()
    {
        inputActions = new PlayerInputActions();
        avatarController = GetComponent<AvatarController>();
    }

    public PlayerInputActions InputActions => inputActions;

    void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.Move.performed += OnMovePerformed;
        inputActions.Player.Move.canceled += OnMoveCanceled;
        inputActions.Player.Jump.performed += OnJumpPerformed;
        inputActions.Player.Click.performed += OnClickPerformed;
        inputActions.Player.Interact.performed += OnInteractPerformed;
    }

    void OnDisable()
    {
        inputActions.Player.Move.performed -= OnMovePerformed;
        inputActions.Player.Move.canceled -= OnMoveCanceled;
        inputActions.Player.Jump.performed -= OnJumpPerformed;
        inputActions.Player.Click.performed -= OnClickPerformed;
        inputActions.Player.Interact.performed -= OnInteractPerformed;
        inputActions.Disable();
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        if (!CanMove) return;
        if (avatarController.AvatarType != AvatarType.Player) return;
        Vector2 move = context.ReadValue<Vector2>();
        avatarController.SetMoveInput(move);
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        if (!CanMove) return;
        if (avatarController.AvatarType != AvatarType.Player) return;
        avatarController.SetMoveInput(Vector2.zero);
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if (!CanJump) return;
        if (avatarController.AvatarType != AvatarType.Player) return;
        avatarController.SetJumpInput(true);
    }

    private void OnClickPerformed(InputAction.CallbackContext context)
    {
        if (!CanMove) return;
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
                }
            }
        }
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        if (!CanInteract) return;
        if (InteractionManager.Instance != null)
        {
            InteractionManager.Instance.InteractWithNearestObject();
        }
    }

    // Metodo per fermare il movimento corrente dell'avatar.
    public void StopMovement()
    {
        if (avatarController != null)
        {
            avatarController.StopMovement();
        }
    }
}
