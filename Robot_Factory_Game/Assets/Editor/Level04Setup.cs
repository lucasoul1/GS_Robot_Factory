using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

/// <summary>
/// Cria a quarta fase com uma esteira dedicada para cada tipo de peça.
/// </summary>
public static class Level04Setup
{
    private const string ThirdLevelPath = "Assets/Scenes/FactoryLevel03.unity";
    private const string FourthLevelPath = "Assets/Scenes/FactoryLevel04.unity";
    private const string MarkerPath = "Assets/Editor/Level04Setup.marker";
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

            if (BuildLevelFour())
            {
                File.Delete(MarkerPath);
                AssetDatabase.Refresh();
            }
        };
    }

    [MenuItem("Tools/GS Robot Factory/Create Level 04 and Update Menu")]
    public static void CreateLevelFour()
    {
        BuildLevelFour();
    }

    private static bool BuildLevelFour()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ThirdLevelPath) == null)
        {
            Debug.LogError("FactoryLevel03.unity não foi encontrada.");
            return false;
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(FourthLevelPath) != null)
        {
            Debug.LogError("FactoryLevel04.unity já existe. A cena não foi sobrescrita para preservar alterações manuais.");
            return false;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return false;

        if (!AssetDatabase.CopyAsset(ThirdLevelPath, FourthLevelPath))
        {
            Debug.LogError("Não foi possível copiar FactoryLevel03 para criar FactoryLevel04.");
            return false;
        }

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Scene scene = EditorSceneManager.OpenScene(FourthLevelPath, OpenSceneMode.Single);
        if (!ConfigureLevelFour())
        {
            AssetDatabase.DeleteAsset(FourthLevelPath);
            return false;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        ConfigureThirdLevelNextButton();
        MainMenuSceneSetup.RefreshLevelSelection();
        EditorSceneManager.OpenScene(FourthLevelPath, OpenSceneMode.Single);
        AssetDatabase.SaveAssets();
        Debug.Log("FactoryLevel04 criada com três esteiras dedicadas, três estações centrais e meta de 60 pontos.");
        return true;
    }

    private static bool ConfigureLevelFour()
    {
        Grid grid = GameObject.Find("Grid")?.GetComponent<Grid>();
        Transform leftConveyor = grid != null ? grid.transform.Find("Conveyor_Left") : null;
        Transform rightConveyor = grid != null ? grid.transform.Find("Conveyor_Right") : null;
        ConveyorSpawner leftSpawner = leftConveyor != null ? leftConveyor.GetComponent<ConveyorSpawner>() : null;
        ConveyorSpawner rightSpawner = rightConveyor != null ? rightConveyor.GetComponent<ConveyorSpawner>() : null;
        GameObject robot = GameObject.Find("Robot_Test");
        GameObject circuitStation = GameObject.Find("Circuit Station");
        GameObject batteryStation = GameObject.Find("Battery Station");
        GameObject metalStation = GameObject.Find("Metal Station");
        GameObject manualTrash = GameObject.Find("Trash_Bin");
        GameSession session = Object.FindFirstObjectByType<GameSession>();
        ProductivityManager productivity = Object.FindFirstObjectByType<ProductivityManager>();
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

        if (grid == null || leftSpawner == null || rightSpawner == null || robot == null
            || circuitStation == null || batteryStation == null || metalStation == null || manualTrash == null
            || session == null || productivity == null || ground == null || decorations == null
            || beltTile == null || plainTile == null || grateTile == null || hazardTile == null
            || itemPrefab == null || circuitData == null || batteryData == null || metalData == null)
        {
            Debug.LogError("A cópia da Fase 3 não contém todos os objetos e assets necessários. A Fase 3 permanece intacta.");
            return false;
        }

        // Coloca as três máquinas lado a lado no centro e deixa o robô abaixo da área de entrega.
        SetCellPosition(grid, circuitStation.transform, new Vector3Int(-3, 1, 0));
        SetCellPosition(grid, batteryStation.transform, new Vector3Int(0, 1, 0));
        SetCellPosition(grid, metalStation.transform, new Vector3Int(3, 1, 0));
        SetCellPosition(grid, robot.transform, new Vector3Int(0, -4, 0));
        SetCellPosition(grid, manualTrash.transform, new Vector3Int(0, -5, 0));

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

        ConfigureLane(grid, leftConveyor, leftSpawner, beltTile, itemPrefab,
            new[] { circuitData }, new Vector3Int(-8, 3, 0), new Vector3Int(-8, -4, 0), 1,
            -8, -3, 3, -90f);
        ConfigureLane(grid, rightConveyor, rightSpawner, beltTile, itemPrefab,
            new[] { metalData }, new Vector3Int(8, -3, 0), new Vector3Int(8, 4, 0), 3,
            8, -3, 3, 90f);
        ConfigureTopLane(grid, beltTile, itemPrefab, batteryData);
        ClearLaneDecorations(decorations);
        PaintStationApproaches(ground, plainTile, grateTile, hazardTile);
        UpdateHudDefaults(80f, 60);
        return true;
    }

    private static void ConfigureLane(
        Grid grid,
        Transform conveyor,
        ConveyorSpawner spawner,
        Tile beltTile,
        CollectibleItem itemPrefab,
        ItemData[] itemOptions,
        Vector3Int spawnCell,
        Vector3Int trashCell,
        int flowDirection,
        int x,
        int minY,
        int maxY,
        float tileRotation)
    {
        Tilemap tilemap = conveyor.GetComponent<Tilemap>();
        tilemap.ClearAllTiles();
        PaintVerticalLane(tilemap, beltTile, x, minY, maxY, tileRotation);
        ConfigureSpawnAndTrash(grid, conveyor, spawnCell, trashCell, out Transform spawnPoint);
        ConfigureSpawner(spawner, itemPrefab, itemOptions, spawnPoint, flowDirection);
        conveyor.GetComponent<TilemapRenderer>().sortingOrder = 2;
    }

    private static void ConfigureTopLane(Grid grid, Tile beltTile, CollectibleItem itemPrefab, ItemData item)
    {
        // A esteira superior leva somente Battery até o descarte no extremo direito.
        GameObject conveyor = new GameObject("Conveyor_Top", typeof(Tilemap), typeof(TilemapRenderer));
        conveyor.transform.SetParent(grid.transform, false);
        Tilemap tilemap = conveyor.GetComponent<Tilemap>();
        conveyor.GetComponent<TilemapRenderer>().sortingOrder = 2;
        PaintHorizontalLane(tilemap, beltTile, -5, 5, 4, 0f);

        ConfigureSpawnAndTrash(grid, conveyor.transform,
            new Vector3Int(-5, 4, 0), new Vector3Int(6, 4, 0), out Transform spawnPoint);
        ConveyorSpawner spawner = conveyor.AddComponent<ConveyorSpawner>();
        ConfigureSpawner(spawner, itemPrefab, new[] { item }, spawnPoint, 0);
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

    private static void PaintVerticalLane(Tilemap tilemap, Tile beltTile, int x, int minY, int maxY, float rotation)
    {
        for (int y = minY; y <= maxY; y++)
            SetRotatedTile(tilemap, beltTile, new Vector3Int(x, y, 0), rotation);
    }

    private static void PaintHorizontalLane(Tilemap tilemap, Tile beltTile, int minX, int maxX, int y, float rotation)
    {
        for (int x = minX; x <= maxX; x++)
            SetRotatedTile(tilemap, beltTile, new Vector3Int(x, y, 0), rotation);
    }

    private static void SetRotatedTile(Tilemap tilemap, Tile tile, Vector3Int cell, float rotation)
    {
        tilemap.SetTile(cell, tile);
        tilemap.SetTileFlags(cell, TileFlags.None);
        tilemap.SetTransformMatrix(cell, Matrix4x4.TRS(
            Vector3.zero,
            Quaternion.Euler(0f, 0f, rotation),
            Vector3.one));
    }

    private static void ClearLaneDecorations(Tilemap decorations)
    {
        // Libera as células das duas faixas verticais e da faixa superior.
        for (int y = -3; y <= 3; y++)
        {
            decorations.SetTile(new Vector3Int(-8, y, 0), null);
            decorations.SetTile(new Vector3Int(8, y, 0), null);
        }

        for (int x = -5; x <= 5; x++)
            decorations.SetTile(new Vector3Int(x, 4, 0), null);
    }

    private static void PaintStationApproaches(Tilemap ground, Tile plain, Tile grate, Tile hazard)
    {
        // Remove as marcações da posição anterior e coloca os acessos na fileira abaixo das máquinas.
        for (int x = -4; x <= 4; x++)
            ground.SetTile(new Vector3Int(x, 1, 0), plain);

        foreach (int stationX in new[] { -3, 0, 3 })
        {
            for (int x = stationX - 1; x <= stationX + 1; x++)
                ground.SetTile(new Vector3Int(x, 0, 0), grate);
            ground.SetTile(new Vector3Int(stationX, 0, 0), hazard);
        }
    }

    private static void UpdateHudDefaults(float timeLimit, int productivityGoal)
    {
        Text[] texts = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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

    private static void ConfigureThirdLevelNextButton()
    {
        Scene scene = EditorSceneManager.OpenScene(ThirdLevelPath, OpenSceneMode.Single);
        ResultScreenController controller = Object.FindFirstObjectByType<ResultScreenController>();
        if (controller == null)
        {
            Debug.LogWarning("ResultScreenController não foi encontrado na Fase 3; NEXT LEVEL não foi configurado.");
            return;
        }

        SerializedObject data = new SerializedObject(controller);
        data.FindProperty("nextLevelScene").stringValue = "FactoryLevel04";
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
