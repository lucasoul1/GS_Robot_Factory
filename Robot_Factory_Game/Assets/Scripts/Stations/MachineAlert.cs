using UnityEngine;

/// <summary>
/// Acompanha há quanto tempo uma estação está sem receber uma peça correta.
/// </summary>
public class MachineAlert : MonoBehaviour
{
    private FactoryAlertManager alertManager;
    private SpriteRenderer alertRenderer;
    private Sprite warningSprite;
    private Sprite criticalSprite;
    private float warningDelay;
    private float criticalDelay;
    private float penaltyInterval;
    private int penaltyPoints;

    private float idleTime;
    private float nextPenaltyTime;
    private bool isWarning;
    private bool isCritical;
    private bool isStopped;

    /// <summary>
    /// Recebe as configurações compartilhadas pelo gerenciador da fábrica.
    /// </summary>
    public void Configure(
        FactoryAlertManager manager,
        SpriteRenderer renderer,
        Sprite yellowSprite,
        Sprite redSprite,
        float yellowDelay,
        float redDelay,
        float repeatPenaltyInterval,
        int penalty)
    {
        alertManager = manager;
        alertRenderer = renderer;
        warningSprite = yellowSprite;
        criticalSprite = redSprite;
        warningDelay = Mathf.Max(0f, yellowDelay);
        criticalDelay = Mathf.Max(warningDelay, redDelay);
        penaltyInterval = Mathf.Max(0.1f, repeatPenaltyInterval);
        penaltyPoints = Mathf.Max(0, penalty);
        isStopped = false;
        ResetAlert();
    }

    private void Update()
    {
        if (isStopped)
            return;

        idleTime += Time.deltaTime;

        if (!isWarning && idleTime >= warningDelay)
        {
            isWarning = true;
            ShowAlert(warningSprite);
        }

        if (!isCritical && idleTime >= criticalDelay)
        {
            isCritical = true;
            nextPenaltyTime = idleTime + penaltyInterval;
            ShowAlert(criticalSprite);
            alertManager.SetCritical(this, true);
            alertManager.ApplyPenalty(penaltyPoints);
        }
        else if (isCritical && idleTime >= nextPenaltyTime)
        {
            nextPenaltyTime += penaltyInterval;
            alertManager.ApplyPenalty(penaltyPoints);
        }
    }

    /// <summary>
    /// Zera a urgência somente depois de a estação processar uma peça correta.
    /// </summary>
    public void OnItemProcessed()
    {
        if (isStopped)
            return;

        ResetAlert();
    }

    /// <summary>
    /// Encerra alertas e penalidades quando a partida termina.
    /// </summary>
    public void StopAlert()
    {
        isStopped = true;
        ResetAlert();
    }

    private void ResetAlert()
    {
        idleTime = 0f;
        isWarning = false;
        isCritical = false;
        nextPenaltyTime = 0f;

        if (alertRenderer != null)
        {
            alertRenderer.sprite = null;
            alertRenderer.enabled = false;
        }

        if (alertManager != null)
            alertManager.SetCritical(this, false);
    }

    private void ShowAlert(Sprite sprite)
    {
        if (alertRenderer == null)
            return;

        alertRenderer.sprite = sprite;
        alertRenderer.enabled = sprite != null;
    }
}
