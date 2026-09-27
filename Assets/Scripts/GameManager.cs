using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Seed / regenerate, player spawn, collectibles, score + countdown timer.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Systems")]
    [SerializeField] TerrainGenerator terrainGenerator;
    [SerializeField] CollectibleSpawner collectibleSpawner;
    [SerializeField] PlayerController player;

    [Header("Seed")]
    [SerializeField] bool randomizeSeedOnStart = true;

    [Header("Timer")]
    [Tooltip("Seconds the player has to collect cubes each run.")]
    [SerializeField] [Min(1f)] float timeLimitSeconds = 120f;

    [Header("UI")]
    [SerializeField] TMP_InputField seedInput;
    [SerializeField] Button randomButton;
    [SerializeField] Button regenerateButton;
    [SerializeField] TextMeshProUGUI scoreText;
    [SerializeField] TextMeshProUGUI timerText;

    public int CollectedCount { get; private set; }

    float timeLeft;
    bool timeUp;
    bool runActive;

    void Awake() => Instance = this;

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

        TickTimer();
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

    public void OnCollectiblePicked()
    {
        if (timeUp)
            return;

        CollectedCount++;
        UpdateScoreUI();
    }

    void RestartRun()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (player != null)
            player.enabled = true;

        GenerateLevel();
    }

    void GenerateLevel()
    {
        if (seedInput != null)
            seedInput.text = terrainGenerator.Seed.ToString();

        CollectedCount = 0;
        timeLeft = timeLimitSeconds;
        timeUp = false;
        runActive = true;

        terrainGenerator.Generate();

        if (collectibleSpawner != null)
            collectibleSpawner.Spawn(terrainGenerator);

        UpdateScoreUI();
        UpdateTimerUI();

        if (player != null)
            StartCoroutine(SpawnPlayerNextFrame());
    }

    void TickTimer()
    {
        if (!runActive || timeUp)
            return;

        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f)
        {
            timeLeft = 0f;
            timeUp = true;
            OnTimeUp();
        }

        UpdateTimerUI();
    }

    void OnTimeUp()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (player != null)
            player.enabled = false;

        UpdateTimerUI();
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = $"Cubes: {CollectedCount}";
    }

    void UpdateTimerUI()
    {
        if (timerText == null)
            return;

        if (timeUp)
        {
            timerText.text = "Time's up!";
            return;
        }

        int total = Mathf.CeilToInt(timeLeft);
        int minutes = total / 60;
        int seconds = total % 60;
        timerText.text = $"{minutes:00}:{seconds:00}";
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
