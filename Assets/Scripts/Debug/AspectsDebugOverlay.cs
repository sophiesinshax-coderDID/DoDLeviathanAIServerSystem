using System.Text;
using UnityEngine;
using UnityEngine.Profiling;
using TMPro;

namespace DivisionOfDivinity.AI
{
    public class AspectsDebugOverlay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CharacterAspects characterAspects;
        [SerializeField] private LeviathanStateMachine stateMachine;
        [SerializeField] private TextMeshProUGUI debugText;

        [Header("Settings")]
        [SerializeField] private KeyCode toggleKey = KeyCode.F3;
        [SerializeField] private float fpsUpdateInterval = 0.25f;

        private float _fpsAccumulator = 0f;
        private int _fpsFrames = 0;
        private float _fpsTimeLeft = 0f;
        private float _currentFps = 0f;
        private bool _isVisible = true;

        private readonly StringBuilder _sb = new StringBuilder();

        private void Awake()
        {
            if (characterAspects == null) characterAspects = FindFirstObjectByType<CharacterAspects>();
            if (stateMachine == null) stateMachine = FindFirstObjectByType<LeviathanStateMachine>();
            if (debugText == null) debugText = GetComponent<TextMeshProUGUI>();
        }

        private void Update()
        {
            // Toggle overlay with F3
            if (Input.GetKeyDown(toggleKey))
            {
                _isVisible = !_isVisible;
                if (debugText != null) debugText.gameObject.SetActive(_isVisible);
            }

            if (!_isVisible) return;

            CalculateFPS();
            RenderOverlay();
        }

        private void CalculateFPS()
        {
            _fpsTimeLeft -= Time.unscaledDeltaTime;
            _fpsAccumulator += Time.unscaledDeltaTime;
            _fpsFrames++;

            if (_fpsTimeLeft <= 0f)
            {
                _currentFps = _fpsFrames / _fpsAccumulator;
                _fpsTimeLeft = fpsUpdateInterval;
                _fpsAccumulator = 0f;
                _fpsFrames = 0;
            }
        }

        private void RenderOverlay()
        {
            if (debugText == null) return;

            _sb.Clear();

            // 1. System Metrics & Memory
            _sb.AppendLine("<b><color=#00E5FF>[ SYSTEM METRICS & HARDWARE ]</color></b> (Press F3 to Hide)");
            string fpsColor = _currentFps >= 55f ? "#00FF00" : (_currentFps >= 30f ? "#FFFF00" : "#FF0000");
            _sb.AppendLine($"FPS: <color={fpsColor}>{_currentFps:F1}</color> | Frame Time: {Time.unscaledDeltaTime * 1000f:F1} ms");
            
            // Memory & VRAM stats
            long totalReservedMemory = Profiler.GetTotalReservedMemoryLong() / (1024 * 1024);
            long allocatedMemory = Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024);
            long systemMemorySize = SystemInfo.systemMemorySize;
            long graphicsMemorySize = SystemInfo.graphicsMemorySize; // Dedicated VRAM in MB

            _sb.AppendLine($"System RAM: {systemMemorySize} MB | Unity Reserved: {totalReservedMemory} MB ({allocatedMemory} MB Used)");
            _sb.AppendLine($"GPU: <color=#FFD700>{SystemInfo.graphicsDeviceName}</color>");
            _sb.AppendLine($"Dedicated VRAM: <color=#00FF7F>{graphicsMemorySize} MB</color>");
            _sb.AppendLine();

