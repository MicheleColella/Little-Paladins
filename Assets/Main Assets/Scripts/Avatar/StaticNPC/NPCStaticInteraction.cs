using UnityEngine;
using UnityEngine.Events;

public class NPCStaticInteraction : MonoBehaviour
{
    public Animator npcAnimator;
    public AudioSource npcAudioSource;
    public string boolParameterName = "Playing";

    public UnityEvent onActivated;
    public UnityEvent onDeactivated;

    public void Toggle()
    {
        bool currentState = npcAnimator.GetBool(boolParameterName);
        bool newState = !currentState;
        npcAnimator.SetBool(boolParameterName, newState);

        if (npcAudioSource.isPlaying)
        {
            npcAudioSource.Stop();
        }
        else
        {
            npcAudioSource.Play();
        }

        if (newState)
        {
            onActivated.Invoke();
        }
        else
        {
            onDeactivated.Invoke();
        }
    }
}
