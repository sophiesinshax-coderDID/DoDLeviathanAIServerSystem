using UnityEngine;
using UnityEngine.Audio;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Audio References")]
    [SerializeField] private AudioMixer mainAudioMixer; // Optional: Drag your main AudioMixer here

    // Default configuration values
    private const string PrefsKey_MasterVol = "Settings_MasterVol";
    private const string PrefsKey_MusicVol = "Settings_MusicVol";
    private const string PrefsKey_SFXVol = "Settings_SFXVol";
    private const string PrefsKey_TargetFPS = "Settings_TargetFPS";
    private const string PrefsKey_VSync = "Settings_VSync";
    private const string PrefsKey_ResolutionIndex = "Settings_ResIndex";
    private const string PrefsKey_Fullscreen = "Settings_Fullscreen";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- AUDIO SETTINGS ---
    public void SetMasterVolume(float volume) // volume range: 0.001 to 1.0 (or -80dB to 0dB)
    {
        PlayerPrefs.SetFloat(PrefsKey_MasterVol, volume);
        float db = volume > 0.001f ? Mathf.Log10(volume) * 20f : -80f;
        if (mainAudioMixer != null) mainAudioMixer.SetFloat("MasterVol", db);
        PlayerPrefs.Save();
    }

    public void SetMusicVolume(float volume)
    {
        PlayerPrefs.SetFloat(PrefsKey_MusicVol, volume);
        float db = volume > 0.001f ? Mathf.Log10(volume) * 20f : -80f;
        if (mainAudioMixer != null) mainAudioMixer.SetFloat("MusicVol", db);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float volume)
    {
        PlayerPrefs.SetFloat(PrefsKey_SFXVol, volume);
        float db = volume > 0.001f ? Mathf.Log10(volume) * 20f : -80f;
        if (mainAudioMixer != null) mainAudioMixer.SetFloat("SFXVol", db);
        PlayerPrefs.Save();
    }

    // --- GRAPHICS & VRAM MANAGEMENT SETTINGS ---
    public void SetTargetFramerate(int targetFPS)
    {
        // Capping framerate prevents your GPU/VRAM from cooking itself rendering unused high frames in menus
        Application.targetFrameRate = targetFPS;
        PlayerPrefs.SetInt(PrefsKey_TargetFPS, targetFPS);
        PlayerPrefs.Save();
        Debug.Log($"[Settings]: Target FPS set to {targetFPS}");
    }

    public void SetVSync(bool enabled)
    {
        QualitySettings.vSyncCount = enabled ? 1 : 0;
        PlayerPrefs.SetInt(PrefsKey_VSync, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetResolution(int width, int height, bool fullscreen)
    {
        Screen.SetResolution(width, height, fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        PlayerPrefs.SetInt(PrefsKey_Fullscreen, fullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    // --- LOAD SAVED PREFERENCES ---
    public void LoadSettings()
    {
        // Load Audio
        float master = PlayerPrefs.GetFloat(PrefsKey_MasterVol, 1f);
        float music = PlayerPrefs.GetFloat(PrefsKey_MusicVol, 1f);
        float sfx = PlayerPrefs.GetFloat(PrefsKey_SFXVol, 1f);
        SetMasterVolume(master);
        SetMusicVolume(music);
        SetSFXVolume(sfx);

        // Load Graphics / VRAM caps
        int targetFPS = PlayerPrefs.GetInt(PrefsKey_TargetFPS, 120);
        SetTargetFramerate(targetFPS);

        int vSync = PlayerPrefs.GetInt(PrefsKey_VSync, 0);
        SetVSync(vSync == 1);

        Debug.Log("[Settings]: Loaded user configurations successfully.");
    }
}