using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Organiza os placeholders existentes em uma sala industrial de teste.
/// </summary>
public static class FactoryScenarioSetup
{
    private const string ScenePath = "Assets/Scenes/FactoryLevel01.unity";
    private const string SpriteFolder = "Assets/Art/Sprites/Placeholders";
    private const string TileFolder = "Assets/Art/Tiles/Placeholders";
    private const string RunMarkerPath = "Assets/Art/Source/ApplyFactoryScenario.txt";

    [InitializeOnLoadMethod]
    private static void ApplyRequestedScenario()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(RunMarkerPath))
            {
                return;
            }

            ApplyScenarioLayout();
            File.Delete(RunMarkerPath);
            AssetDatabase.Refresh();
        };
    }

    [MenuItem("Tools/GS Robot Factory/Apply Industrial Scenario Layout")]
    public static void ApplyScenarioLayout()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        Scene scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        Grid grid = GameObject.Find("Grid")?.GetComponent<Grid>();
        if (grid == null)
        {
            Debug.LogError("Grid não encontrado em FactoryLevel01.");
            return;
        }

        Tilemap ground = FindTilemap(grid.transform, "Ground");
        Tilemap walls = FindTilemap(grid.transform, "Walls");
        if (walls == null)
        {
            walls = FindTilemap(grid.transform, "Environment");
            if (walls != null)
            {
                walls.gameObject.name = "Walls";
            }
        }

        if (ground == null || walls == null)
        {
            Debug.LogError("Os Tilemaps Ground e Walls precisam existir na cena.");
            return;
        }

        Tilemap conveyor = EnsureTilemap(grid.transform, "Conveyor", 2);
        Tilemap decorations = EnsureTilemap(grid.transform, "Decorations", 3);
        Tilemap background = EnsureTilemap(grid.transform, "Background", -1);
        SetSortingOrder(ground, 0);
        SetSortingOrder(walls, 1);

        GameSession gameSession = Object.FindFirstObjectByType<GameSession>();
        if (gameSession != null)
        {
            SerializedObject serializedSession = new SerializedObject(gameSession);
            serializedSession.FindProperty("timeLimit").floatValue = 80f;
            serializedSession.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(gameSession);
        }

        Transform hudCanvas = GameObject.Find("Productivity HUD Canvas")?.transform;
        UnityEngine.UI.Text timerText = hudCanvas != null
            ? hudCanvas.Find("Timer Text")?.GetComponent<UnityEngine.UI.Text>()
            : null;
        if (timerText != null)
        {
            SerializedObject serializedTimer = new SerializedObject(timerText);
            serializedTimer.FindProperty("m_Text").stringValue = "TIME: 01:20";
            serializedTimer.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(timerText);
        }

        Tile conveyorTile = CreateTile("Conveyor_Belt", "Conveyor_Belt");
        Tile machineTile = CreateTile("Machine_Decor", "Machine_Decor");
        Tile crateStackTile = CreateTile("Crate_Stack", "Crate_Stack");
        Tile backgroundTile = CreateTile("Gray_Background", "Gray_Background");
        Tile grateTile = CreateTile("Floor_Grate", "Floor_Grate");
        Tile hazardTile = CreateTile("Floor_Hazard", "Floor_Hazard");
        Tile beaconTile = CreateTile("Floor_Beacon", "Floor_Beacon");

        PaintBackground(background, backgroundTile);
        PaintConveyor(conveyor, conveyorTile);
        PaintFloorZones(ground, grateTile, hazardTile, beaconTile);
        PaintDecorations(decorations, machineTile, crateStackTile);
        PositionObjects(grid);
        ConfigurePixelPerfectCamera();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Layout da fábrica reorganizado e tempo da fase definido em 1:20.");
    }

    private static Tilemap FindTilemap(Transform parent, string tilemapName)
    {
        return parent.GetComponentsInChildren<Tilemap>(true).FirstOrDefault(tilemap => tilemap.name == tilemapName);
    }

    private static Tilemap EnsureTilemap(Transform parent, string tilemapName, int sortingOrder)
    {
        Tilemap tilemap = FindTilemap(parent, tilemapName);
        if (tilemap == null)
        {
            GameObject tilemapObject = new GameObject(tilemapName, typeof(Tilemap), typeof(TilemapRenderer));
            tilemapObject.transform.SetParent(parent, false);
            tilemap = tilemapObject.GetComponent<Tilemap>();
            tilemap.tileAnchor = new Vector3(0.5f, 0.5f, 0f);
        }

        SetSortingOrder(tilemap, sortingOrder);
        return tilemap;
    }

    private static void SetSortingOrder(Tilemap tilemap, int sortingOrder)
    {
        TilemapRenderer tilemapRenderer = tilemap.GetComponent<TilemapRenderer>();
        if (tilemapRenderer == null)
        {
            tilemapRenderer = tilemap.gameObject.AddComponent<TilemapRenderer>();
        }

        tilemapRenderer.sortingOrder = sortingOrder;
    }

    private static Tile CreateTile(string tileName, string spriteName)
    {
        string tilePath = $"{TileFolder}/{tileName}.asset";
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
        if (tile != null)
        {
            return tile;
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/{spriteName}.png");
        if (sprite == null)
        {
            throw new System.InvalidOperationException($"Sprite {spriteName} não foi importado.");
        }

        tile = ScriptableObject.CreateInstance<Tile>();
        tile.name = tileName;
        tile.sprite = sprite;
        AssetDatabase.CreateAsset(tile, tilePath);
        return tile;
    }

    private static void PaintConveyor(Tilemap conveyor, Tile conveyorTile)
    {
        if (conveyor.GetUsedTilesCount() > 0)
        {
            return;
        }

        // A faixa visual é usada pelo ConveyorSpawner como percurso horizontal dos itens.
        for (int x = -2; x <= 3; x++)
        {
            conveyor.SetTile(new Vector3Int(x, 0, 0), conveyorTile);
        }
    }

    private static void PaintBackground(Tilemap background, Tile backgroundTile)
    {
        if (background.GetUsedTilesCount() > 0)
        {
            return;
        }

        // O fundo cinza ultrapassa os limites da sala para cobrir as bordas visíveis da câmera.
        for (int x = -12; x <= 11; x++)
        {
            for (int y = -8; y <= 7; y++)
            {
                background.SetTile(new Vector3Int(x, y, 0), backgroundTile);
            }
        }
    }

    private static void PaintDecorations(Tilemap decorations, Tile machineTile, Tile crateStackTile)
    {
        // Reconstrói apenas as decorações do Tilemap, mantendo livres as células de trabalho.
        decorations.ClearAllTiles();
        decorations.SetTile(new Vector3Int(-8, 1, 0), machineTile);
        decorations.SetTile(new Vector3Int(8, 1, 0), machineTile);
        decorations.SetTile(new Vector3Int(8, -4, 0), crateStackTile);
        decorations.SetTile(new Vector3Int(-9, 5, 0), machineTile);
        decorations.SetTile(new Vector3Int(9, 5, 0), machineTile);
        decorations.SetTile(new Vector3Int(-9, -5, 0), crateStackTile);
        decorations.SetTile(new Vector3Int(9, -5, 0), crateStackTile);
    }

    private static void PaintFloorZones(Tilemap ground, Tile grateTile, Tile hazardTile, Tile beaconTile)
    {
        // Filetes de grade delimitam a faixa da esteira sem ocupar suas células de fluxo.
        for (int x = -2; x <= 3; x++)
        {
            ground.SetTile(new Vector3Int(x, 1, 0), grateTile);
            ground.SetTile(new Vector3Int(x, -1, 0), grateTile);
        }

        // Marcas amarelas sinalizam o início/fim da esteira e a frente de cada estação.
        ground.SetTile(new Vector3Int(-3, 0, 0), hazardTile);
        ground.SetTile(new Vector3Int(4, 0, 0), hazardTile);

        int[] stationCellsX = { -6, -1, 4 };
        foreach (int stationX in stationCellsX)
        {
            for (int offset = -1; offset <= 1; offset++)
                ground.SetTile(new Vector3Int(stationX + offset, 2, 0), grateTile);

            ground.SetTile(new Vector3Int(stationX, 2, 0), hazardTile);
        }

        // Um beacon marca visualmente a área da saída da esteira.
        ground.SetTile(new Vector3Int(5, -1, 0), beaconTile);
    }

    private static void PositionObjects(Grid grid)
    {
        GameObject placeholderRoot = GameObject.Find("TestPlaceholders");
        if (placeholderRoot == null)
        {
            placeholderRoot = new GameObject("TestPlaceholders");
        }

        // Cada objeto fica no centro de uma célula; os colisores são configurados separadamente no Editor.
        SetSpriteObject(placeholderRoot.transform, grid, "Robot_Test", "Robot_Test", -7, -3);
        SetSpriteObject(placeholderRoot.transform, grid, "Component_Circuit", "Component_Circuit", -3, -3);
        SetSpriteObject(placeholderRoot.transform, grid, "Component_Battery", "Component_Battery", 0, -3);
        SetSpriteObject(placeholderRoot.transform, grid, "Component_Metal", "Component_Metal", 3, -3);
        SetSpriteObject(placeholderRoot.transform, grid, "Circuit Station", "Station_Assembly", -6, 3);
        SetSpriteObject(placeholderRoot.transform, grid, "Battery Station", "Station_Intake", -1, 3);
        SetSpriteObject(placeholderRoot.transform, grid, "Metal Station", "Station_Processor", 4, 3);
        SetSpriteObject(placeholderRoot.transform, grid, "Trash_Bin", "Trash_Bin", 7, -3);

        // O carregador anterior era uma quarta estação. Ele sai da sala, mas o sprite continua disponível.
        Transform unusedStation = FindChild(placeholderRoot.transform, "Station_Charge");
        if (unusedStation != null)
        {
            UnityEngine.Object.DestroyImmediate(unusedStation.gameObject);
        }

        Transform oldCrate = FindChild(placeholderRoot.transform, "Component_Crate");
        if (oldCrate != null)
        {
            UnityEngine.Object.DestroyImmediate(oldCrate.gameObject);
        }
    }

    private static void SetSpriteObject(Transform parent, Grid grid, string objectName, string spriteName, int cellX, int cellY)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/{spriteName}.png");
        if (sprite == null)
        {
            throw new System.InvalidOperationException($"Sprite {spriteName} não foi encontrado.");
        }

        Transform child = FindChild(parent, objectName);
        GameObject spriteObject = child != null ? child.gameObject : new GameObject(objectName, typeof(SpriteRenderer));
        if (child == null)
        {
            spriteObject.transform.SetParent(parent, false);
        }

        spriteObject.transform.position = grid.GetCellCenterWorld(new Vector3Int(cellX, cellY, 0));
        SpriteRenderer spriteRenderer = spriteObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = spriteObject.AddComponent<SpriteRenderer>();
        }

        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingOrder = 4;
    }

    private static Transform FindChild(Transform parent, string childName)
    {
        return parent.GetComponentsInChildren<Transform>(true).FirstOrDefault(child => child.name == childName);
    }

    private static void ConfigurePixelPerfectCamera()
    {
        GameObject cameraObject = GameObject.FindGameObjectWithTag("MainCamera");
        if (cameraObject == null || !cameraObject.TryGetComponent(out Camera camera))
        {
            Debug.LogError("Main Camera não encontrada.");
            return;
        }

        UnityEngine.U2D.PixelPerfectCamera pixelPerfectCamera =
            cameraObject.GetComponent<UnityEngine.U2D.PixelPerfectCamera>();
        if (pixelPerfectCamera == null)
        {
            pixelPerfectCamera = cameraObject.AddComponent<UnityEngine.U2D.PixelPerfectCamera>();
        }

        pixelPerfectCamera.assetsPPU = 32;
        pixelPerfectCamera.refResolutionX = 640;
        pixelPerfectCamera.refResolutionY = 360;
        pixelPerfectCamera.upscaleRT = true;
        pixelPerfectCamera.pixelSnapping = false;
        pixelPerfectCamera.cropFrameX = false;
        pixelPerfectCamera.cropFrameY = false;
        pixelPerfectCamera.stretchFill = false;
        camera.orthographic = true;
        camera.orthographicSize = 5.625f;
    }
}
