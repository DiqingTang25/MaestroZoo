using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using MaestroZoo;

public static class MaestroSceneBuilder
{
    private const string ScenePath = "Assets/_Project/Scenes/Main.unity";

    [MenuItem("Maestro Zoo/Build Production Scene")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ═══ CAMERA ═══
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.transform.position = new Vector3(0f, 3.5f, -6f);
        cam.transform.LookAt(new Vector3(0f, 1.2f, 0f));

        // ═══ LIGHT ═══
        var lightGo = new GameObject("Directional Light");
        var dl = lightGo.AddComponent<Light>();
        dl.type = LightType.Directional;
        dl.intensity = 1.5f;
        dl.shadows = LightShadows.Soft;
        dl.color = new Color(1f, 0.95f, 0.82f);
        lightGo.transform.rotation = Quaternion.Euler(55f, -25f, 0f);

        // ═══ FOREST STAGE ═══
        var stageGo = new GameObject("Stage");
        var forestPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Models/Scenes/Forest_House.fbx");
        if (forestPrefab != null)
        {
            var forest = (GameObject)PrefabUtility.InstantiatePrefab(forestPrefab);
            forest.name = "Forest_House";
            forest.transform.SetParent(stageGo.transform);
            forest.transform.localPosition = new Vector3(0f, -0.1f, 3.5f);
            forest.transform.localScale = Vector3.one * 1.0f;
            forest.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        }
        else
        {
            var platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = "Stage_Platform";
            platform.transform.SetParent(stageGo.transform);
            platform.transform.localPosition = new Vector3(0f, -0.3f, 2f);
            platform.transform.localScale = new Vector3(8f, 0.1f, 4f);
        }

        // ═══ GAME DIRECTOR ═══
        var directorGo = new GameObject("GameDirector");
        var chartPlayer = directorGo.AddComponent<ChartPlayer>();
        var audioSource = directorGo.AddComponent<AudioSource>();
        audioSource.volume = 0.55f;
        audioSource.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(
            "Assets/_Project/Resources/Audio/figaro_wedding.m4a");
        chartPlayer.musicSource = audioSource;

        var noteSpawner = directorGo.AddComponent<NoteSpawner>();
        noteSpawner.chartPlayer = chartPlayer;
        noteSpawner.spawnRoot = directorGo.transform;
        noteSpawner.judgeZ = 0f;
        noteSpawner.spawnZ = 8f;

        var judge = directorGo.AddComponent<JudgeManager>();
        judge.chartPlayer = chartPlayer;
        judge.noteSpawner = noteSpawner;

        var nativeInput = directorGo.AddComponent<RokidNativeGestureInput>();
        var handInput = directorGo.AddComponent<RokidHandGestureInput>();
        var inputDispatcher = directorGo.AddComponent<GestureInputDispatcher>();
        inputDispatcher.nativeInput = nativeInput;
        inputDispatcher.handInput = handInput;
        judge.inputBehaviour = inputDispatcher;

        var debugPanel = directorGo.AddComponent<RokidDebugPanel>();
        debugPanel.dispatcher = inputDispatcher;
        debugPanel.nativeInput = nativeInput;
        debugPanel.handInput = handInput;

        var orchestra = directorGo.AddComponent<OrchestraController>();
        orchestra.judgeManager = judge;
        orchestra.stageLight = dl;

        var director = directorGo.AddComponent<MaestroGameDirector>();
        director.autoStartChallenge = true;
        director.chartPlayer = chartPlayer;
        director.noteSpawner = noteSpawner;
        director.judgeManager = judge;
        director.gestureInput = inputDispatcher;
        director.orchestra = orchestra;

        var sfx = directorGo.AddComponent<GameSfxPlayer>();
        sfx.judgeManager = judge;
        sfx.gestureInput = inputDispatcher;

        // ═══ NOTE TEMPLATE ═══
        var noteRoot = new GameObject("FlyingNote");
        noteRoot.transform.SetParent(directorGo.transform);
        noteRoot.SetActive(false);

        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "NoteBody";
        body.transform.SetParent(noteRoot.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = new Vector3(1f, 0.18f, 0.55f);

        var tip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tip.name = "NoteTip";
        tip.transform.SetParent(noteRoot.transform);
        tip.transform.localPosition = new Vector3(0f, 0f, -0.38f);
        tip.transform.localScale = new Vector3(0.55f, 0.12f, 0.3f);

        var rb = noteRoot.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
        noteRoot.AddComponent<FlyingNote>();
        noteRoot.AddComponent<GestureNoteVisual>();

        noteSpawner.notePrefab = noteRoot.GetComponent<FlyingNote>();

        // ═══ ANIMALS ═══
        var animalsRoot = new GameObject("Animals");
        animalsRoot.transform.SetParent(directorGo.transform);

        string[] ids   = { "RabbitDrum",   "FoxViolin",    "BearCello",    "BirdFlute",    "ElephantHorn" };
        string[] roles = { "Drummer",      "Violinist",    "Cellist",      "Flutist",      "Pianist" };
        Vector3[] pos  = { new(-2.3f,1f,0.8f), new(-1.15f,1f,0.3f), new(0f,1.1f,0.9f), new(1.15f,1.3f,0.1f), new(2.3f,1.2f,0.8f) };
        Color[] colors = { new(1f,0.62f,0.75f), new(1f,0.48f,0.18f), new(0.52f,0.33f,0.18f), new(0.35f,0.6f,1f), new(0.48f,0.52f,0.7f) };

        string[] instruments =
        {
            "Assets/Models/Instruments/Drum_Set.fbx",
            "Assets/Models/Instruments/Violin.glb",
            "Assets/Models/Instruments/Cello.fbx",
            "Assets/Models/Instruments/Flute.glb",
            "Assets/Models/Instruments/Grand_Piano.glb"
        };

        // Idle models per animal
        // NOTE: Rabbit model is missing; using Cat (小猫) as placeholder for RabbitDrum.
        string[] idleModels =
        {
            "Assets/Models/Animals/小猫待机.fbx",       // RabbitDrum ← Cat placeholder (Rabbit missing)
            "Assets/Models/Animals/小狐狸待机.fbx",     // FoxViolin
            "Assets/Models/Animals/小熊待机.fbx",       // BearCello
            "Assets/Models/Animals/小鸟待机 - 副本.fbx", // BirdFlute (TODO: rename to 小鸟待机.fbx)
            "Assets/Models/Animals/小象待机.fbx"        // ElephantHorn
        };

        // Score animation models per animal
        // NOTE: Bird score is .blend (needs Blender to auto-convert to FBX in Unity).
        string[] scoreModels =
        {
            "Assets/Models/Animals/小猫得分1.fbx",      // RabbitDrum ← Cat placeholder (Rabbit missing)
            "Assets/Models/Animals/小狐狸得分.fbx",     // FoxViolin
            "Assets/Models/Animals/小熊得分.fbx",       // BearCello
            "Assets/Models/Animals/小鸟得分.blend",     // BirdFlute (blend, needs Blender installed)
            "Assets/Models/Animals/小象得分.fbx"        // ElephantHorn
        };

        for (int i = 0; i < 5; i++)
        {
            BuildAnimal(animalsRoot.transform, ids[i], roles[i], pos[i],
                colors[i], idleModels[i], scoreModels[i], instruments[i]);
        }

        foreach (var a in animalsRoot.GetComponentsInChildren<AnimalPerformer>())
            orchestra.Register(a);

        // ═══ UI ═══
        BuildUI(directorGo.transform, director, judge, orchestra);

        // ═══ EVENT SYSTEM ═══
        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // ═══ SAVE ═══
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log("[Maestro] Scene built: " + ScenePath);
        EditorSceneManager.OpenScene(ScenePath);
    }

    static GameObject BuildAnimal(Transform parent, string id, string role,
        Vector3 pos, Color color, string idleModelPath, string scoreModelPath,
        string instrumentPath)
    {
        var go = new GameObject(id);
        go.transform.SetParent(parent);
        go.transform.localPosition = pos;

        Renderer bodyRenderer = null;
        GameObject idleModel = null;
        GameObject scoreModel = null;

        // --- Idle Model ---
        GameObject idleAsset = AssetDatabase.LoadAssetAtPath<GameObject>(idleModelPath);
        if (idleAsset != null)
        {
            idleModel = (GameObject)PrefabUtility.InstantiatePrefab(idleAsset);
            idleModel.name = "IdleModel";
            idleModel.transform.SetParent(go.transform);
            idleModel.transform.localPosition = Vector3.zero;
            idleModel.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            idleModel.transform.localScale = Vector3.one * 0.55f;
            bodyRenderer = idleModel.GetComponentInChildren<Renderer>();
        }
        else
        {
            // Fallback: primitive capsule when model is missing
            idleModel = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            idleModel.name = "IdleModel_Fallback";
            idleModel.transform.SetParent(go.transform);
            idleModel.transform.localPosition = new Vector3(0, 0.3f, 0);
            idleModel.transform.localScale = new Vector3(0.35f, 0.45f, 0.3f);
            bodyRenderer = idleModel.GetComponent<Renderer>();
            bodyRenderer.material.color = color;
            Debug.LogWarning($"[SceneBuilder] Missing idle model for {id}: {idleModelPath}");
        }

        // --- Score Animation Model ---
        if (!string.IsNullOrEmpty(scoreModelPath))
        {
            GameObject scoreAsset = AssetDatabase.LoadAssetAtPath<GameObject>(scoreModelPath);
            if (scoreAsset != null)
            {
                scoreModel = (GameObject)PrefabUtility.InstantiatePrefab(scoreAsset);
                scoreModel.name = "ScoreModel";
                scoreModel.transform.SetParent(go.transform);
                scoreModel.transform.localPosition = Vector3.zero;
                scoreModel.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                scoreModel.transform.localScale = Vector3.one * 0.55f;
                scoreModel.SetActive(false); // Hidden by default
            }
            else
            {
                Debug.LogWarning($"[SceneBuilder] Missing score model for {id}: {scoreModelPath}");
            }
        }

        // --- Instrument ---
        if (!string.IsNullOrEmpty(instrumentPath))
        {
            var ip = AssetDatabase.LoadAssetAtPath<GameObject>(instrumentPath);
            if (ip != null)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(ip);
                inst.name = "Instrument";
                inst.transform.SetParent(go.transform);
                inst.transform.localPosition = new Vector3(0.38f, 0.18f, -0.18f);
                inst.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                inst.transform.localScale = Vector3.one * 0.28f;
            }
        }

        // --- Label ---
        var lbl = new GameObject("Label");
        lbl.transform.SetParent(go.transform);
        lbl.transform.localPosition = new Vector3(0, 1.05f, 0);
        var tm = lbl.AddComponent<TextMesh>();
        tm.text = role; tm.fontSize = 48;
        tm.anchor = TextAnchor.MiddleCenter; tm.color = Color.white;
        tm.characterSize = 0.035f;

        var ap = go.AddComponent<AnimalPerformer>();
        ap.animalId = id; ap.displayName = role;
        ap.bodyRenderer = bodyRenderer; ap.label = tm;
        ap.idleModel = idleModel;
        ap.scoreModel = scoreModel;

        return go;
    }

