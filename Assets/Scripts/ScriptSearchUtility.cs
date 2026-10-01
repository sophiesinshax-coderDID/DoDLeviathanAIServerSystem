#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class ScriptSearchUtility : EditorWindow
{
    private string searchPhrase = "";
    private Vector2 scrollPosition;
    private List<SearchResult> searchResults = new List<SearchResult>();

    private struct SearchResult
    {
        public string filePath;
        public int lineNumber;
        public string lineContent;
    }

    [MenuItem("Tools/Script Phrase Searcher")]
    public static void ShowWindow()
    {
        GetWindow<ScriptSearchUtility>("Script Searcher");
    }

    private void OnGUI()
    {
        GUILayout.Label("Global Script Phrase Search", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        searchPhrase = EditorGUILayout.TextField("Search Term:", searchPhrase);
        if (GUILayout.Button("Search", GUILayout.Width(80)))
        {
            PerformSearch();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        GUILayout.Label($"Results Found: {searchResults.Count}", EditorStyles.label);
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        foreach (var result in searchResults)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("File:", result.filePath, EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Line {result.lineNumber}: {result.lineContent}");
            
            if (GUILayout.Button("Open Script", GUILayout.Width(100)))
            {
                var asset = AssetDatabase.LoadAssetAtPath<MonoScript>(result.filePath);
                if (asset != null) AssetDatabase.OpenAsset(asset, result.lineNumber);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        EditorGUILayout.EndScrollView();
    }

    private void PerformSearch()
    {
        searchResults.Clear();
        if (string.IsNullOrWhiteSpace(searchPhrase)) return;

        string[] scriptFiles = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);

        foreach (string file in scriptFiles)
        {
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains(searchPhrase))
                {
                    string relativePath = "Assets" + file.Substring(Application.dataPath.Length);
                    
                    searchResults.Add(new SearchResult
                    {
                        filePath = relativePath,
                        lineNumber = i + 1,
                        lineContent = lines[i].Trim()
                    });
                }
            }
        }
        Debug.Log($"[ScriptSearchUtility] Search complete for '{searchPhrase}'. Found {searchResults.Count} matches.");
    }
}
#endif