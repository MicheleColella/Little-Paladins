using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Animator))]
public class AvatarAnimationEvents : MonoBehaviour
{
    [Header("Animation Events")]
    public UnityEvent OnRightFootStep;
    public UnityEvent OnLeftFootStep;
    public UnityEvent OnLand;

    [Header("Dependencies (opzionale)")]
    public AvatarController avatarController;

    private bool wasGrounded = true;

    private void Awake()
    {
        if (avatarController != null)
            wasGrounded = avatarController.IsTouchingGround();
    }

    private void Update()
    {
        if (avatarController != null)
        {
            bool isGrounded = avatarController.IsTouchingGround();
            if (!wasGrounded && isGrounded)
            {
                OnLand?.Invoke();
            }
            wasGrounded = isGrounded;
        }
    }

    public void RightFootStep()
    {
        if (avatarController != null && !avatarController.IsTouchingGround())
            return;
        OnRightFootStep?.Invoke();
    }

    public void LeftFootStep()
    {
        if (avatarController != null && !avatarController.IsTouchingGround())
            return;
        OnLeftFootStep?.Invoke();
    }
}
