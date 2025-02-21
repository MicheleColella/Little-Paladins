using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;

public class MusicManager : MonoBehaviour
{
    [Header("UI Components")]
    public TMP_Dropdown musicDropdown;   
    public Slider volumeSlider;          // Gestisce il volume del Master Mixer

    [Header("Audio Components")]
    public AudioSource audioSource;      // L'AudioSource su cui facciamo il fade
    public AudioMixer audioMixer;        // Il Mixer su cui lo Slider agisce
    public string masterVolumeParam = "Master";

    [Header("Music Clips")]
    public List<AudioClip> musicClips = new List<AudioClip>();

    [Header("Fade Settings")]
    public AnimationCurve fadeOutCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
    public AnimationCurve fadeInCurve  = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    public float fadeDuration = 1.0f;

    private Coroutine musicChangeCoroutine;
    private bool isMuted = false; // Flag per controllare se l'audio è mutato

    private void Start()
    {
        // Popola il TMP_Dropdown con i nomi degli AudioClip
        musicDropdown.ClearOptions();
        List<string> options = new List<string>();
        foreach (AudioClip clip in musicClips)
        {
            options.Add(clip.name);
        }
        musicDropdown.AddOptions(options);

        // Listener per cambio musica e cambio volume
        musicDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        volumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged);

        // Inizializziamo il Mixer in base allo slider
        OnVolumeSliderChanged(volumeSlider.value);

        // Se ci sono clip, avvia il primo all'inizio
        if (musicClips.Count > 0)
        {
            audioSource.clip = musicClips[0];
            audioSource.volume = 1f;   // Volume AudioSource a 1 di default
            audioSource.Play();
            musicDropdown.value = 0;   // Imposta il dropdown sul primo elemento
        }
    }

    // Cambia brano quando si sceglie dal dropdown
    private void OnDropdownValueChanged(int index)
    {
        if (index >= 0 && index < musicClips.Count)
        {
            if (musicChangeCoroutine != null)
                StopCoroutine(musicChangeCoroutine);

            musicChangeCoroutine = StartCoroutine(ChangeMusic(musicClips[index]));
        }
    }

    // Lo slider regola il volume del Master del Mixer (solo se non è mutato)
    private void OnVolumeSliderChanged(float value)
    {
        if (isMuted)
            return; // Se è mutato, non aggiornare il volume

        float dB = -80f; 
        if (value > 0.0001f)
            dB = Mathf.Log10(value) * 20f;

        audioMixer.SetFloat(masterVolumeParam, dB);
    }

    // Coroutine per il cambio brano con fade out/in sull’AudioSource.volume
    private IEnumerator ChangeMusic(AudioClip newClip)
    {
        // FASE 1: FADE OUT (volume AudioSource da valore attuale a 0)
        float startVolume = audioSource.volume;
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / fadeDuration);
            float curveValue = fadeOutCurve.Evaluate(t);
            audioSource.volume = Mathf.Lerp(startVolume, 0f, curveValue);
            yield return null;
        }

        // Cambio clip
        audioSource.Stop();
        audioSource.clip = newClip;
        audioSource.Play();

        // FASE 2: FADE IN (volume AudioSource da 0 a 1)
        startVolume = 0f;
        timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / fadeDuration);
            float curveValue = fadeInCurve.Evaluate(t);
            audioSource.volume = Mathf.Lerp(startVolume, 1f, curveValue);
            yield return null;
        }

        audioSource.volume = 1f;
    }

    // --- NUOVE FUNZIONI PER MUTO / UNMUTE ---

    // Muta l'audio sul Master Mixer e imposta il flag isMuted a true
    public void MuteAudio()
    {
        isMuted = true;
        audioMixer.SetFloat(masterVolumeParam, -80f);
    }

    // Ripristina l'audio in base allo slider e imposta il flag isMuted a false
    public void UnmuteAudio()
    {
        isMuted = false;
        OnVolumeSliderChanged(volumeSlider.value);
    }
}