            // 2. User Profile Telemetry
            _sb.AppendLine("<b><color=#00E5FF>[ USER PROFILE ]</color></b>");
            if (LoginHandler.Instance != null && LoginHandler.Instance.currentUser != null)
            {
                var profile = LoginHandler.Instance.currentUser;
                _sb.AppendLine($"Display Name: <b><color=#FFFFFF>{profile.displayName}</color></b> (Login: {profile.loginName})");
                _sb.AppendLine($"Email: <color=#D3D3D3>{profile.email}</color> | Discord: <color=#7289DA>{profile.discordName}</color>");
                _sb.AppendLine($"Player ID: <color=#FFD700>{profile.playerID}</color>");
                _sb.AppendLine($"Old PID: <color=#A9A9A9>{(string.IsNullOrEmpty(profile.oldPID) ? "None" : profile.oldPID)}</color>");
                _sb.AppendLine($"IP (Creation): {profile.accountCreationIP} | IP (Session): <color=#00FF7F>{profile.currentLoginSessionIP}</color>");
                _sb.AppendLine($"Tier: <color=#FF8C00>{profile.playerTier}</color> | Auth Level: <color=#FF4500>{profile.authLevel}</color>");
            }
            else
            {
                _sb.AppendLine("<color=red>No Active User Profile Found</color>");
            }

            _sb.AppendLine();

            // 3. Character Aspects
            _sb.AppendLine("<b><color=#00E5FF>[ CHARACTER ASPECTS ]</color></b>");
            if (characterAspects != null && characterAspects.aspects != null)
            {
                var aspect = characterAspects.aspects;
                _sb.AppendLine($"Name: <b>{aspect.characterName}</b>");
                _sb.AppendLine($"Karma: <color=#00FF7F>{aspect.karma:+0.0;-0.0;0.0}</color> / 100");
                _sb.AppendLine($"Corruption: <color=#FF4500>{aspect.corruption * 100f:F0}%</color> ({aspect.corruption:F2})");
                _sb.AppendLine($"Heart: <color=#FF69B4>{aspect.heart:F1}</color> / 100");
                _sb.AppendLine($"Soul: <color=#9400D3>{aspect.soul:F1}</color> / 100");
                _sb.AppendLine($"Trait: <color=#FFD700>{aspect.dominantTrait}</color>");
            }
            else
            {
                _sb.AppendLine("<color=red>No CharacterAspects Target Found</color>");
            }

            _sb.AppendLine();

            // 4. Leviathan State Machine
            _sb.AppendLine("<b><color=#00E5FF>[ LEVIATHAN STATE ]</color></b>");
            if (stateMachine != null)
            {
                string stateColor = stateMachine.CurrentState switch
                {
                    LeviathanState.Idle => "#808080",
                    LeviathanState.Processing => "#FFFF00",
                    LeviathanState.SearchingWeb => "#00E5FF",
                    LeviathanState.Speaking => "#00FF00",
                    LeviathanState.Error => "#FF0000",
                    _ => "#FF00FF"
                };

                _sb.AppendLine($"Status: <color={stateColor}><b>{stateMachine.CurrentState.ToString().ToUpper()}</b></color> ({stateMachine.TimeInCurrentState:F1}s)");
                _sb.AppendLine($"Activity: <i><color=#D3D3D3>{stateMachine.CurrentActivityDetails}</color></i>");
            }
            else
            {
                _sb.AppendLine("<color=yellow>No LeviathanStateMachine Target Found</color>");
            }

            _sb.AppendLine();

            // 5. Network Telemetry
            _sb.AppendLine("<b><color=#00E5FF>[ NETWORK TELEMETRY ]</color></b>");
            _sb.AppendLine($"Bytes Sent: <color=#00BFFF>{FormatBytes(LeviathanNetworkTelemetry.TotalBytesSent)}</color>");
            _sb.AppendLine($"Bytes Received: <color=#32CD32>{FormatBytes(LeviathanNetworkTelemetry.TotalBytesReceived)}</color>");
            _sb.AppendLine($"Active Requests: {LeviathanNetworkTelemetry.ActiveRequestsCount}");
            _sb.AppendLine($"Last Packet: {LeviathanNetworkTelemetry.LastPacketSizeText}");

            debugText.text = _sb.ToString();
        }

        private string FormatBytes(long bytes)
        {
            if (bytes > 1024 * 1024) return $"{bytes / (1024f * 1024f):F2} MB";
            if (bytes > 1024) return $"{bytes / 1024f:F2} KB";
            return $"{bytes} B";
        }
    }
}