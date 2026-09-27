using UnityEngine;

/// <summary>
/// Heightmap grid (values in [0..1]) for Terrain.SetHeights.
/// </summary>
public class HeightMapData
{
    public int Resolution { get; }
    public float[,] Heights { get; }

    public HeightMapData(int resolution)
    {
        Resolution = resolution;
        Heights = new float[resolution, resolution];
    }
}
