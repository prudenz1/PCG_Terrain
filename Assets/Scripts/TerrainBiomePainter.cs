using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Paints TerrainLayers by height:
/// low = water / river, then sand shore, grass, rock, snow on peaks.
/// Layers are saved under Assets/TerrainLayers so they are not "Missing" after Play.
/// </summary>
public class TerrainBiomePainter : MonoBehaviour
{
    const string LayersFolder = "Assets/TerrainLayers";

    [SerializeField] bool paintOnGenerate = true;

    [Header("Height thresholds (0..1 normalized height)")]
    [SerializeField] [Range(0f, 1f)] float waterMax = 0.28f;
    [Tooltip("How wide the sand beach is above the water line (not a second biome band).")]
    [SerializeField] [Range(0.005f, 0.12f)] float sandShoreWidth = 0.035f;
    [SerializeField] [Range(0f, 1f)] float grassMax = 0.70f;
    [SerializeField] [Range(0f, 1f)] float snowMin = 0.88f;

    [Header("Layer colors (edit in Inspector)")]
    [SerializeField] Color waterColor = new Color(0.15f, 0.35f, 0.75f);
    [SerializeField] Color sandColor = new Color(0.76f, 0.70f, 0.45f);
    [SerializeField] Color grassColor = new Color(0.25f, 0.55f, 0.22f);
    [SerializeField] Color rockColor = new Color(0.45f, 0.45f, 0.48f);
    [SerializeField] Color snowColor = new Color(0.92f, 0.94f, 0.96f);

    const int Water = 0;
    const int Sand = 1;
    const int Grass = 2;
    const int Rock = 3;
    const int Snow = 4;
    const int LayerCount = 5;

    static readonly string[] LayerNames = { "Water", "Sand", "Grass", "Rock", "Snow" };

    public void Paint(Terrain terrain, HeightMapData heightMap)
    {
        if (!paintOnGenerate || terrain == null || heightMap == null)
            return;

        TerrainData data = terrain.terrainData;
        TerrainLayer[] layers = EnsureLayers(data);
        if (layers == null)
            return;

        int alphaRes = data.alphamapResolution;
        float[,,] map = new float[alphaRes, alphaRes, LayerCount];

        for (int z = 0; z < alphaRes; z++)
        {
            for (int x = 0; x < alphaRes; x++)
            {
                float u = x / (float)(alphaRes - 1);
                float v = z / (float)(alphaRes - 1);
                float h = SampleHeightBilinear(heightMap, u, v);

                float[] w = WeightsForHeight(h);
                Normalize(w);
                for (int i = 0; i < LayerCount; i++)
                    map[z, x, i] = w[i];
            }
        }

        data.SetAlphamaps(0, 0, map);
        terrain.Flush();
    }

    float[] WeightsForHeight(float h)
    {
        // Sand = thin shoreline only, right above water (not a wide lowland biome)
        float sandEnd = Mathf.Min(waterMax + sandShoreWidth, grassMax - 0.02f);

        float[] w = new float[LayerCount];
        w[Water] = 1f - SmoothStep(waterMax - 0.04f, waterMax + 0.01f, h);
        w[Sand] = Band(h, waterMax - 0.01f, waterMax, sandEnd, sandEnd + 0.015f);
        w[Grass] = Band(h, sandEnd - 0.01f, sandEnd, grassMax, grassMax + 0.08f);
        w[Rock] = Band(h, grassMax - 0.06f, grassMax, snowMin, snowMin + 0.05f);
        w[Snow] = SmoothStep(snowMin - 0.04f, snowMin + 0.02f, h);
        return w;
    }

    static float Band(float h, float in0, float in1, float out0, float out1)
    {
        float enter = SmoothStep(in0, in1, h);
        float leave = 1f - SmoothStep(out0, out1, h);
        return enter * leave;
    }

