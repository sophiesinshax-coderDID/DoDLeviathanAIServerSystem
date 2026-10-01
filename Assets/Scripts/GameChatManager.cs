using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // Ensure you have TextMeshPro installed for UI text
using WebSocketSharp; // Or your chosen WebSocket namespace

public class GameChatManager : MonoBehaviour
{
    [Header("Network Settings")]
    public string serverUrl = "ws://localhost:8080";
    public string username = "PlayerOne";

    [Header("UI References")]
    public TMP_Text chatLogText;      // Assign your UI Text/TextMeshPro component
    public CanvasGroup chatCanvasGroup; // Assign the CanvasGroup on your chat panel to control fading/hiding
    
    private WebSocket ws;
    private Queue<string> incomingMessages = new Queue<string>();
    private float lastMessageTime;
    private bool isChatVisible = true;

    void Start()
    {
        InitializeChatUI();
        ConnectToServer();
    }

    void InitializeChatUI()
    {
        if (chatCanvasGroup == null)
        {
            chatCanvasGroup = GetComponent<CanvasGroup>();
            if (chatCanvasGroup == null)
                chatCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        chatLogText.text = "--- CONNECTING TO SECTOR CHAT --- \n";
        lastMessageTime = Time.time;
    }

    void ConnectToServer()
    {
        ws = new WebSocket(serverUrl);

        ws.OnOpen += (sender, e) => {
            Debug.Log("<color=green>Chat:</color> Connected to WebSocket server.");
            // Send Auth packet immediately upon connection
            string authJson = JsonUtility.ToJson(new { type = "AUTH", username = this.username });
            ws.Send(authJson);
        };

        ws.OnMessage += (sender, e) => {
            // Queue the raw string payload to handle safely on Unity's main thread
            lock (incomingMessages)
            {
                incomingMessages.Enqueue(e.Data);
            }
        };

        ws.OnError += (sender, e) => {
            Debug.LogError($"<color=red>Chat Error:</color> {e.Message}");
        };

        ws.Connect();
    }

    void Update()
    {
        // Process any queued incoming messages on the main Unity thread
        lock (incomingMessages)
        {
            while (incomingMessages.Count > 0)
            {
                string rawJson = incomingMessages.Dequeue();
                ProcessIncomingMessage(rawJson);
            }
        }

        // Auto-hide logic: check if 10 seconds have passed since the last message
        if (isChatVisible && Time.time - lastMessageTime > 10f)
        {
            FadeChat(false);
        }
    }

    void ProcessIncomingMessage(string json)
    {
        try
        {
            ChatPacket packet = JsonUtility.FromJson<ChatPacket>(json);
            string formattedMessage = "";

            if (packet.type == "SYSTEM")
            {
                formattedMessage = $"<color=#AAAAAA>[SYSTEM]: {packet.content}</color>\n";
            }
            else
            {
                formattedMessage = $"<b>[{packet.channel}] {packet.sender}:</b> {packet.content}\n";
            }

            // Append to log
            chatLogText.text += formattedMessage;

            // Activity detected: Show chat and reset timer
            lastMessageTime = Time.time;
            if (!isChatVisible)
            {
                FadeChat(true);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to parse chat packet: {ex.Message}");
        }
    }

    void FadeChat(bool show)
    {
        isChatVisible = show;
        StopAllCoroutines();
        StartCoroutine(DoFade(show ? 0f : 1f, show ? 1f : 0f));
    }

    IEnumerator DoFade(float startAlpha, float endAlpha)
    {
        float duration = 0.5f;
        float elapsed = 0f;
        
        // If showing, make it interactive immediately
        if (endAlpha > 0) chatCanvasGroup.alpha = startAlpha;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            chatCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            yield return null;
        }

        chatCanvasGroup.alpha = endAlpha;
    }

    // Call this method from an input field when the player presses Enter to chat
    public void SendChatMessage(string messageContent, string channel = "Global")
    {
        if (ws != null && ws.IsAlive && !string.IsNullOrEmpty(messageContent))
        {
            var msgPayload = new {
                type = "MESSAGE",
                channel = channel,
                content = messageContent
            };
            ws.Send(JsonUtility.ToJson(msgPayload));
            
            // Reset hide timer on outgoing message activity
            lastMessageTime = Time.time;
            if (!isChatVisible) FadeChat(true);
        }
    }

    void OnDestroy()
    {
        if (ws != null && ws.IsAlive)
        {
            ws.Close();
        }
    }
}