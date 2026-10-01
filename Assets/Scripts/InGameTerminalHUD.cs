using UnityEngine;
using TMPro;

public class InGameTerminalHUD : MonoBehaviour
{
    [Header("UI References")]
    public GameObject terminalPanel;
    public TMP_InputField inputField;
    public TextMeshProUGUI outputText;
    public UnityEngine.UI.ScrollRect scrollRect;

    private bool isVisible = false;

    void Start()
    {
        if (terminalPanel != null)
        {
            terminalPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[InGameTerminalHUD]: Terminal Panel reference is not assigned in the Inspector!", this);
        }

        if (inputField != null)
        {
            inputField.onSubmit.AddListener(OnSubmitCommand);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            ToggleTerminal();
        }
    }

    public void ToggleTerminal()
    {
        if (terminalPanel == null) return;

        isVisible = !isVisible;
        terminalPanel.SetActive(isVisible);

        if (isVisible && inputField != null)
        {
            inputField.ActivateInputField();
            inputField.Select();
        }
    }

    public void OnSubmitCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command)) return;

        // Print input to console output view
        if (outputText != null)
        {
            outputText.text += $"\n> {command}\n";
        }

        // Handle local display commands (like clear) directly, pass the rest to the backend
        string cleanCmd = command.Trim().ToLower();
        if (cleanCmd == "clear")
        {
            if (outputText != null) outputText.text = "";
        }
        else
        {
            // Route everything else through the centralized console command processor
            DeveloperConsoleCommands.HandleCommand(command);
        }

        // Reset and lock focus back onto the input field
        if (inputField != null)
        {
            inputField.text = "";
            inputField.ActivateInputField();
            inputField.Select();
        }
        
        // Auto-scroll down to latest entry
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
    
    private void OnDestroy()
    {
        if (inputField != null)
        {
            inputField.onSubmit.RemoveListener(OnSubmitCommand);
        }
    }

    // Allows backend scripts to print feedback strings directly to the terminal UI screen
    public void PrintToTerminal(string message)
    {
        if (outputText != null)
        {
            outputText.text += $"{message}\n";
        }
    }
}