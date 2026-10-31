using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Monta as telas persistentes de vitória e derrota dentro do Canvas da fase.
/// </summary>
public static class ResultScreensSetup
{
    private const string ScenePath = "Assets/Scenes/FactoryLevel01.unity";
    private const string CanvasName = "Productivity HUD Canvas";
    private const string FontPath = "Assets/Art/Fonts/PressStart2P/PressStart2P-Regular.ttf";

    [MenuItem("Tools/GS Robot Factory/Configure Result Screens")]
    public static void ConfigureScene()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Canvas canvas = GameObject.Find(CanvasName)?.GetComponent<Canvas>();
        ConfigureEventSystem();
        GameSession session = Object.FindFirstObjectByType<GameSession>();
        ProductivityManager productivity = Object.FindFirstObjectByType<ProductivityManager>();
        ProductivityHud hud = Object.FindFirstObjectByType<ProductivityHud>();
        Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);

        if (canvas == null || session == null || productivity == null)
        {
            Debug.LogError("Canvas, GameSession ou ProductivityManager não encontrado em FactoryLevel01.");
            return;
        }

        GameObject overlay = CreateOverlay(canvas.transform);
        ResultPanel win = CreatePanel(
            overlay.transform,
            "Victory Panel",
            "PRODUCTIVITY TARGET ACHIEVED",
            "Your unit has been approved for continued operation.",
            new Color(0.25f, 0.85f, 0.55f),
            font);
        ResultPanel lost = CreatePanel(
            overlay.transform,
            "Game Over Panel",
            "PRODUCTIVITY BELOW EXPECTATIONS",
            "Your performance no longer meets factory standards.\nYour unit will be replaced by a newer model.",
            new Color(1f, 0.48f, 0.32f),
            font);

        ResultScreenController controller = canvas.GetComponent<ResultScreenController>();
        if (controller == null)
            controller = canvas.gameObject.AddComponent<ResultScreenController>();

        ConfigureRetryButton(win.retryButton, controller);
        ConfigureRetryButton(lost.retryButton, controller);
        ConfigureNextLevelButton(win.nextLevelButton, controller);

        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("gameSession").objectReferenceValue = session;
        serializedController.FindProperty("overlay").objectReferenceValue = overlay;
        serializedController.FindProperty("winPanel").objectReferenceValue = win.panel;
        serializedController.FindProperty("lostPanel").objectReferenceValue = lost.panel;
        serializedController.FindProperty("winProductivityText").objectReferenceValue = win.productivityText;
        serializedController.FindProperty("winGoalText").objectReferenceValue = win.goalText;
        serializedController.FindProperty("lostProductivityText").objectReferenceValue = lost.productivityText;
        serializedController.FindProperty("lostGoalText").objectReferenceValue = lost.goalText;
        serializedController.FindProperty("nextLevelButton").objectReferenceValue = win.nextLevelButton;

        SerializedProperty hudElements = serializedController.FindProperty("gameplayHudElements");
        hudElements.arraySize = 4;
        hudElements.GetArrayElementAtIndex(0).objectReferenceValue = FindText(canvas.transform, "Productivity Text");
        hudElements.GetArrayElementAtIndex(1).objectReferenceValue = FindText(canvas.transform, "Carrying Text");
        hudElements.GetArrayElementAtIndex(2).objectReferenceValue = FindText(canvas.transform, "Timer Text");
        hudElements.GetArrayElementAtIndex(3).objectReferenceValue = FindText(canvas.transform, "Penalty Text");
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        overlay.SetActive(false);
        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(canvas.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Telas de vitória e derrota configuradas no Canvas de FactoryLevel01.");
    }

    private static GameObject CreateOverlay(Transform canvas)
    {
        GameObject overlay = GetOrCreate(canvas, "Result Overlay", typeof(RectTransform), typeof(Image));
        RectTransform rect = overlay.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = overlay.GetComponent<Image>();
        image.color = new Color(0.025f, 0.035f, 0.055f, 0.92f);
        image.raycastTarget = true;
        overlay.transform.SetAsLastSibling();
        return overlay;
    }

    private static ResultPanel CreatePanel(
        Transform overlay,
        string objectName,
        string title,
        string message,
        Color accent,
        Font font)
    {
        GameObject panel = GetOrCreate(overlay, objectName, typeof(RectTransform), typeof(Image));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = panelRect.anchorMin;
        panelRect.pivot = panelRect.anchorMin;
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(520f, 280f);

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.08f, 0.12f, 0.17f, 1f);
        panelImage.raycastTarget = true;

        CreateText(panel.transform, "Result Title", title, font, 8, accent, new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(490f, 42f), TextAnchor.MiddleCenter);
        CreateText(panel.transform, "Result Message", message, font, 8, Color.white, new Vector2(0.5f, 1f), new Vector2(0f, -77f), new Vector2(480f, 58f), TextAnchor.MiddleCenter);
        Text productivityText = CreateText(panel.transform, "Productivity Value", "PRODUCTIVITY: 0", font, 8, Color.white, new Vector2(0.5f, 1f), new Vector2(0f, -151f), new Vector2(480f, 20f), TextAnchor.MiddleCenter);
        Text goalText = CreateText(panel.transform, "Required Value", "REQUIRED: 100", font, 8, Color.white, new Vector2(0.5f, 1f), new Vector2(0f, -177f), new Vector2(480f, 20f), TextAnchor.MiddleCenter);
        Button retryButton = CreateButton(panel.transform, "Retry Button", font, accent,
            objectName == "Victory Panel" ? new Vector2(-82f, 18f) : Vector2.zero);
        Button nextLevelButton = null;
        if (objectName == "Victory Panel")
            nextLevelButton = CreateButton(panel.transform, "Next Level Button", font, new Color(0.28f, 0.55f, 0.9f), new Vector2(82f, 18f), "NEXT LEVEL");

        return new ResultPanel(panel, productivityText, goalText, retryButton, nextLevelButton);
    }

    private static Text CreateText(
        Transform parent,
        string objectName,
        string content,
        Font font,
        int size,
        Color color,
        Vector2 anchor,
        Vector2 position,
        Vector2 dimensions,
        TextAnchor alignment)
    {
        GameObject textObject = GetOrCreate(parent, objectName, typeof(RectTransform), typeof(Text));
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;

        Text text = textObject.GetComponent<Text>();
        text.text = content;
        text.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(
        Transform parent,
        string objectName,
        Font font,
        Color accent,
        Vector2 position,
        string labelText = "RETRY")
    {
        GameObject buttonObject = GetOrCreate(parent, objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = buttonRect.anchorMin;
        buttonRect.pivot = buttonRect.anchorMin;
        buttonRect.anchoredPosition = position;
        buttonRect.sizeDelta = new Vector2(144f, 34f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = accent;
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        Text label = CreateText(buttonObject.transform, $"{labelText} Label", labelText, font, 8, Color.white,
            new Vector2(0f, 0f), Vector2.zero, new Vector2(144f, 34f), TextAnchor.MiddleCenter);
        label.raycastTarget = false;
        return button;
    }

    private static void ConfigureRetryButton(Button button, ResultScreenController controller)
    {
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(button.onClick, i);

        UnityEventTools.AddPersistentListener(button.onClick, controller.Retry);
        EditorUtility.SetDirty(button);
    }

    private static void ConfigureNextLevelButton(Button button, ResultScreenController controller)
    {
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(button.onClick, i);

        UnityEventTools.AddPersistentListener(button.onClick, controller.NextLevel);
        EditorUtility.SetDirty(button);
    }

    /// <summary>
    /// Garante que os botões UGUI recebam cliques pelo Input System ativo.
    /// </summary>
    private static void ConfigureEventSystem()
    {
        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
            eventSystem = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();

        InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
        if (inputModule == null)
        {
            inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
        }
    }

    private static Text FindText(Transform parent, string objectName)
    {
        return parent.Find(objectName)?.GetComponent<Text>();
    }

    private static GameObject GetOrCreate(Transform parent, string objectName, params System.Type[] components)
    {
        Transform existing = parent.Find(objectName);
        GameObject child = existing != null ? existing.gameObject : new GameObject(objectName, components);
        if (existing != null)
        {
            foreach (System.Type component in components)
            {
                if (child.GetComponent(component) == null)
                    child.AddComponent(component);
            }
        }

        child.transform.SetParent(parent, false);
        return child;
    }

    private readonly struct ResultPanel
    {
        public readonly GameObject panel;
        public readonly Text productivityText;
        public readonly Text goalText;
        public readonly Button retryButton;
        public readonly Button nextLevelButton;

        public ResultPanel(GameObject panel, Text productivityText, Text goalText, Button retryButton, Button nextLevelButton)
        {
            this.panel = panel;
            this.productivityText = productivityText;
            this.goalText = goalText;
            this.retryButton = retryButton;
            this.nextLevelButton = nextLevelButton;
        }
    }
}
