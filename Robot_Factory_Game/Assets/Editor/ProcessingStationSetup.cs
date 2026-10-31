using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Configura as três estações da cena como Prefabs interativos reutilizáveis.
/// </summary>
public static class ProcessingStationSetup
{
    private const string ScenePath = "Assets/Scenes/FactoryLevel01.unity";
    private const string PrefabFolder = "Assets/Prefabs/Stations";
    [MenuItem("Tools/GS Robot Factory/Configure Processing Stations")]
    public static void ConfigureStations()
    {
        ApplySetup();
    }

    private static bool ApplySetup()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        Scene scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Transform placeholders = GameObject.Find("TestPlaceholders")?.transform;
        if (placeholders == null)
        {
            Debug.LogError("TestPlaceholders não encontrado em FactoryLevel01.");
            return false;
        }

        ItemData circuitData = LoadItemData("CircuitBoardData");
        ItemData batteryData = LoadItemData("BatteryData");
        ItemData metalData = LoadItemData("MetalComponentData");
        if (circuitData == null || batteryData == null || metalData == null)
        {
            Debug.LogError("Os três ItemData da etapa de coleta precisam existir antes de configurar as estações.");
            return false;
        }

        EnsureFolder("Assets/Prefabs");
        EnsureFolder(PrefabFolder);

        Transform circuitStation = FindStation(placeholders, "Station_Assembly", "Circuit Station");
        Transform batteryStation = FindStation(placeholders, "Station_Intake", "Battery Station");
        Transform metalStation = FindStation(placeholders, "Station_Processor", "Metal Station");
        if (circuitStation == null || batteryStation == null || metalStation == null)
        {
            Debug.LogError("Não foi possível encontrar os três objetos de estação existentes.");
            return false;
        }

        bool success = ConfigureStation(circuitStation, "Circuit Station", circuitData, "CircuitStation");
        success &= ConfigureStation(batteryStation, "Battery Station", batteryData, "BatteryStation");
        success &= ConfigureStation(metalStation, "Metal Station", metalData, "MetalStation");
        if (!success)
            return false;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Circuit Station, Battery Station e Metal Station configuradas como Prefabs.");
        return true;
    }

    private static ItemData LoadItemData(string assetName)
    {
        return AssetDatabase.LoadAssetAtPath<ItemData>($"Assets/Items/Data/{assetName}.asset");
    }

    private static Transform FindStation(Transform root, string oldName, string newName)
    {
        Transform station = root.Find(newName);
        return station != null ? station : root.Find(oldName);
    }

    private static bool ConfigureStation(Transform station, string stationName, ItemData acceptedItemData, string prefabName)
    {
        station.name = stationName;
        BoxCollider2D collider = station.GetComponent<BoxCollider2D>();
        if (collider == null) collider = station.gameObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = false;
        collider.offset = Vector2.zero;
        collider.size = Vector2.one;

        ProcessingStation processingStation = station.GetComponent<ProcessingStation>();
        if (processingStation == null) processingStation = station.gameObject.AddComponent<ProcessingStation>();
        SerializedObject serializedStation = new SerializedObject(processingStation);
        serializedStation.FindProperty("acceptedItemData").objectReferenceValue = acceptedItemData;
        serializedStation.ApplyModifiedPropertiesWithoutUndo();

        string prefabPath = $"{PrefabFolder}/{prefabName}.prefab";
        PrefabUtility.SaveAsPrefabAssetAndConnect(
            station.gameObject,
            prefabPath,
            InteractionMode.AutomatedAction,
            out bool savedSuccessfully);

        if (!savedSuccessfully)
            Debug.LogError($"Não foi possível salvar o Prefab {prefabName}.", station);

        return savedSuccessfully;
    }

    private static void EnsureFolder(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        string currentPath = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string nextPath = $"{currentPath}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(nextPath))
                AssetDatabase.CreateFolder(currentPath, parts[i]);
            currentPath = nextPath;
        }
    }
}
