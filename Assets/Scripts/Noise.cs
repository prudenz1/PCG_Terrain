using UnityEngine;

/// <summary>
/// Seedable Perlin / FBM helpers for heightmaps (lecture 3: octaves).
/// </summary>
public static class Noise
{
    public static float Perlin(float x, float z, float frequency, Vector2 offset)
    {
        return Mathf.PerlinNoise(x * frequency + offset.x, z * frequency + offset.y);
    }

    public static float FBM(
        float x,
        float z,
        int octaves,
        float frequency,
        float persistence,
        float lacunarity,
        Vector2 offset)
    {
        float value = 0f;
        float amplitude = 1f;
        float maxValue = 0f;
        float freq = frequency;

        for (int i = 0; i < octaves; i++)
        {
            value += Perlin(x, z, freq, offset) * amplitude;
            maxValue += amplitude;
            amplitude *= persistence;
            freq *= lacunarity;
        }

        return maxValue > 0f ? value / maxValue : 0f;
    }

    public static Vector2 OffsetFromSeed(int seed)
    {
        var rng = new System.Random(seed);
        return new Vector2(
            (float)rng.NextDouble() * 10000f,
            (float)rng.NextDouble() * 10000f);
    }
}
