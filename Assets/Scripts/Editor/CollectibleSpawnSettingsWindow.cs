using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor window: Menu → PCG Map → Collectible Spawn Settings
/// </summary>
public class CollectibleSpawnSettingsWindow : EditorWindow
{
    CollectibleSpawner spawner;
    SerializedObject serializedSpawner;

    [MenuItem("PCG Map/Collectible Spawn Settings")]
    static void Open()
    {
        var window = GetWindow<CollectibleSpawnSettingsWindow>("Collectible Spawn");
        window.minSize = new Vector2(280f, 120f);
        window.RefreshTarget();
        window.Show();
    }

    void OnEnable() => RefreshTarget();

    void OnFocus() => RefreshTarget();

    void RefreshTarget()
    {
        spawner = Object.FindFirstObjectByType<CollectibleSpawner>();
        serializedSpawner = spawner != null ? new SerializedObject(spawner) : null;
    }

    void OnGUI()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Collectible Spawn", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        if (spawner == null || serializedSpawner == null)
        {
            EditorGUILayout.HelpBox(
                "CollectibleSpawner not found in the open scene.\nOpen SampleScene (Systems → Collectibles).",
                MessageType.Warning);
            if (GUILayout.Button("Refresh"))
                RefreshTarget();
            return;
        }

        serializedSpawner.Update();

        EditorGUILayout.ObjectField("Spawner", spawner, typeof(CollectibleSpawner), true);

        SerializedProperty countProp = serializedSpawner.FindProperty("count");
        EditorGUILayout.PropertyField(countProp, new GUIContent("Spawn Count", "Number of yellow cubes to spawn"));
        countProp.intValue = Mathf.Max(1, countProp.intValue);

        serializedSpawner.ApplyModifiedProperties();

        EditorGUILayout.Space(8f);
        EditorGUILayout.HelpBox("Applied on Play / Regenerate. Same seed → same positions.", MessageType.Info);
    }
}
