using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DivisionOfDivinity.UI
{
    public class UnifiedTerminalManager : MonoBehaviour
    {
        [Header("Font & Styling")]
        [SerializeField] private TMP_FontAsset terminalFont;
        
        [Header("Configuration")]
        [SerializeField] private int maxLogCharacters = 10000;
        [SerializeField] private string bridgeDirectory = @"C:\Users\sophi\leviathan_ai_bridge";
        [SerializeField] private string batchFileName = "start_server.bat";
        
        [Header("Chat Server Configuration")]
        [SerializeField] private string chatServerBatchName = "start_chatserver.bat";

        [SerializeField] private string ollamaExecutablePath = @"%LOCALAPPDATA%\Programs\Ollama\ollama.exe";

        private Process backendProcess;
        private Process chatServerProcess;
        private Process watchdogProcess;

        private readonly ConcurrentQueue<string> logQueue = new ConcurrentQueue<string>();

        private TMP_InputField terminalInputField;
        private TextMeshProUGUI terminalOutputText;
        private TextMeshProUGUI networkOutputText;
        private ScrollRect terminalScrollRect;
        private ScrollRect networkScrollRect;

        void Awake()
        {
            BuildTopLeftInterface();
        }

        private void Start()
        {
            StartWatchdogService();
            StartBackendService();
            StartChatServerService();
        }

        private void Update()
        {
            ProcessLogQueue();
        }

        private void BuildTopLeftInterface()
        {
            // 1. Ensure we have a Canvas in the scene
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("ProceduralHUD_Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // 2. Create a parent container anchored to the TOP-LEFT
            GameObject container = new GameObject("TopLeft_TerminalContainer", typeof(RectTransform));
            container.transform.SetParent(canvas.transform, false);
            
            RectTransform containerRect = container.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 1); // Top-Left
            containerRect.anchorMax = new Vector2(0, 1);
            containerRect.pivot = new Vector2(0, 1);
            containerRect.anchoredPosition = new Vector2(20f, -20f); 
            containerRect.sizeDelta = new Vector2(820f, 400f);

            HorizontalLayoutGroup hGroup = container.AddComponent<HorizontalLayoutGroup>();
            hGroup.spacing = 15f;
            hGroup.childControlWidth = false;
            hGroup.childControlHeight = false;
            hGroup.childForceExpandWidth = false;
            hGroup.childForceExpandHeight = false;

            // 3. Build Window 1: Interactive Terminal (Left)
            GameObject termWindow = CreateWindowPanel(container.transform, "Terminal_Window", new Vector2(400f, 400f), new Color(0.05f, 0.05f, 0.08f, 0.85f));
            terminalOutputText = CreateScrollableText(termWindow.transform, "Terminal_Output", out terminalScrollRect, new Color(0f, 1f, 0.6f));
            terminalInputField = CreateInputField(termWindow.transform);

            // Hook up command submission event listener
            if (terminalInputField != null)
            {
                terminalInputField.onSubmit.AddListener(OnSubmitCommand);
            }

            // 4. Build Window 2: Network / System Data Log (Right)
            GameObject netWindow = CreateWindowPanel(container.transform, "NetworkData_Window", new Vector2(400f, 400f), new Color(0.08f, 0.05f, 0.05f, 0.85f));
            networkOutputText = CreateScrollableText(netWindow.transform, "Network_Output", out networkScrollRect, new Color(0f, 0.9f, 1f));

            UnityEngine.Debug.Log("[UnifiedTerminalManager]: Top-Left dual terminal interface generated and hooked up successfully.");
        }

        private GameObject CreateWindowPanel(Transform parent, string name, Vector2 size, Color bgColor)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.sizeDelta = size;

            Image bg = panel.GetComponent<Image>();
            bg.color = bgColor;

            return panel;
        }

        private TextMeshProUGUI CreateScrollableText(Transform parent, string name, out ScrollRect outScroll, Color textColor)
        {
            GameObject scrollViewObj = new GameObject(name + "_Scroll", typeof(RectTransform), typeof(ScrollRect));
            scrollViewObj.transform.SetParent(parent, false);
            RectTransform scrollRectTransform = scrollViewObj.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0, 0);
            scrollRectTransform.anchorMax = new Vector2(1, 1);
            scrollRectTransform.sizeDelta = new Vector2(-10f, -40f); 
            scrollRectTransform.anchoredPosition = new Vector2(0, -10f);

            ScrollRect scrollRect = scrollViewObj.GetComponent<ScrollRect>();
            outScroll = scrollRect;

            GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewportObj.transform.SetParent(scrollViewObj.transform, false);
            RectTransform vpRect = viewportObj.GetComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.sizeDelta = Vector2.zero;

            GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            contentObj.transform.SetParent(viewportObj.transform, false);
            RectTransform contentRect = contentObj.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0, 1);
            contentRect.sizeDelta = new Vector2(0, 400);

            VerticalLayoutGroup vGroup = contentObj.GetComponent<VerticalLayoutGroup>();
            vGroup.childControlHeight = true;
            vGroup.childForceExpandHeight = false;

            GameObject textObj = new GameObject("LogText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(contentObj.transform, false);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 1);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.pivot = new Vector2(0, 1);
            textRect.sizeDelta = new Vector2(0, 400);

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            if (terminalFont != null) tmp.font = terminalFont;
            tmp.fontSize = 12;
            tmp.color = textColor;
            tmp.text = $"[{name} Initialized...]\n";

            scrollRect.content = contentRect;
            scrollRect.viewport = vpRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            return tmp;
        }

        private TMP_InputField CreateInputField(Transform parent)
        {
            GameObject inputObj = new GameObject("Terminal_InputField", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            inputObj.transform.SetParent(parent, false);

            RectTransform rect = inputObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0, 5f);
            rect.sizeDelta = new Vector2(-10f, 30f);

            Image bg = inputObj.GetComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.2f, 1f);

            GameObject textArea = new GameObject("Text Area", typeof(RectTransform));
            textArea.transform.SetParent(inputObj.transform, false);
            RectTransform taRect = textArea.GetComponent<RectTransform>();
            taRect.anchorMin = Vector2.zero;
            taRect.anchorMax = Vector2.one;
            taRect.sizeDelta = new Vector2(-10f, -5f);
            taRect.anchoredPosition = Vector2.zero;

            GameObject placeholderObj = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            placeholderObj.transform.SetParent(textArea.transform, false);
            TextMeshProUGUI placeholder = placeholderObj.GetComponent<TextMeshProUGUI>();
            placeholder.text = "Enter system command...";
            placeholder.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            placeholder.fontSize = 12;

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(textArea.transform, false);
            TextMeshProUGUI text = textObj.GetComponent<TextMeshProUGUI>();
            text.text = "";
            text.color = Color.white;
            text.fontSize = 12;

            TMP_InputField inputField = inputObj.GetComponent<TMP_InputField>();
            inputField.textViewport = taRect;
            inputField.textComponent = text;
            inputField.placeholder = placeholder;

            return inputField;
        }

        private void ProcessLogQueue()
        {
            if (networkOutputText == null) return;

            bool hasNewLogs = false;
            while (logQueue.TryDequeue(out string logLine))
            {
                networkOutputText.text += logLine + "\n";
                hasNewLogs = true;
            }

            if (hasNewLogs)
            {
                if (networkOutputText.text.Length > maxLogCharacters)
                {
                    networkOutputText.text = networkOutputText.text.Substring(networkOutputText.text.Length - maxLogCharacters);
                }

                if (networkScrollRect != null)
                {
                    Canvas.ForceUpdateCanvases();
                    networkScrollRect.verticalNormalizedPosition = 0f;
                }
            }
        }

        private void StartWatchdogService()
        {
            try
            {
                string expandedPath = Environment.ExpandEnvironmentVariables(ollamaExecutablePath);
                if (!File.Exists(expandedPath))
                {
                    logQueue.Enqueue($"<color=#FF4500>[OLLAMA ERR]</color> Executable not found at path: {expandedPath}");
                    return;
                }

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = expandedPath,
                    Arguments = "serve",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                startInfo.EnvironmentVariables["OLLAMA_LLM_LIBRARY"] = "cpu";
                startInfo.EnvironmentVariables["HIP_VISIBLE_DEVICES"] = "-1";
                startInfo.EnvironmentVariables["ROCR_VISIBLE_DEVICES"] = "-1";

                watchdogProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

                watchdogProcess.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) logQueue.Enqueue($"<color=#00FF7F>[OLLAMA]</color> {e.Data}"); };
                watchdogProcess.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) logQueue.Enqueue($"<color=#FF4500>[OLLAMA ERR]</color> {e.Data}"); };

                watchdogProcess.Start();
                watchdogProcess.BeginOutputReadLine();
                watchdogProcess.BeginErrorReadLine();

                logQueue.Enqueue("<color=#00FF7F>[SYSTEM]</color> Ollama background service process initialized...");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Failed to start Ollama process: {ex.Message}");
            }
        }

        private void StartBackendService()
        {
            try
            {
                string batchPath = Path.Combine(bridgeDirectory, batchFileName);
                if (!File.Exists(batchPath))
                {
                    logQueue.Enqueue($"<color=#FF4500>[PYTHON ERR]</color> Batch file not found: {batchPath}");
                    return;
                }

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{batchFileName}\"",
                    WorkingDirectory = bridgeDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                backendProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

                backendProcess.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) logQueue.Enqueue($"<color=#00E5FF>[PYTHON]</color> {e.Data}"); };
                backendProcess.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) logQueue.Enqueue($"<color=#FF4500>[PYTHON ERR]</color> {e.Data}"); };

                backendProcess.Start();
                backendProcess.BeginOutputReadLine();
                backendProcess.BeginErrorReadLine();

                logQueue.Enqueue("<color=#00E5FF>[SYSTEM]</color> Main FastAPI bridge starting...");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Failed to start backend process: {ex.Message}");
            }
        }

        private void StartChatServerService()
        {
            try
            {
                string chatBatchPath = Path.Combine(bridgeDirectory, chatServerBatchName);
                if (!File.Exists(chatBatchPath))
                {
                    logQueue.Enqueue($"<color=#FF4500>[CHATNET ERR]</color> Chat server batch file not found: {chatBatchPath}");
                    return;
                }

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{chatServerBatchName}\"",
                    WorkingDirectory = bridgeDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                chatServerProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

                chatServerProcess.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) logQueue.Enqueue($"<color=#FFCC00>[CHATSERVER]</color> {e.Data}"); };
                chatServerProcess.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) logQueue.Enqueue($"<color=#FF4500>[CHATSERVER ERR]</color> {e.Data}"); };

                chatServerProcess.Start();
                chatServerProcess.BeginOutputReadLine();
                chatServerProcess.BeginErrorReadLine();

                logQueue.Enqueue("<color=#FFCC00>[SYSTEM]</color> Chat server process starting...");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Failed to start chat server process: {ex.Message}");
            }
        }

        public void OnSubmitCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command)) return;

            if (terminalOutputText != null)
            {
                terminalOutputText.text += $"\n> {command}\n";
            }
            
            ProcessCommandBackend(command.Trim().ToLower());

            if (terminalInputField != null)
            {
                terminalInputField.text = "";
                terminalInputField.ActivateInputField();
                terminalInputField.Select(); 
            }
            
            if (terminalScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                terminalScrollRect.verticalNormalizedPosition = 0f;
            }
        }

        private void ProcessCommandBackend(string cmd)
        {
            if (cmd == "clear")
            {
                if (terminalOutputText != null) terminalOutputText.text = "";
                return;
            }

            if (cmd == "help")
            {
                PrintHelpMenu();
                return;
            }

            if (terminalOutputText != null)
            {
                terminalOutputText.text += $"Executing system command: {cmd}\n(Type 'help' for a list of available commands)\n";
            }
        }

        private void PrintHelpMenu()
        {
            if (terminalOutputText == null) return;

            terminalOutputText.text += 
                "--- AVAILABLE TERMINAL COMMANDS ---\n" +
                "  help          - Displays this list of available commands.\n" +
                "  clear         - Clears the terminal screen output.\n" +
                "  status        - Check server or connection status (Placeholder).\n" +
                "------------------------------------\n";
        }

        private void OnApplicationQuit()
        {
            KillProcess(backendProcess);
            KillProcess(chatServerProcess);
            KillProcess(watchdogProcess);
        }

        private void KillProcess(Process process)
        {
            if (process != null && !process.HasExited)
            {
                try
                {
                    process.Kill();
                    process.Dispose();
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"Process cleanup warning: {ex.Message}");
                }
            }
        }
    }
}