using UnityEngine;
using UnityEditor;
using System.IO;

public class PersistentDataOpener
{
    [MenuItem("Division of Divinity/Open Persistent Data Path")]
    public static void OpenPath()
    {
        string path = Application.persistentDataPath;
        
        // Ensure the directory exists before trying to open it
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        // Opens the folder in your operating system's file explorer
        EditorUtility.RevealInFinder(path);
    }
}