using System;
using UnityEngine;

/// <summary>
/// Controla a contagem regressiva e encerra a partida com um resultado único.
/// </summary>
public class GameSession : MonoBehaviour
{
    public enum GameState
    {
        Playing,
        Won,
        Lost
    }

    [Header("Configuração da partida")]
    [SerializeField, Min(1f)] private float timeLimit = 80f;
    [SerializeField] private ProductivityManager productivityManager;
    [SerializeField] private FactoryAlertManager factoryAlertManager;

    private float remainingTime;

    public GameState State { get; private set; } = GameState.Playing;
    public float RemainingTime => remainingTime;
    public float TimeLimit => timeLimit;
    public event Action<GameState, int, int> SessionFinished;

    private void Awake()
    {
        remainingTime = timeLimit;

        if (productivityManager == null)
            productivityManager = FindFirstObjectByType<ProductivityManager>();

        if (factoryAlertManager == null)
            factoryAlertManager = FindFirstObjectByType<FactoryAlertManager>();

        if (productivityManager == null)
        {
            Debug.LogError("GameSession precisa de um ProductivityManager na cena.", this);
            enabled = false;
        }
    }

    private void Start()
    {
        if (factoryAlertManager == null)
            return;

        factoryAlertManager.PenaltyDepleted += HandlePenaltyDepleted;

        // Trata a configuração de teste em que a penalidade já começa zerada.
        if (factoryAlertManager.IsPenaltyDepleted)
            HandlePenaltyDepleted();
    }

    private void OnDestroy()
    {
        if (factoryAlertManager != null)
            factoryAlertManager.PenaltyDepleted -= HandlePenaltyDepleted;
    }

    private void Update()
    {
        if (State != GameState.Playing)
            return;

        remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);
        if (remainingTime <= 0f)
            FinishSession();
    }

    /// <summary>
    /// Avalia a meta uma vez e bloqueia controles e novas gerações da esteira.
    /// </summary>
    private void FinishSession()
    {
        GameState result = productivityManager.CurrentProductivity >= productivityManager.ProductivityGoal
            ? GameState.Won
            : GameState.Lost;

        FinishSession(result);
    }

    /// <summary>
    /// Encerra a partida com o resultado indicado, sem permitir avaliações duplicadas.
    /// </summary>
    private void FinishSession(GameState result)
    {
        if (State != GameState.Playing)
            return;

        State = result;

        DisableGameplayControls();
        Debug.Log(State == GameState.Won ? "Game Won." : "Game Lost.", this);
        SessionFinished?.Invoke(
            State,
            productivityManager.CurrentProductivity,
            productivityManager.ProductivityGoal);
    }

    /// <summary>
    /// A penalidade zerada encerra a fase como derrota, mesmo que ainda haja tempo.
    /// </summary>
    private void HandlePenaltyDepleted()
    {
        FinishSession(GameState.Lost);
    }

    /// <summary>
    /// Desativa os componentes de entrada e a geração contínua da esteira.
    /// </summary>
    private static void DisableGameplayControls()
    {
        RobotGridMovement[] movementControllers = FindObjectsByType<RobotGridMovement>(FindObjectsSortMode.None);
        foreach (RobotGridMovement movement in movementControllers)
        {
            movement.StopMovement();
            movement.enabled = false;
        }

        RobotInteraction[] interactionControllers = FindObjectsByType<RobotInteraction>(FindObjectsSortMode.None);
        foreach (RobotInteraction interaction in interactionControllers)
            interaction.enabled = false;

        MachineAlert[] machineAlerts = FindObjectsByType<MachineAlert>();
        foreach (MachineAlert machineAlert in machineAlerts)
            machineAlert.StopAlert();

        ConveyorSpawner[] conveyorSpawners = FindObjectsByType<ConveyorSpawner>(FindObjectsSortMode.None);
        foreach (ConveyorSpawner spawner in conveyorSpawners)
            spawner.enabled = false;
    }
}
