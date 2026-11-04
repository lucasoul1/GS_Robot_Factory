using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Mantém e troca a música de acordo com a cena atual.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    private static MusicManager instance;

    [Header("Faixas de música")]
    [SerializeField] private AudioClip menuTrack;
    [SerializeField] private AudioClip gameplayTrack;

    [Header("Saída de áudio")]
    [SerializeField] private AudioSource audioSource;

    [Header("Aceleração da música nas fases")]
    [SerializeField, Range(0.1f, 1f)] private float gameplayStartPitch = 0.9f;
    [SerializeField, Range(1f, 2f)] private float gameplayEndPitch = 1.3f;

    private const string MainMenuSceneName = "MainMenu";
    private GameSession gameSession;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f;

        SceneManager.sceneLoaded += OnSceneLoaded;
        PlaySceneTrack(SceneManager.GetActiveScene());
    }

    private void OnDestroy()
    {
        if (instance != this)
            return;

        SubscribeToGameSession(null);
        SceneManager.sceneLoaded -= OnSceneLoaded;
        instance = null;
    }

    private void Update()
    {
        if (audioSource.clip != gameplayTrack || gameSession == null || gameSession.State != GameSession.GameState.Playing)
            return;

        // Acelera a reprodução proporcionalmente ao tempo já decorrido da fase.
        float elapsedProgress = 1f - gameSession.RemainingTime / gameSession.TimeLimit;
        audioSource.pitch = Mathf.Lerp(gameplayStartPitch, gameplayEndPitch, Mathf.Clamp01(elapsedProgress));
    }

    /// <summary>
    /// Troca a faixa ao carregar outra cena sem reiniciar a música quando ela é a mesma.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlaySceneTrack(scene);
    }

    private void PlaySceneTrack(Scene scene)
    {
        bool isGameplayScene = scene.name != MainMenuSceneName;
        AudioClip requestedTrack = isGameplayScene ? gameplayTrack : menuTrack;
        SubscribeToGameSession(isGameplayScene ? FindFirstObjectByType<GameSession>() : null);

        // O menu mantém a velocidade normal; cada fase reinicia a progressão de velocidade.
        audioSource.pitch = isGameplayScene ? gameplayStartPitch : 1f;
        if (requestedTrack == null)
        {
            audioSource.Stop();
            audioSource.clip = null;
            return;
        }

        if (audioSource.clip == requestedTrack && audioSource.isPlaying)
            return;

        audioSource.clip = requestedTrack;
        audioSource.Play();
    }

    /// <summary>
    /// Observa o encerramento da fase para interromper a música na tela de resultado.
    /// </summary>
    private void SubscribeToGameSession(GameSession session)
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
    /// Para a faixa de gameplay após vitória ou derrota.
    /// </summary>
    private void HandleSessionFinished(GameSession.GameState state, int productivity, int goal)
    {
        audioSource.Stop();
    }
}
