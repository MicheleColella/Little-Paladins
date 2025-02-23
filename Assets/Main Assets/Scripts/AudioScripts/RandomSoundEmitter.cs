using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class SoundClip
{
    public AudioClip clip;
    public float minVolume = 0.5f;
    public float maxVolume = 1.0f;
    public float minPitch = 0.8f;
    public float maxPitch = 1.2f;
    
    public bool useFade = true;
    public AnimationCurve fadeCurve = AnimationCurve.Linear(0, 1, 1, 0);

    public UnityEvent OnSoundStart;
    public UnityEvent OnSoundEnd;
}

public class RandomSoundEmitter : MonoBehaviour
{
    public List<SoundClip> soundClips;
    public AudioSource audioSourceOBJ;
    public bool audioIs3D = false;

    private SoundClip lastSoundClip;

    public void PlayRandomSound()
    {
        if (soundClips == null || soundClips.Count == 0)
        {
            Debug.LogWarning("Nessun SoundClip assegnato.");
            return;
        }

        SoundClip selectedSound = soundClips[Random.Range(0, soundClips.Count)];
        if (soundClips.Count > 1 && lastSoundClip != null)
        {
            while (selectedSound == lastSoundClip)
                selectedSound = soundClips[Random.Range(0, soundClips.Count)];
        }
        lastSoundClip = selectedSound;

        if (audioSourceOBJ == null)
        {
            Debug.LogError("AudioSource OBJ non assegnato.");
            return;
        }

        GameObject audioGO = new GameObject("RandomSoundAudio");
        audioGO.transform.position = transform.position;
        AudioSource source = audioGO.AddComponent<AudioSource>();
        CopyAudioSourceSettings(audioSourceOBJ, source);
        source.spatialBlend = audioIs3D ? 1f : 0f;

        float randomVolume = Random.Range(selectedSound.minVolume, selectedSound.maxVolume);
        float randomPitch = Random.Range(selectedSound.minPitch, selectedSound.maxPitch);

        source.clip = selectedSound.clip;
        source.volume = randomVolume;
        source.pitch = randomPitch;

        source.Play();
        selectedSound.OnSoundStart?.Invoke();

        if (selectedSound.useFade)
            StartCoroutine(FadeAndDestroy(source, randomVolume, selectedSound.fadeCurve, selectedSound.OnSoundEnd));
        else
            StartCoroutine(WaitAndDestroy(source, selectedSound.OnSoundEnd));
    }

    private void CopyAudioSourceSettings(AudioSource template, AudioSource target)
    {
        target.outputAudioMixerGroup = template.outputAudioMixerGroup;
        target.spatialBlend = template.spatialBlend;
        target.rolloffMode = template.rolloffMode;
        target.minDistance = template.minDistance;
        target.maxDistance = template.maxDistance;
        target.loop = template.loop;
        target.playOnAwake = template.playOnAwake;
        target.mute = template.mute;
        target.bypassEffects = template.bypassEffects;
        target.bypassListenerEffects = template.bypassListenerEffects;
        target.bypassReverbZones = template.bypassReverbZones;
    }

    private IEnumerator FadeAndDestroy(AudioSource source, float baseVolume, AnimationCurve fadeCurve, UnityEvent clipEndEvent)
    {
        float fadeDuration = fadeCurve.keys[fadeCurve.keys.Length - 1].time;
        float clipLength = source.clip.length;
        float fadeStartTime = Mathf.Max(clipLength - fadeDuration, 0f);
        yield return new WaitForSeconds(fadeStartTime);

        float t = 0f;
        while (t < fadeDuration)
        {
            source.volume = baseVolume * fadeCurve.Evaluate(t / fadeDuration);
            t += Time.deltaTime;
            yield return null;
        }
        source.volume = 0;
        clipEndEvent?.Invoke();
        Destroy(source.gameObject);
    }
    
    private IEnumerator WaitAndDestroy(AudioSource source, UnityEvent clipEndEvent)
    {
        yield return new WaitForSeconds(source.clip.length);
        clipEndEvent?.Invoke();
        Destroy(source.gameObject);
    }
}
