using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Configura o gerador na esteira existente da cena de teste.
/// </summary>
public static class ConveyorSpawnerSetup
{
    private const string ScenePath = "Assets/Scenes/FactoryLevel01.unity";
    private const string MarkerPath = "Assets/Editor/ConveyorSpawnerSetup.marker";

    [InitializeOnLoadMethod]
    private static void RunWhenRequested()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(MarkerPath))
                return;

            if (ApplySetup())
            {
                File.Delete(MarkerPath);
                AssetDatabase.Refresh();
            }
        };
    }

    [MenuItem("Tools/GS Robot Factory/Configure Conveyor Spawner")]
    public static void ConfigureSpawner()
    {
        ApplySetup();
    }

    private static bool ApplySetup()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Grid grid = GameObject.Find("Grid")?.GetComponent<Grid>();
        Transform conveyor = grid != null ? grid.transform.Find("Conveyor") : null;
        Tilemap conveyorTilemap = conveyor != null ? conveyor.GetComponent<Tilemap>() : null;
        if (grid == null || conveyorTilemap == null)
        {
            Debug.LogError("Grid/Conveyor não encontrado em FactoryLevel01.");
            return false;
        }

        GameObject prefabObject = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/Items/CollectibleItem.prefab");
        CollectibleItem itemPrefab = prefabObject != null ? prefabObject.GetComponent<CollectibleItem>() : null;
        ItemData circuitData = LoadItemData("CircuitBoardData");
        ItemData batteryData = LoadItemData("BatteryData");
        ItemData metalData = LoadItemData("MetalComponentData");
        if (itemPrefab == null || circuitData == null || batteryData == null || metalData == null)
        {
            Debug.LogError("Prefab coletável e os três ItemData precisam existir antes de configurar a esteira.");
            return false;
        }

        Transform spawnPoint = conveyor.Find("SpawnPoint");
        if (spawnPoint == null)
        {
            spawnPoint = conveyor.Find("OutputPoint");
            if (spawnPoint == null)
            {
                GameObject pointObject = new GameObject("SpawnPoint");
                spawnPoint = pointObject.transform;
                spawnPoint.SetParent(conveyor, false);
            }
            else
            {
                spawnPoint.name = "SpawnPoint";
            }
        }

        // O ponto de geração fica no centro da primeira célula da faixa.
        spawnPoint.position = grid.GetCellCenterWorld(new Vector3Int(-2, 0, 0));

        Transform trashTransform = conveyor.Find("ConveyorTrash");
        GameObject trashObject;
        if (trashTransform == null)
        {
            trashObject = new GameObject("ConveyorTrash");
            trashTransform = trashObject.transform;
            trashTransform.SetParent(conveyor, false);
        }
        else
        {
            trashObject = trashTransform.gameObject;
        }

        trashTransform.position = grid.GetCellCenterWorld(new Vector3Int(4, 0, 0));
        SpriteRenderer trashRenderer = trashObject.GetComponent<SpriteRenderer>();
        if (trashRenderer == null)
            trashRenderer = trashObject.AddComponent<SpriteRenderer>();
        trashRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/Art/Sprites/Placeholders/Trash_Bin.png");
        trashRenderer.sortingOrder = 4;

        BoxCollider2D trashCollider = trashObject.GetComponent<BoxCollider2D>();
        if (trashCollider == null)
            trashCollider = trashObject.AddComponent<BoxCollider2D>();
        trashCollider.isTrigger = true;
        trashCollider.offset = Vector2.zero;
        trashCollider.size = Vector2.one;

        if (trashObject.GetComponent<ConveyorTrash>() == null)
            trashObject.AddComponent<ConveyorTrash>();

        ConveyorSpawner spawner = conveyor.GetComponent<ConveyorSpawner>();
        if (spawner == null)
            spawner = conveyor.gameObject.AddComponent<ConveyorSpawner>();

        SerializedObject serializedSpawner = new SerializedObject(spawner);
        serializedSpawner.FindProperty("collectiblePrefab").objectReferenceValue = itemPrefab;
        serializedSpawner.FindProperty("spawnInterval").floatValue = 2f;
        serializedSpawner.FindProperty("movementSpeed").floatValue = 2f;
        serializedSpawner.FindProperty("spawnPoint").objectReferenceValue = spawnPoint;
        serializedSpawner.FindProperty("generationEnabled").boolValue = true;

        SerializedProperty itemOptions = serializedSpawner.FindProperty("itemOptions");
        itemOptions.arraySize = 3;
        itemOptions.GetArrayElementAtIndex(0).objectReferenceValue = circuitData;
        itemOptions.GetArrayElementAtIndex(1).objectReferenceValue = batteryData;
        itemOptions.GetArrayElementAtIndex(2).objectReferenceValue = metalData;
        serializedSpawner.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("ConveyorSpawner configurado; entrada em (-2, 0), descarte em (4, 0) e geração em sequência.");
        return true;
    }

    private static ItemData LoadItemData(string assetName)
    {
        return AssetDatabase.LoadAssetAtPath<ItemData>($"Assets/Items/Data/{assetName}.asset");
    }
}
