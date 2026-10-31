using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Cria a cena editável do menu inicial e a registra antes das fases no build.
/// </summary>
public static class MainMenuSceneSetup
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string FirstLevelPath = "Assets/Scenes/FactoryLevel01.unity";
    private const string SecondLevelPath = "Assets/Scenes/FactoryLevel02.unity";
    private const string FontPath = "Assets/Art/Fonts/PressStart2P/PressStart2P-Regular.ttf";
    private const string RobotSpritePath = "Assets/Art/Sprites/Placeholders/RobotAnimations/Robot_Idle_Down.png";
    private const string ConveyorSpritePath = "Assets/Art/Sprites/Placeholders/Conveyor_Belt.png";
    private const string CircuitItemSpritePath = "Assets/Art/Sprites/Placeholders/Component_Circuit.png";
    private const string BatteryItemSpritePath = "Assets/Art/Sprites/Placeholders/Component_Battery.png";
    private const string MetalItemSpritePath = "Assets/Art/Sprites/Placeholders/Component_Metal.png";
    private const string CircuitStationSpritePath = "Assets/Art/Sprites/Placeholders/Station_Assembly.png";
    private const string BatteryStationSpritePath = "Assets/Art/Sprites/Placeholders/Station_Charge.png";
    private const string MetalStationSpritePath = "Assets/Art/Sprites/Placeholders/Station_Processor.png";
    private const string TrashSpritePath = "Assets/Art/Sprites/Placeholders/Trash_Bin.png";
    private const string WarningSpritePath = "Assets/Art/Sprites/Alerts/Alert_Warning.png";
    private const string CriticalSpritePath = "Assets/Art/Sprites/Alerts/Alert_Critical.png";

    private static readonly Color BackgroundColor = new Color(0.035f, 0.055f, 0.075f, 1f);
    private static readonly Color PanelColor = new Color(0.07f, 0.105f, 0.135f, 0.98f);
    private static readonly Color ButtonColor = new Color(0.12f, 0.31f, 0.39f, 1f);
    private static readonly Color AccentColor = new Color(0.35f, 0.9f, 0.76f, 1f);
    private static readonly Color MutedColor = new Color(0.38f, 0.46f, 0.49f, 1f);

    [MenuItem("Tools/GS Robot Factory/Create Main Menu")]
    public static void CreateMainMenu()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(FirstLevelPath) == null)
        {
            Debug.LogError("FactoryLevel01.unity não foi encontrada. O menu não foi criado.");
            return;
        }

        if (System.IO.File.Exists(ScenePath)
            && !EditorUtility.DisplayDialog(
                "Recriar menu inicial?",
                "A cena MainMenu existente será substituída. Cancele para preservar alterações feitas manualmente.",
                "Recriar",
                "Cancelar"))
        {
            return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        ConfigureEventSystem();
        ConfigureCamera();

        GameObject canvasObject = new GameObject("Main Menu Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(640f, 360f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        CreateImage(canvasObject.transform, "Background", BackgroundColor, Vector2.zero, new Vector2(640f, 360f), true);
        CreateImage(canvasObject.transform, "Top Accent", AccentColor, new Vector2(0f, 164f), new Vector2(640f, 4f), false);

        GameObject mainPanel = CreatePanel(canvasObject.transform, "Main Panel", new Vector2(460f, 330f));
        CreateText(mainPanel.transform, "Game Title", "ROBOT FACTORY", font, 12, AccentColor, new Vector2(0f, 116f), new Vector2(450f, 36f));
        CreateText(mainPanel.transform, "Subtitle", "AUTOMATED SHIFT PROTOCOL", font, 6, Color.white, new Vector2(0f, 82f), new Vector2(450f, 22f));
        Button playButton = CreateButton(mainPanel.transform, "Play Button", "PLAY", font, ButtonColor, new Vector2(0f, 46f), new Vector2(310f, 36f), 7);
        Button phaseButton = CreateButton(mainPanel.transform, "Select Phase Button", "SELECT LEVEL", font, ButtonColor, new Vector2(0f, 5f), new Vector2(310f, 36f), 6);
        Button tutorialButton = CreateButton(mainPanel.transform, "Tutorial Button", "TUTORIAL", font, ButtonColor, new Vector2(0f, -36f), new Vector2(310f, 36f), 7);
        Button optionsButton = CreateButton(mainPanel.transform, "Options Button", "OPTIONS", font, ButtonColor, new Vector2(0f, -77f), new Vector2(310f, 36f), 7);
        Button quitButton = CreateButton(mainPanel.transform, "Quit Button", "QUIT", font, new Color(0.35f, 0.19f, 0.21f, 1f), new Vector2(0f, -118f), new Vector2(310f, 36f), 7);

        GameObject phasePanel = CreatePanel(canvasObject.transform, "Phase Selection Panel", new Vector2(560f, 330f));
        CreateText(phasePanel.transform, "Phase Selection Title", "SELECT LEVEL", font, 7, AccentColor, new Vector2(0f, 126f), new Vector2(520f, 24f));
        CreateText(phasePanel.transform, "Phase Selection Hint", "LEVEL 1 AVAILABLE", font, 5, Color.white, new Vector2(0f, 100f), new Vector2(520f, 18f));
        Button phaseOneButton = null;
        Button phaseTwoButton = null;
        for (int phase = 1; phase <= 9; phase++)
        {
            int column = (phase - 1) % 3;
            int row = (phase - 1) / 3;
            Vector2 position = new Vector2(-174f + column * 174f, 42f - row * 64f);
            bool available = phase == 1 || (phase == 2 && HasSecondLevel());
            string label = available ? $"LEVEL {phase:00}\nAVAILABLE" : $"LEVEL {phase:00}\nLOCKED";
            Button phaseButtonItem = CreateButton(
                phasePanel.transform,
                $"Phase {phase:00} Button",
                label,
                font,
                available ? ButtonColor : new Color(0.1f, 0.13f, 0.15f, 1f),
                position,
                new Vector2(158f, 52f),
                5);

            if (!available)
            {
                phaseButtonItem.interactable = false;
                phaseButtonItem.GetComponent<Image>().color = new Color(0.1f, 0.13f, 0.15f, 1f);
                phaseButtonItem.GetComponentInChildren<Text>().color = MutedColor;
            }
            else
            {
                if (phase == 1)
                    phaseOneButton = phaseButtonItem;
                else if (phase == 2)
                    phaseTwoButton = phaseButtonItem;
            }
        }
        Button phaseBackButton = CreateButton(phasePanel.transform, "Phase Back Button", "BACK", font, ButtonColor, new Vector2(0f, -135f), new Vector2(180f, 32f), 6);

        GameObject optionsPanel = CreatePanel(canvasObject.transform, "Options Panel", new Vector2(500f, 310f));
        CreateText(optionsPanel.transform, "Options Title", "OPTIONS", font, 8, AccentColor, new Vector2(0f, 116f), new Vector2(450f, 26f));
        CreateText(optionsPanel.transform, "Volume Label", "MASTER VOLUME", font, 6, Color.white, new Vector2(-115f, 52f), new Vector2(210f, 20f));
        Slider volumeSlider = CreateSlider(optionsPanel.transform, new Vector2(-16f, 18f), new Vector2(270f, 18f));
        Text volumeValueText = CreateText(optionsPanel.transform, "Volume Value", "100%", font, 5, AccentColor, new Vector2(180f, 18f), new Vector2(70f, 20f));

        Toggle fullscreenToggle = CreateToggle(optionsPanel.transform, new Vector2(-175f, -38f));
        CreateText(optionsPanel.transform, "Fullscreen Label", "FULLSCREEN", font, 6, Color.white, new Vector2(10f, -38f), new Vector2(250f, 24f));
        Button optionsBackButton = CreateButton(optionsPanel.transform, "Options Back Button", "BACK", font, ButtonColor, new Vector2(0f, -112f), new Vector2(180f, 34f), 6);

        GameObject tutorialPanel = CreatePanel(canvasObject.transform, "Tutorial Panel", new Vector2(560f, 330f));
        TutorialPageSet tutorialPageSet = CreateTutorialPages(tutorialPanel.transform, font);

        MainMenuController controller = canvasObject.AddComponent<MainMenuController>();
        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("mainPanel").objectReferenceValue = mainPanel;
        serializedController.FindProperty("phaseSelectionPanel").objectReferenceValue = phasePanel;
        serializedController.FindProperty("optionsPanel").objectReferenceValue = optionsPanel;
        serializedController.FindProperty("tutorialPanel").objectReferenceValue = tutorialPanel;
        SetObjectArray(serializedController.FindProperty("tutorialPages"), tutorialPageSet.pages);
        serializedController.FindProperty("tutorialPageIndicatorText").objectReferenceValue = tutorialPageSet.pageIndicator;
        serializedController.FindProperty("tutorialPreviousButton").objectReferenceValue = tutorialPageSet.previousButton;
        serializedController.FindProperty("tutorialNextButton").objectReferenceValue = tutorialPageSet.nextButton;
        serializedController.FindProperty("masterVolumeSlider").objectReferenceValue = volumeSlider;
        serializedController.FindProperty("masterVolumeValueText").objectReferenceValue = volumeValueText;
        serializedController.FindProperty("fullscreenToggle").objectReferenceValue = fullscreenToggle;
        serializedController.FindProperty("firstLevelScene").stringValue = "FactoryLevel01";
        serializedController.FindProperty("secondLevelScene").stringValue = "FactoryLevel02";
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        AddClick(playButton, controller.PlayLevelOne);
        AddClick(phaseButton, controller.ShowPhaseSelection);
        AddClick(tutorialButton, controller.ShowTutorial);
        AddClick(optionsButton, controller.ShowOptions);
        AddClick(quitButton, controller.QuitGame);
        AddClick(phaseOneButton, controller.PlayLevelOne);
        AddClick(phaseTwoButton, controller.PlayLevelTwo);
        AddClick(phaseBackButton, controller.ShowMainMenu);
        AddClick(optionsBackButton, controller.ShowMainMenu);
        AddClick(tutorialPageSet.previousButton, controller.PreviousTutorialPage);
        AddClick(tutorialPageSet.nextButton, controller.NextTutorialPage);
        AddClick(tutorialPageSet.backButton, controller.ShowMainMenu);

        phasePanel.SetActive(false);
        optionsPanel.SetActive(false);
        tutorialPanel.SetActive(false);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddMenuSceneToBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Menu inicial criado em Assets/Scenes/MainMenu.unity e adicionado como primeira cena do build.");
    }

    [MenuItem("Tools/GS Robot Factory/Translate Menu and Rebuild Tutorial")]
    public static void AddTutorialToExistingMenu()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Transform canvas = GameObject.Find("Main Menu Canvas")?.transform;
        MainMenuController controller = canvas != null ? canvas.GetComponent<MainMenuController>() : null;
        Transform mainPanel = canvas != null ? canvas.Find("Main Panel") : null;
        Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);

        if (canvas == null || controller == null || mainPanel == null)
        {
            Debug.LogError("Main Menu Canvas, Main Panel ou MainMenuController não encontrado. O tutorial não foi adicionado.");
            return;
        }

        // Reorganiza os quatro botões existentes para abrir espaço ao novo botão.
        SetButtonPosition(mainPanel, "Play Button", new Vector2(0f, 46f));
        SetButtonPosition(mainPanel, "Select Phase Button", new Vector2(0f, 5f));
        SetButtonPosition(mainPanel, "Options Button", new Vector2(0f, -77f));
        SetButtonPosition(mainPanel, "Quit Button", new Vector2(0f, -118f));
        SetButtonLabel(mainPanel, "Play Button", "PLAY");
        SetButtonLabel(mainPanel, "Select Phase Button", "SELECT LEVEL");
        SetButtonLabel(mainPanel, "Tutorial Button", "TUTORIAL");
        SetButtonLabel(mainPanel, "Options Button", "OPTIONS");
        SetButtonLabel(mainPanel, "Quit Button", "QUIT");

        Transform phasePanel = canvas.Find("Phase Selection Panel");
        if (phasePanel != null)
        {
            SetTextContent(phasePanel, "Phase Selection Title", "SELECT LEVEL");
            SetTextContent(phasePanel, "Phase Selection Hint", HasSecondLevel()
                ? "LEVELS 01-02 AVAILABLE"
                : "LEVEL 01 AVAILABLE");
            for (int phase = 1; phase <= 9; phase++)
            {
                bool available = phase == 1 || (phase == 2 && HasSecondLevel());
                string label = available ? $"LEVEL {phase:00}\nAVAILABLE" : $"LEVEL {phase:00}\nLOCKED";
                SetButtonLabel(phasePanel, $"Phase {phase:00} Button", label);
            }

            ConfigurePhaseTwoButton(phasePanel, controller);

            SetButtonLabel(phasePanel, "Phase Back Button", "BACK");
        }

        Transform optionsPanel = canvas.Find("Options Panel");
        if (optionsPanel != null)
        {
            SetTextContent(optionsPanel, "Options Title", "OPTIONS");
            SetTextContent(optionsPanel, "Volume Label", "MASTER VOLUME");
            SetTextContent(optionsPanel, "Fullscreen Label", "FULLSCREEN");
            SetButtonLabel(optionsPanel, "Options Back Button", "BACK");
        }

        Button tutorialButton = FindOrCreateButton(
            mainPanel,
            "Tutorial Button",
            "TUTORIAL",
            font,
            ButtonColor,
            new Vector2(0f, -36f),
            new Vector2(310f, 36f),
            7);

        Transform existingPanel = canvas.Find("Tutorial Panel");
        GameObject tutorialPanel = existingPanel != null
            ? existingPanel.gameObject
            : CreatePanel(canvas, "Tutorial Panel", new Vector2(560f, 330f));

        // A tela antiga tinha um único bloco de texto; substituímos seus filhos pelos dois novos painéis ilustrados.
        for (int i = tutorialPanel.transform.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(tutorialPanel.transform.GetChild(i).gameObject);

        TutorialPageSet tutorialPageSet = CreateTutorialPages(tutorialPanel.transform, font);

        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("tutorialPanel").objectReferenceValue = tutorialPanel;
        SetObjectArray(serializedController.FindProperty("tutorialPages"), tutorialPageSet.pages);
        serializedController.FindProperty("tutorialPageIndicatorText").objectReferenceValue = tutorialPageSet.pageIndicator;
        serializedController.FindProperty("tutorialPreviousButton").objectReferenceValue = tutorialPageSet.previousButton;
        serializedController.FindProperty("tutorialNextButton").objectReferenceValue = tutorialPageSet.nextButton;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        ClearClickEvents(tutorialButton);
        ClearClickEvents(tutorialPageSet.previousButton);
        ClearClickEvents(tutorialPageSet.nextButton);
        ClearClickEvents(tutorialPageSet.backButton);
        AddClick(tutorialButton, controller.ShowTutorial);
        AddClick(tutorialPageSet.previousButton, controller.PreviousTutorialPage);
        AddClick(tutorialPageSet.nextButton, controller.NextTutorialPage);
        AddClick(tutorialPageSet.backButton, controller.ShowMainMenu);
        tutorialPanel.SetActive(false);

        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Menu traduzido para inglês e tutorial em duas páginas atualizado.");
    }

    /// <summary>
    /// Atualiza a seleção de fases após a criação de FactoryLevel02.
    /// </summary>
    [MenuItem("Tools/GS Robot Factory/Refresh Level Selection")]
    public static void RefreshLevelSelection()
    {
        if (!HasSecondLevel())
        {
            Debug.LogError("FactoryLevel02.unity ainda não existe. Crie a fase antes de atualizar o menu.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Transform canvas = GameObject.Find("Main Menu Canvas")?.transform;
        Transform phasePanel = canvas != null ? canvas.Find("Phase Selection Panel") : null;
        MainMenuController controller = canvas != null ? canvas.GetComponent<MainMenuController>() : null;
        if (phasePanel == null || controller == null)
        {
            Debug.LogError("A seleção de fases ou MainMenuController não foi encontrado em MainMenu.");
            return;
        }

        ConfigurePhaseTwoButton(phasePanel, controller);
        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("secondLevelScene").stringValue = "FactoryLevel02";
        serializedController.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AddMenuSceneToBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log("Level 02 foi habilitado na seleção de fases e nas cenas de build.");
    }

    [MenuItem("Tools/GS Robot Factory/Adjust Main Menu Typography")]
    public static void AdjustMainMenuTypography()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Transform canvas = GameObject.Find("Main Menu Canvas")?.transform;
        Transform mainPanel = canvas != null ? canvas.Find("Main Panel") : null;

        if (mainPanel == null)
        {
            Debug.LogError("Main Panel não encontrado na cena MainMenu.");
            return;
        }

        SetTextTypography(mainPanel, "Game Title", 12, new Vector2(450f, 36f));
        SetTextTypography(mainPanel, "Subtitle", 6, new Vector2(450f, 22f));
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Tamanho do título e subtítulo atualizado na cena MainMenu.");
    }

    private static TutorialPageSet CreateTutorialPages(Transform parent, Font font)
    {
        Sprite robot = LoadSprite(RobotSpritePath);
        Sprite conveyor = LoadSprite(ConveyorSpritePath);
        Sprite circuitItem = LoadSprite(CircuitItemSpritePath);
        Sprite batteryItem = LoadSprite(BatteryItemSpritePath);
        Sprite metalItem = LoadSprite(MetalItemSpritePath);
        Sprite circuitStation = LoadSprite(CircuitStationSpritePath);
        Sprite batteryStation = LoadSprite(BatteryStationSpritePath);
        Sprite metalStation = LoadSprite(MetalStationSpritePath);
        Sprite trash = LoadSprite(TrashSpritePath);
        Sprite warning = LoadSprite(WarningSpritePath);
        Sprite critical = LoadSprite(CriticalSpritePath);

        GameObject pageOne = CreateTutorialPage(parent, "Tutorial Page 1", "MOVEMENT & PICKUP", font);
        CreateIllustrationCard(pageOne.transform, "Robot Card", "ROBOT", robot, -170f, font);
        CreateIllustrationCard(pageOne.transform, "Conveyor Card", "CONVEYOR", conveyor, 0f, font);
        CreateIllustrationCard(pageOne.transform, "Item Card", "COMPONENT", circuitItem, 170f, font);
        CreateText(pageOne.transform, "Movement Heading", "MOVEMENT", font, 6, AccentColor,
            new Vector2(0f, 5f), new Vector2(500f, 14f), TextAnchor.MiddleLeft);
        CreateText(pageOne.transform, "Movement Instructions",
            "ARROW KEYS / WASD: move in four directions (no diagonals).\n\nPress once to turn. Press again to move one tile.",
            font, 6, Color.white, new Vector2(0f, -25f), new Vector2(500f, 44f), TextAnchor.MiddleLeft);
        CreateText(pageOne.transform, "Collection Heading", "PICKUP", font, 6, AccentColor,
            new Vector2(0f, -62f), new Vector2(500f, 14f), TextAnchor.MiddleLeft);
        CreateText(pageOne.transform, "Collection Instructions",
            "Stand next to an item and press E to pick it up.\n\nHold a direction to repeat movement after a short delay.",
            font, 6, Color.white, new Vector2(0f, -92f), new Vector2(500f, 44f), TextAnchor.MiddleLeft);

        GameObject pageTwo = CreateTutorialPage(parent, "Tutorial Page 2", "DELIVERY & ALERTS", font);
        CreateStationIllustrationCard(pageTwo.transform, "Circuit Station Card", "CIRCUIT BOARD\nCIRCUIT STATION", circuitItem, circuitStation, -170f, font);
        CreateStationIllustrationCard(pageTwo.transform, "Battery Station Card", "BATTERY\nBATTERY STATION", batteryItem, batteryStation, 0f, font);
        CreateStationIllustrationCard(pageTwo.transform, "Metal Station Card", "METAL COMPONENT\nMETAL STATION", metalItem, metalStation, 170f, font);
        CreateSmallIllustrationCard(pageTwo.transform, "Trash Card", "TRASH", trash, -170f, font);
        CreateSmallIllustrationCard(pageTwo.transform, "Warning Card", "WARNING", warning, 0f, font);
        CreateSmallIllustrationCard(pageTwo.transform, "Critical Card", "CRITICAL", critical, 170f, font);
        CreateText(pageTwo.transform, "Result Instructions",
            "Correct station: +10 PRODUCTIVITY. Wrong items are rejected.\n\nTRASH BIN: discard any item for no points. Ignored alerts reduce PENALTY.\n\nPENALTY at 0 = immediate defeat. At time 0, the target decides the result.",
            font, 6, Color.white, new Vector2(0f, -94f), new Vector2(500f, 64f), TextAnchor.MiddleLeft);

        Text pageIndicator = CreateText(parent, "Tutorial Page Indicator", "1 / 2", font, 6, Color.white,
            new Vector2(230f, 126f), new Vector2(70f, 20f));
        Button previousButton = CreateButton(parent, "Tutorial Previous Button", "< PREVIOUS", font, ButtonColor,
            new Vector2(-170f, -145f), new Vector2(150f, 30f), 6);
        Button backButton = CreateButton(parent, "Tutorial Back Button", "BACK", font, ButtonColor,
            new Vector2(0f, -145f), new Vector2(130f, 30f), 6);
        Button nextButton = CreateButton(parent, "Tutorial Next Button", "NEXT >", font, ButtonColor,
            new Vector2(170f, -145f), new Vector2(150f, 30f), 6);

        pageTwo.SetActive(false);
        previousButton.interactable = false;
        return new TutorialPageSet(new[] { pageOne, pageTwo }, pageIndicator, previousButton, nextButton, backButton);
    }

    private static GameObject CreateTutorialPage(Transform parent, string name, string title, Font font)
    {
        GameObject page = new GameObject(name, typeof(RectTransform));
        page.transform.SetParent(parent, false);
        RectTransform rect = page.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = rect.anchorMin;
        rect.pivot = rect.anchorMin;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(560f, 330f);

        CreateText(page.transform, "Page Title", title, font, 8, AccentColor,
            new Vector2(-35f, 126f), new Vector2(400f, 24f));
        return page;
    }

    private static void SetObjectArray(SerializedProperty property, GameObject[] objects)
    {
        property.arraySize = objects.Length;
        for (int i = 0; i < objects.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
    }

    private static void CreateIllustrationCard(Transform parent, string name, string label, Sprite sprite, float x, Font font)
    {
        GameObject card = CreateImage(parent, name, new Color(0.045f, 0.075f, 0.095f, 1f),
            new Vector2(x, 64f), new Vector2(148f, 76f), false);
        CreateSpriteImage(card.transform, "Illustration", sprite, new Vector2(0f, 10f), new Vector2(42f, 42f));
        CreateText(card.transform, "Caption", label, font, 5, Color.white,
            new Vector2(0f, -26f), new Vector2(138f, 16f));
    }

    private static void CreateStationIllustrationCard(Transform parent, string name, string label, Sprite item, Sprite station, float x, Font font)
    {
        GameObject card = CreateImage(parent, name, new Color(0.045f, 0.075f, 0.095f, 1f),
            new Vector2(x, 58f), new Vector2(158f, 78f), false);
        CreateSpriteImage(card.transform, "Item", item, new Vector2(-38f, 10f), new Vector2(34f, 34f));
        CreateText(card.transform, "Mapping Arrow", ">", font, 7, AccentColor,
            new Vector2(0f, 10f), new Vector2(20f, 24f));
        CreateSpriteImage(card.transform, "Station", station, new Vector2(38f, 10f), new Vector2(34f, 34f));
        CreateText(card.transform, "Caption", label, font, 5, Color.white,
            new Vector2(0f, -27f), new Vector2(150f, 24f));
    }

    private static void CreateSmallIllustrationCard(Transform parent, string name, string label, Sprite sprite, float x, Font font)
    {
        GameObject card = CreateImage(parent, name, new Color(0.045f, 0.075f, 0.095f, 1f),
            new Vector2(x, -38f), new Vector2(148f, 42f), false);
        CreateSpriteImage(card.transform, "Illustration", sprite, new Vector2(-42f, 0f), new Vector2(30f, 30f));
        CreateText(card.transform, "Caption", label, font, 5, Color.white,
            new Vector2(20f, 0f), new Vector2(92f, 20f));
    }

    private static Image CreateSpriteImage(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = rect.anchorMin;
        rect.pivot = rect.anchorMin;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private static Sprite LoadSprite(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Button FindOrCreateButton(Transform parent, string name, string label, Font font, Color color, Vector2 position, Vector2 size, int fontSize)
    {
        Transform existing = parent.Find(name);
        if (existing == null)
            return CreateButton(parent, name, label, font, color, position, size, fontSize);

        Button button = existing.GetComponent<Button>();
        Image image = existing.GetComponent<Image>();
        RectTransform rect = existing.GetComponent<RectTransform>();
        if (button == null || image == null || rect == null)
            return CreateButton(parent, name, label, font, color, position, size, fontSize);

        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        image.color = color;
        button.targetGraphic = image;
        Text labelText = existing.GetComponentInChildren<Text>();
        if (labelText == null)
            labelText = CreateText(existing, "Label", label, font, fontSize, Color.white, Vector2.zero, size - new Vector2(12f, 6f));
        else
        {
            labelText.text = label;
            labelText.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = fontSize;
            labelText.rectTransform.sizeDelta = size - new Vector2(12f, 6f);
        }

        return button;
    }

    private static void SetButtonPosition(Transform parent, string name, Vector2 position)
    {
        Transform button = parent.Find(name);
        if (button != null)
            button.GetComponent<RectTransform>().anchoredPosition = position;
    }

    private static void SetButtonLabel(Transform parent, string buttonName, string label)
    {
        Transform button = parent.Find(buttonName);
        Text labelText = button != null ? button.GetComponentInChildren<Text>() : null;
        if (labelText != null)
            labelText.text = label;
    }

    private static bool HasSecondLevel()
    {
        return AssetDatabase.LoadAssetAtPath<SceneAsset>(SecondLevelPath) != null;
    }

    private static void ConfigurePhaseTwoButton(Transform phasePanel, MainMenuController controller)
    {
        Transform buttonTransform = phasePanel.Find("Phase 02 Button");
        Button button = buttonTransform != null ? buttonTransform.GetComponent<Button>() : null;
        if (button == null)
            return;

        bool available = HasSecondLevel();
        button.interactable = available;
        Image buttonImage = button.GetComponent<Image>();
        Text label = button.GetComponentInChildren<Text>();
        if (buttonImage != null)
            buttonImage.color = available ? ButtonColor : new Color(0.1f, 0.13f, 0.15f, 1f);
        if (label != null)
            label.color = available ? Color.white : MutedColor;

        SetButtonLabel(phasePanel, "Phase 02 Button", available ? "LEVEL 02\nAVAILABLE" : "LEVEL 02\nLOCKED");
        SetTextContent(phasePanel, "Phase Selection Hint", available
            ? "LEVELS 01-02 AVAILABLE"
            : "LEVEL 01 AVAILABLE");

        ClearClickEvents(button);
        if (available && controller != null)
            AddClick(button, controller.PlayLevelTwo);
    }

    private static void SetTextContent(Transform parent, string textName, string content)
    {
        Transform textObject = parent.Find(textName);
        Text text = textObject != null ? textObject.GetComponent<Text>() : null;
        if (text != null)
            text.text = content;
    }

    private static void SetTextTypography(Transform parent, string textName, int fontSize, Vector2 dimensions)
    {
        Transform textObject = parent.Find(textName);
        if (textObject == null)
            return;

        Text text = textObject.GetComponent<Text>();
        if (text != null)
            text.fontSize = fontSize;

        textObject.GetComponent<RectTransform>().sizeDelta = dimensions;
    }

    private static void SetTextPosition(Transform parent, string name, Vector2 position, Vector2 size)
    {
        Transform text = parent.Find(name);
        if (text == null)
            return;

        RectTransform rect = text.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static GameObject CreatePanel(Transform parent, string name, Vector2 size)
    {
        GameObject panel = CreateImage(parent, name, PanelColor, Vector2.zero, size, false);
        panel.transform.SetAsLastSibling();
        return panel;
    }

    private static GameObject CreateImage(Transform parent, string name, Color color, Vector2 position, Vector2 size, bool stretch)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        if (stretch)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
        else
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = rect.anchorMin;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return imageObject;
    }

    private static Text CreateText(Transform parent, string name, string content, Font font, int size, Color color, Vector2 position, Vector2 dimensions, TextAnchor alignment = TextAnchor.MiddleCenter)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = rect.anchorMin;
        rect.pivot = rect.anchorMin;
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

    private static Button CreateButton(Transform parent, string name, string label, Font font, Color color, Vector2 position, Vector2 size, int fontSize)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = rect.anchorMin;
        rect.pivot = rect.anchorMin;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

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
        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        SetCenteredRect(sliderRect, position, size);

        GameObject background = CreateImage(sliderObject.transform, "Background", new Color(0.025f, 0.04f, 0.05f, 1f), Vector2.zero, size, true);
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.2f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.8f);
        fillAreaRect.offsetMin = new Vector2(3f, 0f);
        fillAreaRect.offsetMax = new Vector2(-3f, 0f);

        GameObject fill = CreateImage(fillArea.transform, "Fill", AccentColor, Vector2.zero, Vector2.zero, true);
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

        GameObject handle = CreateImage(handleArea.transform, "Handle", Color.white, Vector2.zero, new Vector2(12f, 18f), false);
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
        RectTransform rect = toggleObject.GetComponent<RectTransform>();
        SetCenteredRect(rect, position, new Vector2(24f, 24f));

        GameObject background = CreateImage(toggleObject.transform, "Background", new Color(0.025f, 0.04f, 0.05f, 1f), Vector2.zero, new Vector2(24f, 24f), false);
        GameObject checkmark = CreateImage(background.transform, "Checkmark", AccentColor, Vector2.zero, new Vector2(12f, 12f), false);
        Toggle toggle = toggleObject.GetComponent<Toggle>();
        toggle.targetGraphic = background.GetComponent<Image>();
        toggle.graphic = checkmark.GetComponent<Image>();
        toggle.isOn = false;
        toggle.navigation = new Navigation { mode = Navigation.Mode.None };
        return toggle;
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
        if (button == null)
            return;

        UnityEventTools.AddPersistentListener(button.onClick, action);
        EditorUtility.SetDirty(button);
    }

    private static void ClearClickEvents(Button button)
    {
        if (button == null)
            return;

        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(button.onClick, i);
    }

    /// <summary>
    /// Configura o módulo visual do EventSystem para receber cliques via Input System.
    /// </summary>
    private static void ConfigureEventSystem()
    {
        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        eventSystemObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }

    private static void ConfigureCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = BackgroundColor;
    }

    private readonly struct TutorialPageSet
    {
        public readonly GameObject[] pages;
        public readonly Text pageIndicator;
        public readonly Button previousButton;
        public readonly Button nextButton;
        public readonly Button backButton;

        public TutorialPageSet(GameObject[] pages, Text pageIndicator, Button previousButton, Button nextButton, Button backButton)
        {
            this.pages = pages;
            this.pageIndicator = pageIndicator;
            this.previousButton = previousButton;
            this.nextButton = nextButton;
            this.backButton = backButton;
        }
    }

    private static void AddMenuSceneToBuildSettings()
    {
        EditorBuildSettingsScene[] currentScenes = EditorBuildSettings.scenes;
        var scenePaths = new System.Collections.Generic.List<string> { ScenePath };
        foreach (EditorBuildSettingsScene scene in currentScenes)
        {
            if (scene.path != ScenePath && !scenePaths.Contains(scene.path))
                scenePaths.Add(scene.path);
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(FirstLevelPath) != null && !scenePaths.Contains(FirstLevelPath))
            scenePaths.Add(FirstLevelPath);
        if (HasSecondLevel() && !scenePaths.Contains(SecondLevelPath))
            scenePaths.Add(SecondLevelPath);

        EditorBuildSettings.scenes = scenePaths
            .ConvertAll(path => new EditorBuildSettingsScene(path, true))
            .ToArray();
    }
}
