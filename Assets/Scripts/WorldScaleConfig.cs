using UnityEngine;

[CreateAssetMenu(fileName = "WorldScaleConfig", menuName = "Leviathan/World Scale Config")]
public class WorldScaleConfig : ScriptableObject
{
    [Header("Grid & Chunk Dimensions")]
    public int gridSize = 250; //250m(blender)
    public int gridHeight = 250; //250m(blender)
    public int chunkSize = 50; //50m(blender)
    public int chunkHeight = 50; //50m(blender)

    [Header("Cosmic & Atmospheric Boundaries")]
    public float worldBarrierRadius = 50000000f; // 50 Million units
    public float atmosphereStart = 100000f;        // 100k units up

    [Header("Surface & Terrain Limits")]
    public float mountainPeak = 10000f;
    public float snowLine = 6000f;
    public float slopeStart = 50f;
    public float hillLimit = 35f;
    public float grassLimit = 15f;
    public float beachLimit = 10f;
    public float groundLevel = 0f;

    [Header("Subterranean & Abyss Layers")]
    public float oceanFloor = -20000f;
    public float bedrockLevel = -3500000f;
    public float shadowStalkerZone = -3500500f;
    public float instanceZoneStart = -4000000f;
    public float instanceZoneEnd = -5000000f;

    // Helper method to determine player zone based on altitude (Y coordinate)
    public WorldZone GetZoneFromAltitude(float altitude)
    {
        if (altitude >= atmosphereStart) return WorldZone.OuterSpaceVoid;
        if (altitude > mountainPeak) return WorldZone.UpperAtmosphere;
        if (altitude >= snowLine) return WorldZone.MountainPeak;
        if (altitude >= groundLevel) return WorldZone.Surface;
        if (altitude >= oceanFloor) return WorldZone.OceanShelf;
        if (altitude >= bedrockLevel) return WorldZone.DeepUnderworld;
        if (altitude >= shadowStalkerZone) return WorldZone.ShadowStalkerAbyss;
        if (altitude >= instanceZoneStart) return WorldZone.InstanceZone;
        return WorldZone.VoidSub_Core; // Fixed: replaced hyphen with underscore
    }
}

public enum WorldZone
{
    OuterSpaceVoid,     // Beyond 100,000 -> Home of the Cosmic Leviathan
    UpperAtmosphere,    // 10,000 to 100,000
    MountainPeak,       // 6,000 to 10,000
    Surface,            // 0 to 6,000
    OceanShelf,         // -20,000 to 0
    DeepUnderworld,     // -3,500,000 to -20,000
    ShadowStalkerAbyss, // -3,500,500 to -3,500,000
    InstanceZone,       // -5,000,000 to -4,000,000
    VoidSub_Core        // Below -5,000,000 (Fixed: replaced hyphen with underscore)
}