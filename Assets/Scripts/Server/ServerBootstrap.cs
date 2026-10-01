using UnityEngine;
using System.IO;
using System;
using DivisionOfDivinity.AI;

namespace DivisionOfDivinity.Core
{
    [DefaultExecutionOrder(-100)]
    public class ServerBootstrap : MonoBehaviour
    {
        [Header("Prefabs / Core Dependencies")]
        [SerializeField] private GameObject loginHandlerPrefab;
        [SerializeField] private GameObject aiControllerPrefab;

        public static ServerBootstrap Instance { get; private set; }
        public static event Action OnServerInitialized;
        
        public bool IsServerReady { get; private set; } = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            RunInitializationSequence();
        }

        private void RunInitializationSequence()
        {
            Debug.Log("[ServerBootstrap]: Initiating core system startup order...");

            // Step 1: Load local .ini configurations first via ServerConfig
            ServerConfig.LoadOrCreateConfig();

            // Step 2: Verify or establish base persistent file paths & directories & ServerData.json
            EnsureServerEnvironment();

            // Step 3: Ensure LoginHandler exists
            if (LoginHandler.Instance == null)
            {
                if (loginHandlerPrefab != null)
                {
                    Instantiate(loginHandlerPrefab);
                }
                else
                {
                    GameObject loginObj = new GameObject("LoginHandler_Auto");
                    loginObj.AddComponent<LoginHandler>();
                }
            }

            // Step 4: Ensure AI Controller exists (Skipped automatically if running a headless server build)
#if !UNITY_SERVER
            if (FindFirstObjectByType<LeviathanAiController>() == null)
            {
                if (aiControllerPrefab != null)
                {
                    Instantiate(aiControllerPrefab);
                }
                else
                {
                    Debug.LogWarning("[ServerBootstrap]: No AI Controller prefab assigned, and none found in scene!");
                }
            }
#endif

            IsServerReady = true;
            
            // Log with details pulled directly from ServerConfig
            Debug.Log($"<color=#00FF99>[ServerBootstrap]: Launching '{ServerConfig.ServerName}' on port {ServerConfig.Port} (Max Players: {ServerConfig.MaxPlayers}). Core initialization sequence complete.</color>");
            
            // Notify any listening systems (like your LoginHandler or network handshake) that it's safe to fire
            OnServerInitialized?.Invoke();
        }

        private void EnsureServerEnvironment()
        {
            string persistentPath = Application.persistentDataPath;
            if (!Directory.Exists(persistentPath))
            {
                Directory.CreateDirectory(persistentPath);
                Debug.Log($"[ServerBootstrap]: Created persistent data directory at {persistentPath}");
            }

            string serverDataPath = Path.Combine(persistentPath, "ServerData.json");
            if (!File.Exists(serverDataPath))
            {
                ServerData defaultData = new ServerData();
                string json = JsonUtility.ToJson(defaultData, true);
                File.WriteAllText(serverDataPath, json);
                Debug.Log("[ServerBootstrap]: Initialized default ServerData.json file.");
            }
        }
    }
}