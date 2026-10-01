using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DivisionOfDivinity.AI
{
    public class LeviathanTestUI : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private LeviathanAiController aiController;
        [SerializeField] private TMP_InputField playerInputField;
        [SerializeField] private Button sendButton;
        [SerializeField] private TextMeshProUGUI dialogueTextOutput;
        [SerializeField] private ScrollRect scrollRect;

        [Header("Switch Brain UI")]
        [SerializeField] private Button switchBrainButton;
        [SerializeField] private TextMeshProUGUI switchBrainText;
        [SerializeField] private Image switchBrainButtonImage;

        [Header("Brain & Player Colors")]
        [SerializeField] private Color playerTargetColor = new Color(0.3f, 0.8f, 1.0f); // Soft Blue
        [SerializeField] private Color geminiTargetColor = new Color(0.2f, 0.7f, 1.0f); // Bright Cyan
        [SerializeField] private Color ollamaTargetColor = new Color(1.0f, 0.5f, 0.2f); // Warm Orange

        private Coroutine timerCoroutine;
        private string chatHistoryBuffer = "";

        private void OnEnable()
        {
            sendButton.onClick.AddListener(OnSendClicked);
            playerInputField.onSubmit.AddListener(OnInputFieldSubmit);

            if (switchBrainButton != null)
                switchBrainButton.onClick.AddListener(OnSwitchBrainClicked);

            if (aiController != null)
            {
                aiController.OnDialogueReceived.AddListener(UpdateDialogueUI);
                aiController.OnSystemMessage.AddListener(AppendSystemMessage);
                aiController.OnProviderChanged.AddListener(UpdateSwitchButtonUI);
            }
            
            Application.logMessageReceived += HandleUnityLog;
        }

        private void OnDisable()
        {
            sendButton.onClick.RemoveListener(OnSendClicked);
            playerInputField.onSubmit.RemoveListener(OnInputFieldSubmit);

            if (switchBrainButton != null)
                switchBrainButton.onClick.RemoveListener(OnSwitchBrainClicked);

            if (aiController != null)
            {
                aiController.OnDialogueReceived.RemoveListener(UpdateDialogueUI);
                aiController.OnSystemMessage.RemoveListener(AppendSystemMessage);
                aiController.OnProviderChanged.RemoveListener(UpdateSwitchButtonUI);
            }

            Application.logMessageReceived -= HandleUnityLog;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (!playerInputField.isFocused && sendButton.interactable)
                {
                    playerInputField.ActivateInputField();
                }
            }
        }

        private void OnSwitchBrainClicked()
        {
            if (aiController != null)
                aiController.ToggleProvider();
        }

        private void UpdateSwitchButtonUI(AiProviderMode activeProvider)
        {
            AiProviderMode targetProvider = (activeProvider == AiProviderMode.Ollama) ? AiProviderMode.Gemini : AiProviderMode.Ollama;
            Color targetColor = (targetProvider == AiProviderMode.Gemini) ? geminiTargetColor : ollamaTargetColor;

            if (switchBrainText != null)
            {
                switchBrainText.text = $"Use {targetProvider}";
                switchBrainText.color = targetColor;
            }

            if (switchBrainButtonImage != null)
            {
                switchBrainButtonImage.color = new Color(targetColor.r, targetColor.g, targetColor.b, 0.25f);
            }
        }

        private void OnInputFieldSubmit(string text)
        {
            OnSendClicked();
        }

        private void OnSendClicked()
        {
            string message = playerInputField.text;
            if (string.IsNullOrWhiteSpace(message)) return;

            sendButton.interactable = false;
            playerInputField.interactable = false;

            string playerName = "Default/Null Player";
            string playerTierStr = "Tier 0";
            
            if (LoginHandler.Instance != null && LoginHandler.Instance.currentUser != null)
            {
                var user = LoginHandler.Instance.currentUser;
                playerName = !string.IsNullOrEmpty(user.displayName) ? user.displayName : user.loginName;
                playerTierStr = $"Tier {user.playerTier}";
            }

            string playerHexColor = ColorUtility.ToHtmlStringRGB(playerTargetColor);
            string mockWorldState = $"{{\"currentZone\": \"Central_Spire\", \"worldCorruption\": 0.42, \"player\": \"{playerName}\", \"status\": \"{playerTierStr}\"}}";

            dialogueTextOutput.text += $"\n<b><color=#{playerHexColor}>{playerName}:</color></b> {message}";
            chatHistoryBuffer = dialogueTextOutput.text;

            playerInputField.text = "";

            if (timerCoroutine != null) StopCoroutine(timerCoroutine);
            timerCoroutine = StartCoroutine(ThinkingTimerCoroutine());

            if (aiController != null)
                aiController.SendPlayerInteraction(message, mockWorldState);
                
            ScrollToBottom();
        }

        public void SendPresetQuery(string presetMessage)
        {
            playerInputField.text = presetMessage;
            OnSendClicked();
        }

        private IEnumerator ThinkingTimerCoroutine()
        {
            int elapsedTime = 0;
            while (true)
            {
                dialogueTextOutput.text = chatHistoryBuffer + $"\n<i>Leviathan is thinking... ({elapsedTime}s)</i>";
                ScrollToBottom();
                yield return new WaitForSeconds(1f);
                elapsedTime++;
            }
        }

        private void UpdateDialogueUI(string aiResponse)
        {
            StopThinkingTimer();

            string brainName = "Leviathan";
            Color brainColor = geminiTargetColor;
            
            if (aiController != null && aiController.CurrentProvider == AiProviderMode.Ollama)
            {
                brainName = "Leviathan (Ollama)";
                brainColor = ollamaTargetColor;
            }
            else
            {
                brainName = "Leviathan (Gemini)";
                brainColor = geminiTargetColor;
            }

            string brainHexColor = ColorUtility.ToHtmlStringRGB(brainColor);

            dialogueTextOutput.text = chatHistoryBuffer + $"\n<b><color=#{brainHexColor}>{brainName}:</color></b> {aiResponse}";
            chatHistoryBuffer = dialogueTextOutput.text;

            UnlockInput();
            ScrollToBottom();
        }

        private void AppendSystemMessage(string systemMessage)
        {
            chatHistoryBuffer += $"\n<color=#888888>{systemMessage}</color>";
            dialogueTextOutput.text = chatHistoryBuffer;

            if (systemMessage.Contains("[System Error]") || systemMessage.Contains("Max retries reached"))
            {
                StopThinkingTimer();
                UnlockInput();
            }

            ScrollToBottom();
        }

        private void HandleUnityLog(string logString, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception)
            {
                string errorMsg = $"<color=#FF3333>[Auto-Captured Error] {logString}</color>";
                AppendSystemManagerMessage_Safe(errorMsg);
            }
        }

        private void AppendSystemManagerMessage_Safe(string msg)
        {
            chatHistoryBuffer += $"\n{msg}";
            dialogueTextOutput.text = chatHistoryBuffer;
            ScrollToBottom();
        }

        private void StopThinkingTimer()
        {
            if (timerCoroutine != null)
            {
                StopCoroutine(timerCoroutine);
                timerCoroutine = null;
            }
        }

        private void UnlockInput()
        {
            sendButton.interactable = true;
            playerInputField.interactable = true;
            playerInputField.ActivateInputField();
        }

        private void ScrollToBottom()
        {
            if (scrollRect == null) return;
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}