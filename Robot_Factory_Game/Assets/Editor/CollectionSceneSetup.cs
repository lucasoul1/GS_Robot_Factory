using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Prepara a cena industrial para testar coleta e transporte.
/// </summary>
public static class CollectionSceneSetup
{
    private const string ScenePath = "Assets/Scenes/FactoryLevel01.unity";
    private const string DataFolder = "Assets/Items/Data";
    private const string PrefabFolder = "Assets/Prefabs/Items";
    private const string SetupMarker = "Assets/Editor/CollectionSceneSetup.marker";

    [InitializeOnLoadMethod]
    private static void RunWhenRequested()
    {
        EditorApplication.delayCall += () =>
        {
            string markerPath = Path.GetFullPath(SetupMarker);
            if (!File.Exists(markerPath))
                return;

            if (ApplySceneSetup())
                File.Delete(markerPath);
        };
    }

    [MenuItem("Tools/GS Robot Factory/Configure Collection Test")]
    public static void ConfigureScene()
    {
        ApplySceneSetup();
    }

    private static bool ApplySceneSetup()
    {
        // Atualiza primeiro os sprites e scripts usados na configuração.
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Scene scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Grid grid = GameObject.Find("Grid")?.GetComponent<Grid>();
        Transform placeholders = GameObject.Find("TestPlaceholders")?.transform;
        if (grid == null || placeholders == null)
        {
            Debug.LogError("Grid ou TestPlaceholders não encontrado em FactoryLevel01.");
            return false;
        }

        Sprite circuitSprite = LoadSprite("Component_Circuit");
        Sprite batterySprite = LoadSprite("Component_Battery");
        Sprite metalSprite = LoadSprite("Component_Metal");
        if (circuitSprite == null || batterySprite == null || metalSprite == null)
        {
            Debug.LogError("Não foi possível carregar os três sprites temporários de componentes.");
            return false;
        }

        EnsureFolder("Assets/Items");
        EnsureFolder(DataFolder);
        EnsureFolder("Assets/Prefabs");
        EnsureFolder(PrefabFolder);

        ItemData circuitData = CreateOrUpdateItemData("CircuitBoardData", "Circuit Board", circuitSprite);
        ItemData batteryData = CreateOrUpdateItemData("BatteryData", "Battery", batterySprite);
        ItemData metalData = CreateOrUpdateItemData("MetalComponentData", "Metal Component", metalSprite);

        RobotGridMovement movement = placeholders.Find("Robot_Test")?.GetComponent<RobotGridMovement>();
        if (movement == null)
        {
            Debug.LogError("RobotGridMovement não encontrado em TestPlaceholders/Robot_Test.");
            return false;
        }

        GameObject robot = movement.gameObject;
        RobotInteraction interaction = robot.GetComponent<RobotInteraction>();
        if (interaction == null) interaction = robot.AddComponent<RobotInteraction>();
        SerializedObject interactionData = new SerializedObject(interaction);
        interactionData.FindProperty("grid").objectReferenceValue = grid;
        interactionData.FindProperty("robotMovement").objectReferenceValue = movement;
        interactionData.ApplyModifiedPropertiesWithoutUndo();

        SpriteRenderer carriedRenderer = CreateCarriedVisual(robot.transform);
        RobotCarry carry = robot.GetComponent<RobotCarry>();
        if (carry == null) carry = robot.AddComponent<RobotCarry>();
        SerializedObject carryData = new SerializedObject(carry);
        carryData.FindProperty("carriedItemRenderer").objectReferenceValue = carriedRenderer;
        carryData.ApplyModifiedPropertiesWithoutUndo();

        ConfigureCollectible(placeholders.Find("Component_Circuit"), circuitData, circuitSprite);
        ConfigureCollectible(placeholders.Find("Component_Battery"), batteryData, batterySprite);
        ConfigureCollectible(placeholders.Find("Component_Metal"), metalData, metalSprite);
        ConfigureBlockingTilemap(grid.transform, "Walls");
        ConfigureBlockingTilemap(grid.transform, "Decorations");
        ConfigureBlockingObject(placeholders, "Circuit Station", "Station_Assembly");
        ConfigureBlockingObject(placeholders, "Battery Station", "Station_Intake");
        ConfigureBlockingObject(placeholders, "Metal Station", "Station_Processor");
        ConfigureBlockingObject(placeholders, "Trash_Bin");
        CreateCollectiblePrefab(circuitData, circuitSprite);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("FactoryLevel01 preparada para testar coleta e transporte.");
        return true;
    }

