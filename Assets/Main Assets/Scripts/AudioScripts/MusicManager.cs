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
    public Slider sfxSlider;             // Gestisce il volume degli SFX
    public Slider backgroundSlider;      // Gestisce il volume della Background Music

    [Header("Audio Components")]
    public AudioSource audioSource;      // L'AudioSource su cui viene effettuato il fade
    public AudioMixer audioMixer;        // Il Mixer su cui gli Slider agiscono
    public string masterVolumeParam = "Master";
    public string sfxVolumeParam = "SFX";
    public string backgroundVolumeParam = "Backgroundmusic";

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
        musicDropdown.ClearOptions();
        List<string> options = new List<string>();
        foreach (AudioClip clip in musicClips)
        {
            options.Add(clip.name);
        }
        musicDropdown.AddOptions(options);

        musicDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        volumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged);
        sfxSlider.onValueChanged.AddListener(OnSFXSliderChanged);
        backgroundSlider.onValueChanged.AddListener(OnBackgroundSliderChanged);

        // Inizializziamo il Mixer in base agli slider
        OnVolumeSliderChanged(volumeSlider.value);
        OnSFXSliderChanged(sfxSlider.value);
        OnBackgroundSliderChanged(backgroundSlider.value);

        // Se ci sono clip, avvia il primo all'inizio
        if (musicClips.Count > 0)
        {
            audioSource.clip = musicClips[0];
            audioSource.volume = 1f;
            audioSource.Play();
            musicDropdown.value = 0;
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

    // Regola il volume del Master Mixer (solo se non è mutato)
    private void OnVolumeSliderChanged(float value)
    {
        if (isMuted)
            return;

        float dB = -80f; 
        if (value > 0.0001f)
            dB = Mathf.Log10(value) * 20f;

        audioMixer.SetFloat(masterVolumeParam, dB);
    }

    // Regola il volume degli SFX
    private void OnSFXSliderChanged(float value)
    {
        float dB = -80f;
        if (value > 0.0001f)
            dB = Mathf.Log10(value) * 20f;

        audioMixer.SetFloat(sfxVolumeParam, dB);
    }

    // Regola il volume della Background Music
    private void OnBackgroundSliderChanged(float value)
    {
        float dB = -80f;
        if (value > 0.0001f)
            dB = Mathf.Log10(value) * 20f;

        audioMixer.SetFloat(backgroundVolumeParam, dB);
    }

    private IEnumerator ChangeMusic(AudioClip newClip)
    {
        // FADE OUT
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

        // FADE IN
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

    // Il pulsante mute/smute influenza solo il Master
    public void MuteAudio()
    {
        isMuted = true;
        audioMixer.SetFloat(masterVolumeParam, -80f);
    }

    public void UnmuteAudio()
    {
        isMuted = false;
        OnVolumeSliderChanged(volumeSlider.value);
    }
}
