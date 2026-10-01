using UnityEngine;

namespace DivisionOfDivinity.AI
{
    public class WorldCycleSeedManager : MonoBehaviour
    {
        public static WorldCycleSeedManager Instance;

        [Header("Current World State")]
        public int ActiveWorldSeed { get; private set; }
        public bool IsCycleInitialized { get; private set; } = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void InitializeCycleWithDevSeed(string sophieKey, string luciKey, char checksum)
        {
            string combinedSeedSource = sophieKey + luciKey + checksum;
            ActiveWorldSeed = combinedSeedSource.GetHashCode();

            // Seed Unity's random state so all procedural spawns, terrain, or loot match the handshake seed
            Random.InitState(ActiveWorldSeed);
            IsCycleInitialized = true;

            Debug.Log($"[World Cycle Seed Manager]: Cycle initialized with Dev-Anchored Seed: {ActiveWorldSeed} (Keys: {sophieKey}, {luciKey} | Checksum: {checksum})");
            
            // Trigger downstream world/terrain/leviathan generators here
            ApplySeedToGenerators();
        }

        private void ApplySeedToGenerators()
        {
            // If you have procedural terrain or noise managers, you can hook them up here:
            // var terrainGen = FindFirstObjectByType<ProceduralTerrainGenerator>();
            // if (terrainGen != null) terrainGen.GenerateWithSeed(ActiveWorldSeed);
        }
    }
}