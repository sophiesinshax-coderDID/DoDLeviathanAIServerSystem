#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public class AssetCleanupUtility : EditorWindow
{
    [MenuItem("Tools/Clean Project Duplicates & Broken Assets")]
    public static void CleanProject()
    {
        Debug.Log("[AssetCleanupUtility] Scanning project for duplicate scripts and broken assets...");

        // 1. Find all ProceduralLoginUI.cs files to fix CS0101 / CS0111 duplicate definitions
        string[] scriptGuids = AssetDatabase.FindAssets("ProceduralLoginUI t:Script");
        List<string> scriptPaths = scriptGuids.Select(AssetDatabase.GUIDToAssetPath).ToList();

        if (scriptPaths.Count > 1)
        {
            Debug.LogWarning($"[AssetCleanupUtility] Found {scriptPaths.Count} definitions of ProceduralLoginUI.cs. Cleaning duplicates...");
            // Keep the first one, delete the rest
            for (int i = 1; i < scriptPaths.Count; i++)
            {
                string fullPath = Path.GetFullPath(scriptPaths[i]);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    // Also delete the .meta file
                    if (File.Exists(fullPath + ".meta"))
                        File.Delete(fullPath + ".meta");
                    
                    Debug.Log($"[AssetCleanupUtility] Deleted duplicate script: {scriptPaths[i]}");
                }
            }
        }
        else
        {
            Debug.Log("[AssetCleanupUtility] No duplicate ProceduralLoginUI scripts found.");
        }

        // 2. Scan for legacy/broken plugins like outdated YoutubeExplode.dll causing runtime crash warnings
        string[] pluginGuids = AssetDatabase.FindAssets("YoutubeExplode t:AssemblyDefinitionAsset");
        foreach (var guid in pluginGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Debug.LogWarning($"[AssetCleanupUtility] Found legacy plugin assembly reference: {path}. Consider removing if causing runtime crashes.");
        }

        // Refresh database to clear compilation errors
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Cleanup Complete", "Project scan finished! Check the Console for details.", "OK");
    }
}
#endif