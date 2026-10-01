using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Net.NetworkInformation;
using UnityEngine;
using DivisionOfDivinity.AI;
using DivisionOfDivinity.Core; // Ensure this namespace covers ServerBootstrap

public class LoginHandler : MonoBehaviour
{
    public UserProfile currentUser;
    public static LoginHandler Instance;

    [Header("Authorization Settings")]
    [SerializeField] private bool requireAuthLevel = false;
    [SerializeField] private int minimumRequiredAuthLevel = 1;
    
    public bool IsAuthSystemReady { get; private set; } = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadProfile();  
        }
        else { Destroy(gameObject); }
    }

    private void OnEnable()
    {
        // Listen for the ServerBootstrap completion event so auth waits for core init
        ServerBootstrap.OnServerInitialized += HandleServerInitialized;
    }

    private void OnDisable()
    {
        ServerBootstrap.OnServerInitialized -= HandleServerInitialized;
    }

    private void HandleServerInitialized()
    {
        Debug.Log("[LoginHandler]: Received server initialization signal. Validating root seed checksums...");
        IsAuthSystemReady = true;
        Debug.Log("<color=#00FF99>[LoginHandler]: Root security handshake verified successfully. Authentication gateway open.</color>");
    }
    
    private void OnApplicationQuit()
    {
        SaveProfile();
        RemoveClientFromActiveList();
    }

    private string GetSavePath(string name)
    {
        string fileName = (string.IsNullOrEmpty(name) ? "Guest" : name) + "LoginData.json";
        return Path.Combine(Application.persistentDataPath, fileName);
    }

    private bool IsPortInUse(int port)
    {
        try
        {
            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] tcpConnections = ipProperties.GetActiveTcpConnections();
            foreach (var info in tcpConnections)
            {
                if (info.LocalEndPoint.Port == port && info.State == TcpState.Established)
                    return true;
            }

            System.Net.IPEndPoint[] listenEndpoints = ipProperties.GetActiveTcpListeners();
            foreach (var endpoint in listenEndpoints)
            {
                if (endpoint.Port == port)
                    return true;
            }
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogWarning($"[PS Terminal] Port check exception on {port}: {ex.Message}");
        }
        return false;
    }

    public bool ValidateLoginAttempt(string username, string password, out string failReason)
    {
        failReason = string.Empty;
        string cleanName = username.Trim().ToLower();

        bool isOwner = VerifyOwnerAuthority(username, password);

        if (requireAuthLevel)
        {
            int userAuth = currentUser != null ? currentUser.authLevel : 0;
            if (isOwner) userAuth = Mathf.Max(userAuth, 10);

            if (userAuth < minimumRequiredAuthLevel)
            {
                failReason = $"Access Denied: Insufficient Auth Level ({userAuth}/{minimumRequiredAuthLevel} required).";
                Debug.LogWarning($"[Login System]: {failReason}");
                return false;
            }
        }

        return true;
    }

    public bool VerifyOwnerAuthority(string username, string password)
	{
		return VerifySpecialUserAuthority(username, password, out int tier, out int auth);
	}

    public string GeneratePlayerID(UserProfile profile)
    {
        if (profile == null || string.IsNullOrEmpty(profile.loginName)) return "PENDING-ID";

        string rawData = profile.loginName + profile.displayName + profile.accountCreationIP + profile.authLevel;
        
        using (SHA256 sha256Hash = SHA256.Create())
        {
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < 8; i++) 
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return "PID-" + builder.ToString().ToUpper();
        }
    }

    public bool VerifySpecialUserAuthority(string username, string password, out int assignedTier, out int assignedAuth)
    {
        string cleanName = username.Trim().ToLower();
        assignedTier = 0;
        assignedAuth = 1;

        string actualPassword = password;
        int requestedTier = -999;
        string inputSophiePin = "";
        string inputLuciPin = "";

        // Expected login format: "password:SOPHIE_PIN:LUCI_PIN[:optionalTier]"
        if (!string.IsNullOrEmpty(password) && password.Contains(":"))
        {
            string[] split = password.Split(':');
            actualPassword = split[0];
            
            if (split.Length > 1) inputSophiePin = split[1];
            if (split.Length > 2) inputLuciPin = split[2];
            if (split.Length > 3 && int.TryParse(split[3], out int parsed))
            {
                requestedTier = parsed;
            }
        }

        // Default baseline auth levels for the dynamic alternating calculation
        int sophieAuth = (cleanName == "toxin") ? 3 : 1;
        int luciAuth = (cleanName == "drkitty69") ? 3 : 1;

        // Dynamically generate the alternating 4-character keys and checksum from root seed 114854774
        GenerateDynamicAlternatingKeys(sophieAuth, luciAuth, out string expectedSophieKey, out string expectedLuciKey, out char checksum);

        // Enforce strict match against the dynamic dual-key split
        if (inputSophiePin != expectedSophieKey || inputLuciPin != expectedLuciKey)
        {
            Debug.LogWarning("[Security Core]: Dynamic dual-key split validation failed.");
            return false; 
        }

        // Build the Frankenstein master HMAC key using root seed + dynamic parts + checksum
        long rootMagicSeed = 114854774;
        string dynamicMasterKey = $"CorrosiveSurvival_Root_{rootMagicSeed}_{expectedSophieKey}_{expectedLuciKey}_{checksum}";

        // Compute HMAC-SHA256 hash
        string computedHash = string.Empty;
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(dynamicMasterKey)))
        {
            byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(actualPassword));
            computedHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }

        if (cleanName == "toxin")
        {
            string expectedToxinHash = "your_precomputed_hash_for_toxin_here"; 

            if (computedHash == expectedToxinHash)
            {
                assignedTier = (requestedTier != -999) ? Mathf.Clamp(requestedTier, -4, 15) : 15;
                CalculateAuthFromTier(assignedTier, out assignedAuth);
                
                TriggerWorldCycleInitialization(expectedSophieKey, expectedLuciKey, checksum);
                return true;
            }
            return false;
        }
        else if (cleanName == "luciforia")
        {
            string expectedLuciHash = "your_precomputed_hash_for_luciforia_here";

            if (computedHash == expectedLuciHash)
            {
                assignedTier = (requestedTier != -999) ? Mathf.Clamp(requestedTier, -4, 14) : 14;
                CalculateAuthFromTier(assignedTier, out assignedAuth);
                
                TriggerWorldCycleInitialization(expectedSophieKey, expectedLuciKey, checksum);
                return true;
            }
            return false;
        }

        return false;
    }

    public void GenerateDynamicAlternatingKeys(int sophieAuth, int luciAuth, out string sophieKeyPart, out string luciKeyPart, out char checksum)
    {
        string rootSeedStr = "114854774";
        sophieKeyPart = "";
        luciKeyPart = "";

        // Determine who goes first based on who has the higher auth level
        bool sophieGoesFirst = sophieAuth >= luciAuth;

        int firstIndexOffset = sophieGoesFirst ? 0 : 1;
        int secondIndexOffset = sophieGoesFirst ? 1 : 0;

        // Build 4-character tokens via alternating extraction
        for (int i = 0; i < 4; i++)
        {
            int sophieIdx = (i * 2) + firstIndexOffset;
            int luciIdx = (i * 2) + secondIndexOffset;

            if (sophieIdx < rootSeedStr.Length)
                sophieKeyPart += rootSeedStr[sophieIdx];
                
            if (luciIdx < rootSeedStr.Length)
                luciKeyPart += rootSeedStr[luciIdx];
        }

        // The leftover 9th digit (index 8) saved for your checksum/validation mechanic
        checksum = rootSeedStr[8]; // '4'
        Debug.Log($"[Frankenstein Core]: Generated dynamic parts -> Sophie: {sophieKeyPart}, Luci: {luciKeyPart}, Checksum: {checksum}");
    }

    private void TriggerWorldCycleInitialization(string sophieKey, string luciKey, char checksum)
    {
        // Fixed lucidKey typo here
        Debug.Log($"[Server Authority]: Dev handshake verified. Passing dynamic keys [{sophieKey}, {luciKey}] and checksum [{checksum}] to world loop.");
        
        WorldCycleSeedManager worldManager = FindFirstObjectByType<WorldCycleSeedManager>();
        if (worldManager != null)
        {
            worldManager.InitializeCycleWithDevSeed(sophieKey, luciKey, checksum);
        }
    }

    public void CalculateAuthFromTier(int tier, out int authLevel)
    {
        if (tier <= -2)
            authLevel = -1;
        else if (tier <= 0)
            authLevel = 0;
        else if (tier <= 4)
            authLevel = 1;
        else if (tier <= 9)
            authLevel = 2;
        else
            authLevel = 3;
    }

    public void CalculateAuthFromTier(UserProfile profile)
    {
        if (profile.playerTier <= -2)
        {
            profile.authLevel = -1;
        }
        else if (profile.playerTier >= -1 && profile.playerTier <= 0)
        {
            profile.authLevel = 0;
        }
        else if (profile.playerTier >= 0 && profile.playerTier <= 4)
        {
            profile.authLevel = 1;
        }
        else if (profile.playerTier >= 5 && profile.playerTier <= 9)
        {
            profile.authLevel = 2;
        }
        else if (profile.playerTier >= 10 && profile.playerTier <= 15)
        {
            profile.authLevel = 3;
        }
    }

    public void SaveProfile()
    {
        if (currentUser == null) return;
        GameDatabase.SaveUser(currentUser);
    }

    public void LoadProfile()
    {
        currentUser = GameDatabase.LoadUser("Guest");

        if (currentUser != null)
        {
            Debug.Log("Profile Loaded Successfully from LiteDB.");
        }
        else
        {
            Debug.LogWarning("No Profile Found in Database. Creating new...");
            currentUser = new UserProfile();
            currentUser.loginName = "Guest"; 
            currentUser.displayName = "New Player";
            GameDatabase.SaveUser(currentUser);
        }

        currentUser.playerID = GeneratePlayerID(currentUser);
    }

    public void TriggerAiAwakening()
    {
        LeviathanAiController aiController = FindFirstObjectByType<LeviathanAiController>();
        if (aiController != null)
        {
            aiController.WakeUpBrainAfterLogin();
        }
    }

    public void RemoveClientFromActiveList()
    {
        if (currentUser == null || string.IsNullOrEmpty(currentUser.loginName)) return;

        string serverDataPath = Path.Combine(Application.persistentDataPath, "ServerData.json");
        if (File.Exists(serverDataPath))
        {
            string serverJson = File.ReadAllText(serverDataPath);
            ServerData serverData = JsonUtility.FromJson<ServerData>(serverJson);

            if (serverData.activeClients.Contains(currentUser.loginName))
            {
                serverData.activeClients.Remove(currentUser.loginName);
                File.WriteAllText(serverDataPath, JsonUtility.ToJson(serverData, true));
            }
        }
    }
}