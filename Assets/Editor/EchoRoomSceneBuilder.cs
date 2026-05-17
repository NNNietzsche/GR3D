using System.IO;
using EchoRoom.Board;
using EchoRoom.Core;
using EchoRoom.LLM;
using EchoRoom.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class EchoRoomSceneBuilder
{
    [MenuItem("Tools/Echo Room/Build Starter Scene", false, 1)]
    public static void BuildStarterScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientLight = new Color(0.18f, 0.17f, 0.20f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.035f, 0.032f, 0.045f);
        RenderSettings.fogDensity = 0.018f;

        CreateCamera();
        CreateLighting();
        CreateRoom();

        GameObject gameObject = new GameObject("Echo Room Game");
        EchoRoomGameManager manager = gameObject.AddComponent<EchoRoomGameManager>();
        GeminiClient gemini = gameObject.AddComponent<GeminiClient>();
        manager.GeminiClient = gemini;

        GameObject boardRoot = new GameObject("Board");
        PlayerPieceView pieceView = boardRoot.AddComponent<PlayerPieceView>();
        boardRoot.AddComponent<BoardLabelLocalizer>();
        BuildBoard(boardRoot.transform);
        BuildPlayers(boardRoot.transform);
        Connect(pieceView, "gameManager", manager);

        Canvas canvas = CreateCanvas();
        CreateEventSystem();
        EchoRoomUI ui = CreateUi(canvas.transform, manager);

        Selection.activeObject = ui.gameObject;
        Directory.CreateDirectory("Assets/Scenes");
        string scenePath = "Assets/Scenes/Main.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        EditorUtility.DisplayDialog("Echo Room", "主场景已生成：\n" + scenePath + "\n\n新版 UI 和棋盘美术已应用。", "OK");
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.025f, 0.023f, 0.034f);
        camera.orthographic = false;
        camera.fieldOfView = 42f;
        camera.transform.position = new Vector3(0f, 7.2f, -8.8f);
        camera.transform.rotation = Quaternion.Euler(50f, 0f, 0f);
        cameraObject.tag = "MainCamera";
    }

    private static void CreateLighting()
    {
        GameObject keyObject = new GameObject("Key Light");
        Light key = keyObject.AddComponent<Light>();
        key.type = LightType.Directional;
        key.intensity = 1.55f;
        key.color = new Color(1f, 0.90f, 0.78f);
        keyObject.transform.rotation = Quaternion.Euler(48f, -36f, 0f);

        GameObject fillObject = new GameObject("Table Glow");
        Light fill = fillObject.AddComponent<Light>();
        fill.type = LightType.Point;
        fill.intensity = 4.2f;
        fill.range = 8.5f;
        fill.color = new Color(0.32f, 0.82f, 0.76f);
        fillObject.transform.position = new Vector3(0f, 2.0f, 0f);

        GameObject rimObject = new GameObject("Rim Light");
        Light rim = rimObject.AddComponent<Light>();
        rim.type = LightType.Point;
        rim.intensity = 2.1f;
        rim.range = 7f;
        rim.color = new Color(0.62f, 0.48f, 1f);
        rimObject.transform.position = new Vector3(-4.6f, 2.2f, -3.2f);
    }

    private static void CreateRoom()
    {
        Material table = CreateMaterial("Velvet Table", new Color(0.035f, 0.075f, 0.075f), 0.35f, 0.08f);
        Material floor = CreateMaterial("Dark Floor", new Color(0.025f, 0.023f, 0.030f), 0.15f, 0.02f);
        Material wall = CreateMaterial("Back Wall", new Color(0.055f, 0.044f, 0.070f), 0.18f, 0.02f);
        Material line = CreateMaterial("Table Inlay", new Color(0.24f, 0.86f, 0.80f), 0.55f, 0.10f);

        CreateCube("Floor", new Vector3(0f, -0.18f, 0.25f), new Vector3(14f, 0.12f, 11f), floor);
        CreateCube("Back Wall", new Vector3(0f, 1.9f, 4.25f), new Vector3(14f, 4.0f, 0.16f), wall);
        CreateCube("Game Table", new Vector3(0f, -0.03f, -0.05f), new Vector3(12.7f, 0.18f, 9.7f), table);
        CreateCube("Table Edge Front", new Vector3(0f, 0.08f, -4.98f), new Vector3(12.9f, 0.18f, 0.06f), line);
        CreateCube("Table Edge Back", new Vector3(0f, 0.08f, 4.63f), new Vector3(12.9f, 0.18f, 0.06f), line);
        CreateCube("Table Edge Left", new Vector3(-6.48f, 0.08f, -0.18f), new Vector3(0.06f, 0.18f, 9.6f), line);
        CreateCube("Table Edge Right", new Vector3(6.48f, 0.08f, -0.18f), new Vector3(0.06f, 0.18f, 9.6f), line);
        CreateDice(new Vector3(5.45f, 0.38f, -3.95f));
        CreateCardStack(new Vector3(-5.35f, 0.30f, 3.55f));
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static void CreateEventSystem()
    {
        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private static EchoRoomUI CreateUi(Transform canvas, EchoRoomGameManager manager)
    {
        GameObject uiObject = new GameObject("Echo Room UI");
        uiObject.transform.SetParent(canvas, false);
        EchoRoomUI ui = uiObject.AddComponent<EchoRoomUI>();

        Text sceneTitle = CreateText(canvas, "Scene Title", "ECHO ROOM", new Vector2(0f, 1f), new Vector2(28f, -24f), 30, new Color(0.93f, 0.96f, 1f));
        sceneTitle.alignment = TextAnchor.MiddleLeft;
        sceneTitle.rectTransform.sizeDelta = new Vector2(420f, 58f);

        Text turn = CreateText(canvas, "Current Turn", "当前：Player 1", new Vector2(0f, 1f), new Vector2(30f, -72f), 18, new Color(0.64f, 0.93f, 0.88f));
        turn.alignment = TextAnchor.MiddleLeft;
        turn.rectTransform.sizeDelta = new Vector2(360f, 42f);

        Text target = CreateText(canvas, "Target Score", "目标 10", new Vector2(1f, 1f), new Vector2(-30f, -26f), 18, new Color(0.98f, 0.84f, 0.43f));
        target.alignment = TextAnchor.MiddleRight;
        target.rectTransform.sizeDelta = new Vector2(230f, 48f);

        Dropdown languageDropdown = CreateDropdown(canvas, "Language Dropdown", new Vector2(1f, 1f), new Vector2(-30f, -74f), new Vector2(150f, 36f));

        Text scoreboard = CreatePanelText(canvas, "Scoreboard", "分数榜", new Vector2(0f, 0.5f), new Vector2(28f, 54f), new Vector2(245f, 205f), 17);
        Text log = CreatePanelText(canvas, "Log", "游戏记录", new Vector2(1f, 0.5f), new Vector2(-28f, -6f), new Vector2(360f, 410f), 15);

        Button rollButton = CreateButton(canvas, "Roll Button", "掷骰子", new Vector2(1f, 0f), new Vector2(-34f, 32f), ButtonStyle.Primary);
        rollButton.GetComponent<RectTransform>().sizeDelta = new Vector2(178f, 54f);

        GameObject setupPanel = CreatePanel(canvas, "Setup Panel", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 390f), new Color(0.035f, 0.030f, 0.045f, 0.96f));
        AddPanelAccent(setupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(580f, 3f), new Color(0.31f, 0.82f, 0.78f, 0.95f));

        Text setupTitle = CreateText(setupPanel.transform, "Setup Title", "开始 Echo Room", new Vector2(0.5f, 1f), new Vector2(0f, -42f), 28, Color.white);
        setupTitle.rectTransform.sizeDelta = new Vector2(540f, 50f);

        Text setupHint = CreateText(setupPanel.transform, "Setup Hint", "设置今晚的氛围、人数和胜利分", new Vector2(0.5f, 1f), new Vector2(0f, -82f), 15, new Color(0.70f, 0.74f, 0.82f));
        setupHint.rectTransform.sizeDelta = new Vector2(540f, 36f);

        Text promptLabel = CreateText(setupPanel.transform, "Prompt Label", "背景主题", new Vector2(0f, 1f), new Vector2(54f, -122f), 16, new Color(0.84f, 0.89f, 0.95f));
        promptLabel.alignment = TextAnchor.MiddleLeft;
        promptLabel.rectTransform.sizeDelta = new Vector2(160f, 34f);
        InputField promptInput = CreateInputField(setupPanel.transform, "Prompt Input", "朋友在客厅围坐玩真心话大冒险，桌上有饮料和骰子。", new Vector2(0.5f, 1f), new Vector2(0f, -162f), new Vector2(520f, 46f));

        Text playerLabel = CreateText(setupPanel.transform, "Player Count Label", "人数", new Vector2(0f, 1f), new Vector2(54f, -224f), 16, new Color(0.84f, 0.89f, 0.95f));
        playerLabel.alignment = TextAnchor.MiddleLeft;
        playerLabel.rectTransform.sizeDelta = new Vector2(80f, 34f);
        InputField playerCountInput = CreateInputField(setupPanel.transform, "Player Count Input", "4", new Vector2(0f, 1f), new Vector2(126f, -224f), new Vector2(96f, 42f));

        Text targetLabel = CreateText(setupPanel.transform, "Target Score Label", "胜利分", new Vector2(0f, 1f), new Vector2(294f, -224f), 16, new Color(0.84f, 0.89f, 0.95f));
        targetLabel.alignment = TextAnchor.MiddleLeft;
        targetLabel.rectTransform.sizeDelta = new Vector2(100f, 34f);
        InputField targetScoreInput = CreateInputField(setupPanel.transform, "Target Score Input", "10", new Vector2(0f, 1f), new Vector2(394f, -224f), new Vector2(96f, 42f));

        Button startButton = CreateButton(setupPanel.transform, "Start Button", "开始游戏", new Vector2(0.5f, 0f), new Vector2(0f, 42f), ButtonStyle.Primary);
        startButton.GetComponent<RectTransform>().sizeDelta = new Vector2(200f, 52f);

        GameObject cardPanel = CreatePanel(canvas, "Card Panel", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(610f, 320f), new Color(0.040f, 0.025f, 0.035f, 0.97f));
        AddPanelAccent(cardPanel.transform, new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(560f, 3f), new Color(0.98f, 0.36f, 0.42f, 0.95f));
        Text cardTitle = CreateText(cardPanel.transform, "Card Title", "挑战", new Vector2(0.5f, 1f), new Vector2(0f, -50f), 25, new Color(1f, 0.88f, 0.92f));
        cardTitle.rectTransform.sizeDelta = new Vector2(540f, 50f);
        Text cardBody = CreateText(cardPanel.transform, "Card Body", "题目内容", new Vector2(0.5f, 0.5f), new Vector2(0f, 18f), 19, Color.white);
        cardBody.rectTransform.sizeDelta = new Vector2(520f, 135f);

        Button primaryButton = CreateButton(cardPanel.transform, "Primary Button", "完成", new Vector2(0.5f, 0f), new Vector2(112f, 30f), ButtonStyle.Danger);
        primaryButton.GetComponent<RectTransform>().sizeDelta = new Vector2(170f, 46f);
        Button secondaryButton = CreateButton(cardPanel.transform, "Secondary Button", "跳过", new Vector2(0.5f, 0f), new Vector2(-112f, 30f), ButtonStyle.Secondary);
        secondaryButton.GetComponent<RectTransform>().sizeDelta = new Vector2(170f, 46f);
        cardPanel.SetActive(false);

        GameObject winnerPanel = CreatePanel(canvas, "Winner Panel", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440f, 190f), new Color(0.035f, 0.030f, 0.020f, 0.97f));
        AddPanelAccent(winnerPanel.transform, new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(390f, 3f), new Color(0.98f, 0.78f, 0.30f, 0.95f));
        Text winnerText = CreateText(winnerPanel.transform, "Winner Text", "WINNER", new Vector2(0.5f, 0.5f), Vector2.zero, 28, Color.white);
        winnerText.rectTransform.sizeDelta = new Vector2(390f, 120f);
        winnerPanel.SetActive(false);

        SerializedObject uiSo = new SerializedObject(ui);
        uiSo.FindProperty("gameManager").objectReferenceValue = manager;
        uiSo.FindProperty("sceneTitle").objectReferenceValue = sceneTitle;
        uiSo.FindProperty("currentTurn").objectReferenceValue = turn;
        uiSo.FindProperty("targetScore").objectReferenceValue = target;
        uiSo.FindProperty("scoreboardText").objectReferenceValue = scoreboard;
        uiSo.FindProperty("logText").objectReferenceValue = log;
        uiSo.FindProperty("rollButton").objectReferenceValue = rollButton;
        uiSo.FindProperty("languageDropdown").objectReferenceValue = languageDropdown;
        uiSo.FindProperty("setupPanel").objectReferenceValue = setupPanel;
        uiSo.FindProperty("setupTitle").objectReferenceValue = setupTitle;
        uiSo.FindProperty("setupHint").objectReferenceValue = setupHint;
        uiSo.FindProperty("promptLabel").objectReferenceValue = promptLabel;
        uiSo.FindProperty("playerLabel").objectReferenceValue = playerLabel;
        uiSo.FindProperty("setupTargetLabel").objectReferenceValue = targetLabel;
        uiSo.FindProperty("promptInput").objectReferenceValue = promptInput;
        uiSo.FindProperty("targetScoreInput").objectReferenceValue = targetScoreInput;
        uiSo.FindProperty("playerCountInput").objectReferenceValue = playerCountInput;
        uiSo.FindProperty("startButton").objectReferenceValue = startButton;
        uiSo.FindProperty("winnerPanel").objectReferenceValue = winnerPanel;
        uiSo.FindProperty("winnerText").objectReferenceValue = winnerText;
        uiSo.FindProperty("cardPanel").objectReferenceValue = cardPanel;
        uiSo.FindProperty("cardTitle").objectReferenceValue = cardTitle;
        uiSo.FindProperty("cardBody").objectReferenceValue = cardBody;
        uiSo.FindProperty("primaryButton").objectReferenceValue = primaryButton;
        uiSo.FindProperty("secondaryButton").objectReferenceValue = secondaryButton;
        uiSo.ApplyModifiedPropertiesWithoutUndo();

        return ui;
    }

    private static GameObject CreatePanel(Transform parent, string name, Vector2 anchor, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        Image image = panel.AddComponent<Image>();
        image.color = color;
        return panel;
    }

    private static void AddPanelAccent(Transform parent, Vector2 anchor, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject accent = new GameObject("Accent");
        accent.transform.SetParent(parent, false);
        RectTransform rect = accent.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        Image image = accent.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private static Text CreatePanelText(Transform parent, string name, string text, Vector2 anchor, Vector2 anchoredPosition, Vector2 size, int fontSize)
    {
        GameObject panel = CreatePanel(parent, name + " Panel", anchor, anchoredPosition, size + new Vector2(30f, 30f), new Color(0.020f, 0.018f, 0.027f, 0.68f));
        AddPanelAccent(panel.transform, new Vector2(0.5f, 1f), new Vector2(0f, -5f), new Vector2(size.x - 18f, 2f), new Color(0.28f, 0.76f, 0.72f, 0.85f));
        Text label = CreateText(panel.transform, name, text, new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), fontSize, new Color(0.88f, 0.92f, 0.96f));
        label.alignment = TextAnchor.UpperLeft;
        label.rectTransform.sizeDelta = size;
        return label;
    }

    private static void BuildBoard(Transform root)
    {
        Material matStart = CreateMaterial("Cell Start", new Color(0.08f, 0.48f, 0.30f), 0.45f, 0.04f);
        Material matTruth = CreateMaterial("Cell Truth", new Color(0.34f, 0.28f, 0.78f), 0.50f, 0.03f);
        Material matDare = CreateMaterial("Cell Dare", new Color(0.78f, 0.25f, 0.29f), 0.48f, 0.03f);
        Material matFortune = CreateMaterial("Cell Fortune", new Color(0.78f, 0.54f, 0.18f), 0.52f, 0.04f);
        Material matSpecial = CreateMaterial("Cell Special", new Color(0.13f, 0.66f, 0.62f), 0.58f, 0.03f);
        Material matDark = CreateMaterial("Cell Dark", new Color(0.13f, 0.15f, 0.20f), 0.40f, 0.02f);
        Material matBorder = CreateMaterial("Cell Border", new Color(0.025f, 0.023f, 0.034f), 0.15f, 0.02f);

        CreateCube("Board Base", new Vector3(0f, 0.03f, 0f), new Vector3(11.35f, 0.14f, 9.05f), matBorder).transform.SetParent(root, true);

        for (int i = 0; i < BoardRules.Cells.Count; i++)
        {
            BoardCell cell = BoardRules.Cells[i];
            Vector3 position = GetBoardPosition(cell.Id);

            GameObject shadow = CreateCube("Cell Shadow", position + new Vector3(0.05f, 0.10f, -0.05f), new Vector3(0.95f, 0.10f, 0.95f), matBorder);
            shadow.transform.SetParent(root, true);

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = string.Format("Cell_{0:00}_{1}", cell.Id, cell.Label);
            cube.transform.SetParent(root, false);
            cube.transform.position = position + Vector3.up * 0.18f;
            cube.transform.localScale = new Vector3(0.92f, 0.24f, 0.92f);
            cube.GetComponent<Renderer>().sharedMaterial = GetMaterial(cell.Type, matStart, matTruth, matDare, matFortune, matSpecial, matDark);

            GameObject label = new GameObject("Label");
            label.transform.SetParent(cube.transform, false);
            label.transform.localPosition = new Vector3(0f, 0.62f, 0f);
            label.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            TextMesh text = label.AddComponent<TextMesh>();
            text.text = cell.Label;
            text.fontSize = 28;
            text.characterSize = 0.055f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = Color.white;

            GameObject number = new GameObject("Number");
            number.transform.SetParent(cube.transform, false);
            number.transform.localPosition = new Vector3(-0.33f, 0.63f, 0.32f);
            number.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            TextMesh numberText = number.AddComponent<TextMesh>();
            numberText.text = cell.Id.ToString("00");
            numberText.fontSize = 20;
            numberText.characterSize = 0.04f;
            numberText.anchor = TextAnchor.MiddleCenter;
            numberText.alignment = TextAlignment.Center;
            numberText.color = new Color(1f, 1f, 1f, 0.72f);
        }
    }

    private static void BuildPlayers(Transform root)
    {
        Color[] colors =
        {
            new Color(0.65f, 0.55f, 0.98f),
            new Color(0.98f, 0.42f, 0.45f),
            new Color(0.20f, 0.82f, 0.60f),
            new Color(0.98f, 0.75f, 0.25f),
            new Color(0.38f, 0.64f, 1.0f),
            new Color(0.96f, 0.44f, 0.72f)
        };

        Material baseMat = CreateMaterial("Player Base", new Color(0.035f, 0.033f, 0.045f), 0.35f, 0.02f);

        for (int i = 0; i < colors.Length; i++)
        {
            Vector3 start = GetBoardPosition(0) + new Vector3((i - 2.5f) * 0.12f, 0.40f, 0f);
            GameObject baseDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseDisc.name = "Player Base " + (i + 1);
            baseDisc.transform.SetParent(root, false);
            baseDisc.transform.position = start + Vector3.down * 0.08f;
            baseDisc.transform.localScale = new Vector3(0.28f, 0.045f, 0.28f);
            baseDisc.GetComponent<Renderer>().sharedMaterial = baseMat;

            GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            piece.name = "Player Piece " + (i + 1);
            piece.transform.SetParent(root, false);
            piece.transform.position = start + Vector3.up * 0.22f;
            piece.transform.localScale = Vector3.one * 0.32f;
            piece.GetComponent<Renderer>().sharedMaterial = CreateMaterial("Player " + (i + 1), colors[i], 0.65f, 0.08f);
        }
    }

    private static Vector3 GetBoardPosition(int id)
    {
        int row;
        int col;
        if (id <= 9)
        {
            row = 0;
            col = id;
        }
        else if (id <= 16)
        {
            row = id - 9;
            col = 9;
        }
        else if (id <= 26)
        {
            row = 7;
            col = 26 - id;
        }
        else
        {
            row = 35 - id;
            col = 0;
        }

        return new Vector3(col * 1.08f - 4.86f, 0f, row * 1.08f - 3.78f);
    }

    private static Material GetMaterial(CellType type, Material start, Material truth, Material dare, Material fortune, Material special, Material dark)
    {
        if (type == CellType.Start || type == CellType.Free) return start;
        if (type == CellType.Truth) return truth;
        if (type == CellType.Dare) return dare;
        if (type == CellType.Fortune) return fortune;
        if (type == CellType.All || type == CellType.Double) return special;
        return dark;
    }

    private static GameObject CreateCube(string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.position = position;
        cube.transform.localScale = scale;
        cube.GetComponent<Renderer>().sharedMaterial = material;
        return cube;
    }

    private static Material CreateMaterial(string name, Color color, float smoothness, float metallic)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.name = name;
        mat.color = color;
        mat.SetFloat("_Glossiness", smoothness);
        mat.SetFloat("_Metallic", metallic);
        return mat;
    }

    private static void CreateDice(Vector3 position)
    {
        Material diceMat = CreateMaterial("Ivory Dice", new Color(0.90f, 0.93f, 0.96f), 0.55f, 0.02f);
        Material pipMat = CreateMaterial("Dice Pips", new Color(0.04f, 0.05f, 0.07f), 0.30f, 0.00f);

        GameObject dice = GameObject.CreatePrimitive(PrimitiveType.Cube);
        dice.name = "3D Dice";
        dice.transform.position = position;
        dice.transform.rotation = Quaternion.Euler(14f, 26f, -9f);
        dice.transform.localScale = Vector3.one * 0.54f;
        dice.GetComponent<Renderer>().sharedMaterial = diceMat;

        Vector3[] pipOffsets =
        {
            new Vector3(-0.16f, 0.29f, -0.16f),
            new Vector3(0.16f, 0.29f, 0.16f),
            new Vector3(0f, 0.30f, 0f),
            new Vector3(-0.16f, 0.29f, 0.16f),
            new Vector3(0.16f, 0.29f, -0.16f)
        };

        for (int i = 0; i < pipOffsets.Length; i++)
        {
            GameObject pip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pip.name = "Dice Pip";
            pip.transform.SetParent(dice.transform, false);
            pip.transform.localPosition = pipOffsets[i];
            pip.transform.localScale = new Vector3(0.08f, 0.012f, 0.08f);
            pip.GetComponent<Renderer>().sharedMaterial = pipMat;
        }
    }

    private static void CreateCardStack(Vector3 position)
    {
        Material cardA = CreateMaterial("Card Stack A", new Color(0.35f, 0.28f, 0.76f), 0.42f, 0.02f);
        Material cardB = CreateMaterial("Card Stack B", new Color(0.78f, 0.24f, 0.34f), 0.42f, 0.02f);
        Material edge = CreateMaterial("Card Edge", new Color(0.90f, 0.84f, 0.72f), 0.30f, 0.00f);

        for (int i = 0; i < 5; i++)
        {
            Material mat = i % 2 == 0 ? cardA : cardB;
            GameObject card = CreateCube("Challenge Card", position + new Vector3(0.03f * i, 0.035f * i, -0.02f * i), new Vector3(1.08f, 0.035f, 0.72f), mat);
            card.transform.rotation = Quaternion.Euler(0f, -13f, 0f);
        }

        GameObject band = CreateCube("Card Band", position + new Vector3(0.09f, 0.20f, -0.04f), new Vector3(1.14f, 0.04f, 0.08f), edge);
        band.transform.rotation = Quaternion.Euler(0f, -13f, 0f);
    }

    private enum ButtonStyle
    {
        Primary,
        Secondary,
        Danger
    }

    private static Button CreateButton(Transform parent, string name, string text, Vector2 anchor, Vector2 anchoredPosition, ButtonStyle style)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.sizeDelta = new Vector2(180f, 48f);
        rect.anchoredPosition = anchoredPosition;

        Image image = obj.AddComponent<Image>();
        if (style == ButtonStyle.Primary) image.color = new Color(0.32f, 0.80f, 0.76f);
        if (style == ButtonStyle.Secondary) image.color = new Color(0.11f, 0.12f, 0.17f);
        if (style == ButtonStyle.Danger) image.color = new Color(0.96f, 0.38f, 0.44f);
        Outline outline = obj.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.45f);
        outline.effectDistance = new Vector2(0f, -3f);

        Button button = obj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = image.color * 1.12f;
        colors.pressedColor = image.color * 0.82f;
        colors.disabledColor = new Color(0.22f, 0.23f, 0.27f, 0.55f);
        button.colors = colors;

        Text label = CreateText(obj.transform, "Text", text, new Vector2(0.5f, 0.5f), Vector2.zero, 18, style == ButtonStyle.Secondary ? Color.white : Color.black);
        label.raycastTarget = false;
        label.rectTransform.sizeDelta = rect.sizeDelta;
        return button;
    }

    private static Dropdown CreateDropdown(Transform parent, string name, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = obj.AddComponent<Image>();
        image.color = new Color(0.035f, 0.032f, 0.050f, 0.90f);
        Outline outline = obj.AddComponent<Outline>();
        outline.effectColor = new Color(0.28f, 0.76f, 0.72f, 0.55f);
        outline.effectDistance = new Vector2(1f, -1f);

        Dropdown dropdown = obj.AddComponent<Dropdown>();
        dropdown.targetGraphic = image;

        Text label = CreateText(obj.transform, "Label", "中文", new Vector2(0f, 0.5f), new Vector2(12f, 0f), 15, Color.white);
        label.alignment = TextAnchor.MiddleLeft;
        label.raycastTarget = false;
        label.rectTransform.sizeDelta = new Vector2(size.x - 38f, size.y);
        dropdown.captionText = label;

        Text arrow = CreateText(obj.transform, "Arrow", "v", new Vector2(1f, 0.5f), new Vector2(-14f, 0f), 14, new Color(0.65f, 0.95f, 0.90f));
        arrow.raycastTarget = false;
        arrow.rectTransform.sizeDelta = new Vector2(24f, size.y);

        GameObject template = CreatePanel(obj.transform, "Template", new Vector2(0f, 0f), new Vector2(0f, -4f), new Vector2(size.x, 96f), new Color(0.025f, 0.023f, 0.035f, 0.98f));
        RectTransform templateRect = template.GetComponent<RectTransform>();
        templateRect.pivot = new Vector2(0f, 1f);
        templateRect.anchorMin = new Vector2(0f, 0f);
        templateRect.anchorMax = new Vector2(0f, 0f);

        GameObject item = new GameObject("Item");
        item.transform.SetParent(template.transform, false);
        RectTransform itemRect = item.AddComponent<RectTransform>();
        itemRect.anchorMin = new Vector2(0f, 1f);
        itemRect.anchorMax = new Vector2(1f, 1f);
        itemRect.pivot = new Vector2(0.5f, 1f);
        itemRect.anchoredPosition = Vector2.zero;
        itemRect.sizeDelta = new Vector2(0f, 30f);
        Image itemImage = item.AddComponent<Image>();
        itemImage.color = new Color(0.06f, 0.055f, 0.075f, 0.98f);
        Toggle toggle = item.AddComponent<Toggle>();
        toggle.targetGraphic = itemImage;

        Text itemLabel = CreateText(item.transform, "Item Label", "中文", new Vector2(0f, 0.5f), new Vector2(12f, 0f), 15, Color.white);
        itemLabel.alignment = TextAnchor.MiddleLeft;
        itemLabel.raycastTarget = false;
        itemLabel.rectTransform.sizeDelta = new Vector2(size.x - 22f, 30f);
        dropdown.itemText = itemLabel;
        dropdown.template = templateRect;
        template.SetActive(false);

        return dropdown;
    }

    private static InputField CreateInputField(Transform parent, string name, string text, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = obj.AddComponent<Image>();
        image.color = new Color(0.92f, 0.95f, 0.98f, 0.96f);

        InputField input = obj.AddComponent<InputField>();
        input.targetGraphic = image;
        input.text = text;

        Text inputText = CreateText(obj.transform, "Text", text, new Vector2(0.5f, 0.5f), Vector2.zero, 16, new Color(0.04f, 0.05f, 0.07f));
        inputText.alignment = TextAnchor.MiddleLeft;
        inputText.raycastTarget = false;
        inputText.rectTransform.sizeDelta = size - new Vector2(24f, 8f);
        input.textComponent = inputText;

        Text placeholder = CreateText(obj.transform, "Placeholder", text, new Vector2(0.5f, 0.5f), Vector2.zero, 16, new Color(0f, 0f, 0f, 0.35f));
        placeholder.alignment = TextAnchor.MiddleLeft;
        placeholder.raycastTarget = false;
        placeholder.rectTransform.sizeDelta = size - new Vector2(24f, 8f);
        input.placeholder = placeholder;

        return input;
    }

    private static Text CreateText(Transform parent, string name, string text, Vector2 anchor, Vector2 anchoredPosition, int size, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.sizeDelta = new Vector2(900f, 80f);
        rect.anchoredPosition = anchoredPosition;

        Text label = obj.AddComponent<Text>();
        label.text = text;
        label.fontSize = size;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = color;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        return label;
    }

    private static void Connect(Object target, string propertyName, Object value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        property.objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }
}
