using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

[RequireComponent(typeof(AvatarController))]
[RequireComponent(typeof(Rigidbody))]
public class AvatarAnimatorController : MonoBehaviour
{
    [Header("Riferimenti")]
    [Tooltip("Riferimento all'Animator (assegnabile dall'Inspector).")]
    public Animator animator;

    // Riferimenti ai componenti
    private AvatarController avatarController;
    private Rigidbody rb;
    private NavMeshAgent navAgent;

    void Awake()
    {
        avatarController = GetComponent<AvatarController>();
        if (avatarController == null)
        {
            Debug.LogError("AvatarController non trovato sul GameObject.");
        }
        rb = GetComponent<Rigidbody>();
        navAgent = GetComponent<NavMeshAgent>();
    }

    void OnEnable()
    {
        if (avatarController != null)
        {
            avatarController.OnJump.AddListener(HandleJump);
        }
    }

    void OnDisable()
    {
        if (avatarController != null)
        {
            avatarController.OnJump.RemoveListener(HandleJump);
        }
    }

    void Update()
    {
        if (animator == null) return;
        
        float speed = 0f;

        if (navAgent != null && navAgent.enabled)
        {
            Vector3 horizontalVelocity = new Vector3(navAgent.velocity.x, 0, navAgent.velocity.z);
            speed = horizontalVelocity.magnitude;
        }
        else
        {
            Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            speed = horizontalVelocity.magnitude;
        }

        // Calcolo velocità massima da AvatarController.
        float maxSpeed = avatarController.GetMaxSpeed();
        float normalizedSpeed = Mathf.Clamp01(speed / maxSpeed);
        animator.SetFloat("Vel", normalizedSpeed);

        // Aggiorniamo lo stato del contatto con il terreno
        bool isGrounded = avatarController.IsTouchingGround();
        animator.SetBool("IsGrounded", isGrounded);
    }

    private void HandleJump()
    {
        if (animator != null)
        {
            animator.SetTrigger("Jump");
        }
    }
}
