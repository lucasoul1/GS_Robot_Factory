using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

/// <summary>
/// Cria a quinta fase com três faixas horizontais para itens diferentes.
/// </summary>
public static class Level05Setup
{
    private const string FourthLevelPath = "Assets/Scenes/FactoryLevel04.unity";
    private const string FifthLevelPath = "Assets/Scenes/FactoryLevel05.unity";
    private const string MarkerPath = "Assets/Editor/Level05Setup.marker";
    private const string BeltTilePath = "Assets/Art/Tiles/Placeholders/Conveyor_Belt.asset";
    private const string PlainTilePath = "Assets/Art/Tiles/Placeholders/Floor_Plain.asset";
    private const string GrateTilePath = "Assets/Art/Tiles/Placeholders/Floor_Grate.asset";
    private const string HazardTilePath = "Assets/Art/Tiles/Placeholders/Floor_Hazard.asset";
    private const string TrashSpritePath = "Assets/Art/Sprites/Placeholders/Trash_Bin.png";

    [InitializeOnLoadMethod]
    private static void RunWhenRequested()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(MarkerPath))
                return;

            if (BuildLevelFive())
            {
                File.Delete(MarkerPath);
                AssetDatabase.Refresh();
            }
        };
    }

    [MenuItem("Tools/GS Robot Factory/Create Level 05 and Update Menu")]
    public static void CreateLevelFive()
    {
        BuildLevelFive();
    }

    private static bool BuildLevelFive()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(FourthLevelPath) == null)
        {
            Debug.LogError("FactoryLevel04.unity não foi encontrada.");
            return false;
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(FifthLevelPath) != null)
        {
            Debug.LogError("FactoryLevel05.unity já existe. A cena não foi sobrescrita para preservar alterações manuais.");
            return false;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return false;

        if (!AssetDatabase.CopyAsset(FourthLevelPath, FifthLevelPath))
        {
            Debug.LogError("Não foi possível copiar FactoryLevel04 para criar FactoryLevel05.");
            return false;
        }

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Scene scene = EditorSceneManager.OpenScene(FifthLevelPath, OpenSceneMode.Single);
        if (!ConfigureLevelFive())
        {
            AssetDatabase.DeleteAsset(FifthLevelPath);
            return false;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        ConfigureFourthLevelNextButton();
        MainMenuSceneSetup.RefreshLevelSelection();
        EditorSceneManager.OpenScene(FifthLevelPath, OpenSceneMode.Single);
        AssetDatabase.SaveAssets();
        Debug.Log("FactoryLevel05 criada com três esteiras horizontais e meta de 60 pontos.");
        return true;
    }

    private static bool ConfigureLevelFive()
    {
        Grid grid = GameObject.Find("Grid")?.GetComponent<Grid>();
        Transform topConveyor = grid != null ? grid.transform.Find("Conveyor_Top") : null;
        Transform middleConveyor = grid != null ? grid.transform.Find("Conveyor_Left") : null;
        Transform bottomConveyor = grid != null ? grid.transform.Find("Conveyor_Right") : null;
        ConveyorSpawner topSpawner = topConveyor != null ? topConveyor.GetComponent<ConveyorSpawner>() : null;
        ConveyorSpawner middleSpawner = middleConveyor != null ? middleConveyor.GetComponent<ConveyorSpawner>() : null;
        ConveyorSpawner bottomSpawner = bottomConveyor != null ? bottomConveyor.GetComponent<ConveyorSpawner>() : null;
        GameObject robot = GameObject.Find("Robot_Test");
        GameObject circuitStation = GameObject.Find("Circuit Station");
        GameObject batteryStation = GameObject.Find("Battery Station");
        GameObject metalStation = GameObject.Find("Metal Station");
        GameObject manualTrash = GameObject.Find("Trash_Bin");
        GameSession session = Object.FindAnyObjectByType<GameSession>();
        ProductivityManager productivity = Object.FindAnyObjectByType<ProductivityManager>();
        Tilemap ground = FindTilemap(grid, "Ground");
        Tilemap decorations = FindTilemap(grid, "Decorations");
        Tile beltTile = AssetDatabase.LoadAssetAtPath<Tile>(BeltTilePath);
        Tile plainTile = AssetDatabase.LoadAssetAtPath<Tile>(PlainTilePath);
        Tile grateTile = AssetDatabase.LoadAssetAtPath<Tile>(GrateTilePath);
        Tile hazardTile = AssetDatabase.LoadAssetAtPath<Tile>(HazardTilePath);
        CollectibleItem itemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/Items/CollectibleItem.prefab")?.GetComponent<CollectibleItem>();
        ItemData circuitData = LoadItemData("CircuitBoardData");
        ItemData batteryData = LoadItemData("BatteryData");
        ItemData metalData = LoadItemData("MetalComponentData");

        if (grid == null || topConveyor == null || middleConveyor == null || bottomConveyor == null
            || topSpawner == null || middleSpawner == null || bottomSpawner == null || robot == null
            || circuitStation == null || batteryStation == null || metalStation == null || manualTrash == null
            || session == null || productivity == null || ground == null || decorations == null
            || beltTile == null || plainTile == null || grateTile == null || hazardTile == null
            || itemPrefab == null || circuitData == null || batteryData == null || metalData == null)
        {
            Debug.LogError("A cópia da Fase 4 não contém todos os objetos e assets necessários. A Fase 4 permanece intacta.");
            return false;
        }

        // Coloca duas estações no vão superior e uma no vão inferior entre as faixas.
        SetCellPosition(grid, circuitStation.transform, new Vector3Int(-3, 2, 0));
        SetCellPosition(grid, batteryStation.transform, new Vector3Int(3, 2, 0));
        SetCellPosition(grid, metalStation.transform, new Vector3Int(0, -2, 0));
        SetCellPosition(grid, robot.transform, new Vector3Int(0, -5, 0));
        SetCellPosition(grid, manualTrash.transform, new Vector3Int(3, -5, 0));

        SerializedObject sessionData = new SerializedObject(session);
        sessionData.FindProperty("timeLimit").floatValue = 80f;
        sessionData.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(session);

        SerializedObject productivityData = new SerializedObject(productivity);
        productivityData.FindProperty("productivityGoal").intValue = 60;
        SerializedProperty multiplier = productivityData.FindProperty("pointsMultiplier");
        if (multiplier != null)
            multiplier.intValue = 1;
        productivityData.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(productivity);

        ConfigureHorizontalLane(grid, topConveyor, topSpawner, beltTile, itemPrefab,
            new[] { circuitData }, new Vector3Int(5, 4, 0), new Vector3Int(-6, 4, 0), 2,
            -5, 5, 4, 180f, "Conveyor_Top");
        ConfigureHorizontalLane(grid, middleConveyor, middleSpawner, beltTile, itemPrefab,
            new[] { batteryData }, new Vector3Int(-5, 0, 0), new Vector3Int(6, 0, 0), 0,
            -5, 5, 0, 0f, "Conveyor_Middle");
        ConfigureHorizontalLane(grid, bottomConveyor, bottomSpawner, beltTile, itemPrefab,
            new[] { metalData }, new Vector3Int(5, -4, 0), new Vector3Int(-6, -4, 0), 2,
            -5, 5, -4, 180f, "Conveyor_Bottom");

        ClearLaneDecorations(decorations);
        PaintStationApproaches(ground, plainTile, grateTile, hazardTile);
        UpdateHudDefaults(80f, 60);
        return true;
    }

    private static void ConfigureHorizontalLane(
        Grid grid,
        Transform conveyor,
        ConveyorSpawner spawner,
        Tile beltTile,
        CollectibleItem itemPrefab,
        ItemData[] itemOptions,
        Vector3Int spawnCell,
        Vector3Int trashCell,
        int flowDirection,
        int minX,
        int maxX,
        int y,
        float tileRotation,
        string objectName)
    {
        conveyor.name = objectName;
        Tilemap tilemap = conveyor.GetComponent<Tilemap>();
        tilemap.ClearAllTiles();
        PaintHorizontalLane(tilemap, beltTile, minX, maxX, y, tileRotation);
        ConfigureSpawnAndTrash(grid, conveyor, spawnCell, trashCell, out Transform spawnPoint);
        ConfigureSpawner(spawner, itemPrefab, itemOptions, spawnPoint, flowDirection);
        conveyor.GetComponent<TilemapRenderer>().sortingOrder = 2;
    }

    private static void PaintHorizontalLane(Tilemap tilemap, Tile beltTile, int minX, int maxX, int y, float rotation)
    {
        for (int x = minX; x <= maxX; x++)
        {
            Vector3Int cell = new Vector3Int(x, y, 0);
            tilemap.SetTile(cell, beltTile);
            tilemap.SetTileFlags(cell, TileFlags.None);
            tilemap.SetTransformMatrix(cell, Matrix4x4.TRS(
                Vector3.zero,
                Quaternion.Euler(0f, 0f, rotation),
                Vector3.one));
        }
    }

    private static void ConfigureSpawnAndTrash(
        Grid grid,
        Transform conveyor,
        Vector3Int spawnCell,
        Vector3Int trashCell,
        out Transform spawnPoint)
    {
        Transform existingSpawn = conveyor.Find("SpawnPoint");
        if (existingSpawn == null)
        {
            GameObject spawnObject = new GameObject("SpawnPoint");
            existingSpawn = spawnObject.transform;
            existingSpawn.SetParent(conveyor, false);
        }

        spawnPoint = existingSpawn;
        spawnPoint.position = grid.GetCellCenterWorld(spawnCell);

        Transform trash = conveyor.Find("ConveyorTrash");
        if (trash == null)
        {
            GameObject trashObject = new GameObject("ConveyorTrash");
            trash = trashObject.transform;
            trash.SetParent(conveyor, false);
        }

        trash.position = grid.GetCellCenterWorld(trashCell);
        SpriteRenderer renderer = trash.GetComponent<SpriteRenderer>();
        if (renderer == null)
            renderer = trash.gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(TrashSpritePath);
        renderer.sortingOrder = 4;

        BoxCollider2D collider = trash.GetComponent<BoxCollider2D>();
        if (collider == null)
            collider = trash.gameObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.offset = Vector2.zero;
        collider.size = Vector2.one;

        if (trash.GetComponent<ConveyorTrash>() == null)
            trash.gameObject.AddComponent<ConveyorTrash>();
    }

    private static void ConfigureSpawner(
        ConveyorSpawner spawner,
        CollectibleItem itemPrefab,
        ItemData[] itemOptions,
        Transform spawnPoint,
        int flowDirection)
    {
        SerializedObject data = new SerializedObject(spawner);
        data.FindProperty("collectiblePrefab").objectReferenceValue = itemPrefab;
        data.FindProperty("spawnInterval").floatValue = 2f;
        data.FindProperty("movementSpeed").floatValue = 2f;
        data.FindProperty("flowDirection").enumValueIndex = flowDirection;
        data.FindProperty("spawnPoint").objectReferenceValue = spawnPoint;
        data.FindProperty("generationEnabled").boolValue = true;
        SerializedProperty options = data.FindProperty("itemOptions");
        options.arraySize = itemOptions.Length;
        for (int i = 0; i < itemOptions.Length; i++)
            options.GetArrayElementAtIndex(i).objectReferenceValue = itemOptions[i];
        data.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(spawner);
    }

    private static void ClearLaneDecorations(Tilemap decorations)
    {
        // Retira somente decorações nas três fileiras ocupadas pelas esteiras.
        int[] laneRows = { 4, 0, -4 };
        foreach (int row in laneRows)
        {
            for (int x = -5; x <= 5; x++)
                decorations.SetTile(new Vector3Int(x, row, 0), null);
        }
    }

    private static void PaintStationApproaches(Tilemap ground, Tile plain, Tile grate, Tile hazard)
    {
        // Limpa as marcações da Fase 4 e identifica as células de acesso das três máquinas.
        for (int x = -4; x <= 4; x++)
        {
            ground.SetTile(new Vector3Int(x, 0, 0), plain);
            ground.SetTile(new Vector3Int(x, 1, 0), plain);
            ground.SetTile(new Vector3Int(x, -3, 0), plain);
        }

        MarkApproach(ground, grate, hazard, new Vector3Int(-3, 1, 0));
        MarkApproach(ground, grate, hazard, new Vector3Int(3, 1, 0));
        MarkApproach(ground, grate, hazard, new Vector3Int(0, -3, 0));
    }

    private static void MarkApproach(Tilemap ground, Tile grate, Tile hazard, Vector3Int center)
    {
        for (int x = center.x - 1; x <= center.x + 1; x++)
            ground.SetTile(new Vector3Int(x, center.y, 0), grate);
        ground.SetTile(center, hazard);
    }

    private static void UpdateHudDefaults(float timeLimit, int productivityGoal)
    {
        Text[] texts = Object.FindObjectsByType<Text>(FindObjectsInactive.Include);
        Text timer = texts.FirstOrDefault(text => text.name == "Timer Text");
        Text productivity = texts.FirstOrDefault(text => text.name == "Productivity Text");
        if (timer != null)
        {
            int seconds = Mathf.CeilToInt(timeLimit);
            timer.text = $"TIME: {seconds / 60:00}:{seconds % 60:00}";
            EditorUtility.SetDirty(timer);
        }

        if (productivity != null)
        {
            productivity.text = $"PRODUCTIVITY: 0 / {productivityGoal}";
            EditorUtility.SetDirty(productivity);
        }
    }

    private static void ConfigureFourthLevelNextButton()
    {
        Scene scene = EditorSceneManager.OpenScene(FourthLevelPath, OpenSceneMode.Single);
        ResultScreenController controller = Object.FindAnyObjectByType<ResultScreenController>();
        if (controller == null)
        {
            Debug.LogWarning("ResultScreenController não foi encontrado na Fase 4; NEXT LEVEL não foi configurado.");
            return;
        }

        SerializedObject data = new SerializedObject(controller);
        data.FindProperty("nextLevelScene").stringValue = "FactoryLevel05";
        data.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static Tilemap FindTilemap(Grid grid, string tilemapName)
    {
        if (grid == null)
            return null;

        return grid.GetComponentsInChildren<Tilemap>(true)
            .FirstOrDefault(tilemap => tilemap.name == tilemapName);
    }

    private static ItemData LoadItemData(string assetName)
    {
        return AssetDatabase.LoadAssetAtPath<ItemData>($"Assets/Items/Data/{assetName}.asset");
    }

    private static void SetCellPosition(Grid grid, Transform target, Vector3Int cell)
    {
        target.position = grid.GetCellCenterWorld(cell);
    }
}
