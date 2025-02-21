using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class SoundClip
{
    [Tooltip("Clip Audio")]
    public AudioClip clip;

    [Header("Clip Audio Settings")]
    [Tooltip("Volume minimo casuale per questo clip")]
    public float minVolume = 0.5f;
    [Tooltip("Volume massimo casuale per questo clip")]
    public float maxVolume = 1.0f;
    [Tooltip("Pitch minimo casuale per questo clip")]
    public float minPitch = 0.8f;
    [Tooltip("Pitch massimo casuale per questo clip")]
    public float maxPitch = 1.2f;
    
    [Header("Fade Settings per il Clip")]
    [Tooltip("Se impostato su true, viene applicato il fade out tramite la fadeCurve")]
    public bool useFade = true;
    [Tooltip("Curva per regolare il fade out di questo clip. La durata del fade out è data dall'ultima chiave della curva.")]
    public AnimationCurve fadeCurve = AnimationCurve.Linear(0, 1, 1, 0);

    [Header("Events per il Clip")]
    public UnityEvent OnSoundStart;
    public UnityEvent OnSoundEnd;
}

public class RandomSoundEmitter : MonoBehaviour
{
    [Header("Sound Clips Settings")]
    [Tooltip("Lista dei SoundClip con le rispettive impostazioni")]
    public List<SoundClip> soundClips;

    [Header("Audio Source Settings")]
    [Tooltip("Se assegnato, questo AudioSource verrà usato come template per la configurazione")]
    public AudioSource audioSourceOBJ;

    // Per evitare di riprodurre due volte di seguito lo stesso clip (se possibile)
    private SoundClip lastSoundClip;

    /// <summary>
    /// Riproduce un suono casuale dalla lista usando un nuovo GameObject dotato di AudioSource.
    /// L'audiosource viene configurato copiando le impostazioni dal template audioSourceOBJ.
    /// Il GameObject viene distrutto automaticamente al termine del clip.
    /// </summary>
    public void PlayRandomSound()
    {
        if (soundClips == null || soundClips.Count == 0)
        {
            Debug.LogWarning("Nessun SoundClip assegnato.");
            return;
        }

        SoundClip selectedSound;
        if (soundClips.Count > 1 && lastSoundClip != null)
        {
            do
            {
                selectedSound = soundClips[Random.Range(0, soundClips.Count)];
            } while (selectedSound == lastSoundClip);
        }
        else
        {
            selectedSound = soundClips[Random.Range(0, soundClips.Count)];
        }
        lastSoundClip = selectedSound;

        if (audioSourceOBJ == null)
        {
            Debug.LogError("AudioSource OBJ non assegnato.");
            return;
        }

        // Crea un nuovo GameObject vuoto con un AudioSource
        GameObject audioGO = new GameObject("RandomSoundAudio");
        audioGO.transform.position = transform.position;
        AudioSource source = audioGO.AddComponent<AudioSource>();
        CopyAudioSourceSettings(audioSourceOBJ, source);

        // Calcola volume e pitch casuali
        float randomVolume = Random.Range(selectedSound.minVolume, selectedSound.maxVolume);
        float randomPitch = Random.Range(selectedSound.minPitch, selectedSound.maxPitch);

        // Verifica i valori generati
        //Debug.Log("RandomVolume generato: " + randomVolume);
        //Debug.Log("RandomPitch generato: " + randomPitch);

        // Imposta il clip e i parametri
        source.clip = selectedSound.clip;
        source.volume = randomVolume;
        source.pitch = randomPitch;

        source.Play();
        selectedSound.OnSoundStart?.Invoke();

        if (selectedSound.useFade)
        {
            StartCoroutine(FadeAndDestroy(source, randomVolume, selectedSound.fadeCurve, selectedSound.OnSoundEnd));
        }
        else
        {
            StartCoroutine(WaitAndDestroy(source, selectedSound.OnSoundEnd));
        }
    }

    /// <summary>
    /// Copia alcune delle impostazioni dell'AudioSource template nel target.
    /// </summary>
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
        // Aggiungi altre proprietà se necessario
    }

    /// <summary>
    /// Coroutine che gestisce il fade out del suono secondo la curva specifica e distrugge il GameObject.
    /// La durata del fade è data dall'ultima chiave della fadeCurve.
    /// </summary>
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

    /// <summary>
    /// Coroutine che attende la fine del clip per invocare l'evento di fine e distruggere il GameObject.
    /// </summary>
    private IEnumerator WaitAndDestroy(AudioSource source, UnityEvent clipEndEvent)
    {
        yield return new WaitForSeconds(source.clip.length);
        clipEndEvent?.Invoke();
        Destroy(source.gameObject);
    }
}