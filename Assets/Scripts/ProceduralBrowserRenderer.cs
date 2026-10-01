using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class ProceduralBrowserRenderer : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RawImage browserScreenDisplay; // The UI element showing the webpage/video
    [SerializeField] private VideoPlayer browserVideoPlayer;    // Video player linked to the radio or browser
    [SerializeField] private RenderTexture browserRenderTexture;

    void Start()
    {
        if (browserVideoPlayer != null && browserRenderTexture != null)
        {
            // Direct the video player's output into the Render Texture
            browserVideoPlayer.targetTexture = browserRenderTexture;
            
            // Assign that texture to the UI RawImage element on your browser canvas
            if (browserScreenDisplay != null)
            {
                browserScreenDisplay.texture = browserRenderTexture;
            }
        }
    }

    // Call this when your browser navigates to a video URL or search query
    public async void LoadUrlOrSearch(string query)
    {
        Debug.Log($"[ProceduralBrowser]: Parsing target -> {query}");
        
        // Tie it directly into your YoutubeRadio instance
        if (YoutubeRadio.Instance != null)
        {
            string result = await YoutubeRadio.Instance.PlayYoutubeAudio(query);
            Debug.Log($"[ProceduralBrowser]: Stream result -> {result}");
        }
    }
}