using UnityEngine;

/// <summary>
/// Builds a Perlin/FBM heightmap and applies it to a Unity Terrain.
/// </summary>
public class TerrainGenerator : MonoBehaviour
{
    [Header("Terrain")]
    [SerializeField] Terrain terrain;
    [Tooltip("Used when Auto Resolution is off. Must be 2^n + 1 (e.g. 129, 257, 1025).")]
    [SerializeField] int heightmapResolution = 257;
    [SerializeField] bool autoResolution = true;
    [Tooltip("Target meters per height sample when Auto Resolution is on. Lower = sharper, slower.")]
    [SerializeField] [Range(2f, 40f)] float metersPerSample = 8f;
    [SerializeField] float worldSize = 200f;
    [SerializeField] float heightScale = 40f;

    [Header("Noise (Perlin FBM)")]
    [SerializeField] int seed = 42;
    [SerializeField] int octaves = 4;
    [SerializeField] float frequency = 0.01f;
    [SerializeField] [Range(0.1f, 1f)] float persistence = 0.5f;
    [SerializeField] float lacunarity = 2f;

    [Header("Biomes")]
    [SerializeField] TerrainBiomePainter biomePainter;

    public HeightMapData CurrentHeightMap { get; private set; }
    public int Seed => seed;

    public void SetSeed(int newSeed) => seed = newSeed;

    public HeightMapData Generate()
    {
        EnsureTerrain();
        CurrentHeightMap = BuildHeightMap();
        ApplyToTerrain(CurrentHeightMap);

        if (biomePainter == null)
            biomePainter = GetComponent<TerrainBiomePainter>();
        if (biomePainter != null)
            biomePainter.Paint(terrain, CurrentHeightMap);

        return CurrentHeightMap;
    }

    public Vector3 GetSpawnPosition()
    {
        if (terrain == null || terrain.terrainData == null)
            return new Vector3(0f, 5f, 0f);

        Vector3 tpos = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;
        float x = tpos.x + size.x * 0.5f;
        float z = tpos.z + size.z * 0.5f;

        float height01 = 0.5f;
        if (CurrentHeightMap != null)
        {
            int mid = CurrentHeightMap.Resolution / 2;
            height01 = CurrentHeightMap.Heights[mid, mid];
        }

        return new Vector3(x, tpos.y + height01 * size.y + 2f, z);
    }

    HeightMapData BuildHeightMap()
    {
        int res = ResolveHeightmapResolution();
        heightmapResolution = res;

        var map = new HeightMapData(res);
        Vector2 offset = Noise.OffsetFromSeed(seed);

        float min = float.MaxValue;
        float max = float.MinValue;

        for (int z = 0; z < res; z++)
        {
            for (int x = 0; x < res; x++)
            {
                float nx = x / (float)(res - 1);
                float nz = z / (float)(res - 1);

                float h = Noise.FBM(
                    nx * worldSize,
                    nz * worldSize,
                    octaves,
                    frequency,
                    persistence,
                    lacunarity,
                    offset);

                map.Heights[z, x] = h;
                if (h < min) min = h;
                if (h > max) max = h;
            }
        }

        float range = max - min;
        if (range < 0.0001f)
            range = 1f;

        for (int z = 0; z < res; z++)
        for (int x = 0; x < res; x++)
            map.Heights[z, x] = (map.Heights[z, x] - min) / range;

        return map;
    }

    int ResolveHeightmapResolution()
    {
        if (!autoResolution)
            return NormalizeResolution(heightmapResolution);

        // Keep detail when World Size grows (129 over 10k looks soapy)
        int target = Mathf.RoundToInt(worldSize / Mathf.Max(1f, metersPerSample)) + 1;
        return NormalizeResolution(Mathf.Clamp(target, 129, 2049));
    }

    void ApplyToTerrain(HeightMapData map)
    {
        TerrainData data = terrain.terrainData;
        data.heightmapResolution = map.Resolution;
        // Alphamap must track height detail or biomes blur across huge worlds
        int alphaRes = Mathf.ClosestPowerOfTwo(Mathf.Max(64, map.Resolution - 1));
        data.alphamapResolution = Mathf.Clamp(alphaRes, 64, 2048);
        data.size = new Vector3(worldSize, heightScale, worldSize);
        data.SetHeights(0, 0, map.Heights);
        terrain.transform.position = Vector3.zero;
        terrain.Flush();
    }

    void EnsureTerrain()
    {
        if (terrain == null)
            terrain = FindFirstObjectByType<Terrain>();

        if (terrain == null)
        {
            var go = Terrain.CreateTerrainGameObject(new TerrainData());
            go.name = "Terrain";
            terrain = go.GetComponent<Terrain>();
        }

        if (terrain.terrainData == null)
            terrain.terrainData = new TerrainData();
    }

    static int NormalizeResolution(int resolution)
    {
        int[] allowed = { 33, 65, 129, 257, 513, 1025, 2049, 4097 };
        int best = allowed[0];
        int bestDist = Mathf.Abs(resolution - best);
        foreach (int a in allowed)
        {
            int d = Mathf.Abs(resolution - a);
            if (d < bestDist)
            {
                best = a;
                bestDist = d;
            }
        }

        return best;
    }
}
