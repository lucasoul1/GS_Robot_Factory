using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Gera sprites temporários de teste e os organiza na cena da fábrica.
/// </summary>
public static class FactoryPlaceholderGenerator
{
    private const string SourceAtlasPath = "Assets/Art/Source/FactoryPlaceholderAtlas_Source.png";
    private const string SpriteFolder = "Assets/Art/Sprites/Placeholders";
    private const string TileFolder = "Assets/Art/Tiles/Placeholders";
    private const string ScenePath = "Assets/Scenes/FactoryLevel01.unity";

    private static readonly Color32[] Palette =
    {
        new Color32(17, 27, 43, 255),
        new Color32(38, 56, 79, 255),
        new Color32(91, 111, 132, 255),
        new Color32(193, 202, 214, 255),
        new Color32(255, 198, 36, 255),
        new Color32(17, 197, 224, 255),
        new Color32(239, 58, 69, 255),
        new Color32(39, 211, 93, 255),
    };

    private static readonly string[] SpriteNames =
    {
        "Floor_Plain", "Floor_Hazard", "Floor_Grate", "Wall_Panel",
        "Robot_Test", "Component_Crate", "Component_Circuit", "Component_Battery",
        "Station_Assembly", "Station_Intake", "Station_Processor", "Station_Charge",
        "Floor_CornerMark", "Floor_Pipe", "Crate_Stack", "Floor_Beacon",
    };

    [InitializeOnLoadMethod]
    private static void ProcessPendingAtlas()
    {
        EditorApplication.delayCall += () =>
        {
            string projectPath = Directory.GetParent(Application.dataPath)?.FullName;
            string sourcePath = Path.Combine(projectPath ?? string.Empty, SourceAtlasPath);
            if (File.Exists(sourcePath))
            {
                GeneratePlaceholderArtAndTilemap();
            }
        };
    }

    [MenuItem("Tools/GS Robot Factory/Generate Placeholder Art and Tilemap")]
    public static void GeneratePlaceholderArtAndTilemap()
    {
        string projectPath = Directory.GetParent(Application.dataPath)?.FullName;
        string sourcePath = Path.Combine(projectPath ?? string.Empty, SourceAtlasPath);
        if (!File.Exists(sourcePath))
        {
            Debug.LogError($"Atlas temporário não encontrado em {SourceAtlasPath}.");
            return;
        }

        Directory.CreateDirectory(SpriteFolder);
        Directory.CreateDirectory(TileFolder);
        GenerateSprites(sourcePath);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        Tile floorTile = CreateTile("Floor_Plain", "Floor_Plain");
        Tile hazardTile = CreateTile("Floor_Hazard", "Floor_Hazard");
        Tile grateTile = CreateTile("Floor_Grate", "Floor_Grate");
        Tile wallTile = CreateTile("Wall_Panel", "Wall_Panel");

        AssetDatabase.SaveAssets();
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
        Tilemap environment = FindTilemap(grid.transform, "Environment");
        if (ground == null || environment == null)
        {
            Debug.LogError("Os Tilemaps Ground e Environment precisam existir na cena.");
            return;
        }

        PaintFloor(ground, floorTile, hazardTile, grateTile);
        PaintBorder(environment, wallTile);
        ConfigurePixelPerfectCamera();
        EnsureGlobalLight();
        CreateTestObjects(grid);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        File.Delete(sourcePath);
        Debug.Log("Sprites temporários, assets Tile e tilemap de teste foram criados em FactoryLevel01.");
    }

    private static void GenerateSprites(string sourcePath)
    {
        Texture2D atlas = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            if (!ImageConversion.LoadImage(atlas, File.ReadAllBytes(sourcePath), false))
            {
                throw new InvalidOperationException("A Unity não conseguiu ler o atlas gerado.");
            }

            if (atlas.width / 4 < 4 || atlas.height / 4 < 4)
            {
                throw new InvalidOperationException("O atlas precisa ter uma grade 4×4.");
            }

            for (int index = 0; index < SpriteNames.Length; index++)
            {
                string outputPath = Path.Combine(SpriteFolder, SpriteNames[index] + ".png").Replace('\\', '/');
                if (File.Exists(outputPath))
                {
                    continue;
                }

                Texture2D spriteTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false, true);
                int column = index % 4;
                int rowFromTop = index / 4;
                int cellStartX = Mathf.FloorToInt(column * atlas.width / 4f);
                int cellEndX = Mathf.FloorToInt((column + 1) * atlas.width / 4f);
                int cellStartY = Mathf.FloorToInt(rowFromTop * atlas.height / 4f);
                int cellEndY = Mathf.FloorToInt((rowFromTop + 1) * atlas.height / 4f);
                int cellWidth = cellEndX - cellStartX;
                int cellHeight = cellEndY - cellStartY;

                for (int y = 0; y < 32; y++)
                {
                    for (int x = 0; x < 32; x++)
                    {
                        int sourceX = cellStartX + Mathf.FloorToInt((x + 0.5f) * cellWidth / 32f);
                        int sourceYFromTop = cellStartY + Mathf.FloorToInt((31 - y + 0.5f) * cellHeight / 32f);
                        int sourceY = atlas.height - 1 - sourceYFromTop;
                        Color sourceColor = atlas.GetPixel(Mathf.Min(sourceX, atlas.width - 1), Mathf.Clamp(sourceY, 0, atlas.height - 1));
                        spriteTexture.SetPixel(x, y, Quantize(sourceColor));
                    }
                }

                spriteTexture.Apply(false, false);
                File.WriteAllBytes(outputPath, spriteTexture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(spriteTexture);
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(atlas);
        }
    }

