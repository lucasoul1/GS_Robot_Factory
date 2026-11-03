using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

/// <summary>
/// Cria a terceira fase com duas esteiras verticais e libera sua seleção no menu.
/// </summary>
public static class Level03Setup
{
    private const string SecondLevelPath = "Assets/Scenes/FactoryLevel02.unity";
    private const string ThirdLevelPath = "Assets/Scenes/FactoryLevel03.unity";
    private const string MarkerPath = "Assets/Editor/Level03Setup.marker";
    private const string RepairMarkerPath = "Assets/Editor/Level03Repair.marker";
    private const string GoalUpdateMarkerPath = "Assets/Editor/Level03GoalUpdate.marker";
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
            if (File.Exists(GoalUpdateMarkerPath))
            {
                if (UpdateLevelThreeProductivityGoal())
                {
                    File.Delete(GoalUpdateMarkerPath);
                    AssetDatabase.Refresh();
                }

                return;
            }

            if (File.Exists(RepairMarkerPath))
            {
                if (RepairExistingLevelThree())
                {
                    File.Delete(RepairMarkerPath);
                    AssetDatabase.Refresh();
                }

                return;
            }

            if (!File.Exists(MarkerPath))
                return;

            if (BuildLevelThree())
            {
                File.Delete(MarkerPath);
                AssetDatabase.Refresh();
            }
        };
    }

    [MenuItem("Tools/GS Robot Factory/Create Level 03 and Update Menu")]
    public static void CreateLevelThree()
    {
        BuildLevelThree();
    }

    [MenuItem("Tools/GS Robot Factory/Repair Level 03 Conveyor Layout")]
    public static void RepairLevelThree()
    {
        RepairExistingLevelThree();
    }

    [MenuItem("Tools/GS Robot Factory/Set Level 03 Productivity Goal to 60")]
    public static void SetLevelThreeProductivityGoal()
    {
        UpdateLevelThreeProductivityGoal();
    }

    private static bool UpdateLevelThreeProductivityGoal()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ThirdLevelPath) == null)
        {
            Debug.LogError("FactoryLevel03.unity não foi encontrada.");
            return false;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return false;

        Scene scene = EditorSceneManager.OpenScene(ThirdLevelPath, OpenSceneMode.Single);
        ProductivityManager productivity = Object.FindFirstObjectByType<ProductivityManager>();
        if (productivity == null)
        {
            Debug.LogError("ProductivityManager não foi encontrado na Fase 3.");
            return false;
        }

        SerializedObject productivityData = new SerializedObject(productivity);
        productivityData.FindProperty("productivityGoal").intValue = 60;
        productivityData.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(productivity);
        UpdateHudDefaults(80f, 60);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Meta de produtividade da Fase 3 atualizada para 60. Pontuação por item preservada.");
        return true;
    }

    private static bool RepairExistingLevelThree()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ThirdLevelPath) == null)
        {
            Debug.LogError("FactoryLevel03.unity não foi encontrada.");
            return false;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return false;

        Scene scene = EditorSceneManager.OpenScene(ThirdLevelPath, OpenSceneMode.Single);
        Grid grid = GameObject.Find("Grid")?.GetComponent<Grid>();
        Transform rightConveyor = grid != null ? grid.transform.Find("Conveyor_Right") : null;
        ConveyorSpawner rightSpawner = rightConveyor != null ? rightConveyor.GetComponent<ConveyorSpawner>() : null;
        Tilemap rightTilemap = rightConveyor != null ? rightConveyor.GetComponent<Tilemap>() : null;
        Tilemap decorations = grid != null
            ? grid.GetComponentsInChildren<Tilemap>(true).FirstOrDefault(tilemap => tilemap.name == "Decorations")
            : null;
        Tile beltTile = AssetDatabase.LoadAssetAtPath<Tile>(BeltTilePath);

        if (grid == null || rightSpawner == null || rightTilemap == null || decorations == null || beltTile == null)
        {
            Debug.LogError("A esteira direita, o Grid ou o Tilemap Decorations não foi encontrado na Fase 3.");
            return false;
        }

        // Alinha o ponto de geração ao início inferior da faixa direita.
        SerializedObject spawnerData = new SerializedObject(rightSpawner);
        Transform spawnPoint = spawnerData.FindProperty("spawnPoint").objectReferenceValue as Transform;
        if (spawnPoint == null)
        {
            Debug.LogError("O ponto de geração da esteira direita não está configurado.");
            return false;
        }

        spawnPoint.position = grid.GetCellCenterWorld(new Vector3Int(8, -3, 0));
        PaintVerticalLane(rightTilemap, beltTile, 8, -3, 3, 90f);
        rightConveyor.GetComponent<TilemapRenderer>().sortingOrder = 2;

        // Remove apenas os tiles decorativos que estavam cobrindo as duas esteiras.
        for (int y = -3; y <= 3; y++)
        {
            decorations.SetTile(new Vector3Int(-8, y, 0), null);
            decorations.SetTile(new Vector3Int(8, y, 0), null);
        }

        EditorUtility.SetDirty(spawnPoint);
        EditorUtility.SetDirty(decorations);
        EditorUtility.SetDirty(rightTilemap);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Fase 3 corrigida: spawn direito alinhado à esteira e decoração removida das duas faixas.");
        return true;
    }

    private static bool BuildLevelThree()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SecondLevelPath) == null)
        {
            Debug.LogError("FactoryLevel02.unity não foi encontrada.");
            return false;
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ThirdLevelPath) != null)
        {
            Debug.LogError("FactoryLevel03.unity já existe. A cena não foi sobrescrita para preservar alterações manuais.");
            return false;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return false;

        if (!AssetDatabase.CopyAsset(SecondLevelPath, ThirdLevelPath))
        {
            Debug.LogError("Não foi possível copiar FactoryLevel02 para criar FactoryLevel03.");
            return false;
        }

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Scene scene = EditorSceneManager.OpenScene(ThirdLevelPath, OpenSceneMode.Single);
        if (!ConfigureLevel())
        {
            AssetDatabase.DeleteAsset(ThirdLevelPath);
            return false;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        ConfigureSecondLevelNextButton();
        MainMenuSceneSetup.RefreshLevelSelection();
        EditorSceneManager.OpenScene(ThirdLevelPath, OpenSceneMode.Single);
        AssetDatabase.SaveAssets();
        Debug.Log("FactoryLevel03 criada com duas esteiras verticais, fluxo separado de itens e tempo de 1:20.");
        return true;
    }

    private static bool ConfigureLevel()
    {
        Grid grid = GameObject.Find("Grid")?.GetComponent<Grid>();
        GameObject robot = GameObject.Find("Robot_Test");
        GameObject circuitStation = GameObject.Find("Circuit Station");
        GameObject batteryStation = GameObject.Find("Battery Station");
        GameObject metalStation = GameObject.Find("Metal Station");
        GameObject trashBin = GameObject.Find("Trash_Bin");
        GameSession session = Object.FindFirstObjectByType<GameSession>();
        ProductivityManager productivity = Object.FindFirstObjectByType<ProductivityManager>();
        ConveyorSpawner originalSpawner = GameObject.Find("Conveyor")?.GetComponent<ConveyorSpawner>();
        Tilemap ground = grid != null
            ? grid.GetComponentsInChildren<Tilemap>(true).FirstOrDefault(tilemap => tilemap.name == "Ground")
            : null;

        Tile beltTile = AssetDatabase.LoadAssetAtPath<Tile>(BeltTilePath);
        Tile plainTile = AssetDatabase.LoadAssetAtPath<Tile>(PlainTilePath);
        Tile grateTile = AssetDatabase.LoadAssetAtPath<Tile>(GrateTilePath);
        Tile hazardTile = AssetDatabase.LoadAssetAtPath<Tile>(HazardTilePath);
        CollectibleItem itemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/Items/CollectibleItem.prefab")?.GetComponent<CollectibleItem>();
        ItemData circuitData = LoadItemData("CircuitBoardData");
        ItemData batteryData = LoadItemData("BatteryData");
        ItemData metalData = LoadItemData("MetalComponentData");

        if (grid == null || robot == null || circuitStation == null || batteryStation == null
            || metalStation == null || trashBin == null || session == null || productivity == null
            || originalSpawner == null || ground == null || beltTile == null || plainTile == null
            || grateTile == null || hazardTile == null || itemPrefab == null
            || circuitData == null || batteryData == null || metalData == null)
        {
            Debug.LogError("A cópia da Fase 2 não contém todos os objetos e assets necessários. A Fase 2 permanece intacta.");
            return false;
        }

        SetCellPosition(grid, robot.transform, new Vector3Int(0, -4, 0));
        SetCellPosition(grid, circuitStation.transform, new Vector3Int(-3, 2, 0));
        SetCellPosition(grid, batteryStation.transform, new Vector3Int(0, 2, 0));
        SetCellPosition(grid, metalStation.transform, new Vector3Int(3, 2, 0));
        SetCellPosition(grid, trashBin.transform, new Vector3Int(0, -5, 0));

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

        ConfigureExistingConveyor(grid, beltTile, originalSpawner, itemPrefab, circuitData, batteryData);
        ConfigureRightConveyor(grid, beltTile, itemPrefab, metalData);
        ClearConveyorDecoration(grid.transform);
        PaintStationApproaches(grid.transform, plainTile, grateTile, hazardTile);
        UpdateHudDefaults(80f, 60);
        return true;
    }

    private static void ConfigureExistingConveyor(
        Grid grid,
        Tile beltTile,
        ConveyorSpawner spawner,
        CollectibleItem itemPrefab,
        ItemData firstItem,
        ItemData secondItem)
    {
        // Reaproveita a faixa e o spawner da cópia, preservando os sistemas que já existem.
        Transform conveyor = spawner.transform;
        conveyor.name = "Conveyor_Left";
        Tilemap tilemap = conveyor.GetComponent<Tilemap>();
        tilemap.ClearAllTiles();
        PaintVerticalLane(tilemap, beltTile, -8, -3, 3, -90f);

        Transform spawnPoint = conveyor.Find("SpawnPoint");
        Transform trash = conveyor.Find("ConveyorTrash");
        SetPoint(grid, spawnPoint, new Vector3Int(-8, 3, 0));
        ConfigureTrash(grid, trash, new Vector3Int(-8, -4, 0));
        ConfigureSpawner(spawner, itemPrefab, new[] { firstItem, secondItem }, spawnPoint, 1);
    }

    private static void ConfigureRightConveyor(Grid grid, Tile beltTile, CollectibleItem itemPrefab, ItemData item)
    {
        // A faixa da direita usa somente Metal Component e conduz os itens para cima.
        Transform gridTransform = grid.transform;
        GameObject conveyorObject = new GameObject("Conveyor_Right", typeof(Tilemap), typeof(TilemapRenderer));
        conveyorObject.transform.SetParent(gridTransform, false);
        Tilemap tilemap = conveyorObject.GetComponent<Tilemap>();
        TilemapRenderer renderer = conveyorObject.GetComponent<TilemapRenderer>();
        renderer.sortingOrder = 2;
        PaintVerticalLane(tilemap, beltTile, 8, -3, 3, 90f);

        GameObject spawnObject = new GameObject("SpawnPoint");
        spawnObject.transform.SetParent(conveyorObject.transform, false);
        SetPoint(grid, spawnObject.transform, new Vector3Int(8, -3, 0));
        GameObject trashObject = new GameObject("ConveyorTrash");
        trashObject.transform.SetParent(conveyorObject.transform, false);
        ConfigureTrash(grid, trashObject.transform, new Vector3Int(8, 4, 0));

        ConveyorSpawner spawner = conveyorObject.AddComponent<ConveyorSpawner>();
        ConfigureSpawner(spawner, itemPrefab, new[] { item }, spawnObject.transform, 3);
    }

    private static void PaintVerticalLane(Tilemap tilemap, Tile beltTile, int x, int minY, int maxY, float rotation)
    {
        // Gira cada tile para acompanhar visualmente o sentido do fluxo configurado no spawner.
        for (int y = minY; y <= maxY; y++)
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

    private static void ConfigureTrash(Grid grid, Transform trash, Vector3Int cell)
    {
        if (trash == null)
            return;

        SetPoint(grid, trash, cell);
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

    private static void PaintStationApproaches(Transform gridTransform, Tile plain, Tile grate, Tile hazard)
    {
        Tilemap ground = gridTransform.GetComponentsInChildren<Tilemap>(true)
            .FirstOrDefault(tilemap => tilemap.name == "Ground");
        if (ground == null)
            return;

        int[] formerStationColumns = { -9, 6 };
        foreach (int column in formerStationColumns)
        {
            for (int x = column - 1; x <= column + 1; x++)
                ground.SetTile(new Vector3Int(x, 2, 0), plain);
        }

        foreach (int stationX in new[] { -3, 0, 3 })
        {
            for (int x = stationX - 1; x <= stationX + 1; x++)
                ground.SetTile(new Vector3Int(x, 1, 0), grate);
            ground.SetTile(new Vector3Int(stationX, 1, 0), hazard);
        }
    }

    private static void ClearConveyorDecoration(Transform gridTransform)
    {
        Tilemap decorations = gridTransform.GetComponentsInChildren<Tilemap>(true)
            .FirstOrDefault(tilemap => tilemap.name == "Decorations");
        if (decorations == null)
            return;

        // Mantém livres as células das duas faixas para que a esteira fique visível.
        for (int y = -3; y <= 3; y++)
        {
            decorations.SetTile(new Vector3Int(-8, y, 0), null);
            decorations.SetTile(new Vector3Int(8, y, 0), null);
        }

        EditorUtility.SetDirty(decorations);
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

    private static void ConfigureSecondLevelNextButton()
    {
        Scene scene = EditorSceneManager.OpenScene(SecondLevelPath, OpenSceneMode.Single);
        ResultScreenController controller = Object.FindFirstObjectByType<ResultScreenController>();
        if (controller == null)
        {
            Debug.LogWarning("ResultScreenController não foi encontrado na Fase 2; NEXT LEVEL não foi configurado.");
            return;
        }

        SerializedObject data = new SerializedObject(controller);
        data.FindProperty("nextLevelScene").stringValue = "FactoryLevel03";
        data.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void SetCellPosition(Grid grid, Transform target, Vector3Int cell)
    {
        target.position = grid.GetCellCenterWorld(cell);
    }

    private static void SetPoint(Grid grid, Transform target, Vector3Int cell)
    {
        if (target != null)
            target.position = grid.GetCellCenterWorld(cell);
    }

    private static ItemData LoadItemData(string assetName)
    {
        return AssetDatabase.LoadAssetAtPath<ItemData>($"Assets/Items/Data/{assetName}.asset");
    }
}
