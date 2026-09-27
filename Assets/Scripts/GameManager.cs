using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Seed / regenerate flow and player spawn on terrain.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Systems")]
    [SerializeField] TerrainGenerator terrainGenerator;
    [SerializeField] PlayerController player;

    [Header("Seed")]
    [SerializeField] bool randomizeSeedOnStart = true;

    [Header("UI")]
    [SerializeField] TMP_InputField seedInput;
    [SerializeField] Button randomButton;
    [SerializeField] Button regenerateButton;

    void Start()
    {
        if (randomButton != null)
            randomButton.onClick.AddListener(OnRandomClicked);
        if (regenerateButton != null)
            regenerateButton.onClick.AddListener(OnApplyClicked);

        if (randomizeSeedOnStart)
            terrainGenerator.SetSeed(Random.Range(0, 99999));

        GenerateLevel();
    }

    void Update()
    {
        bool typing = seedInput != null && seedInput.isFocused;
        if (!typing && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            OnRandomClicked();
    }

    public void OnApplyClicked()
    {
        if (seedInput != null && int.TryParse(seedInput.text, out int seed))
            terrainGenerator.SetSeed(seed);
        else
            terrainGenerator.SetSeed(Random.Range(0, 99999));

        RestartRun();
    }

    public void OnRandomClicked()
    {
        terrainGenerator.SetSeed(Random.Range(0, 99999));
        RestartRun();
    }

    void RestartRun()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        GenerateLevel();
    }

    void GenerateLevel()
    {
        if (seedInput != null)
            seedInput.text = terrainGenerator.Seed.ToString();

        terrainGenerator.Generate();

        if (player != null)
            StartCoroutine(SpawnPlayerNextFrame());
    }

    System.Collections.IEnumerator SpawnPlayerNextFrame()
    {
        yield return null;
        yield return new WaitForFixedUpdate();
        Physics.SyncTransforms();

        Vector3 spawn = terrainGenerator.GetSpawnPosition();
        player.Teleport(spawn);

        if (Physics.Raycast(spawn + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 200f)
            && hit.collider is TerrainCollider)
        {
            player.Teleport(hit.point + Vector3.up * 1.1f);
        }
    }
}
