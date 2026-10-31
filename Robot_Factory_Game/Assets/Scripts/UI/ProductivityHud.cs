using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Apresenta produtividade e item carregado sem conter regras de pontuação.
/// </summary>
public class ProductivityHud : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private ProductivityManager productivityManager;
    [SerializeField] private RobotCarry robotCarry;
    [SerializeField] private Canvas hudCanvas;
    [SerializeField] private Text productivityText;
    [SerializeField] private Text carryingText;
    [SerializeField] private Text timerText;
    [SerializeField] private Font hudFont;
    [SerializeField] private GameSession gameSession;

    private ItemData lastCarriedItem;

    private void Start()
    {
        ResolveReferences();
        CreateMissingHudElements();

        if (productivityManager != null)
        {
            productivityManager.ProductivityChanged += UpdateProductivity;
            UpdateProductivity(
                productivityManager.CurrentProductivity,
                productivityManager.ProductivityGoal);
        }
        else if (productivityText != null)
        {
            productivityText.text = "PRODUCTIVITY: 0 / 100";
        }

        UpdateCarrying();
        UpdateTimer();
    }

    private void OnDestroy()
    {
        if (productivityManager != null)
            productivityManager.ProductivityChanged -= UpdateProductivity;
    }

    private void Update()
    {
        UpdateTimer();

        if (robotCarry == null)
            return;

        ItemData currentItem = robotCarry.CarriedItemData;
        if (currentItem == lastCarriedItem)
            return;

        lastCarriedItem = currentItem;
        UpdateCarrying();
    }

    private void ResolveReferences()
    {
        if (productivityManager == null)
            productivityManager = FindFirstObjectByType<ProductivityManager>();

        if (robotCarry == null)
            robotCarry = FindFirstObjectByType<RobotCarry>();

        if (gameSession == null)
            gameSession = FindFirstObjectByType<GameSession>();
    }

    /// <summary>
    /// Reutiliza o Canvas e os textos montados no Editor ou cria uma interface mínima.
    /// </summary>
    private void CreateMissingHudElements()
    {
        if (hudCanvas == null && (productivityText == null || carryingText == null))
        {
            GameObject canvasObject = new GameObject("Productivity HUD Canvas");
            hudCanvas = canvasObject.AddComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hudCanvas.sortingOrder = 110;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(640f, 360f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        if (productivityText == null)
            productivityText = CreateText("Productivity Text", false);

        if (carryingText == null)
            carryingText = CreateText("Carrying Text", true);

        if (timerText == null)
            timerText = CreateTimerText();
    }

    private Text CreateText(string objectName, bool alignRight)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(hudCanvas.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = alignRight ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
        textRect.anchorMax = textRect.anchorMin;
        textRect.pivot = alignRight ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
        textRect.anchoredPosition = alignRight ? new Vector2(-12f, -12f) : new Vector2(12f, -12f);
        textRect.sizeDelta = new Vector2(alignRight ? 240f : 220f, 18f);

        Text text = textObject.GetComponent<Text>();
        text.font = hudFont != null ? hudFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 8;
        text.color = Color.white;
        text.alignment = alignRight ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
        text.raycastTarget = false;
        Shadow shadow = textObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
        shadow.effectDistance = new Vector2(1f, -1f);
        return text;
    }

    /// <summary>
    /// Cria o texto do cronômetro no centro superior do Canvas.
    /// </summary>
    private Text CreateTimerText()
    {
        GameObject textObject = new GameObject("Timer Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(hudCanvas.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 1f);
        textRect.anchorMax = textRect.anchorMin;
        textRect.pivot = textRect.anchorMin;
        textRect.anchoredPosition = new Vector2(0f, -12f);
        textRect.sizeDelta = new Vector2(140f, 18f);

        Text text = textObject.GetComponent<Text>();
        text.font = hudFont != null ? hudFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 8;
        text.color = Color.white;
        text.alignment = TextAnchor.UpperCenter;
        text.raycastTarget = false;

        Shadow shadow = textObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
        shadow.effectDistance = new Vector2(1f, -1f);
        return text;
    }

    private void UpdateProductivity(int current, int goal)
    {
        if (productivityText != null)
            productivityText.text = $"PRODUCTIVITY: {current} / {goal}";
    }

    private void UpdateCarrying()
    {
        if (carryingText == null)
            return;

        string itemName = lastCarriedItem != null ? lastCarriedItem.ItemName : "NONE";
        carryingText.text = $"CARRYING: {itemName}";
    }

    /// <summary>
    /// Exibe o tempo arredondado para cima, em minutos e segundos.
    /// </summary>
    private void UpdateTimer()
    {
        if (timerText == null || gameSession == null)
            return;

        int totalSeconds = Mathf.CeilToInt(gameSession.RemainingTime);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        timerText.text = $"TIME: {minutes:00}:{seconds:00}";
    }
}
