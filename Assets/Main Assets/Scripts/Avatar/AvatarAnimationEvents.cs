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
        // Se l'avatar ritrova il contatto con il terreno (dopo non averlo avuto) attiva OnLand.
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

    // Funzioni da invocare tramite Animation Events (da assegnare nell'Animator)
    public void RightFootStep()
    {
        // Se l'avatar non tocca il pavimento, non invoca l'evento
        if (avatarController != null && !avatarController.IsTouchingGround())
            return;
        OnRightFootStep?.Invoke();
    }

    public void LeftFootStep()
    {
        // Se l'avatar non tocca il pavimento, non invoca l'evento
        if (avatarController != null && !avatarController.IsTouchingGround())
            return;
        OnLeftFootStep?.Invoke();
    }
}
