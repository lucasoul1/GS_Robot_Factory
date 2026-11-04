using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Centraliza os efeitos de coleta e entrega bem-sucedidas.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class PlayerActionSoundManager : MonoBehaviour
{
    private static PlayerActionSoundManager instance;

    [Header("Sons das ações do robô")]
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private AudioClip deliverySound;

    [Header("Saída de áudio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField, Range(0f, 1f)] private float volume = 0.75f;

    private readonly List<ProcessingStation> connectedStations = new List<ProcessingStation>();
    private RobotCarry robotCarry;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        // Mantém o áudio independente dos objetos temporários de cada cena.
        transform.SetParent(null, true);
        DontDestroyOnLoad(gameObject);

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 1f;

        SceneManager.sceneLoaded += OnSceneLoaded;
        ConnectToSceneObjects();
    }

    private void OnDestroy()
    {
        if (instance != this)
            return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        DisconnectFromSceneObjects();
        instance = null;
    }

    /// <summary>
    /// Troca as assinaturas de eventos para os objetos da cena recém-carregada.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        audioSource.Stop();
        ConnectToSceneObjects();
    }

    private void ConnectToSceneObjects()
    {
        DisconnectFromSceneObjects();

        robotCarry = FindFirstObjectByType<RobotCarry>();
        if (robotCarry != null)
            robotCarry.ItemCollected += HandleItemCollected;

        ProcessingStation[] stations = FindObjectsByType<ProcessingStation>(FindObjectsSortMode.None);
        foreach (ProcessingStation station in stations)
        {
            station.ItemProcessed += HandleItemProcessed;
            connectedStations.Add(station);
        }
    }

    private void DisconnectFromSceneObjects()
    {
        if (robotCarry != null)
            robotCarry.ItemCollected -= HandleItemCollected;

        robotCarry = null;

        foreach (ProcessingStation station in connectedStations)
        {
            if (station != null)
                station.ItemProcessed -= HandleItemProcessed;
        }

        connectedStations.Clear();
    }

    private void HandleItemCollected(ItemData itemData)
    {
        PlaySound(pickupSound);
    }

    private void HandleItemProcessed(ItemData itemData)
    {
        PlaySound(deliverySound);
    }

    /// <summary>
    /// Reproduz o efeito sem interromper a música ou outros sistemas de áudio.
    /// </summary>
    private void PlaySound(AudioClip sound)
    {
        if (sound != null)
            audioSource.PlayOneShot(sound, volume);
    }
}
