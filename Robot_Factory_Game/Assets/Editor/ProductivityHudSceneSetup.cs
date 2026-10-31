using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Cria os elementos persistentes do HUD de produtividade na cena da fábrica.
/// </summary>
public static class ProductivityHudSceneSetup
{
    private const string ScenePath = "Assets/Scenes/FactoryLevel01.unity";
    private const string CanvasName = "Productivity HUD Canvas";
    private const string FontPath = "Assets/Art/Fonts/PressStart2P/PressStart2P-Regular.ttf";

    [MenuItem("Tools/GS Robot Factory/Configure Productivity HUD")]
    public static void ConfigureScene()
    {
        ConfigureFontImport();
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Font hudFont = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        GameObject productivityObject = GameObject.Find("Productivity");
        if (productivityObject == null)
            productivityObject = new GameObject("Productivity");

        ProductivityManager manager = productivityObject.GetComponent<ProductivityManager>();
        if (manager == null)
            manager = productivityObject.AddComponent<ProductivityManager>();

        ProductivityHud hud = productivityObject.GetComponent<ProductivityHud>();
        if (hud == null)
            hud = productivityObject.AddComponent<ProductivityHud>();

        GameSession gameSession = productivityObject.GetComponent<GameSession>();
        if (gameSession == null)
            gameSession = productivityObject.AddComponent<GameSession>();

        Canvas canvas = CreateCanvas();
        Text productivityText = CreateText(canvas.transform, "Productivity Text", false, hudFont);
        Text carryingText = CreateText(canvas.transform, "Carrying Text", true, hudFont);
        Text penaltyText = CreatePenaltyText(canvas.transform, hudFont);
        Text timerText = CreateTimerText(canvas.transform, hudFont);
        RobotCarry robotCarry = Object.FindFirstObjectByType<RobotCarry>();

        SerializedObject serializedHud = new SerializedObject(hud);
        serializedHud.FindProperty("productivityManager").objectReferenceValue = manager;
        serializedHud.FindProperty("robotCarry").objectReferenceValue = robotCarry;
        serializedHud.FindProperty("hudCanvas").objectReferenceValue = canvas;
        serializedHud.FindProperty("productivityText").objectReferenceValue = productivityText;
        serializedHud.FindProperty("carryingText").objectReferenceValue = carryingText;
        serializedHud.FindProperty("timerText").objectReferenceValue = timerText;
        serializedHud.FindProperty("hudFont").objectReferenceValue = hudFont;
        serializedHud.FindProperty("gameSession").objectReferenceValue = gameSession;
        serializedHud.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject serializedSession = new SerializedObject(gameSession);
        serializedSession.FindProperty("productivityManager").objectReferenceValue = manager;
        serializedSession.ApplyModifiedPropertiesWithoutUndo();

        FactoryAlertManager alertManager = productivityObject.GetComponent<FactoryAlertManager>();
        if (alertManager == null)
            alertManager = GameObject.Find("FactoryAlertManager")?.GetComponent<FactoryAlertManager>();
        if (alertManager != null)
        {
            SerializedObject serializedAlerts = new SerializedObject(alertManager);
            serializedAlerts.FindProperty("penaltyText").objectReferenceValue = penaltyText;
            serializedAlerts.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(alertManager);
        }

        EditorUtility.SetDirty(productivityObject);
        EditorUtility.SetDirty(hud);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Productivity HUD Canvas e referências salvos em FactoryLevel01.");
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = GameObject.Find(CanvasName);
        if (canvasObject == null)
            canvasObject = new GameObject(CanvasName, typeof(RectTransform));

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        if (canvas == null)
            canvas = canvasObject.AddComponent<Canvas>();

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 110;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(640f, 360f);
        scaler.matchWidthOrHeight = 0.5f;

        if (canvasObject.GetComponent<GraphicRaycaster>() == null)
            canvasObject.AddComponent<GraphicRaycaster>();

        return canvas;
    }

    private static Text CreateText(Transform parent, string objectName, bool alignRight, Font hudFont)
    {
        Transform existing = parent.Find(objectName);
        GameObject textObject = existing != null
            ? existing.gameObject
            : new GameObject(objectName, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);

        Text text = textObject.GetComponent<Text>();
        if (text == null)
            text = textObject.AddComponent<Text>();

        RectTransform rect = textObject.GetComponent<RectTransform>();
        Vector2 anchor = alignRight ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = alignRight ? new Vector2(-12f, -12f) : new Vector2(12f, -12f);
        rect.sizeDelta = new Vector2(alignRight ? 240f : 220f, 18f);

        text.text = alignRight ? "CARRYING: NONE" : "PRODUCTIVITY: 0 / 100";
        text.font = hudFont != null ? hudFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 8;
        text.color = Color.white;
        text.alignment = alignRight ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        Shadow shadow = textObject.GetComponent<Shadow>();
        if (shadow == null)
            shadow = textObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
        shadow.effectDistance = new Vector2(1f, -1f);
        return text;
    }

    private static Text CreatePenaltyText(Transform parent, Font hudFont)
    {
        const string objectName = "Penalty Text";
        Transform existing = parent.Find(objectName);
        GameObject textObject = existing != null
            ? existing.gameObject
            : new GameObject(objectName, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);

        Text text = textObject.GetComponent<Text>();
        if (text == null)
            text = textObject.AddComponent<Text>();

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(12f, 12f);
        rect.sizeDelta = new Vector2(150f, 18f);

        text.text = "PENALTY: 100";
        text.font = hudFont != null ? hudFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 8;
        text.color = Color.white;
        text.alignment = TextAnchor.UpperLeft;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        Shadow shadow = textObject.GetComponent<Shadow>();
        if (shadow == null)
            shadow = textObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
        shadow.effectDistance = new Vector2(1f, -1f);
        return text;
    }

    private static Text CreateTimerText(Transform parent, Font hudFont)
    {
        const string objectName = "Timer Text";
        Transform existing = parent.Find(objectName);
        GameObject textObject = existing != null
            ? existing.gameObject
            : new GameObject(objectName, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);

        Text text = textObject.GetComponent<Text>();
        if (text == null)
            text = textObject.AddComponent<Text>();

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = rect.anchorMin;
        rect.pivot = rect.anchorMin;
        rect.anchoredPosition = new Vector2(0f, -12f);
        rect.sizeDelta = new Vector2(140f, 18f);

        text.text = "TIME: 01:20";
        text.font = hudFont != null ? hudFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 8;
        text.color = Color.white;
        text.alignment = TextAnchor.UpperCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        Shadow shadow = textObject.GetComponent<Shadow>();
        if (shadow == null)
            shadow = textObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
        shadow.effectDistance = new Vector2(1f, -1f);
        return text;
    }

    /// <summary>
    /// Mantém os caracteres nítidos nas dimensões de pixel da fonte.
    /// </summary>
    private static void ConfigureFontImport()
    {
        TrueTypeFontImporter fontImporter = AssetImporter.GetAtPath(FontPath) as TrueTypeFontImporter;
        if (fontImporter == null)
        {
            AssetDatabase.ImportAsset(FontPath, ImportAssetOptions.ForceSynchronousImport);
            fontImporter = AssetImporter.GetAtPath(FontPath) as TrueTypeFontImporter;
        }

        if (fontImporter == null)
        {
            Debug.LogError($"Fonte de Pixel Art não encontrada em {FontPath}.");
            return;
        }

        fontImporter.fontRenderingMode = FontRenderingMode.HintedRaster;
        fontImporter.fontSize = 8;
        fontImporter.SaveAndReimport();
    }
}
