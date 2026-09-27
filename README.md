# PCG_Terrain (PCG_map)

Unity project (Unity 6.3 LTS, URP): procedural open terrain with a Perlin/FBM heightmap.

Course: *Procedural Content Generation* (Innopolis, Fall 2026).

Repository: [github.com/prudenz1/PCG_Terrain](https://github.com/prudenz1/PCG_Terrain)

## Algorithm

**Perlin Noise + FBM (fractal Brownian motion)** → heightmap → Unity Terrain.

1. Build a `resolution × resolution` height grid.
2. For each (x, z): sum octaves of Perlin noise (frequency / persistence / lacunarity).
3. Normalize heights to `[0..1]`, scale by `heightScale`.
4. Apply via `TerrainData.SetHeights`.
5. Paint biome layers by height (water → sand → grass → rock → snow).

Same `seed` → same terrain.

## Scripts

| Script | Role |
|--------|------|
| `HeightMapData.cs` | Height grid, seed, sizes |
| `Noise.cs` | Perlin / FBM helpers |
| `TerrainGenerator.cs` | Noise → heightmap → Terrain |
| `TerrainBiomePainter.cs` | Alphamap layers by height |
| `PlayerController.cs` | Movement / camera on terrain |
| `GameManager.cs` | Seed, regenerate, player spawn |

## Unity

- Open this repo root in Unity 6.3 LTS.
- Scene: `Assets/Scenes/SampleScene.unity`
