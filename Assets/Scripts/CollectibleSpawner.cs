using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns yellow collectible cubes on walkable terrain heights (seedable).
/// </summary>
public class CollectibleSpawner : MonoBehaviour
{
    [Header("Spawn")]
    [Tooltip("How many yellow cubes to place on the map.")]
    [SerializeField] [Min(1)] int count = 8;

    [Header("Placement")]
    [SerializeField] Transform collectiblesRoot;
    [SerializeField] float cubeSize = 1f;
    [SerializeField] float heightOffset = 1.2f;
    [Tooltip("Normalized height band where cubes may spawn (above water, below peaks).")]
    [SerializeField] [Range(0f, 1f)] float minHeight = 0.32f;
    [SerializeField] [Range(0f, 1f)] float maxHeight = 0.72f;
    [SerializeField] float minDistanceFromPlayer = 40f;
    [SerializeField] float minDistanceBetween = 25f;
    [SerializeField] int maxAttempts = 2000;

    Material collectibleMat;

    public int SpawnedCount { get; private set; }

    public void Spawn(TerrainGenerator terrainGenerator)
    {
        if (collectiblesRoot == null)
            collectiblesRoot = transform;

        Clear();

        if (terrainGenerator == null || terrainGenerator.CurrentHeightMap == null)
        {
            SpawnedCount = 0;
            return;
        }

        Terrain terrain = terrainGenerator.Terrain;
        HeightMapData map = terrainGenerator.CurrentHeightMap;
        if (terrain == null || terrain.terrainData == null)
        {
            SpawnedCount = 0;
            return;
        }

        Vector3 tpos = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;
        Vector3 playerXZ = new Vector3(tpos.x + size.x * 0.5f, 0f, tpos.z + size.z * 0.5f);

        // Space pickups farther on large maps; cube size stays as set in Inspector
        float worldScale = Mathf.Clamp(size.x / 200f, 1f, 25f);
        float minFromPlayer = minDistanceFromPlayer * worldScale;
        float minBetween = minDistanceBetween * worldScale;

        var rng = new System.Random(terrainGenerator.Seed + 17);
        var placed = new List<Vector3>(count);
        EnsureMaterial();

        int attempts = 0;
        while (placed.Count < count && attempts < maxAttempts)
        {
            attempts++;
            float u = (float)rng.NextDouble();
            float v = (float)rng.NextDouble();
            float h01 = SampleHeight01(map, u, v);
            if (h01 < minHeight || h01 > maxHeight)
                continue;

            float x = tpos.x + u * size.x;
            float z = tpos.z + v * size.z;
            Vector3 flat = new Vector3(x, 0f, z);
            if (Vector3.Distance(flat, playerXZ) < minFromPlayer)
                continue;

            bool tooClose = false;
            for (int i = 0; i < placed.Count; i++)
            {
                if (Vector3.Distance(flat, new Vector3(placed[i].x, 0f, placed[i].z)) < minBetween)
                {
                    tooClose = true;
                    break;
                }
            }

            if (tooClose)
                continue;

            float y = terrain.SampleHeight(new Vector3(x, 0f, z)) + tpos.y + heightOffset + cubeSize * 0.5f;
            Vector3 world = new Vector3(x, y, z);
            SpawnOne(world);
            placed.Add(world);
        }

        SpawnedCount = placed.Count;
    }

    void SpawnOne(Vector3 position)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Collectible";
        go.transform.SetParent(collectiblesRoot, false);
        go.transform.position = position;
        go.transform.localScale = Vector3.one * cubeSize;

        var renderer = go.GetComponent<Renderer>();
        renderer.sharedMaterial = collectibleMat;

        var col = go.GetComponent<Collider>();
        col.isTrigger = true;

        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        go.AddComponent<Collectible>();
    }

    void Clear()
    {
        for (int i = collectiblesRoot.childCount - 1; i >= 0; i--)
            Destroy(collectiblesRoot.GetChild(i).gameObject);
    }

    void EnsureMaterial()
    {
        if (collectibleMat != null)
            return;

        collectibleMat = new Material(Shader.Find("Universal Render Pipeline/Lit"))
        {
            color = new Color(1f, 0.85f, 0.1f)
        };
    }

    static float SampleHeight01(HeightMapData map, float u, float v)
    {
        int res = map.Resolution;
        float fx = u * (res - 1);
        float fz = v * (res - 1);
        int x0 = Mathf.Clamp(Mathf.FloorToInt(fx), 0, res - 1);
        int z0 = Mathf.Clamp(Mathf.FloorToInt(fz), 0, res - 1);
        int x1 = Mathf.Min(x0 + 1, res - 1);
        int z1 = Mathf.Min(z0 + 1, res - 1);
        float tx = fx - x0;
        float tz = fz - z0;

        float h00 = map.Heights[z0, x0];
        float h10 = map.Heights[z0, x1];
        float h01 = map.Heights[z1, x0];
        float h11 = map.Heights[z1, x1];
        return Mathf.Lerp(Mathf.Lerp(h00, h10, tx), Mathf.Lerp(h01, h11, tx), tz);
    }
}
