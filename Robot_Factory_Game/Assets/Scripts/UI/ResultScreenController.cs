using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;

/// <summary>
/// Exibe o painel correspondente ao resultado e permite reiniciar a fase.
/// </summary>
public class ResultScreenController : MonoBehaviour
{
    [Header("Referências da partida")]
    [SerializeField] private GameSession gameSession;
    [SerializeField] private GameObject overlay;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject lostPanel;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private string nextLevelScene;

    [Header("Textos de resultado")]
    [SerializeField] private Text winProductivityText;
    [SerializeField] private Text winGoalText;
    [SerializeField] private Text lostProductivityText;
    [SerializeField] private Text lostGoalText;

    [Header("HUD da partida")]
    [SerializeField] private Graphic[] gameplayHudElements;

    private void Awake()
    {
        if (overlay != null)
            overlay.SetActive(false);

        if (winPanel != null)
            winPanel.SetActive(false);

        if (lostPanel != null)
            lostPanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (gameSession != null)
            gameSession.SessionFinished += ShowResult;
    }

    private void Start()
    {
        UpdateNextLevelButton();

        if (gameSession == null)
            gameSession = FindFirstObjectByType<GameSession>();

        if (gameSession == null)
        {
            Debug.LogError("ResultScreenController precisa de um GameSession na cena.", this);
            return;
        }

        // A inscrição também é feita aqui para cobrir a referência localizada no Start.
        gameSession.SessionFinished -= ShowResult;
        gameSession.SessionFinished += ShowResult;

        if (gameSession.State != GameSession.GameState.Playing)
        {
            ProductivityManager manager = FindFirstObjectByType<ProductivityManager>();
            if (manager != null)
                ShowResult(gameSession.State, manager.CurrentProductivity, manager.ProductivityGoal);
        }
    }

    private void OnDisable()
    {
        if (gameSession != null)
            gameSession.SessionFinished -= ShowResult;
    }

    /// <summary>
    /// Atualiza os valores e deixa visível somente o painel do resultado alcançado.
    /// </summary>
    private void ShowResult(GameSession.GameState state, int productivity, int goal)
    {
        bool won = state == GameSession.GameState.Won;

        if (overlay != null)
            overlay.SetActive(true);

        if (winPanel != null)
            winPanel.SetActive(won);

        if (lostPanel != null)
            lostPanel.SetActive(!won);

        SetScoreText(winProductivityText, "PRODUCTIVITY: ", productivity);
        SetScoreText(winGoalText, "REQUIRED: ", goal);
        SetScoreText(lostProductivityText, "PRODUCTIVITY: ", productivity);
        SetScoreText(lostGoalText, "REQUIRED: ", goal);

        if (gameplayHudElements == null)
            return;

        foreach (Graphic hudElement in gameplayHudElements)
        {
            if (hudElement != null)
                hudElement.enabled = false;
        }
    }

    /// <summary>
    /// Reinicia a cena inteira para restaurar os estados iniciais da fase.
    /// </summary>
    public void Retry()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Abre a próxima fase quando seu nome estiver configurado e ela estiver nas cenas disponíveis.
    /// </summary>
    public void NextLevel()
    {
        if (!CanLoadNextLevel())
        {
            Debug.LogWarning("Configure a próxima cena e inclua-a nas cenas de build antes de usar NEXT LEVEL.", this);
            return;
        }

        SceneManager.LoadScene(nextLevelScene);
    }

    private void UpdateNextLevelButton()
    {
        if (nextLevelButton != null)
            nextLevelButton.interactable = CanLoadNextLevel();
    }

    private bool CanLoadNextLevel()
    {
        return !string.IsNullOrWhiteSpace(nextLevelScene)
            && Application.CanStreamedLevelBeLoaded(nextLevelScene);
    }

    private static void SetScoreText(Text text, string label, int value)
    {
        if (text != null)
            text.text = $"{label}{value}";
    }
}
