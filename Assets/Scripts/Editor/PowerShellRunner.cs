using System.Diagnostics;
using UnityEngine;

public class PowerShellRunner : MonoBehaviour
{
    public static void RunCommand(string command)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo()
        {
            FileName = "powershell.exe",
            Arguments = $"-Command \"{command}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true // Set to false if you want the external terminal window to pop up
        };

        using (Process process = Process.Start(startInfo))
        {
            string output = process.StandardOutput.ReadToEnd();
            string errors = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (!string.IsNullOrEmpty(output))
            {
                UnityEngine.Debug.Log($"[PowerShell Output]: {output}");
            }

            if (!string.IsNullOrEmpty(errors))
            {
                UnityEngine.Debug.LogError($"[PowerShell Error]: {errors}");
            }
        }
    }

    // Example trigger
    [ContextMenu("Test Ollama Status")]
    public void TestOllama()
    {
        RunCommand("ollama list");
    }
}