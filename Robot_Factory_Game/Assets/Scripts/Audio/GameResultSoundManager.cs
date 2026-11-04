using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Toca o efeito correspondente quando uma fase termina.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class GameResultSoundManager : MonoBehaviour
{
    private static GameResultSoundManager instance;

    [Header("Sons de resultado")]
    [SerializeField] private AudioClip victorySound;
    [SerializeField] private AudioClip defeatSound;

    [Header("Saída de áudio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField, Range(0f, 1f)] private float volume = 0.8f;

    private GameSession gameSession;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        // Separa o controlador da hierarquia da cena para que o Retry não o destrua com o MusicManager duplicado.
        transform.SetParent(null, true);
        DontDestroyOnLoad(gameObject);

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 1f;

        SceneManager.sceneLoaded += OnSceneLoaded;
        BindToGameSession(FindFirstObjectByType<GameSession>());
    }

    private void OnDestroy()
    {
        if (instance != this)
            return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        BindToGameSession(null);
        instance = null;
    }

    /// <summary>
    /// Atualiza a sessão observada após carregar outra cena.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        audioSource.Stop();
        BindToGameSession(FindFirstObjectByType<GameSession>());
    }

    private void BindToGameSession(GameSession session)
    {
        if (gameSession == session)
            return;

        if (gameSession != null)
            gameSession.SessionFinished -= HandleSessionFinished;

        gameSession = session;

        if (gameSession != null)
            gameSession.SessionFinished += HandleSessionFinished;
    }

    /// <summary>
    /// Reproduz um único efeito de acordo com o estado final da partida.
    /// </summary>
    private void HandleSessionFinished(GameSession.GameState state, int productivity, int goal)
    {
        AudioClip resultSound = state == GameSession.GameState.Won ? victorySound : defeatSound;
        if (resultSound == null)
            return;

        audioSource.PlayOneShot(resultSound, volume);
    }
}
