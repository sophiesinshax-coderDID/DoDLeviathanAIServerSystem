using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DivisionOfDivinity.UI
{
    public class ProceduralTopLeftTerminals : MonoBehaviour
    {
        [Header("Font & Styling")]
        [SerializeField] private TMP_FontAsset terminalFont;
        
        private GameObject rootCanvasObject;
        private TMP_InputField terminalInputField;
        private TextMeshProUGUI terminalOutputText;
        private TextMeshProUGUI networkOutputText;
        private TextMeshProUGUI characterStatsOutputText;

        void Awake()
        {
            BuildTopLeftInterface();
        }

        private void BuildTopLeftInterface()
        {
            // 1. Ensure we have a Canvas in the scene
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("ProceduralHUD_Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // 2. Create a parent container anchored to the TOP-LEFT (Expanded width to accommodate 3 panels: 400 + 400 + 400 + spacing)
            GameObject container = new GameObject("TopLeft_TerminalContainer", typeof(RectTransform));
            container.transform.SetParent(canvas.transform, false);
            
            RectTransform containerRect = container.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 1); // Top-Left
            containerRect.anchorMax = new Vector2(0, 1);
            containerRect.pivot = new Vector2(0, 1);
            containerRect.anchoredPosition = new Vector2(20f, -20f); // Slight margin from edge
            containerRect.sizeDelta = new Vector2(1235f, 400f);

            // Add a horizontal layout group to hold our side-by-side windows
            HorizontalLayoutGroup hGroup = container.AddComponent<HorizontalLayoutGroup>();
            hGroup.spacing = 15f;
            hGroup.childControlWidth = false;
            hGroup.childControlHeight = false;
            hGroup.childForceExpandWidth = false;
            hGroup.childForceExpandHeight = false;

            // 3. Build Window 1: Interactive Terminal (Left)
            GameObject termWindow = CreateWindowPanel(container.transform, "Terminal_Window", new Vector2(400f, 400f), new Color(0.05f, 0.05f, 0.08f, 0.85f));
            terminalOutputText = CreateScrollableText(termWindow.transform, "Terminal_Output", out ScrollRect termScroll, new Color(0f, 1f, 0.6f));
            terminalInputField = CreateInputField(termWindow.transform);

            // 4. Build Window 2: Network / System Data Log (Middle)
            GameObject netWindow = CreateWindowPanel(container.transform, "NetworkData_Window", new Vector2(400f, 400f), new Color(0.08f, 0.05f, 0.05f, 0.85f));
            networkOutputText = CreateScrollableText(netWindow.transform, "Network_Output", out ScrollRect netScroll, new Color(0f, 0.9f, 1f));

            // 5. Build Window 3: Character Stats / Aspect (Right)
            GameObject charWindow = CreateWindowPanel(container.transform, "CharacterStats_Window", new Vector2(400f, 400f), new Color(0.05f, 0.08f, 0.06f, 0.85f));
            characterStatsOutputText = CreateScrollableText(charWindow.transform, "Character_Output", out ScrollRect charScroll, new Color(1f, 0.8f, 0.2f));
            
            // Populate initial character info
            UpdateCharacterAspectDisplay("IDLE", 100f, 50f, Vector3.zero, 0);

            Debug.Log("[ProceduralUI]: Top-Left triple terminal/aspect interface generated successfully via code.");
        }

        private GameObject CreateWindowPanel(Transform parent, string name, Vector2 size, Color bgColor)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.sizeDelta = size;

            Image bg = panel.GetComponent<Image>();
            bg.color = bgColor;

            return panel;
        }

        private TextMeshProUGUI CreateScrollableText(Transform parent, string name, out ScrollRect outScroll, Color textColor)
        {
            // ScrollView container
            GameObject scrollViewObj = new GameObject(name + "_Scroll", typeof(RectTransform), typeof(ScrollRect));
            scrollViewObj.transform.SetParent(parent, false);
            RectTransform scrollRectTransform = scrollViewObj.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0, 0);
            scrollRectTransform.anchorMax = new Vector2(1, 1);
            scrollRectTransform.sizeDelta = new Vector2(-10f, -40f); 
            scrollRectTransform.anchoredPosition = new Vector2(0, -10f);

            ScrollRect scrollRect = scrollViewObj.GetComponent<ScrollRect>();
            outScroll = scrollRect;

            // Viewport
            GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewportObj.transform.SetParent(scrollViewObj.transform, false);
            RectTransform vpRect = viewportObj.GetComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.sizeDelta = Vector2.zero;

            // Content
            GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            contentObj.transform.SetParent(viewportObj.transform, false);
            RectTransform contentRect = contentObj.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0, 1);
            contentRect.sizeDelta = new Vector2(0, 400);

            VerticalLayoutGroup vGroup = contentObj.GetComponent<VerticalLayoutGroup>();
            vGroup.childControlHeight = true;
            vGroup.childForceExpandHeight = false;

            // Text element
            GameObject textObj = new GameObject("LogText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(contentObj.transform, false);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 1);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.pivot = new Vector2(0, 1);
            textRect.sizeDelta = new Vector2(0, 400);

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            if (terminalFont != null) tmp.font = terminalFont;
            tmp.fontSize = 12;
            tmp.color = textColor; 
            tmp.text = $"[{name} Initialized...]\n";

            scrollRect.content = contentRect;
            scrollRect.viewport = vpRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            return tmp;
        }

        private TMP_InputField CreateInputField(Transform parent)
        {
            GameObject inputObj = new GameObject("Terminal_InputField", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            inputObj.transform.SetParent(parent, false);

            RectTransform rect = inputObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0, 5f);
            rect.sizeDelta = new Vector2(-10f, 30f);

            Image bg = inputObj.GetComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.2f, 1f);

            // Text Area child
            GameObject textArea = new GameObject("Text Area", typeof(RectTransform));
            textArea.transform.SetParent(inputObj.transform, false);
            RectTransform taRect = textArea.GetComponent<RectTransform>();
            taRect.anchorMin = Vector2.zero;
            taRect.anchorMax = Vector2.one;
            taRect.sizeDelta = new Vector2(-10f, -5f);
            taRect.anchoredPosition = Vector2.zero;

            // Placeholder
            GameObject placeholderObj = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            placeholderObj.transform.SetParent(textArea.transform, false);
            TextMeshProUGUI placeholder = placeholderObj.GetComponent<TextMeshProUGUI>();
            placeholder.text = "Enter system command...";
            placeholder.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            placeholder.fontSize = 12;

            // Text
            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(textArea.transform, false);
            TextMeshProUGUI text = textObj.GetComponent<TextMeshProUGUI>();
            text.text = "";
            text.color = Color.white;
            text.fontSize = 12;

            TMP_InputField inputField = inputObj.GetComponent<TMP_InputField>();
            inputField.textViewport = taRect;
            inputField.textComponent = text;
            inputField.placeholder = placeholder;

            return inputField;
        }

        public void UpdateCharacterAspectDisplay(string playerState, float health, float stamina, Vector3 coords, int karma)
        {
            if (characterStatsOutputText == null) return;

            characterStatsOutputText.text = 
                "=== CHARACTER STATUS ASPECT ===\n" +
                $" STATE:   {playerState}\n" +
                $" HEALTH:  {health:F1} / 100\n" +
                $" STAMINA: {stamina:F1} / 100\n" +
                $" COORDS:  X:{coords.x:F1} Y:{coords.y:F1} Z:{coords.z:F1}\n" +
                $" KARMA:   {karma}\n" +
                "-------------------------------\n" +
                " [ACTIVE BUFFS / EFFECTS]\n" +
                " - Corrosion Resistance: Active\n" +
                " - Hemi-Sync Focus 10: Linked\n";
        }
    }
}