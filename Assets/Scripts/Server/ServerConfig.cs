using System.IO;
using UnityEngine;

public static class ServerConfig
{
    private static string ConfigPath => Path.Combine(Application.dataPath, "../server.ini");

    // Config variables
    public static int Port { get; private set; } = 9050;
    public static int MaxPlayers { get; private set; } = 50;
    public static string ServerName { get; private set; } = "Corrosive Survival Alpha";
    public static bool EnableDebugLogs { get; private set; } = true;

    public static void LoadOrCreateConfig()
    {
        if (!File.Exists(ConfigPath))
        {
            CreateDefaultConfig();
        }

        foreach (var line in File.ReadAllLines(ConfigPath))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";")) continue;

            var parts = line.Split('=');
            if (parts.Length != 2) continue;

            string key = parts[0].Trim().ToLower();
            string value = parts[1].Trim();

            switch (key)
            {
                case "port":
                    if (int.TryParse(value, out int p)) Port = p;
                    break;
                case "max_players":
                    if (int.TryParse(value, out int mp)) MaxPlayers = mp;
                    break;
                case "server_name":
                    ServerName = value;
                    break;
                case "enable_debug":
                    if (bool.TryParse(value, out bool dbg)) EnableDebugLogs = dbg;
                    break;
            }
        }

        Debug.Log($"[Config]: Loaded server configuration. Port: {Port}, Max Players: {MaxPlayers}");
    }

    private static void CreateDefaultConfig()
    {
        string defaultContent = 
            "; Corrosive Survival Server Configuration\n" +
            "port = 9050\n" +
            "max_players = 50\n" +
            "server_name = Corrosive Survival Alpha\n" +
            "enable_debug = true\n";

        File.WriteAllText(ConfigPath, defaultContent);
        Debug.Log("[Config]: Created default server.ini file.");
    }
}		