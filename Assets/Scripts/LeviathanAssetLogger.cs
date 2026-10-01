using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DivisionOfDivinity.AI
{
    [Serializable]
    public class ConversationLogEntry
    {
        public string sender;
        public string messageText;
        public string aiThoughts;          // Captured internal monologue / thinking process
        public string timestamp;
        public float durationSeconds;      // Exact round-trip execution time
        public string performanceNotes;    // Context on why it took that long (payload size, VRAM, etc.)
    }

    [Serializable]
    public class ConversationSessionWrapper
    {
        public string sessionIdentifier = "T2-LUIS - Active Dev Session";
        public List<ConversationLogEntry> history = new List<ConversationLogEntry>();
    }

    public static class LeviathanAssetLogger
    {
        // Points directly to your Unity Assets folder
        private static string DirectoryPath => Path.Combine(Application.dataPath, "T2-LUIS - Conversations");
        private static string FilePath => Path.Combine(DirectoryPath, "T2-LUIS_Active_Conversation.json");

        public static void LogInteraction(string sender, string message, string thoughts, float duration, string notes)
        {
            try
            {
                if (!Directory.Exists(DirectoryPath))
                {
                    Directory.CreateDirectory(DirectoryPath);
                }

                ConversationSessionWrapper wrapper = LoadSession();

                wrapper.history.Add(new ConversationLogEntry
                {
                    sender = sender,
                    messageText = message,
                    aiThoughts = string.IsNullOrEmpty(thoughts) ? "N/A (Direct Response)" : thoughts,
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    durationSeconds = duration,
                    performanceNotes = notes
                });

                string json = JsonUtility.ToJson(wrapper, true);
                File.WriteAllText(FilePath, json);

                // Instantly refreshes the Unity Editor asset database so the file shows up in the project window
                #if UNITY_EDITOR
                AssetDatabase.Refresh();
                #endif
            }
            catch (Exception e)
            {
                Debug.LogError($"[LeviathanAssetLogger] Failed to write asset log: {e.Message}");
            }
        }

        private static ConversationSessionWrapper LoadSession()
        {
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                return JsonUtility.FromJson<ConversationSessionWrapper>(json) ?? new ConversationSessionWrapper();
            }
            return new ConversationSessionWrapper();
        }
    }
}