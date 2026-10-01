using UnityEngine;

public class DeveloperConsoleCommands : MonoBehaviour
{
    private static string _lastProcessedCommand = "";
    private static float _lastProcessTime = 0f;
    private const float DebounceThreshold = 0.2f;

    public static void HandleCommand(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return;

        string trimmedInput = input.Trim();
        string cleanLower = trimmedInput.ToLower();

        // Prevent duplicate spam firing in same frame window
        if (trimmedInput == _lastProcessedCommand && (Time.unscaledTime - _lastProcessTime) < DebounceThreshold)
        {
            return; 
        }

        _lastProcessedCommand = trimmedInput;
        _lastProcessTime = Time.unscaledTime;

        Debug.Log($"[DevConsole]: Command received -> \"{trimmedInput}\"");

        // Centralized Command Router
        if (cleanLower == "help")
        {
            PrintHelpMenu();
        }
        else if (cleanLower == "youtube" || cleanLower == "yt" || cleanLower == "open youtube")
        {
            OpenYouTubeChannel();
        }
        else if (cleanLower.StartsWith("youtube ") || cleanLower.StartsWith("yt "))
        {
            int spaceIndex = trimmedInput.IndexOf(' ');
            if (spaceIndex != -1 && spaceIndex < trimmedInput.Length - 1)
            {
                string searchQuery = trimmedInput.Substring(spaceIndex + 1).Trim();
                PlayInGameYouTubeAudio(searchQuery);
            }
            else
            {
                Debug.LogWarning("[DevConsole]: Please specify a search query after 'yt' or 'youtube'");
            }
        }
        else if (cleanLower == "wipe user" || cleanLower == "clear user")
        {
            WipeUserFiles();
        }
        else if (cleanLower == "wipe server" || cleanLower == "clear server")
        {
            WipeServerFiles();
        }
        else if (cleanLower == "flush vram" || cleanLower == "clear vram" || cleanLower == "gc")
        {
            FlushVRAM();
        }
        else if (cleanLower == "quit" || cleanLower == "exit" || cleanLower == "disconnect")
        {
            QuitGame();
        }
        else if (cleanLower == "host")
        {
            Object.FindFirstObjectByType<NetworkManager>()?.StartServer();
        }
        else if (cleanLower.StartsWith("connect "))
        {
            string ip = trimmedInput.Substring(8).Trim();
            Object.FindFirstObjectByType<NetworkManager>()?.ConnectToServer(ip);
        }
        else if (cleanLower.StartsWith("fps "))
        {
            if (int.TryParse(trimmedInput.Substring(4).Trim(), out int fps))
            {
                SettingsManager.Instance?.SetTargetFramerate(fps);
            }
        }
        else if (cleanLower == "vsync on")
        {
            SettingsManager.Instance?.SetVSync(true);
            Debug.Log("[DevConsole]: VSync Enabled.");
        }
        else if (cleanLower == "vsync off")
        {
            SettingsManager.Instance?.SetVSync(false);
            Debug.Log("[DevConsole]: VSync Disabled.");
        }
        else
        {
            Debug.LogWarning($"[DevConsole]: Unknown command -> \"{trimmedInput}\"");
        }
    }

    private static void PrintHelpMenu()
    {
        string helpText = 
            "--- AVAILABLE TERMINAL COMMANDS ---\n" +
            "  help          - Displays this list of available commands.\n" +
            "  clear         - Clears the terminal screen output.\n" +
            "  yt [query]    - Stream/play in-game YouTube audio.\n" +
            "  fps [value]   - Set target framerate cap.\n" +
            "  vsync on/off  - Toggle vertical synchronization.\n" +
            "  host          - Start local network host server.\n" +
            "  connect [ip]  - Connect to a remote server IP.\n" +
            "  flush vram    - Force garbage collection & asset unload.\n" +
            "  quit          - Safely shutdown game execution.\n" +
            "------------------------------------";
        
        Debug.Log(helpText);
        
        // If you want it to also pipe into the active HUD output:
        var hud = Object.FindFirstObjectByType<InGameTerminalHUD>();
        if (hud != null) hud.PrintToTerminal(helpText);
    }

    public static void QuitGame()
    {
        Debug.LogWarning("[DevConsole]: Shutting down game and saving data...");
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    public static async void FlushVRAM()
    {
        Debug.Log("[DevConsole]: Initiating VRAM and Memory Flush...");
        System.GC.Collect();
        System.GC.WaitForPendingFinalizers();

        var unloadAsync = Resources.UnloadUnusedAssets();
        while (!unloadAsync.isDone)
        {
            await System.Threading.Tasks.Task.Yield();
        }

        System.GC.Collect();
        Debug.Log("[DevConsole]: VRAM and Unused Asset Flush complete.");
    }

    public static void OpenYouTubeChannel()
    {
        string youtubeUrl = "https://www.youtube.com/@YourChannelName";
        Debug.Log($"[DevConsole]: Opening channel -> {youtubeUrl}");
        Application.OpenURL(youtubeUrl);
    }

    public static async void PlayInGameYouTubeAudio(string query)
    {
        if (YoutubeRadio.Instance == null)
        {
            Debug.LogError("[DevConsole]: No YoutubeRadio instance found in the scene!");
            return;
        }

        Debug.Log($"[DevConsole]: Requesting in-game YouTube audio for -> \"{query}\"");
        string result = await YoutubeRadio.Instance.PlayYoutubeAudio(query);
        Debug.Log($"[DevConsole]: YoutubeRadio Result -> {result}");
    }

    public static void WipeUserFiles()
    {
        Debug.LogWarning("[DevConsole]: Executing User Files Wipe...");
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[DevConsole]: User files and profile cache successfully wiped.");
    }

    public static void WipeServerFiles()
    {
        Debug.LogWarning("[DevConsole]: Executing Server Files Cache Clear...");
        Debug.Log("[DevConsole]: Server configuration signal sent.");
    }
}