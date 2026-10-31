using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Cria sprites de teste, clips e o Animator Controller do robô na cena da fábrica.
/// </summary>
public static class RobotAnimationSetup
{
    private const string ScenePath = "Assets/Scenes/FactoryLevel01.unity";
    private const string SpriteFolder = "Assets/Art/Sprites/Placeholders/RobotAnimations";
    private const string AnimationFolder = "Assets/Art/Animations/Robot";
    private const string ControllerPath = AnimationFolder + "/Robot.controller";
    private const string FontlessVisualName = "RobotVisual";

    private static readonly string[] Directions = { "Down", "Up", "Left", "Right" };

    [MenuItem("Tools/GS Robot Factory/Configure Robot Animations")]
    public static void ConfigureScene()
    {
        EnsureFolder("Assets/Art/Sprites/Placeholders");
        EnsureFolder(SpriteFolder);
        EnsureFolder("Assets/Art/Animations");
        EnsureFolder(AnimationFolder);
        CreateTemporarySprites();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        Sprite[,] sprites = LoadSprites();
        if (sprites == null)
            return;

        AnimatorController controller = CreateAnimatorController(sprites);
        if (controller == null)
            return;

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        RobotGridMovement movement = Object.FindFirstObjectByType<RobotGridMovement>();
        if (movement == null)
        {
            Debug.LogError("RobotGridMovement não encontrado em FactoryLevel01.");
            return;
        }

        ConfigureRobotVisual(movement, controller, sprites[0, 0]);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Animações temporárias do robô configuradas em FactoryLevel01.");
    }

    private static void CreateTemporarySprites()
    {
        for (int direction = 0; direction < Directions.Length; direction++)
        {
            WriteSpriteIfMissing($"Robot_Idle_{Directions[direction]}", direction, 0);
            WriteSpriteIfMissing($"Robot_Walk_{Directions[direction]}_A", direction, -1);
            WriteSpriteIfMissing($"Robot_Walk_{Directions[direction]}_B", direction, 1);
        }

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach (string file in Directory.GetFiles(SpriteFolder, "*.png"))
        {
            string assetPath = file.Replace('\\', '/');
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
        }
    }

