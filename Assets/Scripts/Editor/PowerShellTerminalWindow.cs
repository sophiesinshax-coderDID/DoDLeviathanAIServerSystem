using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using UnityEditor;
using UnityEngine;
using LiteDB; // Required for LiteDB queries

namespace DivisionOfDivinity.AI.Editor
{
    public class PowerShellTerminalWindow : EditorWindow
    {
        private string commandInput = "ollama list";
        private string terminalOutput = "Terminal initialized. Ready for instructions.\n";
        private Vector2 scrollPos;
        private Vector2 dbScrollPos; // Scroll position for database viewer

        private bool isPythonServerRunning = false;
        private bool isChatServerRunning = false;
        private bool isOllamaRunning = false;
        private float lastCheckTime;
        private float checkInterval = 3.0f;

        // Database Viewer state
        private string dbQueryOutput = "Click 'Query LiteDB Users' to inspect records.";

        [MenuItem("Division of Divinity/PowerShell Terminal")]
        public static void ShowWindow()
        {
            GetWindow<PowerShellTerminalWindow>("PS Terminal");
        }

        private void OnEnable()
        {
            CheckSystemStatuses();
            UnityEngine.Debug.Log("[PS Terminal] Window enabled and status check initialized.");
        }

        private void Update()
        {
            if (Time.realtimeSinceStartup - lastCheckTime > checkInterval)
            {
                CheckSystemStatuses();
                lastCheckTime = Time.realtimeSinceStartup;
                Repaint();
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("Unity PowerShell Advanced Console & DB Inspector", EditorStyles.boldLabel);
            
            // Status Indicator Bar
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            DrawStatusIndicator("Python (5000)", isPythonServerRunning);
            GUILayout.Space(10);
            DrawStatusIndicator("Chat Server (8080)", isChatServerRunning); 
            GUILayout.Space(10);
            DrawStatusIndicator("Ollama (11434)", isOllamaRunning);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh Status", GUILayout.Width(100)))
            {
                CheckSystemStatuses();
                UnityEngine.Debug.Log("[PS Terminal] Status manually refreshed.");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Action Buttons Bar
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Ollama List"))
            {
                RunPowerShellCommand("ollama list");
            }
            if (GUILayout.Button("Check Ports"))
            {
                RunPowerShellCommand("netstat -ano | findstr \"5000 8080 11434\"");
            }
            if (GUILayout.Button("Start AI Bridge"))
            {
                // Fixed: Use Start-Process so it spawns detached without locking Unity's output streams
                RunPowerShellCommand("Start-Process python -ArgumentList '-m uvicorn main:app --host 127.0.0.1 --port 5000' -WindowStyle Hidden");
                CheckSystemStatuses();
            }
            if (GUILayout.Button("Start Chat Server"))
			{
				string pythonScriptPath = @"C:\Users\sophi\leviathan_ai_bridge"; // Point to your python folder
				RunPowerShellCommand($"Start-Process python -ArgumentList '-m uvicorn main:app --host 127.0.0.1 --port 8080 --reload' -WorkingDirectory \"{pythonScriptPath}\" -WindowStyle Hidden");
				CheckSystemStatuses();
			}
            if (GUILayout.Button("Kill Ollama"))
            {
                RunPowerShellCommand("taskkill /f /im ollama.exe");
                CheckSystemStatuses();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Database Navigation / Query Section
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("LiteDB User Records Inspector", EditorStyles.boldLabel);
            if (GUILayout.Button("Query LiteDB Users", GUILayout.Width(140)))
            {
                LoadLiteDBUserRecords();
            }
            EditorGUILayout.EndHorizontal();

            dbScrollPos = EditorGUILayout.BeginScrollView(dbScrollPos, GUILayout.Height(130));
            EditorGUILayout.SelectableLabel(dbQueryOutput, EditorStyles.textArea, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // Standard Terminal Output Log
            EditorGUILayout.LabelField("Output Log:", EditorStyles.label);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(150));
            EditorGUILayout.TextArea(terminalOutput, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            commandInput = EditorGUILayout.TextField("Command", commandInput);
            if (GUILayout.Button("Execute", GUILayout.Width(80)))
            {
                RunPowerShellCommand(commandInput);
                CheckSystemStatuses();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Clear Output"))
            {
                terminalOutput = string.Empty;
                dbQueryOutput = "Output cleared.";
            }
        }

        private void LoadLiteDBUserRecords()
        {
            try
            {
                string dbPath = Path.Combine(Application.persistentDataPath, "CorrosiveWorld.db");
                
                if (!File.Exists(dbPath))
                {
                    dbPath = Path.Combine(Directory.GetCurrentDirectory(), "CorrosiveWorld.db");
                }

                if (!File.Exists(dbPath))
                {
                    dbQueryOutput = $"[Warning]: Could not find 'CorrosiveWorld.db' in persistent data path or project root.\nSearched Path: {dbPath}";
                    return;
                }

                using (var db = new LiteDatabase($"Filename={dbPath};Connection=shared"))
                {
                    var col = db.GetCollection<UserProfile>("users");
                    var results = col.FindAll();

                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    int count = 0;

                    sb.AppendLine($"--- Connected to DB: {Path.GetFileName(dbPath)} ---");
                    foreach (var user in results)
                    {
                        count++;
                        sb.AppendLine($"[{count}] Login: {user.loginName} | Display: {user.displayName} | ID: {user.playerID} | Auth Level: {user.authLevel} | Tier: {user.playerTier}");
                    }

                    if (count == 0)
                    {
                        sb.AppendLine("Database is connected, but no user records were found in the 'users' collection.");
                    }
                    else
                    {
                        sb.AppendLine($"\nTotal records found: {count}");
                    }

                    dbQueryOutput = sb.ToString();
                    UnityEngine.Debug.Log($"[PS Terminal] Successfully queried LiteDB. Found {count} records.");
                }
            }
            catch (System.Exception ex)
            {
                dbQueryOutput = $"[DB Query Error]: {ex.Message}";
                UnityEngine.Debug.LogError($"[PS Terminal DB Error] {ex.Message}");
            }
        }

        private void DrawStatusIndicator(string label, bool isRunning)
        {
            GUIContent content = new GUIContent($" {label}");
            GUIStyle style = new GUIStyle(EditorStyles.label);
            
            Color originalColor = GUI.color;
            GUI.color = isRunning ? Color.green : Color.red;
            GUILayout.Label("●", style, GUILayout.Width(12));
            GUI.color = originalColor;
            
            GUILayout.Label(content);
        }

        private void CheckSystemStatuses()
        {
            isPythonServerRunning = IsPortInUse(5000);
            isChatServerRunning = IsPortInUse(8080);
            isOllamaRunning = IsPortInUse(11434);
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

        private void RunPowerShellCommand(string command)
        {
            string logStamp = $"\n> [{System.DateTime.Now:HH:mm:ss}] {command}\n";
            terminalOutput += logStamp;
            UnityEngine.Debug.Log($"[PS Terminal Executing] {command}");

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -NonInteractive -Command \"{command}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = new Process())
                {
                    process.StartInfo = startInfo;
                    
                    string standardOutput = "";
                    string standardError = "";

                    process.OutputDataReceived += (sender, args) => {
                        if (!string.IsNullOrEmpty(args.Data))
                            standardOutput += args.Data + "\n";
                    };

                    process.ErrorDataReceived += (sender, args) => {
                        if (!string.IsNullOrEmpty(args.Data))
                            standardError += args.Data + "\n";
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    process.WaitForExit(5000);

                    if (!string.IsNullOrEmpty(standardOutput))
                    {
                        terminalOutput += standardOutput;
                        UnityEngine.Debug.Log($"[PS Output]: {standardOutput.Trim()}");
                    }

                    if (!string.IsNullOrEmpty(standardError))
                    {
                        string errFormatted = $"[ERROR]: {standardError.Trim()}\n";
                        terminalOutput += errFormatted;
                        UnityEngine.Debug.LogError($"[PS Error]: {standardError.Trim()}");
                    }
                    
                    if (string.IsNullOrEmpty(standardOutput) && string.IsNullOrEmpty(standardError))
                    {
                        terminalOutput += "[Process executed or dispatched to background thread successfully]\n";
                    }
                }
            }
            catch (System.Exception ex)
            {
                string exceptionMsg = $"[Exception]: {ex.Message}\n";
                terminalOutput += exceptionMsg;
                UnityEngine.Debug.LogError($"[PS Terminal Exception] {ex.Message}");
            }

            Repaint();
        }
    }
}