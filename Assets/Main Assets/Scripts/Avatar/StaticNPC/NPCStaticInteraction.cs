using UnityEngine;

public class NPCStaticInteraction : MonoBehaviour
{
    public Animator npcAnimator;

    public AudioSource npcAudioSource;

    public string boolParameterName = "Playing";

    public void Toggle()
    {
        bool currentState = npcAnimator.GetBool(boolParameterName);
        npcAnimator.SetBool(boolParameterName, !currentState);

        if (npcAudioSource.isPlaying)
        {
            npcAudioSource.Stop();
        }
        else
        {
            npcAudioSource.Play();
        }
    }
}