    private static Sprite LoadSprite(string spriteName)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Sprites/Placeholders/{spriteName}.png");
    }

    private static ItemData CreateOrUpdateItemData(string assetName, string displayName, Sprite sprite)
    {
        string path = $"{DataFolder}/{assetName}.asset";
        ItemData data = AssetDatabase.LoadAssetAtPath<ItemData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<ItemData>();
            AssetDatabase.CreateAsset(data, path);
        }

        SerializedObject serializedData = new SerializedObject(data);
        serializedData.FindProperty("itemName").stringValue = displayName;
        serializedData.FindProperty("itemSprite").objectReferenceValue = sprite;
        serializedData.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data);
        return data;
    }

    private static SpriteRenderer CreateCarriedVisual(Transform robot)
    {
        Transform visual = robot.Find("CarriedItemVisual");
        GameObject visualObject = visual != null ? visual.gameObject : new GameObject("CarriedItemVisual");
        if (visual == null) visualObject.transform.SetParent(robot, false);

        visualObject.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        visualObject.transform.localRotation = Quaternion.identity;
        visualObject.transform.localScale = Vector3.one;

        SpriteRenderer renderer = visualObject.GetComponent<SpriteRenderer>();
        if (renderer == null) renderer = visualObject.AddComponent<SpriteRenderer>();
        renderer.sprite = null;
        renderer.sortingOrder = 5;
        renderer.enabled = false;
        return renderer;
    }

    private static void ConfigureCollectible(Transform itemTransform, ItemData data, Sprite sprite)
    {
        if (itemTransform == null)
        {
            Debug.LogError($"Objeto de teste para {data.ItemName} não encontrado.");
            return;
        }

        GameObject itemObject = itemTransform.gameObject;
        SpriteRenderer renderer = itemObject.GetComponent<SpriteRenderer>();
        if (renderer == null) renderer = itemObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;

        BoxCollider2D collider = itemObject.GetComponent<BoxCollider2D>();
        if (collider == null) collider = itemObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = false;
        collider.offset = Vector2.zero;
        collider.size = Vector2.one;

        CollectibleItem collectible = itemObject.GetComponent<CollectibleItem>();
        if (collectible == null) collectible = itemObject.AddComponent<CollectibleItem>();
        SerializedObject serializedItem = new SerializedObject(collectible);
        serializedItem.FindProperty("itemData").objectReferenceValue = data;
        serializedItem.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureBlockingTilemap(Transform grid, string tilemapName)
    {
        Transform tilemap = grid.Find(tilemapName);
        if (tilemap != null && tilemap.GetComponent<TilemapCollider2D>() == null)
            tilemap.gameObject.AddComponent<TilemapCollider2D>();
    }

    private static void ConfigureBlockingObject(Transform root, params string[] objectNames)
    {
        Transform blockingObject = null;
        foreach (string objectName in objectNames)
        {
            blockingObject = root.Find(objectName);
            if (blockingObject != null)
                break;
        }

        if (blockingObject == null) return;

        BoxCollider2D collider = blockingObject.GetComponent<BoxCollider2D>();
        if (collider == null) collider = blockingObject.gameObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = false;
        collider.offset = Vector2.zero;
        collider.size = Vector2.one;
    }

    private static void CreateCollectiblePrefab(ItemData data, Sprite sprite)
    {
        const string path = "Assets/Prefabs/Items/CollectibleItem.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            return;

        GameObject prefabSource = new GameObject("CollectibleItem");
        SpriteRenderer renderer = prefabSource.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 4;
        BoxCollider2D collider = prefabSource.AddComponent<BoxCollider2D>();
        collider.isTrigger = false;
        collider.offset = Vector2.zero;
        collider.size = Vector2.one;
        CollectibleItem collectible = prefabSource.AddComponent<CollectibleItem>();
        SerializedObject serializedItem = new SerializedObject(collectible);
        serializedItem.FindProperty("itemData").objectReferenceValue = data;
        serializedItem.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(prefabSource, path);
        Object.DestroyImmediate(prefabSource);
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
