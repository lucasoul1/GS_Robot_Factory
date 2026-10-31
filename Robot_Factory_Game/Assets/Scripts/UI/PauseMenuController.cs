using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Pausa a fase, abre as opções e permite retornar ao menu principal.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    private const string VolumePreferenceKey = "RobotFactory.MasterVolume";
    private const string FullscreenPreferenceKey = "RobotFactory.Fullscreen";
    private const string MainMenuScene = "MainMenu";

    [Header("Referências da partida")]
    [SerializeField] private GameSession gameSession;
    [SerializeField] private GameObject pauseOverlay;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject optionsPanel;

    [Header("Opções")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Text masterVolumeValueText;
    [SerializeField] private Toggle fullscreenToggle;

    private float previousTimeScale = 1f;
    private bool isPaused;
    private RobotGridMovement[] movementControllers;
    private bool[] movementEnabledStates;
    private RobotInteraction[] interactionControllers;
    private bool[] interactionEnabledStates;

    private void Awake()
    {
        if (gameSession == null)
            gameSession = FindFirstObjectByType<GameSession>();

        if (pauseOverlay != null)
            pauseOverlay.SetActive(false);

        if (pausePanel != null)
            pausePanel.SetActive(true);

        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        LoadOptions();
    }

    private void OnDestroy()
    {
        if (isPaused)
        {
            SetGameplayInputEnabled(true);
            Time.timeScale = previousTimeScale;
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
            return;

        if (gameSession != null && gameSession.State != GameSession.GameState.Playing)
            return;

        if (!isPaused)
            PauseGame();
        else if (optionsPanel != null && optionsPanel.activeSelf)
            ShowPausePanel();
        else
            ResumeGame();
    }

    /// <summary>
    /// Congela o tempo e bloqueia as entradas de gameplay enquanto a pausa está aberta.
    /// </summary>
    public void PauseGame()
    {
        if (isPaused || (gameSession != null && gameSession.State != GameSession.GameState.Playing))
            return;

        isPaused = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        SetGameplayInputEnabled(false);
        ShowPausePanel();
    }

    /// <summary>
    /// Fecha os painéis e restaura o tempo e os controles anteriores.
    /// </summary>
    public void ResumeGame()
    {
        if (!isPaused)
            return;

        if (pauseOverlay != null)
            pauseOverlay.SetActive(false);

        Time.timeScale = previousTimeScale;
        SetGameplayInputEnabled(true);
        isPaused = false;
    }

    /// <summary>
    /// Mostra a tela de opções dentro do menu de pausa.
    /// </summary>
    public void ShowOptions()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (optionsPanel != null)
            optionsPanel.SetActive(true);
    }

    /// <summary>
    /// Retorna da tela de opções para os controles da pausa.
    /// </summary>
    public void ShowPausePanel()
    {
        if (pauseOverlay != null)
            pauseOverlay.SetActive(true);

        if (pausePanel != null)
            pausePanel.SetActive(true);

        if (optionsPanel != null)
            optionsPanel.SetActive(false);
    }

    /// <summary>
    /// Aplica e salva o volume geral escolhido.
    /// </summary>
    public void ApplyMasterVolume(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat(VolumePreferenceKey, volume);
        PlayerPrefs.Save();

        if (masterVolumeValueText != null)
            masterVolumeValueText.text = $"{Mathf.RoundToInt(volume * 100f)}%";
    }

    /// <summary>
    /// Aplica e salva a opção de tela cheia.
    /// </summary>
    public void ApplyFullscreen(bool enabled)
    {
        Screen.fullScreen = enabled;
        PlayerPrefs.SetInt(FullscreenPreferenceKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Restaura a escala global antes de carregar o menu principal.
    /// </summary>
    public void ReturnToMainMenu()
    {
        if (!Application.CanStreamedLevelBeLoaded(MainMenuScene))
        {
            Debug.LogError("MainMenu não está incluída nas cenas disponíveis para carregamento.", this);
            return;
        }

        SetGameplayInputEnabled(true);
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene(MainMenuScene);
    }

    private void LoadOptions()
    {
        float volume = PlayerPrefs.GetFloat(VolumePreferenceKey, 1f);
        bool fullscreen = PlayerPrefs.GetInt(FullscreenPreferenceKey, Screen.fullScreen ? 1 : 0) == 1;

        if (masterVolumeSlider != null)
            masterVolumeSlider.SetValueWithoutNotify(volume);

        if (fullscreenToggle != null)
            fullscreenToggle.SetIsOnWithoutNotify(fullscreen);

        ApplyMasterVolume(volume);
        ApplyFullscreen(fullscreen);
    }

    private void SetGameplayInputEnabled(bool enabled)
    {
        if (!enabled)
        {
            movementControllers = FindObjectsByType<RobotGridMovement>(FindObjectsSortMode.None);
            movementEnabledStates = new bool[movementControllers.Length];
            for (int i = 0; i < movementControllers.Length; i++)
            {
                movementEnabledStates[i] = movementControllers[i].enabled;
                movementControllers[i].enabled = false;
            }

            interactionControllers = FindObjectsByType<RobotInteraction>(FindObjectsSortMode.None);
            interactionEnabledStates = new bool[interactionControllers.Length];
            for (int i = 0; i < interactionControllers.Length; i++)
            {
                interactionEnabledStates[i] = interactionControllers[i].enabled;
                interactionControllers[i].enabled = false;
            }

            return;
        }

        RestoreEnabledStates(movementControllers, movementEnabledStates);
        RestoreEnabledStates(interactionControllers, interactionEnabledStates);
    }

    private static void RestoreEnabledStates<T>(T[] components, bool[] enabledStates) where T : Behaviour
    {
        if (components == null || enabledStates == null)
            return;

        for (int i = 0; i < components.Length && i < enabledStates.Length; i++)
        {
            if (components[i] != null)
                components[i].enabled = enabledStates[i];
        }
    }
}
