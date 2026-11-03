using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controla a navegação do menu inicial e as opções básicas do jogo.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Painéis do menu")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject phaseSelectionPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private GameObject[] tutorialPages;
    [SerializeField] private Text tutorialPageIndicatorText;
    [SerializeField] private Button tutorialPreviousButton;
    [SerializeField] private Button tutorialNextButton;

    [Header("Opções")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Text masterVolumeValueText;
    [SerializeField] private Toggle fullscreenToggle;

    [Header("Cena inicial")]
    [SerializeField] private string firstLevelScene = "FactoryLevel01";
    [SerializeField] private string secondLevelScene = "FactoryLevel02";
    [SerializeField] private string thirdLevelScene = "FactoryLevel03";
    [SerializeField] private string fourthLevelScene = "FactoryLevel04";
    [SerializeField] private string fifthLevelScene = "FactoryLevel05";

    private int currentTutorialPage;

    private const string VolumePreferenceKey = "RobotFactory.MasterVolume";
    private const string FullscreenPreferenceKey = "RobotFactory.Fullscreen";

    private void Start()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(ApplyMasterVolume);

        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(ApplyFullscreen);

        LoadOptions();
        SetTutorialPage(0);
        ShowMainMenu();
    }

    private void OnDestroy()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.RemoveListener(ApplyMasterVolume);

        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.RemoveListener(ApplyFullscreen);
    }

    /// <summary>
    /// Exibe somente a tela principal do menu.
    /// </summary>
    public void ShowMainMenu()
    {
        SetVisiblePanel(mainPanel);
    }

    /// <summary>
    /// Abre a grade de fases disponíveis.
    /// </summary>
    public void ShowPhaseSelection()
    {
        SetVisiblePanel(phaseSelectionPanel);
    }

    /// <summary>
    /// Abre os ajustes básicos de áudio e tela.
    /// </summary>
    public void ShowOptions()
    {
        SetVisiblePanel(optionsPanel);
    }

    /// <summary>
    /// Abre o painel informativo com os controles e o fluxo da fase.
    /// </summary>
    public void ShowTutorial()
    {
        SetVisiblePanel(tutorialPanel);
        SetTutorialPage(0);
    }

    /// <summary>
    /// Avança para a próxima página informativa do tutorial.
    /// </summary>
    public void NextTutorialPage()
    {
        SetTutorialPage(currentTutorialPage + 1);
    }

    /// <summary>
    /// Retorna à página anterior do tutorial.
    /// </summary>
    public void PreviousTutorialPage()
    {
        SetTutorialPage(currentTutorialPage - 1);
    }

    /// <summary>
    /// Inicia a primeira fase quando ela está incluída nas cenas de build.
    /// </summary>
    public void PlayLevelOne()
    {
        LoadLevel(firstLevelScene);
    }

    /// <summary>
    /// Inicia a segunda fase quando ela está incluída nas cenas de build.
    /// </summary>
    public void PlayLevelTwo()
    {
        LoadLevel(secondLevelScene);
    }

    /// <summary>
    /// Inicia a terceira fase quando ela está incluída nas cenas de build.
    /// </summary>
    public void PlayLevelThree()
    {
        LoadLevel(thirdLevelScene);
    }

    /// <summary>
    /// Inicia a quarta fase quando ela está incluída nas cenas de build.
    /// </summary>
    public void PlayLevelFour()
    {
        LoadLevel(fourthLevelScene);
    }

    /// <summary>
    /// Inicia a quinta fase quando ela está incluída nas cenas de build.
    /// </summary>
    public void PlayLevelFive()
    {
        LoadLevel(fifthLevelScene);
    }

    /// <summary>
    /// Aplica e salva o volume geral escolhido no menu.
    /// </summary>
    public void ApplyMasterVolume(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat(VolumePreferenceKey, volume);
        PlayerPrefs.Save();
        UpdateVolumeLabel(volume);
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
    /// Encerra o jogo compilado; no Editor, informa que o botão foi acionado.
    /// </summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        Debug.Log("The QUIT button was pressed. Application.Quit works in a compiled build.", this);
#else
        Application.Quit();
#endif
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

    private void LoadLevel(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"A cena da fase '{sceneName}' não está nas cenas de build.", this);
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    private void SetVisiblePanel(GameObject visiblePanel)
    {
        if (mainPanel != null)
            mainPanel.SetActive(mainPanel == visiblePanel);

        if (phaseSelectionPanel != null)
            phaseSelectionPanel.SetActive(phaseSelectionPanel == visiblePanel);

        if (optionsPanel != null)
            optionsPanel.SetActive(optionsPanel == visiblePanel);

        if (tutorialPanel != null)
            tutorialPanel.SetActive(tutorialPanel == visiblePanel);
    }

    private void UpdateVolumeLabel(float volume)
    {
        if (masterVolumeValueText != null)
            masterVolumeValueText.text = $"{Mathf.RoundToInt(volume * 100f)}%";
    }

    private void SetTutorialPage(int pageIndex)
    {
        if (tutorialPages == null || tutorialPages.Length == 0)
            return;

        currentTutorialPage = Mathf.Clamp(pageIndex, 0, tutorialPages.Length - 1);

        for (int i = 0; i < tutorialPages.Length; i++)
        {
            if (tutorialPages[i] != null)
                tutorialPages[i].SetActive(i == currentTutorialPage);
        }

        if (tutorialPageIndicatorText != null)
            tutorialPageIndicatorText.text = $"{currentTutorialPage + 1} / {tutorialPages.Length}";

        if (tutorialPreviousButton != null)
            tutorialPreviousButton.interactable = currentTutorialPage > 0;

        if (tutorialNextButton != null)
            tutorialNextButton.interactable = currentTutorialPage < tutorialPages.Length - 1;
    }
}
