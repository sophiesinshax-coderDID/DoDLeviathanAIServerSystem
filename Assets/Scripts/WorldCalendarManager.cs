using UnityEngine;
using System;
using System.IO;

[Serializable]
public class WorldAgeData
{
    public long totalMilliseconds; 
}

public class WorldCalendarManager : MonoBehaviour
{
    public static WorldCalendarManager Instance { get; private set; }

    public enum CelestialEvent { None, Eclipse, BloodMoon, YearEnd }

    [Header("Time Settings")]
    [Tooltip("Multiplier for world time progression. 1 = Realtime, 10 = 10x faster.")]
    public float timeScale = 10f; 
    private double currentTotalMs;
    private string filePath;

    [Header("Current Date Display")]
    public string worldAgeDisplay;

    [Header("Inspector Debug (Read Only)")]
    public long Year;
    public long Month;
    public long Week;
    public long DayOfMonth;
    public long DayOfYear;
    public float Hour;
    public CelestialEvent currentActiveEvent = CelestialEvent.None;

    private CelestialEvent lastLoggedEvent = CelestialEvent.None;
    private bool isServerRunning = false;

    public double GetCurrentTotalMs() => currentTotalMs;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Path mapped to shared server data directory
        filePath = Path.Combine(Application.persistentDataPath, "WorldCalendarData.json");
        LoadCalendar();
        
        // Start ticking when the server/manager boots
        StartServerClock();
    }

    public void StartServerClock()
    {
        isServerRunning = true;
        Debug.Log($"<color=cyan>[WorldCalendarManager]: Leviathan World Clock started. World Age: {currentTotalMs}ms</color>");
    }

    public void StopServerClock()
    {
        isServerRunning = false;
        SaveCalendar();
        Debug.Log("<color=orange>[WorldCalendarManager]: Leviathan World Clock paused and saved.</color>");
    }

    void Update()
    {
        if (!isServerRunning) return;

        // Advance world time based on server time scale
        currentTotalMs += Time.deltaTime * 1000 * timeScale;
        UpdateCalendarLogic();
        
        // Auto-save roughly every 60 real-world seconds
        if (Time.frameCount % 3600 == 0) 
        {
            SaveCalendar();
        }
    }

    void UpdateCalendarLogic()
    {
        double ms = currentTotalMs;

        Hour = (float)((ms / (1000 * 60 * 60)) % 24);
        long totalDays = (long)(ms / (1000 * 60 * 60 * 24));
        
        DayOfMonth = (totalDays % 28) + 1;
        Week = ((DayOfMonth - 1) / 7) + 1;
        
        long totalMonths = totalDays / 28;
        Month = (totalMonths % 13) + 1;
        Year = totalMonths / 13;
        DayOfYear = (totalDays % (28 * 13)) + 1;

        worldAgeDisplay = $"Year {Year}, Month {Month}, Week {Week}, Day {DayOfMonth} ({((int)Hour):D2}:{(int)((ms/60000)%60):D2})";

        CheckCelestialEvents();
    }

    void CheckCelestialEvents()
    {
        CelestialEvent detectedEvent = CelestialEvent.None;

        if (DayOfYear == 364) 
        {
            detectedEvent = CelestialEvent.YearEnd;
        }
        else if (DayOfYear == 167) 
        {
            detectedEvent = CelestialEvent.Eclipse;
        }
        else if (DayOfMonth == 14 && IsNight()) 
        {
            detectedEvent = CelestialEvent.BloodMoon;
        }

        currentActiveEvent = detectedEvent;

        if (currentActiveEvent != lastLoggedEvent)
        {
            if (currentActiveEvent != CelestialEvent.None)
            {
                string hexColor = GetEventColor(currentActiveEvent);
                Debug.Log($"<color={hexColor}><b>[LEVIATHAN EVENT START]</b></color> {currentActiveEvent} has begun on Day {DayOfYear} of Year {Year}!");
            }
            else
            {
                Debug.Log("<color=white>[LEVIATHAN EVENT END]</color> The celestial anomaly has subsided.");
            }
            
            lastLoggedEvent = currentActiveEvent;
        }
    }

    private string GetEventColor(CelestialEvent ev)
    {
        switch (ev)
        {
            case CelestialEvent.Eclipse: return "#4B0082"; 
            case CelestialEvent.BloodMoon: return "#FF0000"; 
            case CelestialEvent.YearEnd: return "#00FF7F"; 
            default: return "#FFFFFF";
        }
    }

    public float GetTotalHours() => Hour;
    public long GetCurrentDayOfYear() => DayOfYear;
    public long GetCurrentDayOfMonth() => DayOfMonth;
    public bool IsNight() => Hour >= 18 || Hour <= 6;

    public void SaveCalendar()
    {
        WorldAgeData data = new WorldAgeData { totalMilliseconds = (long)currentTotalMs };
        File.WriteAllText(filePath, JsonUtility.ToJson(data, true));
    }

    void LoadCalendar()
    {
        if (File.Exists(filePath))
        {
            try
            {
                WorldAgeData data = JsonUtility.FromJson<WorldAgeData>(File.ReadAllText(filePath));
                currentTotalMs = data.totalMilliseconds;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[WorldCalendarManager]: Failed to load world calendar data: {e.Message}");
            }
        }
    }

    private void OnGUI()
    {
        // Optional debug overlay showing server time state
        GUI.color = Color.yellow;
        GUI.Label(new Rect(10, 10, 600, 20), "LEVIATHAN WORLD CLOCK: " + worldAgeDisplay);
        
        if (currentActiveEvent != CelestialEvent.None)
        {
            GUI.color = Color.red;
            GUI.Label(new Rect(10, 30, 600, 20), "<b>ACTIVE CELESTIAL EVENT: " + currentActiveEvent.ToString().ToUpper() + "</b>");
        }
    }

    private void OnApplicationQuit() 
    {
        StopServerClock();
    }
}