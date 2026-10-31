using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Configura alertas das estações, a penalidade da etapa anterior e o véu crítico.
/// </summary>
public class FactoryAlertManager : MonoBehaviour
{
    [Header("Sprites de alerta")]
    [SerializeField] private Sprite warningSprite;
    [SerializeField] private Sprite criticalSprite;

    [Header("Tempos")]
    [SerializeField, Min(0f)] private float warningDelay = 15f;
    [SerializeField, Min(0f)] private float criticalDelay = 30f;
    [SerializeField, Min(0.1f)] private float penaltyInterval = 10f;

    [Header("Pontuação")]
    [SerializeField, Min(0)] private int startingScore = 100;
    [SerializeField, Min(0)] private int penaltyPoints = 10;
    [SerializeField] private Text penaltyText;

    private readonly HashSet<MachineAlert> criticalStations = new HashSet<MachineAlert>();
    private Image redOverlay;
    private int score;

    /// <summary>
    /// Notifica quando a penalidade chega a zero pela primeira vez.
    /// </summary>
    public event Action PenaltyDepleted;

    public bool IsPenaltyDepleted => score <= 0;

    private void Awake()
    {
        score = startingScore;
        CreateHud();
        ConfigureStations();
        UpdateHud();
    }

    /// <summary>
    /// Cria o ícone sobre cada estação e aplica a mesma regra de tempo.
    /// </summary>
    private void ConfigureStations()
    {
        ProcessingStation[] stations = FindObjectsByType<ProcessingStation>(FindObjectsSortMode.None);

        foreach (ProcessingStation station in stations)
        {
            MachineAlert machineAlert = station.GetComponent<MachineAlert>();
            if (machineAlert == null)
                machineAlert = station.gameObject.AddComponent<MachineAlert>();

            Transform iconTransform = station.transform.Find("AlertPopup");
            if (iconTransform == null)
            {
                GameObject iconObject = new GameObject("AlertPopup");
                iconTransform = iconObject.transform;
                iconTransform.SetParent(station.transform, false);
                iconTransform.localPosition = new Vector3(0f, 0.8f, 0f);
                iconTransform.localScale = Vector3.one * 0.65f;
            }

            SpriteRenderer iconRenderer = iconTransform.GetComponent<SpriteRenderer>();
            if (iconRenderer == null)
                iconRenderer = iconTransform.gameObject.AddComponent<SpriteRenderer>();

            iconRenderer.sortingOrder = 20;
            iconRenderer.enabled = false;
            machineAlert.Configure(
                this,
                iconRenderer,
                warningSprite,
                criticalSprite,
                warningDelay,
                criticalDelay,
                penaltyInterval,
                penaltyPoints);
        }
    }

    /// <summary>
    /// Cria o véu vermelho de urgência; o texto da penalidade está no Canvas da cena.
    /// </summary>
    private void CreateHud()
    {
        GameObject canvasObject = new GameObject("Factory Alert HUD");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(640f, 360f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject overlayObject = new GameObject("Critical Red Overlay", typeof(RectTransform), typeof(Image));
        overlayObject.transform.SetParent(canvasObject.transform, false);
        RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        redOverlay = overlayObject.GetComponent<Image>();
        redOverlay.color = new Color(1f, 0f, 0f, 0.24f);
        redOverlay.raycastTarget = false;
        redOverlay.enabled = false;

    }

    /// <summary>
    /// Mantém a tela vermelha até todas as estações críticas serem atendidas.
    /// </summary>
    public void SetCritical(MachineAlert station, bool isActive)
    {
        if (station == null)
            return;

        if (isActive)
            criticalStations.Add(station);
        else
            criticalStations.Remove(station);

        if (redOverlay != null)
            redOverlay.enabled = criticalStations.Count > 0;
    }

    /// <summary>
    /// Aplica a perda configurada sem permitir que a pontuação fique negativa.
    /// </summary>
    public void ApplyPenalty(int points)
    {
        if (score <= 0)
            return;

        score = Mathf.Max(0, score - points);
        UpdateHud();
        Debug.Log($"Machine alert: -{points} points. Score: {score}.", this);

        if (score == 0)
            PenaltyDepleted?.Invoke();
    }

    private void UpdateHud()
    {
        if (penaltyText != null)
            penaltyText.text = $"PENALTY: {score}";
    }
}
