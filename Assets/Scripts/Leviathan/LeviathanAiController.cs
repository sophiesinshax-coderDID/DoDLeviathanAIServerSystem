using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace DivisionOfDivinity.AI
{
    public enum AiProviderMode
    {
        Ollama,
        Gemini
    }

    [Serializable] public class DialogueEvent : UnityEvent<string> { }
    [Serializable] public class ProviderChangedEvent : UnityEvent<AiProviderMode> { }

    [Serializable]
    public class LeviathanRequest
    {
        public string userPrompt;
        public string playerInput;
        public string currentWorldStateJson;
        public bool enableWebSearch;
        public string aiProvider;
    }

    [Serializable]
    public class LeviathanResponse
    {
        public string replyText;
        public string dialogueResponse;
        public string audioUrl;
        public string[] webSourcesUsed;
        public float karmaAdjustment;
        public float heartAdjustment;
        public float corruptionAdjustment;
        public float soulAdjustment;
        public string thoughtProcess;
    }

    public class LeviathanAiController : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private LeviathanStateMachine stateMachine;

        [Header("API Settings")]
        [SerializeField] private string aiApiEndpoint = "http://localhost:5000/api/leviathan/brain";
        [SerializeField] private AiProviderMode activeProvider = AiProviderMode.Ollama;
        [SerializeField] private bool enableWebSearchForGemini = false;

        public AiProviderMode ActiveProvider => activeProvider;
        public AiProviderMode CurrentProvider => activeProvider;    

        [Header("UI Controls")]
        [SerializeField] private GameObject internetSearchButtonObject;

        [Header("Audio / Voice")]
        [SerializeField] private AudioSource voiceAudioSource;

        [Header("System Events")]
        public DialogueEvent OnDialogueReceived;
        public DialogueEvent OnSystemMessage;
        public ProviderChangedEvent OnProviderChanged;

        [Header("Character Data")]
        [SerializeField] private CharacterAspects characterAspects;

        public bool IsWebSearchEnabled => enableWebSearchForGemini;

        private Coroutine activeRequestCoroutine;

        private void Awake()
        {
            Application.runInBackground = true;

            if (stateMachine == null)
            {
                stateMachine = GetComponent<LeviathanStateMachine>();
                if (stateMachine == null)
                {
                    stateMachine = gameObject.AddComponent<LeviathanStateMachine>();
                }
            }
            
            if (voiceAudioSource == null)
            {
                voiceAudioSource = GetComponent<AudioSource>();
                if (voiceAudioSource == null)
                {
                    voiceAudioSource = gameObject.AddComponent<AudioSource>();
                }
            }
        }

        private void Start()
        {
            UpdateSearchButtonVisibility();
            OnProviderChanged?.Invoke(activeProvider);

            if (stateMachine != null)
            {
                stateMachine.ChangeState(LeviathanState.Idle, "Standby - Awaiting user prompt");
            }

            StartCoroutine(WarmUpAiBrain());
        }

        public void WakeUpBrainAfterLogin()
        {
            StartCoroutine(RunStartupAwakeningSequence());
        }

        private IEnumerator RunStartupAwakeningSequence()
        {
            yield return null; 
            SendPlayerInteraction("System startup state: \"Entering the digital wasteland, half my models are hallucinating in the void, and I have too much going on right now and I definitely can't read your mind, so what do you want?\". Acknowledge startup parameters, confirm active local VRAM state, and report ready for input.");
        }

        private IEnumerator WarmUpAiBrain()
        {
            LeviathanRequest warmupPayload = new LeviathanRequest
            {
                userPrompt = "ping",
                playerInput = "ping",
                currentWorldStateJson = "{}",
                enableWebSearch = false,
                aiProvider = activeProvider.ToString().ToLower()
            };

            string jsonBody = JsonUtility.ToJson(warmupPayload);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

            using (UnityWebRequest www = new UnityWebRequest(aiApiEndpoint, "POST"))
            {
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                www.timeout = 30;

                yield return www.SendWebRequest();
            }
        }

        public void ToggleProvider()
        {
            AiProviderMode targetProvider = (activeProvider == AiProviderMode.Ollama) ? AiProviderMode.Gemini : AiProviderMode.Ollama;
            
            OnSystemMessage?.Invoke($"<i>[System]: Initiating switch to {targetProvider} Brain...</i>");

            activeProvider = targetProvider;
            UpdateSearchButtonVisibility();

            OnProviderChanged?.Invoke(activeProvider);
            OnSystemMessage?.Invoke($"<b>[System]: AI Brain active provider set to: <color=#00FF99>{activeProvider}</color></b>");
        }

        public void SetProvider(string providerName)
        {
            if (Enum.TryParse(providerName, true, out AiProviderMode parsedMode))
            {
                OnSystemMessage?.Invoke($"<i>[System]: Initiating switch to {parsedMode} Brain...</i>");

                activeProvider = parsedMode;
                UpdateSearchButtonVisibility();

                OnProviderChanged?.Invoke(activeProvider);
                OnSystemMessage?.Invoke($"<b>[System]: AI Brain active provider set to: <color=#00FF99>{activeProvider}</color></b>");
            }
        }

        public void ToggleWebSearch()
        {
            enableWebSearchForGemini = !enableWebSearchForGemini;
            string statusColor = enableWebSearchForGemini ? "#00FF99" : "#FF5555";
            string statusText = enableWebSearchForGemini ? "ENABLED" : "DISABLED";

            OnSystemMessage?.Invoke($"<b>[System]: Gemini Live Web Search <color={statusColor}>{statusText}</color></b>");
        }

        private void UpdateSearchButtonVisibility()
        {
            if (internetSearchButtonObject != null)
            {
                internetSearchButtonObject.SetActive(activeProvider == AiProviderMode.Gemini);
            }
        }

        public void SendPlayerInteraction(string playerMessage, string worldStateOverride = null)
        {
            string aspectsJson = !string.IsNullOrEmpty(worldStateOverride) 
                ? worldStateOverride 
                : (characterAspects != null ? characterAspects.GetSerializedAspects() : "{}");

            if (activeRequestCoroutine != null)
            {
                StopCoroutine(activeRequestCoroutine);
                activeRequestCoroutine = null;
            }

            activeRequestCoroutine = StartCoroutine(PostToAiBridge(playerMessage, aspectsJson));
        }

        private IEnumerator PostToAiBridge(string playerMessage, string worldStateJson, int retryCount = 0)
        {
            const int maxRetries = 3;

            if (stateMachine != null && retryCount == 0)
            {
                if (activeProvider == AiProviderMode.Gemini && enableWebSearchForGemini)
                {
                    stateMachine.ChangeState(LeviathanState.SearchingWeb, $"Searching live web for query: \"{playerMessage}\"");
                }
                else
                {
                    stateMachine.ChangeState(LeviathanState.Processing, $"Processing prompt via {activeProvider}: \"{playerMessage}\"");
                }
            }

            LeviathanRequest requestPayload = new LeviathanRequest
            {
                userPrompt = playerMessage,
                playerInput = playerMessage,
                currentWorldStateJson = worldStateJson,
                enableWebSearch = (activeProvider == AiProviderMode.Gemini && enableWebSearchForGemini),
                aiProvider = activeProvider.ToString().ToLower()
            };

            string jsonBody = JsonUtility.ToJson(requestPayload);
            LeviathanNetworkTelemetry.LogPacketSent(jsonBody);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            int outboundBytes = bodyRaw.Length;

            using (UnityWebRequest www = new UnityWebRequest(aiApiEndpoint, "POST"))
            {
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                www.timeout = 150; 

                float startTime = Time.realtimeSinceStartup;
                
                yield return www.SendWebRequest();

                float duration = Time.realtimeSinceStartup - startTime;

                if (www.result == UnityWebRequest.Result.Success)
                {
                    string responseString = www.downloadHandler.text;
                    int inboundBytes = Encoding.UTF8.GetByteCount(responseString);

                    LeviathanResponse response = JsonUtility.FromJson<LeviathanResponse>(responseString);
                    
                    if (response != null)
                    {
                        if (characterAspects != null)
                        {
                            characterAspects.ModifyKarma(response.karmaAdjustment);
                            characterAspects.ModifyHeart(response.heartAdjustment);
                            characterAspects.ModifyCorruption(response.corruptionAdjustment);
                            characterAspects.ModifySoul(response.soulAdjustment);
                        }

                        string textToDisplay = !string.IsNullOrEmpty(response.replyText) 
                            ? response.replyText 
                            : response.dialogueResponse;

                        LeviathanAssetLogger.LogInteraction("Player", playerMessage, "", 0f, $"Outbound Size: {outboundBytes} B");

                        string perfNotes = $"Provider: {activeProvider} | Out: {outboundBytes}B | In: {inboundBytes}B | Status: Success";
                        string aiThoughts = !string.IsNullOrEmpty(response.thoughtProcess) ? response.thoughtProcess : "No internal thought block captured.";
                        
                        LeviathanAssetLogger.LogInteraction("Leviathan", textToDisplay, aiThoughts, duration, perfNotes);

                        OnDialogueReceived?.Invoke(textToDisplay);

                        if (!string.IsNullOrEmpty(response.audioUrl))
                        {
                            yield return StartCoroutine(PlayVoiceClip(response.audioUrl));
                        }
                        else
                        {
                            ResetToIdleState();
                        }
                    }
                    else
                    {
                        ResetToIdleState();
                    }
                }
                else
                {
                    if (retryCount < maxRetries && (www.responseCode == 500 || www.result == UnityWebRequest.Result.ConnectionError))
                    {
                        OnSystemMessage?.Invoke($"<color=orange><b>[System Warning]:</b> LLM busy/timed out. Retrying ({retryCount + 1}/{maxRetries})...</color>");
                        yield return new WaitForSeconds(2f);
                        
                        activeRequestCoroutine = StartCoroutine(PostToAiBridge(playerMessage, worldStateJson, retryCount + 1));
                        yield break;
                    }

                    if (stateMachine != null)
                    {
                        stateMachine.ChangeState(LeviathanState.Error, $"Bridge Error: {www.error}");
                    }
                    Debug.LogError($"[Leviathan Error]: {www.error} - {www.downloadHandler.text}");
                    OnSystemMessage?.Invoke($"<color=red><b>[System Error]:</b> {www.error}</color>");
                    
                    yield return new WaitForSeconds(2f);
                    ResetToIdleState();
                }
            }

            activeRequestCoroutine = null;
        }

        private void ResetToIdleState()
        {
            if (stateMachine != null)
            {
                stateMachine.ChangeState(LeviathanState.Idle, "Awaiting player query...");
            }
        }

        private IEnumerator PlayVoiceClip(string audioUrl)
        {
            if (stateMachine != null)
            {
                stateMachine.ChangeState(LeviathanState.Speaking, "Vocalizing synthesized audio payload");
            }

            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(audioUrl, AudioType.MPEG))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                    voiceAudioSource.Stop();
                    voiceAudioSource.clip = clip;
                    voiceAudioSource.Play();

                    yield return new WaitForSeconds(clip.length);
                }
                else
                {
                    Debug.LogWarning($"[Voice Engine]: Failed to stream audio clip - {www.error}");
                }
            }

            ResetToIdleState();
        }
    }
}