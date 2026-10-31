using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

/// <summary>
/// Cria a segunda fase a partir da fase existente, preservando seus sistemas.
/// </summary>
public static class Level02Setup
{
    private const string FirstLevelPath = "Assets/Scenes/FactoryLevel01.unity";
    private const string SecondLevelPath = "Assets/Scenes/FactoryLevel02.unity";
    private const string PlainTilePath = "Assets/Art/Tiles/Placeholders/Floor_Plain.asset";
    private const string GrateTilePath = "Assets/Art/Tiles/Placeholders/Floor_Grate.asset";
    private const string HazardTilePath = "Assets/Art/Tiles/Placeholders/Floor_Hazard.asset";

    [MenuItem("Tools/GS Robot Factory/Create Level 02 and Update Menu")]
    public static void CreateLevelTwo()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(FirstLevelPath) == null)
        {
            Debug.LogError("FactoryLevel01.unity não foi encontrada.");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SecondLevelPath) != null)
        {
            Debug.LogError("FactoryLevel02.unity já existe. A cena não foi sobrescrita para preservar alterações manuais.");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        if (!AssetDatabase.CopyAsset(FirstLevelPath, SecondLevelPath))
        {
            Debug.LogError("Não foi possível copiar FactoryLevel01 para criar FactoryLevel02.");
            return;
        }

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Scene secondLevel = EditorSceneManager.OpenScene(SecondLevelPath, OpenSceneMode.Single);
        if (!ConfigureSecondLevel(secondLevel))
            return;

        EditorSceneManager.MarkSceneDirty(secondLevel);
        EditorSceneManager.SaveScene(secondLevel);
        ConfigureFirstLevelNextButton();
        MainMenuSceneSetup.RefreshLevelSelection();
        Debug.Log("FactoryLevel02 criada: rotas mais longas, meta 120 e tempo de 1:10. A Fase 1 foi preservada.");
    }

    [MenuItem("Tools/GS Robot Factory/Refresh Level 02 HUD Defaults")]
    public static void RefreshLevelTwoHudDefaults()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SecondLevelPath) == null)
        {
            Debug.LogError("FactoryLevel02.unity ainda não existe.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(SecondLevelPath, OpenSceneMode.Single);
        UpdateHudDefaults(70f, 120);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static bool ConfigureSecondLevel(Scene scene)
    {
        Grid grid = GameObject.Find("Grid")?.GetComponent<Grid>();
        GameObject robot = GameObject.Find("Robot_Test");
        GameObject circuitStation = GameObject.Find("Circuit Station");
        GameObject batteryStation = GameObject.Find("Battery Station");
        GameObject metalStation = GameObject.Find("Metal Station");
        GameObject trashBin = GameObject.Find("Trash_Bin");
        GameSession session = Object.FindFirstObjectByType<GameSession>();
        ProductivityManager productivity = Object.FindFirstObjectByType<ProductivityManager>();

        if (grid == null || robot == null || circuitStation == null || batteryStation == null
            || metalStation == null || trashBin == null || session == null || productivity == null)
        {
            Debug.LogError("A cópia da fase não contém todos os objetos necessários. A Fase 1 permanece intacta.");
            return false;
        }

        // Distribui as estações nas extremidades para alongar as rotas do jogador.
        SetCellPosition(grid, robot.transform, new Vector3Int(-8, -3, 0));
        SetCellPosition(grid, circuitStation.transform, new Vector3Int(-8, 3, 0));
        SetCellPosition(grid, batteryStation.transform, new Vector3Int(7, 3, 0));
        SetCellPosition(grid, metalStation.transform, new Vector3Int(7, -3, 0));
        SetCellPosition(grid, trashBin.transform, new Vector3Int(-9, 0, 0));

        SerializedObject sessionData = new SerializedObject(session);
        sessionData.FindProperty("timeLimit").floatValue = 70f;
        sessionData.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(session);

        SerializedObject productivityData = new SerializedObject(productivity);
        productivityData.FindProperty("productivityGoal").intValue = 120;
        productivityData.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(productivity);

        UpdateHudDefaults(70f, 120);

        PaintStationMarkers(grid.transform);
        return true;
    }

    private static void UpdateHudDefaults(float timeLimit, int productivityGoal)
    {
        Text[] texts = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Text timerText = texts.FirstOrDefault(text => text.name == "Timer Text");
        Text productivityText = texts.FirstOrDefault(text => text.name == "Productivity Text");

        if (timerText != null)
        {
            int totalSeconds = Mathf.CeilToInt(timeLimit);
            timerText.text = $"TIME: {totalSeconds / 60:00}:{totalSeconds % 60:00}";
            EditorUtility.SetDirty(timerText);
        }

        if (productivityText != null)
        {
            productivityText.text = $"PRODUCTIVITY: 0 / {productivityGoal}";
            EditorUtility.SetDirty(productivityText);
        }
    }

    private static void SetCellPosition(Grid grid, Transform target, Vector3Int cell)
    {
        target.position = grid.GetCellCenterWorld(cell);
    }

    private static void PaintStationMarkers(Transform gridTransform)
    {
        Tilemap ground = gridTransform.GetComponentsInChildren<Tilemap>(true)
            .FirstOrDefault(tilemap => tilemap.name == "Ground");
        Tile plain = AssetDatabase.LoadAssetAtPath<Tile>(PlainTilePath);
        Tile grate = AssetDatabase.LoadAssetAtPath<Tile>(GrateTilePath);
        Tile hazard = AssetDatabase.LoadAssetAtPath<Tile>(HazardTilePath);
        if (ground == null || plain == null || grate == null || hazard == null)
        {
            Debug.LogWarning("Não foi possível atualizar as marcações de piso das estações na Fase 2.");
            return;
        }

        // Limpa apenas as marcações antigas das estações e repõe o piso industrial comum.
        int[] oldStationColumns = { -6, -1, 4 };
        foreach (int column in oldStationColumns)
        {
            for (int x = column - 1; x <= column + 1; x++)
                ground.SetTile(new Vector3Int(x, 2, 0), plain);
        }

        MarkStationApproach(ground, grate, hazard, new Vector3Int(-8, 2, 0));
        MarkStationApproach(ground, grate, hazard, new Vector3Int(7, 2, 0));
        MarkStationApproach(ground, grate, hazard, new Vector3Int(7, -2, 0));
    }

    private static void MarkStationApproach(Tilemap ground, Tile grate, Tile hazard, Vector3Int approachCell)
    {
        for (int x = approachCell.x - 1; x <= approachCell.x + 1; x++)
            ground.SetTile(new Vector3Int(x, approachCell.y, 0), grate);

        ground.SetTile(approachCell, hazard);
    }

    private static void ConfigureFirstLevelNextButton()
    {
        Scene scene = EditorSceneManager.OpenScene(FirstLevelPath, OpenSceneMode.Single);
        ResultScreenController resultController = Object.FindFirstObjectByType<ResultScreenController>();
        if (resultController == null)
        {
            Debug.LogWarning("ResultScreenController não foi encontrado na Fase 1; o botão NEXT LEVEL não foi configurado.");
            return;
        }

        SerializedObject resultData = new SerializedObject(resultController);
        resultData.FindProperty("nextLevelScene").stringValue = "FactoryLevel02";
        resultData.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(resultController);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
