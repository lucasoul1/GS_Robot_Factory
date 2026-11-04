using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Cria o Prefab do controlador de música e o adiciona às cenas do jogo.
/// </summary>
public static class MusicSceneSetup
{
    private const string PrefabFolder = "Assets/Prefabs/Audio";
    private const string PrefabPath = PrefabFolder + "/MusicManager.prefab";
    private const string MenuTrackPath = "Assets/Audio/Music/Menu_8Bit.wav";
    private const string GameplayTrackPath = "Assets/Audio/Music/Gameplay_8Bit.wav";
    private const string MarkerPath = "Assets/Editor/MusicSceneSetup.marker";

    private static readonly string[] ScenePaths =
    {
        "Assets/Scenes/MainMenu.unity",
        "Assets/Scenes/FactoryLevel01.unity",
        "Assets/Scenes/FactoryLevel02.unity",
        "Assets/Scenes/FactoryLevel03.unity",
        "Assets/Scenes/FactoryLevel04.unity",
        "Assets/Scenes/FactoryLevel05.unity"
    };

    [InitializeOnLoadMethod]
    private static void RunWhenRequested()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(MarkerPath))
                return;

            if (!ConfigureMusic())
                return;

            File.Delete(MarkerPath);
            if (File.Exists(MarkerPath + ".meta"))
                File.Delete(MarkerPath + ".meta");
            AssetDatabase.Refresh();
        };
    }

    [MenuItem("Tools/GS Robot Factory/Configure Music")]
    public static void ConfigureMusicFromMenu()
    {
        ConfigureMusic();
    }

    private static bool ConfigureMusic()
    {
        AudioClip menuTrack = AssetDatabase.LoadAssetAtPath<AudioClip>(MenuTrackPath);
        AudioClip gameplayTrack = AssetDatabase.LoadAssetAtPath<AudioClip>(GameplayTrackPath);
        if (menuTrack == null || gameplayTrack == null)
        {
            Debug.LogError("As duas faixas de música precisam estar importadas antes de configurar o MusicManager.");
            return false;
        }

        EnsureFolder("Assets/Prefabs");
        EnsureFolder(PrefabFolder);
        GameObject managerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (managerPrefab == null)
        {
            managerPrefab = CreateMusicPrefab(menuTrack, gameplayTrack);
            if (managerPrefab == null)
                return false;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return false;

        SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (string scenePath in ScenePaths)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
                {
                    Debug.LogError($"A cena {scenePath} não foi encontrada; nenhuma outra cena foi modificada depois deste ponto.");
                    return false;
                }

                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                if (!EnsureAudioListener(scene))
                    return false;

                MusicManager existingManager = Object.FindAnyObjectByType<MusicManager>();
                if (existingManager == null)
                {
                    PrefabUtility.InstantiatePrefab(managerPrefab);
                    EditorSceneManager.MarkSceneDirty(scene);
                }

                EditorSceneManager.SaveScene(scene);
            }
        }
        finally
        {
            EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("MusicManager e AudioListener configurados no menu e nas cinco fases.");
        return true;
    }

    private static bool EnsureAudioListener(Scene scene)
    {
        AudioListener[] listeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        if (listeners.Length > 0)
            return true;

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError($"A cena {scene.name} não tem AudioListener nem uma Main Camera para recebê-lo.");
            return false;
        }

        mainCamera.gameObject.AddComponent<AudioListener>();
        EditorUtility.SetDirty(mainCamera.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);
        return true;
    }

    private static GameObject CreateMusicPrefab(AudioClip menuTrack, AudioClip gameplayTrack)
    {
        GameObject managerObject = new GameObject("MusicManager");
        AudioSource source = managerObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.volume = 0.5f;
        source.spatialBlend = 0f;

        MusicManager manager = managerObject.AddComponent<MusicManager>();
        SerializedObject managerData = new SerializedObject(manager);
        managerData.FindProperty("menuTrack").objectReferenceValue = menuTrack;
        managerData.FindProperty("gameplayTrack").objectReferenceValue = gameplayTrack;
        managerData.FindProperty("audioSource").objectReferenceValue = source;
        managerData.FindProperty("gameplayStartPitch").floatValue = 0.9f;
        managerData.FindProperty("gameplayEndPitch").floatValue = 1.3f;
        managerData.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(managerObject, PrefabPath);
        Object.DestroyImmediate(managerObject);
        return prefab;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        int separator = folderPath.LastIndexOf('/');
        string parent = folderPath.Substring(0, separator);
        string folderName = folderPath.Substring(separator + 1);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }
}
