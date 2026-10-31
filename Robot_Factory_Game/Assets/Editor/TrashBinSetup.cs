using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Configura a lixeira já existente na cena como um objeto interativo reutilizável.
/// </summary>
public static class TrashBinSetup
{
    private const string ScenePath = "Assets/Scenes/FactoryLevel01.unity";
    private const string PrefabPath = "Assets/Prefabs/Stations/TrashBin.prefab";
    private const string SetupMarker = "Assets/Editor/TrashBinSetup.marker";

    // O marcador solicita a aplicação única da configuração na cena existente.
    [InitializeOnLoadMethod]
    private static void RunWhenRequested()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(SetupMarker))
                return;

            if (ApplySetup())
            {
                File.Delete(SetupMarker);
                AssetDatabase.Refresh();
            }
        };
    }

    [MenuItem("Tools/GS Robot Factory/Configure Trash Bin")]
    public static void ConfigureTrashBin()
    {
        ApplySetup();
    }

    private static bool ApplySetup()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Transform placeholders = GameObject.Find("TestPlaceholders")?.transform;
        Transform trashBin = placeholders != null ? placeholders.Find("Trash_Bin") : null;
        if (trashBin == null)
        {
            Debug.LogError("Trash_Bin não encontrado em FactoryLevel01.");
            return false;
        }

        BoxCollider2D collider = trashBin.GetComponent<BoxCollider2D>();
        if (collider == null)
            collider = trashBin.gameObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = false;
        collider.offset = Vector2.zero;
        collider.size = Vector2.one;

        if (trashBin.GetComponent<TrashBin>() == null)
            trashBin.gameObject.AddComponent<TrashBin>();

        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Prefabs/Stations");
        PrefabUtility.SaveAsPrefabAssetAndConnect(
            trashBin.gameObject,
            PrefabPath,
            InteractionMode.AutomatedAction,
            out bool savedSuccessfully);

        if (!savedSuccessfully)
        {
            Debug.LogError("Não foi possível salvar o Prefab TrashBin.", trashBin);
            return false;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Trash_Bin configurada como objeto interativo e conectada ao Prefab TrashBin.");
        return true;
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
