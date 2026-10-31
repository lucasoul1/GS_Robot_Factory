using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Monta a cena inicial da fábrica sem alterar a cena de exemplo.
/// </summary>
public static class FactoryLevelSetup
{
    private const string TemplatePath = "Assets/Settings/Scenes/URP2DSceneTemplate.unity";
    private const string FactoryScenePath = "Assets/Scenes/FactoryLevel01.unity";

    [MenuItem("Tools/GS Robot Factory/Build FactoryLevel01")]
    public static void BuildFactoryLevel01()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(FactoryScenePath) == null)
        {
            if (!AssetDatabase.CopyAsset(TemplatePath, FactoryScenePath))
            {
                Debug.LogError($"Não foi possível copiar o template {TemplatePath}.");
                return;
            }
        }

        AssetDatabase.Refresh();
        Scene scene = EditorSceneManager.OpenScene(FactoryScenePath, OpenSceneMode.Single);
        ConfigureCamera();
        EnsureGridAndTilemaps();
        AddSceneToBuildSettings();
        ConfigurePlayerResolution();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        string projectPath = Directory.GetParent(Application.dataPath)?.FullName;
        string pendingAtlasPath = Path.Combine(projectPath ?? string.Empty, "Assets/Art/Source/FactoryPlaceholderAtlas_Source.png");
        if (File.Exists(pendingAtlasPath))
        {
            FactoryPlaceholderGenerator.GeneratePlaceholderArtAndTilemap();
        }

        if (AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Placeholders/Conveyor_Belt.png") != null)
        {
            FactoryScenarioSetup.ApplyScenarioLayout();
        }

        Debug.Log("FactoryLevel01 foi criada e salva. SampleScene foi preservada.");
    }

    private static void ConfigureCamera()
    {
        GameObject cameraObject = GameObject.FindGameObjectWithTag("MainCamera");
        if (cameraObject == null || !cameraObject.TryGetComponent(out Camera camera))
        {
            Debug.LogError("O template não contém uma Main Camera válida.");
            return;
        }

        cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 0f, -10f), Quaternion.identity);
        camera.orthographic = true;
        camera.orthographicSize = 5.625f;
        camera.rect = new Rect(0f, 0f, 1f, 1f);
        camera.allowMSAA = false;
        camera.allowDynamicResolution = false;
    }

    private static void EnsureGridAndTilemaps()
    {
        GameObject gridObject = GameObject.Find("Grid");
        if (gridObject == null)
        {
            gridObject = new GameObject("Grid", typeof(Grid));
        }

        if (!gridObject.TryGetComponent(out Grid grid))
        {
            grid = gridObject.AddComponent<Grid>();
        }

        grid.cellLayout = GridLayout.CellLayout.Rectangle;
        grid.cellSize = new Vector3(1f, 1f, 0f);
        grid.cellGap = Vector3.zero;
        grid.cellSwizzle = GridLayout.CellSwizzle.XYZ;

        EnsureTilemap("Ground", gridObject.transform, 0);
        EnsureTilemap("Environment", gridObject.transform, 1);
    }

    private static void EnsureTilemap(string tilemapName, Transform parent, int sortingOrder)
    {
        Tilemap[] childTilemaps = parent.GetComponentsInChildren<Tilemap>(true);
        Tilemap tilemap = childTilemaps.FirstOrDefault(item => item.name == tilemapName);

        // Corrige o nome que estava escrito incorretamente na camada ambiental existente.
        if (tilemap == null && tilemapName == "Environment")
        {
            tilemap = childTilemaps.FirstOrDefault(item => item.name == "Enviroment");
            if (tilemap != null)
            {
                tilemap.gameObject.name = tilemapName;
            }
        }

        if (tilemap == null)
        {
            GameObject tilemapObject = new GameObject(tilemapName, typeof(Tilemap), typeof(TilemapRenderer));
            tilemapObject.transform.SetParent(parent, false);
            tilemap = tilemapObject.GetComponent<Tilemap>();
        }

        tilemap.tileAnchor = new Vector3(0.5f, 0.5f, 0f);

        TilemapRenderer tilemapRenderer = tilemap.GetComponent<TilemapRenderer>();
        if (tilemapRenderer == null)
        {
            tilemapRenderer = tilemap.gameObject.AddComponent<TilemapRenderer>();
        }

        tilemapRenderer.sortingOrder = sortingOrder;
    }

    private static void AddSceneToBuildSettings()
    {
        EditorBuildSettingsScene[] currentScenes = EditorBuildSettings.scenes;
        if (currentScenes.Any(scene => scene.path == FactoryScenePath))
        {
            return;
        }

        EditorBuildSettings.scenes = currentScenes
            .Concat(new[] { new EditorBuildSettingsScene(FactoryScenePath, true) })
            .ToArray();
    }

    private static void ConfigurePlayerResolution()
    {
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
    }
}
