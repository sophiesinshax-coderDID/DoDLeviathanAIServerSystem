using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ProceduralLoginUI : MonoBehaviour
{
    [Header("UI References")]
    public Canvas loginCanvas;
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public Button continueButton;
    public TextMeshProUGUI errorText;

    private void Awake()
    {
        BuildLoginUI();
    }

    public void BuildLoginUI()
    {
        // Ensure EventSystem exists in the scene for raycasts
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
        }

        // Setup Canvas & GraphicRaycaster if missing
        if (loginCanvas == null)
        {
            GameObject canvasObj = new GameObject("LoginWindowPanel", typeof(RectTransform));
            loginCanvas = canvasObj.AddComponent<Canvas>();
            loginCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>(); // Crucial for UI raycasts
        }
        else if (loginCanvas.GetComponent<GraphicRaycaster>() == null)
        {
            loginCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        // Configure input fields and buttons with explicit Raycast Targets enabled
        if (usernameInput != null)
        {
            var bg = usernameInput.GetComponent<Image>();
            if (bg != null) bg.raycastTarget = true;
        }

        if (passwordInput != null)
        {
            var bg = passwordInput.GetComponent<Image>();
            if (bg != null) bg.raycastTarget = true;
        }

        if (continueButton != null)
        {
            var bg = continueButton.GetComponent<Image>();
            if (bg != null) bg.raycastTarget = true;
            continueButton.onClick.AddListener(OnContinueClicked);
        }
    }

    private void OnContinueClicked()
    {
        string username = usernameInput != null ? usernameInput.text : string.Empty;
        string password = passwordInput != null ? passwordInput.text : string.Empty;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            if (errorText != null)
                errorText.text = "Error: Username and password fields cannot be empty.";
            return;
        }

        // Clear error and proceed with authentication logic
        if (errorText != null) errorText.text = string.Empty;
        Debug.Log($"[ProceduralLoginUI] Authenticating user: {username}");
    }
}