    private static void WriteSpriteIfMissing(string spriteName, int direction, int walkOffset)
    {
        string path = $"{SpriteFolder}/{spriteName}.png";
        if (File.Exists(path))
            return;

        Color32 transparent = new Color32(0, 0, 0, 0);
        Color32 outline = new Color32(18, 30, 45, 255);
        Color32 shell = new Color32(79, 111, 133, 255);
        Color32 light = new Color32(156, 189, 201, 255);
        Color32 panel = new Color32(39, 64, 82, 255);
        Color32 cyan = new Color32(74, 229, 239, 255);
        Color32 leg = new Color32(42, 62, 77, 255);
        Color32[] pixels = Enumerable.Repeat(transparent, 32 * 32).ToArray();

        // Os dois pixels de deslocamento deixam os frames de caminhada distinguíveis.
        int shift = walkOffset;
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                int sourceX = x - shift;
                if (!IsRobotBody(sourceX, y))
                    continue;

                bool edge = !IsRobotBody(sourceX - 1, y)
                    || !IsRobotBody(sourceX + 1, y)
                    || !IsRobotBody(sourceX, y - 1)
                    || !IsRobotBody(sourceX, y + 1);

                Color32 color = edge ? outline : (y > 21 || sourceX < 10 ? light : shell);
                if (y >= 11 && y <= 18 && sourceX >= 12 && sourceX <= 19)
                    color = panel;
                SetPixel(pixels, x, y, color);
            }
        }

        // A placa central e o conector traseiro dão detalhes simples ao corpo do robô.
        FillRect(pixels, 14 + shift, 13, 4, 3, cyan);
        FillRect(pixels, 15 + shift, 25, 2, 2, light);

        // Os sensores cianos ficam na borda voltada para a direção do clip.
        switch (direction)
        {
            case 0: // Down
                FillRect(pixels, 10 + shift, 7, 4, 2, outline);
                FillRect(pixels, 18 + shift, 7, 4, 2, outline);
                FillRect(pixels, 11 + shift, 7, 2, 1, cyan);
                FillRect(pixels, 19 + shift, 7, 2, 1, cyan);
                break;
            case 1: // Up
                FillRect(pixels, 10 + shift, 23, 4, 2, outline);
                FillRect(pixels, 18 + shift, 23, 4, 2, outline);
                FillRect(pixels, 11 + shift, 24, 2, 1, cyan);
                FillRect(pixels, 19 + shift, 24, 2, 1, cyan);
                break;
            case 2: // Left
                FillRect(pixels, 6 + shift, 17, 2, 4, outline);
                FillRect(pixels, 6 + shift, 9, 2, 4, outline);
                FillRect(pixels, 6 + shift, 18, 1, 2, cyan);
                FillRect(pixels, 6 + shift, 10, 1, 2, cyan);
                break;
            default: // Right
                FillRect(pixels, 24 + shift, 17, 2, 4, outline);
                FillRect(pixels, 24 + shift, 9, 2, 4, outline);
                FillRect(pixels, 25 + shift, 18, 1, 2, cyan);
                FillRect(pixels, 25 + shift, 10, 1, 2, cyan);
                break;
        }

        // Os pés mudam de posição entre os dois frames de caminhada.
        if (walkOffset != 0)
        {
            int leftFoot = walkOffset < 0 ? 7 : 10;
            int rightFoot = walkOffset < 0 ? 19 : 22;
            FillRect(pixels, leftFoot, 1, 3, 3, leg);
            FillRect(pixels, rightFoot, 1, 3, 3, leg);
        }

        Texture2D texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.SetPixels32(pixels);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
    }

    private static bool IsRobotBody(int x, int y)
    {
        if (y >= 3 && y <= 5) return x >= 11 && x <= 20;
        if (y >= 6 && y <= 8) return x >= 8 && x <= 23;
        if (y >= 9 && y <= 20) return x >= 6 && x <= 25;
        if (y >= 21 && y <= 24) return x >= 8 && x <= 23;
        if (y >= 25 && y <= 27) return x >= 11 && x <= 20;
        return false;
    }

    private static void FillRect(Color32[] pixels, int x, int y, int width, int height, Color32 color)
    {
        for (int row = 0; row < height; row++)
        for (int column = 0; column < width; column++)
            SetPixel(pixels, x + column, y + row, color);
    }

    private static void SetPixel(Color32[] pixels, int x, int y, Color32 color)
    {
        if (x >= 0 && x < 32 && y >= 0 && y < 32)
            pixels[x + y * 32] = color;
    }

    private static Sprite[,] LoadSprites()
    {
        Sprite[,] sprites = new Sprite[4, 3];
        for (int direction = 0; direction < Directions.Length; direction++)
        {
            sprites[direction, 0] = LoadSprite($"Robot_Idle_{Directions[direction]}");
            sprites[direction, 1] = LoadSprite($"Robot_Walk_{Directions[direction]}_A");
            sprites[direction, 2] = LoadSprite($"Robot_Walk_{Directions[direction]}_B");
        }

        foreach (Sprite sprite in sprites)
        {
            if (sprite == null)
            {
                Debug.LogError("Não foi possível carregar os sprites temporários do robô.");
                return null;
            }
        }

        return sprites;
    }

    private static Sprite LoadSprite(string spriteName)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/{spriteName}.png");
    }

    private static AnimatorController CreateAnimatorController(Sprite[,] sprites)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        controller.parameters = new AnimatorControllerParameter[0];
        controller.AddParameter("Direction", AnimatorControllerParameterType.Int);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        foreach (ChildAnimatorState state in stateMachine.states)
            stateMachine.RemoveState(state.state);
        foreach (AnimatorStateTransition transition in stateMachine.anyStateTransitions)
            stateMachine.RemoveAnyStateTransition(transition);

        AnimatorState[] idleStates = new AnimatorState[4];
        AnimatorState[] walkStates = new AnimatorState[4];

        for (int direction = 0; direction < Directions.Length; direction++)
        {
            AnimationClip idleClip = CreateOrLoadClip($"Robot_Idle_{Directions[direction]}", sprites[direction, 0], null);
            AnimationClip walkClip = CreateOrLoadClip(
                $"Robot_Walk_{Directions[direction]}",
                sprites[direction, 1],
                sprites[direction, 2]);

            idleStates[direction] = stateMachine.AddState($"Idle {Directions[direction]}");
            idleStates[direction].motion = idleClip;
            walkStates[direction] = stateMachine.AddState($"Walk {Directions[direction]}");
            walkStates[direction].motion = walkClip;

            AddAnyStateTransition(stateMachine, idleStates[direction], direction, false);
            AddAnyStateTransition(stateMachine, walkStates[direction], direction, true);
        }

        stateMachine.defaultState = idleStates[0];
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static AnimationClip CreateOrLoadClip(string clipName, Sprite firstFrame, Sprite secondFrame)
    {
        string path = $"{AnimationFolder}/{clipName}.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip != null)
            return clip;

        clip = new AnimationClip { frameRate = 8f };
        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
        ObjectReferenceKeyframe[] frames = secondFrame == null
            ? new[]
            {
                new ObjectReferenceKeyframe { time = 0f, value = firstFrame },
                new ObjectReferenceKeyframe { time = 0.2f, value = firstFrame }
            }
            : new[]
            {
                new ObjectReferenceKeyframe { time = 0f, value = firstFrame },
                new ObjectReferenceKeyframe { time = 0.125f, value = secondFrame },
                new ObjectReferenceKeyframe { time = 0.24f, value = secondFrame }
            };

        AnimationUtility.SetObjectReferenceCurve(clip, binding, frames);
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static void AddAnyStateTransition(AnimatorStateMachine stateMachine, AnimatorState state, int direction, bool isMoving)
    {
        AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(state);
        transition.hasExitTime = false;
        transition.duration = 0.04f;
        transition.canTransitionToSelf = false;
        transition.AddCondition(AnimatorConditionMode.Equals, direction, "Direction");
        transition.AddCondition(isMoving ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, "IsMoving");
    }

    private static void ConfigureRobotVisual(RobotGridMovement movement, AnimatorController controller, Sprite previewSprite)
    {
        GameObject robot = movement.gameObject;
        SpriteRenderer rootRenderer = robot.GetComponent<SpriteRenderer>();
        Transform visualTransform = robot.transform.Find(FontlessVisualName);
        GameObject visualObject = visualTransform != null ? visualTransform.gameObject : new GameObject(FontlessVisualName);
        if (visualTransform == null)
            visualObject.transform.SetParent(robot.transform, false);

        visualObject.transform.localPosition = Vector3.zero;
        visualObject.transform.localRotation = Quaternion.identity;
        visualObject.transform.localScale = Vector3.one;

        SpriteRenderer visualRenderer = visualObject.GetComponent<SpriteRenderer>();
        if (visualRenderer == null)
            visualRenderer = visualObject.AddComponent<SpriteRenderer>();
        visualRenderer.sprite = previewSprite;
        if (rootRenderer != null)
        {
            visualRenderer.sortingLayerID = rootRenderer.sortingLayerID;
            visualRenderer.sortingOrder = rootRenderer.sortingOrder;
            visualRenderer.color = rootRenderer.color;
            rootRenderer.enabled = false;
        }

        Animator animator = visualObject.GetComponent<Animator>();
        if (animator == null)
            animator = visualObject.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;

        RobotAnimationController animationController = visualObject.GetComponent<RobotAnimationController>();
        if (animationController == null)
            animationController = visualObject.AddComponent<RobotAnimationController>();

        SerializedObject serializedController = new SerializedObject(animationController);
        serializedController.FindProperty("animator").objectReferenceValue = animator;
        serializedController.FindProperty("robotMovement").objectReferenceValue = movement;
        serializedController.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(robot);
        EditorUtility.SetDirty(visualObject);
    }

    private static void EnsureFolder(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        string currentPath = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string nextPath = $"{currentPath}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(nextPath))
                AssetDatabase.CreateFolder(currentPath, parts[i]);
            currentPath = nextPath;
        }
    }
}
