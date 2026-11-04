using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Monta o menu de pausa e suas opções editáveis na cena da fase.
/// </summary>
public static class PauseMenuSceneSetup
{
    private const string ScenePath = "Assets/Scenes/FactoryLevel01.unity";
    private const string CanvasName = "Productivity HUD Canvas";
    private const string FontPath = "Assets/Art/Fonts/PressStart2P/PressStart2P-Regular.ttf";

    private static readonly Color PanelColor = new Color(0.07f, 0.105f, 0.135f, 1f);
    private static readonly Color ButtonColor = new Color(0.12f, 0.31f, 0.39f, 1f);
    private static readonly Color AccentColor = new Color(0.35f, 0.9f, 0.76f, 1f);

    [MenuItem("Tools/GS Robot Factory/Configure Pause Menu")]
    public static void ConfigureScene()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Canvas canvas = GameObject.Find(CanvasName)?.GetComponent<Canvas>();
        GameSession gameSession = Object.FindFirstObjectByType<GameSession>();
        Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);

        if (canvas == null || gameSession == null)
        {
            Debug.LogError("Productivity HUD Canvas ou GameSession não encontrado em FactoryLevel01.");
            return;
        }

        ConfigureEventSystem();

        Transform previousOverlay = canvas.transform.Find("Pause Overlay");
        if (previousOverlay != null)
            Undo.DestroyObjectImmediate(previousOverlay.gameObject);

        GameObject overlay = CreateOverlay(canvas.transform);
        GameObject pausePanel = CreatePanel(overlay.transform, "Pause Panel", new Vector2(340f, 250f));
        CreateText(pausePanel.transform, "Pause Title", "PAUSED", font, 10, AccentColor, new Vector2(0f, 82f), new Vector2(300f, 28f));
        Button resumeButton = CreateButton(pausePanel.transform, "Resume Button", "RESUME", font, ButtonColor, new Vector2(0f, 28f), new Vector2(260f, 36f), 7);
        Button optionsButton = CreateButton(pausePanel.transform, "Pause Options Button", "OPTIONS", font, ButtonColor, new Vector2(0f, -20f), new Vector2(260f, 36f), 7);
        Button mainMenuButton = CreateButton(pausePanel.transform, "Main Menu Button", "MAIN MENU", font, new Color(0.35f, 0.19f, 0.21f, 1f), new Vector2(0f, -68f), new Vector2(260f, 36f), 6);

        GameObject optionsPanel = CreatePanel(overlay.transform, "Pause Options Panel", new Vector2(460f, 280f));
        CreateText(optionsPanel.transform, "Options Title", "OPTIONS", font, 9, AccentColor, new Vector2(0f, 96f), new Vector2(400f, 26f));
        CreateText(optionsPanel.transform, "Volume Label", "MASTER VOLUME", font, 6, Color.white, new Vector2(-70f, 48f), new Vector2(240f, 20f));
        Slider volumeSlider = CreateSlider(optionsPanel.transform, new Vector2(-5f, 12f), new Vector2(280f, 18f));
        Text volumeValueText = CreateText(optionsPanel.transform, "Volume Value", "100%", font, 5, AccentColor, new Vector2(175f, 12f), new Vector2(70f, 20f));
        Toggle fullscreenToggle = CreateToggle(optionsPanel.transform, new Vector2(-155f, -42f));
        CreateText(optionsPanel.transform, "Fullscreen Label", "FULLSCREEN", font, 6, Color.white, new Vector2(25f, -42f), new Vector2(250f, 24f));
        Button optionsBackButton = CreateButton(optionsPanel.transform, "Options Back Button", "BACK", font, ButtonColor, new Vector2(0f, -100f), new Vector2(180f, 34f), 6);

        PauseMenuController controller = canvas.GetComponent<PauseMenuController>();
        if (controller == null)
            controller = canvas.gameObject.AddComponent<PauseMenuController>();

        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("gameSession").objectReferenceValue = gameSession;
        serializedController.FindProperty("pauseOverlay").objectReferenceValue = overlay;
        serializedController.FindProperty("pausePanel").objectReferenceValue = pausePanel;
        serializedController.FindProperty("optionsPanel").objectReferenceValue = optionsPanel;
        serializedController.FindProperty("masterVolumeSlider").objectReferenceValue = volumeSlider;
        serializedController.FindProperty("masterVolumeValueText").objectReferenceValue = volumeValueText;
        serializedController.FindProperty("fullscreenToggle").objectReferenceValue = fullscreenToggle;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        AddClick(resumeButton, controller.ResumeGame);
        AddClick(optionsButton, controller.ShowOptions);
        AddClick(mainMenuButton, controller.ReturnToMainMenu);
        AddClick(optionsBackButton, controller.ShowPausePanel);
        AddSliderListener(volumeSlider, controller);
        AddToggleListener(fullscreenToggle, controller);

        optionsPanel.SetActive(false);
        overlay.SetActive(false);
        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(canvas.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Pause menu configurado em FactoryLevel01.");
    }

    private static GameObject CreateOverlay(Transform parent)
    {
        GameObject overlay = new GameObject("Pause Overlay", typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(parent, false);
        RectTransform rect = overlay.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = overlay.GetComponent<Image>();
        image.color = new Color(0.015f, 0.025f, 0.04f, 0.88f);
        image.raycastTarget = true;
        overlay.transform.SetAsLastSibling();
        return overlay;
    }

    private static GameObject CreatePanel(Transform parent, string name, Vector2 size)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        SetCenteredRect(panel.GetComponent<RectTransform>(), Vector2.zero, size);
        Image image = panel.GetComponent<Image>();
        image.color = PanelColor;
        image.raycastTarget = true;
        return panel;
    }

    private static Text CreateText(Transform parent, string name, string content, Font font, int size, Color color, Vector2 position, Vector2 dimensions)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        SetCenteredRect(textObject.GetComponent<RectTransform>(), position, dimensions);

        Text text = textObject.GetComponent<Text>();
        text.text = content;
        text.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(Transform parent, string name, string label, Font font, Color color, Vector2 position, Vector2 size, int fontSize)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        SetCenteredRect(buttonObject.GetComponent<RectTransform>(), position, size);

        Image image = buttonObject.GetComponent<Image>();
        image.color = color;
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };

        Text labelText = CreateText(buttonObject.transform, "Label", label, font, fontSize, Color.white, Vector2.zero, size - new Vector2(12f, 6f));
        labelText.raycastTarget = false;
        return button;
    }

    private static Slider CreateSlider(Transform parent, Vector2 position, Vector2 size)
    {
        GameObject sliderObject = new GameObject("Master Volume Slider", typeof(RectTransform), typeof(Slider));
        sliderObject.transform.SetParent(parent, false);
        SetCenteredRect(sliderObject.GetComponent<RectTransform>(), position, size);

        GameObject track = CreateImage(sliderObject.transform, "Track", new Color(0.025f, 0.04f, 0.05f, 1f), Vector2.zero, size);
        track.GetComponent<Image>().raycastTarget = true;
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.2f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.8f);
        fillAreaRect.offsetMin = new Vector2(3f, 0f);
        fillAreaRect.offsetMax = new Vector2(-3f, 0f);

        GameObject fill = CreateImage(fillArea.transform, "Fill", AccentColor, Vector2.zero, Vector2.zero);
        fill.GetComponent<Image>().raycastTarget = true;
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderObject.transform, false);
        RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(4f, 0f);
        handleAreaRect.offsetMax = new Vector2(-4f, 0f);

        GameObject handle = CreateImage(handleArea.transform, "Handle", Color.white, Vector2.zero, new Vector2(12f, 18f));
        handle.GetComponent<Image>().raycastTarget = true;
        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        slider.fillRect = fillRect;
        slider.handleRect = handle.GetComponent<RectTransform>();
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        return slider;
    }

    private static Toggle CreateToggle(Transform parent, Vector2 position)
    {
        GameObject toggleObject = new GameObject("Fullscreen Toggle", typeof(RectTransform), typeof(Toggle));
        toggleObject.transform.SetParent(parent, false);
        SetCenteredRect(toggleObject.GetComponent<RectTransform>(), position, new Vector2(24f, 24f));
        GameObject background = CreateImage(toggleObject.transform, "Background", new Color(0.025f, 0.04f, 0.05f, 1f), Vector2.zero, new Vector2(24f, 24f));
        GameObject checkmark = CreateImage(background.transform, "Checkmark", AccentColor, Vector2.zero, new Vector2(12f, 12f));

        Toggle toggle = toggleObject.GetComponent<Toggle>();
        toggle.targetGraphic = background.GetComponent<Image>();
        toggle.graphic = checkmark.GetComponent<Image>();
        toggle.navigation = new Navigation { mode = Navigation.Mode.None };
        return toggle;
    }

    private static GameObject CreateImage(Transform parent, string name, Color color, Vector2 position, Vector2 size)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        SetCenteredRect(imageObject.GetComponent<RectTransform>(), position, size);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return imageObject;
    }

    private static void SetCenteredRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = rect.anchorMin;
        rect.pivot = rect.anchorMin;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void AddClick(Button button, UnityEngine.Events.UnityAction action)
    {
        ClearClickEvents(button);
        UnityEventTools.AddPersistentListener(button.onClick, action);
        EditorUtility.SetDirty(button);
    }

    private static void AddSliderListener(Slider slider, PauseMenuController controller)
    {
        for (int i = slider.onValueChanged.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(slider.onValueChanged, i);
        UnityEventTools.AddPersistentListener(slider.onValueChanged, controller.ApplyMasterVolume);
        EditorUtility.SetDirty(slider);
    }

    private static void AddToggleListener(Toggle toggle, PauseMenuController controller)
    {
        for (int i = toggle.onValueChanged.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(toggle.onValueChanged, i);
        UnityEventTools.AddPersistentListener(toggle.onValueChanged, controller.ApplyFullscreen);
        EditorUtility.SetDirty(toggle);
    }

    private static void ClearClickEvents(Button button)
    {
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(button.onClick, i);
    }

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
}