    static void BuildUI(Transform parent, MaestroGameDirector director,
        JudgeManager judge, OrchestraController orchestra)
    {
        var canvasGo = new GameObject("Canvas");
        var c = canvasGo.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        var hudGo = new GameObject("GameHud");
        hudGo.transform.SetParent(canvasGo.transform);
        var hud = hudGo.AddComponent<GameHud>();

        hud.scoreText    = Txt("Score",    canvasGo.transform, 80, -45,  0, 1,        44, TextAnchor.MiddleLeft);
        hud.songNameText = Txt("SongName", canvasGo.transform, 0,  -45,  0.5f, 1,    22, TextAnchor.MiddleCenter);
        hud.comboText    = Txt("Combo",    canvasGo.transform, 0,  -150, 0.5f, 0.5f, 56, TextAnchor.MiddleCenter);
        hud.maxComboText = Txt("MaxCombo", canvasGo.transform, 80, -80,  0, 1,        18, TextAnchor.MiddleLeft);
        hud.judgeText    = Txt("Judgment", canvasGo.transform, 0,  -250, 0.5f, 0.5f, 38, TextAnchor.MiddleCenter);
        hud.maxComboText.color = new Color(1, 1, 1, 0.45f);

        var resultsPanel = new GameObject("ResultsPanel");
        resultsPanel.transform.SetParent(canvasGo.transform);
        var resultsRt = resultsPanel.AddComponent<RectTransform>();
        resultsRt.anchorMin = resultsRt.anchorMax = new Vector2(0.5f, 0.5f);
        resultsRt.anchoredPosition = Vector2.zero;
        resultsRt.sizeDelta = new Vector2(760, 430);
        var resultsImage = resultsPanel.AddComponent<Image>();
        resultsImage.color = new Color(0.03f, 0.04f, 0.06f, 0.84f);

        hud.resultsPanel = resultsPanel;
        hud.resultsTitleText = Txt("ResultsTitle", resultsPanel.transform, 0, 150, 0.5f, 0.5f, 32, TextAnchor.MiddleCenter);
        hud.resultsScoreText = Txt("ResultsScore", resultsPanel.transform, 0, 70, 0.5f, 0.5f, 64, TextAnchor.MiddleCenter);
        hud.resultsStatsText = Txt("ResultsStats", resultsPanel.transform, 0, -70, 0.5f, 0.5f, 28, TextAnchor.MiddleCenter);
        hud.resultsStatsText.rectTransform.sizeDelta = new Vector2(680, 180);
        hud.SetResultsVisible(false);

        // Progress bar
        var prog = new GameObject("Progress");
        prog.transform.SetParent(canvasGo.transform);
        var prt = prog.AddComponent<RectTransform>();
        prt.anchorMin = new Vector2(0, 0); prt.anchorMax = new Vector2(1, 0);
        prt.anchoredPosition = new Vector2(0, 32); prt.sizeDelta = new Vector2(-100, 6);
        var ps = prog.AddComponent<Slider>();
        var pbg = new GameObject("BG"); pbg.transform.SetParent(prog.transform);
        pbg.AddComponent<RectTransform>().Stretch();
        pbg.AddComponent<Image>().color = new Color(0, 0, 0, 0.3f);
        var pfi = new GameObject("Fill"); pfi.transform.SetParent(prog.transform);
        var pfrt = pfi.AddComponent<RectTransform>(); pfrt.Stretch();
        var pimg = pfi.AddComponent<Image>(); pimg.color = new Color(1, 0.82f, 0.12f);
        ps.targetGraphic = pimg; ps.fillRect = pfrt; ps.handleRect = null;
        hud.progressBar = ps;

        // Fever glow
        var fv = new GameObject("Fever"); fv.transform.SetParent(canvasGo.transform);
        var fvrt = fv.AddComponent<RectTransform>(); fvrt.Stretch();
        fv.AddComponent<Image>().color = new Color(1, 0.85f, 0.05f, 0);

        // Mood bar
        var mb = new GameObject("Mood"); mb.transform.SetParent(canvasGo.transform);
        var mbrt = mb.AddComponent<RectTransform>();
        mbrt.anchorMin = mbrt.anchorMax = new Vector2(1, 1);
        mbrt.anchoredPosition = new Vector2(-50, -25); mbrt.sizeDelta = new Vector2(140, 8);
        var ms = mb.AddComponent<Slider>();
        var mbg = new GameObject("MBG"); mbg.transform.SetParent(mb.transform);
        mbg.AddComponent<RectTransform>().Stretch();
        mbg.AddComponent<Image>().color = new Color(0, 0, 0, 0.3f);
        var mfi = new GameObject("MFill"); mfi.transform.SetParent(mb.transform);
        var mfrt = mfi.AddComponent<RectTransform>(); mfrt.Stretch();
        var mfimg = mfi.AddComponent<Image>(); mfimg.color = new Color(0.25f, 0.78f, 0.35f);
        ms.targetGraphic = mfimg; ms.fillRect = mfrt; ms.handleRect = null;
        hud.moodBar = ms;

        var connGo = new GameObject("HudConnector");
        connGo.transform.SetParent(parent);
        var conn = connGo.AddComponent<HudConnector>();
        conn.gameHud = hud; conn.gameDirector = director;
        conn.judgeManager = judge; conn.orchestra = orchestra;

        var feedback = canvasGo.AddComponent<GestureFeedbackDisplay>();
        feedback.gestureInput = director.gestureInput;
    }

    static Text Txt(string n, Transform p, float x, float y, float ax, float ay, int s, TextAnchor a)
    {
        var go = new GameObject(n); go.transform.SetParent(p);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(ax, ay);
        rt.anchoredPosition = new Vector2(x, y); rt.sizeDelta = new Vector2(500, 80);
        var t = go.AddComponent<Text>();
        t.fontSize = s; t.alignment = a; t.color = Color.white; t.raycastTarget = false;
        return t;
    }
}

public static class RectEx
{
    public static void Stretch(this RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }
}
