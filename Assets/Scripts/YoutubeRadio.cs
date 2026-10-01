using UnityEngine;
using UnityEngine.Video;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

public class YouTubeCache
{
    private class CacheEntry
    {
        public string StreamUrl;
        public float Timestamp;
    }

    private Dictionary<string, CacheEntry> _cache = new Dictionary<string, CacheEntry>();
    private float cacheDurationSeconds = 1800f; // Cache valid for 30 minutes

    public bool TryGetCachedUrl(string query, out string url)
    {
        url = null;
        if (_cache.TryGetValue(query, out var entry))
        {
            if (Time.time - entry.Timestamp < cacheDurationSeconds)
            {
                url = entry.StreamUrl;
                return true;
            }
            else
            {
                _cache.Remove(query); // Expired
            }
        }
        return false;
    }

    public void AddToCache(string query, string url)
    {
        _cache[query] = new CacheEntry
        {
            StreamUrl = url,
            Timestamp = Time.time
        };
    }
} // <-- This closing brace was missing!

public class YoutubeRadio : MonoBehaviour
{
    public static YoutubeRadio Instance { get; private set; }
    public VideoPlayer audioPlayer; 
    private YoutubeClient youtube;

    private string pendingUrl = "";
    private bool shouldStartPlay = false;

    private YouTubeCache _cache = new YouTubeCache();

    void Awake() 
    { 
        Instance = this; 
        youtube = new YoutubeClient(); 
    }

    public void PlayLocalClip(AudioClip clip)
    {
        if (audioPlayer.isPlaying) audioPlayer.Stop();
        AudioSource source = GetComponent<AudioSource>();
        source.clip = clip;
        source.Play();
    }

    void Update()
    {
        if (shouldStartPlay)
        {
            shouldStartPlay = false;

            audioPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            audioPlayer.SetTargetAudioSource(0, GetComponent<AudioSource>());
            
            audioPlayer.url = pendingUrl;
            
            if(GetComponent<AudioSource>().isPlaying)
            {
                GetComponent<AudioSource>().Stop(); 
            }
            
            audioPlayer.Play();
            Debug.Log("URL FOUND AND PLAYING: " + pendingUrl);
        }
    }

    public async Task<string> PlayYoutubeAudio(string searchQuery)
    {
        string cleanQuery = searchQuery.ToLower().Trim();

        if (_cache.TryGetCachedUrl(cleanQuery, out string cachedUrl))
        {
            Debug.Log($"[YoutubeRadio]: Cache hit! Playing stream directly for -> \"{cleanQuery}\"");
            pendingUrl = cachedUrl;
            shouldStartPlay = true;
            return $"SUCCESS|Cached Stream|{cachedUrl}";
        }

        try
        {
            Debug.Log($"[YoutubeRadio]: Cache miss. Fetching fresh stream for -> \"{cleanQuery}\"");
            YoutubeExplode.Search.VideoSearchResult video = null;
            await foreach (var batch in youtube.Search.GetVideosAsync(cleanQuery))
            {
                video = batch;
                break; 
            }

            if (video == null) return "ERROR|No results found.";

            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);
            
            var streamInfo = streamManifest.GetMuxedStreams()
                .Where(s => s.Container == YoutubeExplode.Videos.Streams.Container.Mp4)
                .OrderBy(s => s.VideoQuality.Label)
                .FirstOrDefault();

            if (streamInfo == null) return "ERROR|No compatible Mp4 stream found.";

            pendingUrl = streamInfo.Url;
            shouldStartPlay = true;

            _cache.AddToCache(cleanQuery, streamInfo.Url);

            return $"SUCCESS|{video.Title}|{streamInfo.Url}";
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Radio Error: {e.Message}");
            return $"ERROR|{e.Message}";
        }
    }
}