    static float SmoothStep(float a, float b, float t)
    {
        if (Mathf.Approximately(a, b))
            return t < a ? 0f : 1f;
        return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, t));
    }

    static void Normalize(float[] w)
    {
        float sum = 0f;
        for (int i = 0; i < w.Length; i++)
            sum += Mathf.Max(0f, w[i]);

        if (sum < 0.0001f)
        {
            w[Grass] = 1f;
            return;
        }

        for (int i = 0; i < w.Length; i++)
            w[i] = Mathf.Max(0f, w[i]) / sum;
    }

    static float SampleHeightBilinear(HeightMapData map, float u, float v)
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

    TerrainLayer[] EnsureLayers(TerrainData data)
    {
        Color[] colors = { waterColor, sandColor, grassColor, rockColor, snowColor };
        var layers = new TerrainLayer[LayerCount];

        for (int i = 0; i < LayerCount; i++)
        {
            layers[i] = GetOrCreateLayer(LayerNames[i], colors[i], i == Water ? 0.25f : 0.02f);
            if (layers[i] == null)
                return null;
        }

        if (data.alphamapResolution < 32)
            data.alphamapResolution = Mathf.ClosestPowerOfTwo(Mathf.Max(32, data.heightmapResolution - 1));

        data.terrainLayers = layers;
        return layers;
    }

    TerrainLayer GetOrCreateLayer(string layerName, Color color, float smoothness)
    {
#if UNITY_EDITOR
        if (!AssetDatabase.IsValidFolder(LayersFolder))
            AssetDatabase.CreateFolder("Assets", "TerrainLayers");

        string texPath = $"{LayersFolder}/{layerName}_Diffuse.png";
        string maskPath = $"{LayersFolder}/{layerName}_Mask.png";
        string layerPath = $"{LayersFolder}/{layerName}.terrainlayer";

        Texture2D diffuse = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (diffuse == null)
        {
            var temp = MakeSolidTexture(color, 16);
            WritePng(temp, texPath);
            DestroyImmediate(temp);
            AssetDatabase.ImportAsset(texPath);
            SetTextureImporter(texPath, false);
            diffuse = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        }
        // Do not SetPixels on imported textures (not readable) — edit colors via PNG assets if needed

        Texture2D mask = AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath);
        if (mask == null)
        {
            var tempMask = MakeMaskTexture(smoothness);
            WritePng(tempMask, maskPath);
            DestroyImmediate(tempMask);
            AssetDatabase.ImportAsset(maskPath);
            SetTextureImporter(maskPath, true);
            mask = AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath);
        }

        TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
        if (layer == null)
        {
            layer = new TerrainLayer();
            AssetDatabase.CreateAsset(layer, layerPath);
        }

        layer.diffuseTexture = diffuse;
        layer.maskMapTexture = mask;
        layer.tileSize = new Vector2(12f, 12f);
        layer.metallic = 0f;
        layer.smoothness = smoothness;
        EditorUtility.SetDirty(layer);
        AssetDatabase.SaveAssets();
        return layer;
#else
        // Build / player: runtime-only layers
        var runtime = new TerrainLayer
        {
            diffuseTexture = MakeSolidTexture(color, 8),
            maskMapTexture = MakeMaskTexture(smoothness),
            tileSize = new Vector2(12f, 12f),
            metallic = 0f,
            smoothness = smoothness,
            name = layerName
        };
        return runtime;
#endif
    }

#if UNITY_EDITOR
    static void SetTextureImporter(string path, bool isMask)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        importer.sRGBTexture = !isMask;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    static void WritePng(Texture2D tex, string path)
    {
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
    }
#endif

    static Texture2D MakeSolidTexture(Color color, int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear,
            name = "BiomeDiffuse"
        };
        FillTexture(tex, color);
        return tex;
    }

    static Texture2D MakeMaskTexture(float smoothness)
    {
        // R=metallic, G=AO, B=height, A=smoothness (URP Terrain Lit)
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear,
            name = "BiomeMask"
        };
        FillTexture(tex, new Color(0f, 1f, 0f, Mathf.Clamp01(smoothness)));
        return tex;
    }

    static void FillTexture(Texture2D tex, Color color)
    {
        var pixels = new Color[tex.width * tex.height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply();
    }
}