    private static Color32 Quantize(Color sourceColor)
    {
        if (sourceColor.a < 0.5f)
        {
            return new Color32(0, 0, 0, 0);
        }

        Color32 nearest = Palette[0];
        float nearestDistance = float.MaxValue;
        foreach (Color32 color in Palette)
        {
            float red = sourceColor.r * 255f - color.r;
            float green = sourceColor.g * 255f - color.g;
            float blue = sourceColor.b * 255f - color.b;
            float distance = red * red + green * green + blue * blue;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = color;
            }
        }

        return nearest;
    }

    private static Tile CreateTile(string tileName, string spriteName)
    {
        string tileAssetPath = $"{TileFolder}/{tileName}.asset";
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(tileAssetPath);
        if (tile != null)
        {
            return tile;
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/{spriteName}.png");
        if (sprite == null)
        {
            throw new InvalidOperationException($"Sprite {spriteName} não foi importado.");
        }

        tile = ScriptableObject.CreateInstance<Tile>();
        tile.name = tileName;
        tile.sprite = sprite;
        AssetDatabase.CreateAsset(tile, tileAssetPath);
        return tile;
    }

    private static Tilemap FindTilemap(Transform parent, string tilemapName)
    {
        return parent.GetComponentsInChildren<Tilemap>(true).FirstOrDefault(tilemap => tilemap.name == tilemapName);
    }

    private static void PaintFloor(Tilemap ground, Tile floorTile, Tile hazardTile, Tile grateTile)
    {
        if (ground.GetUsedTilesCount() > 0)
        {
            Debug.LogWarning("Ground já contém tiles; a pintura automática foi ignorada para preservar o conteúdo existente.");
            return;
        }

        for (int x = -10; x < 10; x++)
        {
            for (int y = -6; y < 6; y++)
            {
                Tile tile = floorTile;
                if (y == 0 && x >= -2 && x <= 2)
                {
                    tile = hazardTile;
                }
                else if ((x == 5 || x == 6) && y == 2)
                {
                    tile = grateTile;
                }

                ground.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }
    }

    private static void PaintBorder(Tilemap environment, Tile wallTile)
    {
        if (environment.GetUsedTilesCount() > 0)
        {
            Debug.LogWarning("Environment já contém tiles; a borda automática foi ignorada para preservar o conteúdo existente.");
            return;
        }

        for (int x = -10; x < 10; x++)
        {
            environment.SetTile(new Vector3Int(x, -6, 0), wallTile);
            environment.SetTile(new Vector3Int(x, 5, 0), wallTile);
        }

        for (int y = -5; y < 5; y++)
        {
            environment.SetTile(new Vector3Int(-10, y, 0), wallTile);
            environment.SetTile(new Vector3Int(9, y, 0), wallTile);
        }
    }

    private static void CreateTestObjects(Grid grid)
    {
        if (GameObject.Find("TestPlaceholders") != null)
        {
            Debug.LogWarning("TestPlaceholders já existe; os objetos temporários não foram duplicados.");
            return;
        }

        GameObject parent = new GameObject("TestPlaceholders");
        CreateSpriteObject(parent.transform, grid, "Robot_Test", "Robot_Test", -5, -2);
        CreateSpriteObject(parent.transform, grid, "Component_Crate", "Component_Crate", -2, -2);
        CreateSpriteObject(parent.transform, grid, "Component_Circuit", "Component_Circuit", 1, -2);
        CreateSpriteObject(parent.transform, grid, "Component_Battery", "Component_Battery", 4, -2);
        CreateSpriteObject(parent.transform, grid, "Station_Assembly", "Station_Assembly", -6, 3);
        CreateSpriteObject(parent.transform, grid, "Station_Intake", "Station_Intake", -2, 3);
        CreateSpriteObject(parent.transform, grid, "Station_Processor", "Station_Processor", 2, 3);
        CreateSpriteObject(parent.transform, grid, "Station_Charge", "Station_Charge", 6, 3);
    }

    private static void EnsureGlobalLight()
    {
        if (UnityEngine.Object.FindAnyObjectByType<Light2D>() != null)
        {
            return;
        }

        GameObject lightObject = new GameObject("Global Light 2D");
        Light2D globalLight = lightObject.AddComponent<Light2D>();
        globalLight.lightType = Light2D.LightType.Global;
        globalLight.color = Color.white;
        globalLight.intensity = 1f;
    }

    private static void ConfigurePixelPerfectCamera()
    {
        GameObject cameraObject = GameObject.FindGameObjectWithTag("MainCamera");
        if (cameraObject == null || !cameraObject.TryGetComponent(out Camera camera))
        {
            Debug.LogError("Main Camera não encontrada para a configuração Pixel Perfect.");
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

    private static void CreateSpriteObject(Transform parent, Grid grid, string objectName, string spriteName, int cellX, int cellY)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/{spriteName}.png");
        if (sprite == null)
        {
            throw new InvalidOperationException($"Sprite {spriteName} não foi encontrado.");
        }

        GameObject spriteObject = new GameObject(objectName, typeof(SpriteRenderer));
        spriteObject.transform.SetParent(parent, false);
        spriteObject.transform.position = grid.GetCellCenterWorld(new Vector3Int(cellX, cellY, 0));
        SpriteRenderer spriteRenderer = spriteObject.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingOrder = 2;
    }
}
