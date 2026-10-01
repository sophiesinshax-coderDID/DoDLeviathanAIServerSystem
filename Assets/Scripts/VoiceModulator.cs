using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class VoiceModulator : MonoBehaviour
{
    private AudioSource audioSource;

    [Header("Core Acoustic Adjustments")]
    [Range(0.5f, 2.0f)]
    [Tooltip("Adjusts playback tempo independently of pitch if supported, or acts as a time scaler.")]
    public float tempo = 1.0f;

    [Range(-3.0f, 3.0f)]
    [Tooltip("Pitch shift. Lower values create deep, resonant tones; higher values create sharp or squeaky tones.")]
    public float pitch = 1.0f;

    [Header("Character Identity Parameters")]
    [Range(0.0f, 1.0f)]
    [Tooltip("Simulates vocal age (0.0 = youthful/high resonance, 1.0 = weathered/deep texture).")]
    public float age = 0.5f;

    [Range(-1.0f, 1.0f)]
    [Tooltip("Vocal tract bias (-1.0 = feminine/compact formant shift, 1.0 = masculine/expanded formant shift).")]
    public float genderBias = 0.0f;

    [Header("Biological & Linguistic Encoding")]
    [Tooltip("True = standard humanoid vocal filters; False = alien, beast, or biomechanical distortion filters.")]
    public bool isHuman = true;

    [Tooltip("True = standard English speech synthesis; False = custom constructed language phoneme mapping.")]
    public bool speakEnglish = true;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        ApplyModulationToAudioSource();
    }

    private void ApplyModulationToAudioSource()
    {
        if (audioSource != null)
        {
            // Base pitch modified by gender formant and age shift
            float computedPitch = pitch + (genderBias * 0.15f) - ((age - 0.5f) * 0.1f);
            audioSource.pitch = Mathf.Clamp(computedPitch, -3f, 3f);
            
            // Note: Unity's native AudioSource handles pitch changes, which inherently scales 
            // speed together. If you integrate an external DSP or time-pitch plugin (like Rubber Band),
            // you can feed the separate 'tempo' variable directly into it.
        }
    }

    // --- UI Slider & Toggle Hookup Methods ---

    public void SetTempo(float value)
    {
        tempo = Mathf.Clamp(value, 0.5f, 2.0f);
    }

    public void SetPitch(float value)
    {
        pitch = Mathf.Clamp(value, -3.0f, 3.0f);
    }

    public void SetAge(float value)
    {
        age = Mathf.Clamp01(value);
    }

    public void SetGenderBias(float value)
    {
        genderBias = Mathf.Clamp(value, -1.0f, 1.0f);
    }

    public void SetIsHuman(bool humanState)
    {
        isHuman = humanState;
    }

    public void SetSpeakEnglish(bool englishState)
    {
        speakEnglish = englishState;
    }
}