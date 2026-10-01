using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace DivisionOfDivinity.AI.Editor
{
    public class ProjectTelemetryTool : EditorWindow
    {
        private static List<string> recentLogs = new List<string>();
        private int maxLogHistory = 15;

        [InitializeOnLoadMethod]
        private static void InitializeLogCapture()
        {
            Application.logMessageReceived += HandleLogCapture;
        }

        private static void HandleLogCapture(string logString, string stackTrace, LogType type)
        {
            string logEntry = $"[{type}] {System.DateTime.Now:HH:mm:ss} - {logString}";
            if (type == LogType.Error || type == LogType.Exception)
            {
                logEntry += $"\nStack Trace:\n{stackTrace}";
            }

            recentLogs.Add(logEntry);
            if (recentLogs.Count > 30) // Keep rolling buffer
            {
                recentLogs.RemoveAt(0);
            }
        }

        [MenuItem("Division of Divinity/Export Project Telemetry")]
        public static void ExportTelemetry()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== DIVISION OF DIVINITY: PROJECT TELEMETRY REPORT ===");
            sb.AppendLine($"Generated: {System.DateTime.Now}");
            sb.AppendLine($"Unity Version: {Application.unityVersion}");
            sb.AppendLine($"Active Scene: {SceneManager.GetActiveScene().name}\n");

            // 1. Scene Hierarchy Snapshot
            sb.AppendLine("--- SCENE HIERARCHY ---");
            Scene activeScene = SceneManager.GetActiveScene();
            GameObject[] rootObjects = activeScene.GetRootGameObjects();
            foreach (GameObject rootObj in rootObjects)
            {
                AppendGameObjectHierarchy(rootObj, sb, 0);
            }
            sb.AppendLine();

            // 2. Recent Console Logs
            sb.AppendLine("--- RECENT CONSOLE LOGS & ERRORS ---");
            if (recentLogs.Count == 0)
            {
                sb.AppendLine("No logs captured yet in this session.");
            }
            else
            {
                int startIndex = Mathf.Max(0, recentLogs.Count - 15);
                for (int i = startIndex; i < recentLogs.Count; i++)
                {
                    sb.AppendLine(recentLogs[i]);
                }
            }

            // Save to Project Root
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "ProjectTelemetry_Report.txt");
            File.WriteAllText(filePath, sb.ToString());
            
            Debug.Log($"[Telemetry Tool] Report successfully exported to: {filePath}");
            EditorUtility.RevealInFinder(filePath);
        }

        private static void AppendGameObjectHierarchy(GameObject obj, StringBuilder sb, int depth)
        {
            string indent = new string(' ', depth * 2);
            string activeStatus = obj.activeInHierarchy ? "" : " (Inactive)";
            sb.AppendLine($"{indent}- {obj.name}{activeStatus}");

            foreach (Transform child in obj.transform)
            {
                AppendGameObjectHierarchy(child.gameObject, sb, depth + 1);
            }
        }
    }
}