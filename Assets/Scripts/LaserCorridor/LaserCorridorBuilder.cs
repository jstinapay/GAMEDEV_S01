using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// One-click builder for the Resident Evil style laser corridor.
/// Menu: LaserCorridor / Build Laser Corridor Level.
/// Idempotent: deletes the old LaserCorridorLevel root and rebuilds.
/// Also runnable headless for CI: BuildFromCommandLine().
/// </summary>
public static class LaserCorridorBuilder
{
    const string RootName = "LaserCorridorLevel";
    const float CorridorWidth = 6f;
    const float CorridorHeight = 4f;
    const float CorridorLength = 160f; // long run: start z=2, goal z=150
    const float CorridorMidZ = 73f;
    const float GoalZ = 150f;
    const float FarZ = 153f;
    static readonly Vector3 StartPos = new Vector3(0f, 1.1f, 2f);
    static readonly Vector3 SpawnPos = new Vector3(0f, 1.5f, 146f);
    const float CleanupZ = -6f;
    const int StripeCount = 38;

#if UNITY_EDITOR
    [MenuItem("LaserCorridor/Build Laser Corridor Level")]
    public static void BuildFromMenu()
    {
        Build();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Laser corridor built. Press Play to test. Tune via GameManager / LaserSpawn objects.");
    }

    /// <summary>Headless: Unity -batchmode -projectPath . -executeMethod LaserCorridorBuilder.BuildFromCommandLine -quit</summary>
    public static void BuildFromCommandLine()
    {
        try
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid())
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Build();
            string path = "Assets/Scenes/LaserCorridor.unity";
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), path);
            Debug.Log($"Saved {path}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"LaserCorridor build failed: {e}");
        }
    }
#endif

    public static void Build()
    {
        // Clean previous build.
        var old = GameObject.Find(RootName);
        if (old != null) Object.DestroyImmediate(old);
        // Clean old manager/UI duplicates the builder owns.
        foreach (var name in new[] { "GameManager", "GameHUD", "FinishHUD", "LaserSpawn", "Lasers", "GoalTrigger" })
        {
            var g = GameObject.Find(name);
            if (g != null && (g.transform.parent == null)) Object.DestroyImmediate(g);
        }

        var root = new GameObject(RootName);

        Material wallMat = MakeLit(new Color(0.12f, 0.12f, 0.14f), 0.8f, 0.4f);
        Material floorMat = MakeLit(new Color(0.16f, 0.16f, 0.18f), 0.7f, 0.5f);
        Material stripeMat = MakeEmissive(new Color(0.3f, 0.3f, 0.35f), Color.white * 0.4f);
        Material redGlow = MakeEmissive(Color.red * 0.5f, Color.red * 2.5f);
        Material greenGlow = MakeEmissive(Color.green * 0.5f, Color.green * 2f);

        // Floor, walls, ceiling (enclosed so player can't skip).
        AddBox(root.transform, "Floor", new Vector3(0f, -0.25f, CorridorMidZ),
            new Vector3(CorridorWidth + 1f, 0.5f, CorridorLength + 8f), floorMat, false);
        AddBox(root.transform, "Wall_Left", new Vector3(-(CorridorWidth / 2f + 0.25f), 2f, CorridorMidZ),
            new Vector3(0.5f, CorridorHeight + 0.5f, CorridorLength + 8f), wallMat, false);
        AddBox(root.transform, "Wall_Right", new Vector3(CorridorWidth / 2f + 0.25f, 2f, CorridorMidZ),
            new Vector3(0.5f, CorridorHeight + 0.5f, CorridorLength + 8f), wallMat, false);
        AddBox(root.transform, "Ceiling", new Vector3(0f, CorridorHeight + 0.25f, CorridorMidZ),
            new Vector3(CorridorWidth + 1f, 0.5f, CorridorLength + 8f), wallMat, false);
        AddBox(root.transform, "BackWall", new Vector3(0f, 2f, -4f),
            new Vector3(CorridorWidth + 1f, CorridorHeight + 1f, 0.5f), wallMat, false);
        AddBox(root.transform, "FarWall", new Vector3(0f, 2f, FarZ),
            new Vector3(CorridorWidth + 1f, CorridorHeight + 1f, 0.5f), wallMat, false);

        // Repeating floor stripes for speed readability (visual only, no colliders).
        for (int i = 0; i < StripeCount; i++)
        {
            var s = AddBox(root.transform, $"Stripe_{i:00}", new Vector3(0f, 0.01f, 2f + i * 4f),
                new Vector3(CorridorWidth, 0.02f, 0.3f), stripeMat, true);
            s.GetComponent<BoxCollider>().enabled = false;
        }

        // Red glow strips along walls (RE vibe, cheap emissive).
        var stripL = AddBox(root.transform, "GlowStrip_L", new Vector3(-2.9f, 2.6f, CorridorMidZ),
            new Vector3(0.1f, 0.15f, CorridorLength), redGlow, true);
        stripL.GetComponent<BoxCollider>().enabled = false;
        var stripR = AddBox(root.transform, "GlowStrip_R", new Vector3(2.9f, 2.6f, CorridorMidZ),
            new Vector3(0.1f, 0.15f, CorridorLength), redGlow, true);
        stripR.GetComponent<BoxCollider>().enabled = false;

        // Start / goal pads.
        var startPad = AddBox(root.transform, "StartPad", new Vector3(0f, 0.02f, 2f),
            new Vector3(3f, 0.04f, 3f), greenGlow, true);
        startPad.GetComponent<BoxCollider>().enabled = false;
        var goalPad = AddBox(root.transform, "GoalPad", new Vector3(0f, 0.02f, GoalZ),
            new Vector3(3.5f, 0.04f, 3f), greenGlow, true);
        goalPad.GetComponent<BoxCollider>().enabled = false;
        AddBeacon(root.transform, "StartBeacon", new Vector3(2.4f, 1.5f, 2f), Color.green);
        AddBeacon(root.transform, "EndBeacon", new Vector3(-2.4f, 1.5f, GoalZ), Color.green);
        AddBeacon(root.transform, "SpawnGate_L", new Vector3(-2.9f, 1.5f, SpawnPos.z), Color.red);
        AddBeacon(root.transform, "SpawnGate_R", new Vector3(2.9f, 1.5f, SpawnPos.z), Color.red);

        AddLights(root.transform);

        // Spawner + container.
        var spawnGo = new GameObject("LaserSpawn");
        spawnGo.transform.position = SpawnPos;
        var spawner = spawnGo.AddComponent<LaserSpawner>();
        spawner.spawnInterval = 0.6f;
        spawner.laserSpeed = 20f;
        spawner.laserDamage = 34;
        spawner.cleanupZ = CleanupZ;
        spawner.corridorHalfWidth = 2.2f;
        spawner.maxActive = 14;
        spawner.chaserWeight = 45;
        spawner.sweeperWeight = 25;
        spawner.lowBarWeight = 30;
        spawner.rampOverTime = true;
        spawner.minInterval = 0.4f;
        spawner.rampEverySeconds = 10f;
        spawner.doubleChance = 0.6f;
        spawner.doubleAfterSeconds = 6f;
        spawner.tripleChance = 0.2f;
        spawner.tripleAfterSeconds = 20f;
        spawner.sweeperMinZ = 15f;
        spawner.sweeperMaxZ = 145f;
        spawner.homingStrength = 2.2f;
        spawner.homingAfterSeconds = 12f;
        spawner.speedJitter = 0.3f;
        var container = new GameObject("Lasers");
        spawner.laserContainer = container.transform;
        spawner.laserPrefab = LoadOrBuildLaserPrefab(redGlow);

        // Bonus panels spread over the long run, plus a late invulnerability save.
        CreateBonusPanel(root.transform, "Bonus_Heal", new Vector3(0f, 0.12f, 15f),
            BonusPanel.BonusType.Heal, 0f, 0f, 25);
        CreateBonusPanel(root.transform, "Bonus_Speed", new Vector3(-1.5f, 0.12f, 40f),
            BonusPanel.BonusType.SpeedBoost, 6f, 1.5f, 0);
        CreateBonusPanel(root.transform, "Bonus_Shield", new Vector3(1.5f, 0.12f, 75f),
            BonusPanel.BonusType.DamageReduction, 8f, 0.5f, 0);
        CreateBonusPanel(root.transform, "Bonus_Invuln", new Vector3(0f, 0.12f, 105f),
            BonusPanel.BonusType.Invulnerability, 3f, 0f, 0);
        CreateBonusPanel(root.transform, "Bonus_Speed2", new Vector3(1.5f, 0.12f, 125f),
            BonusPanel.BonusType.SpeedBoost, 6f, 1.5f, 0);

        // Goal trigger.
        var goalGo = new GameObject("GoalTrigger");
        goalGo.transform.position = new Vector3(0f, 1.5f, GoalZ);
        var goalCol = goalGo.AddComponent<BoxCollider>();
        goalCol.size = new Vector3(CorridorWidth, 3f, 2.5f);
        goalCol.isTrigger = true;
        goalGo.AddComponent<LaserGoal>();

        EnsurePlayerAndCamera();
        var ui = EnsureUI();
        EnsureManager(spawner, ui);

        // Wire spawner origin explicitly.
        spawner.spawnOrigin = spawnGo.transform;
    }

    // ---- pieces ----

    static GameObject AddBox(Transform parent, string name, Vector3 pos, Vector3 size, Material mat, bool noCollideDelete)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = size;
        if (mat != null) go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    static void AddBeacon(Transform parent, string name, Vector3 pos, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(0.35f, 2f, 0.35f);
        var mat = MakeEmissive(color * 0.6f, color * 2f);
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        go.GetComponent<Collider>().enabled = false;
    }

    static void AddLights(Transform parent)
    {
        // Keep existing directional if present; otherwise add a dim one.
        if (Object.FindAnyObjectByType<Light>() == null)
        {
            var dir = new GameObject("Directional Light");
            dir.transform.SetParent(parent, false);
            dir.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var l = dir.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 0.6f;
            l.color = new Color(0.7f, 0.8f, 1f);
        }
        AddPoint(parent, "RedLight_1", new Vector3(0f, 3f, 12f), Color.red, 12f);
        AddPoint(parent, "RedLight_2", new Vector3(0f, 3f, 32f), Color.red, 12f);
        AddPoint(parent, "RedLight_3", new Vector3(0f, 3f, 52f), Color.red, 12f);
        AddPoint(parent, "RedLight_4", new Vector3(0f, 3f, 72f), Color.red, 12f);
        AddPoint(parent, "RedLight_5", new Vector3(0f, 3f, 92f), Color.red, 12f);
        AddPoint(parent, "RedLight_6", new Vector3(0f, 3f, 112f), Color.red, 12f);
        AddPoint(parent, "RedLight_7", new Vector3(0f, 3f, 132f), Color.red, 12f);
        AddPoint(parent, "GreenLight_Goal", new Vector3(0f, 3f, GoalZ), Color.green, 10f);
    }

    static void AddPoint(Transform parent, string name, Vector3 pos, Color c, float range)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = c;
        l.intensity = 8f;
        l.range = range;
    }

    static void CreateBonusPanel(Transform parent, string name, Vector3 pos,
        BonusPanel.BonusType type, float duration, float magnitude, int heal)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(2f, 0.2f, 2f);
        var col = go.GetComponent<BoxCollider>();
        col.isTrigger = true;
        // Tall trigger so jumping over the plate still collects it (see BonusPanel.Awake).
        col.size = new Vector3(1f, 8f, 1f);
        col.center = new Vector3(0f, 3.5f, 0f);
        var bp = go.AddComponent<BonusPanel>();
        bp.bonusType = type;
        bp.duration = duration <= 0f ? 6f : duration;
        bp.magnitude = magnitude;
        bp.healAmount = heal;
        bp.respawnDelay = 0f; // one-shot per run; manager resets
        bp.panelRenderer = go.GetComponent<Renderer>();
    }

    static GameObject LoadOrBuildLaserPrefab(Material redGlow)
    {
#if UNITY_EDITOR
        const string prefabPath = "Assets/Prefabs/LaserCorridor/Laser.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existing != null) return existing;
        // Build and save prefab so the project has a real asset.
        var go = new GameObject("Laser");
        go.transform.localScale = new Vector3(0.35f, 2.6f, 0.35f);
        var filter = go.AddComponent<MeshFilter>();
        var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        filter.sharedMesh = tmp.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(tmp);
        var rend = go.AddComponent<MeshRenderer>();
        rend.sharedMaterial = redGlow;
        var col = go.AddComponent<BoxCollider>();
        col.isTrigger = true;
        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        var laser = go.AddComponent<Laser>();
        laser.speed = 10f;
        laser.damage = 20;
        System.IO.Directory.CreateDirectory("Assets/Prefabs/LaserCorridor");
        var saved = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);
        return saved;
#else
        return null;
#endif
    }

    /// <summary>
    /// Reshapes an already-built (shorter/easier) corridor to the current spec
    /// at runtime, so Play picks it up without a manual rebuild. Idempotent,
    /// null-safe, and runtime-safe (no editor APIs). Called by the manager.
    /// </summary>
    public static void UpgradeLevelInPlace()
    {
        var rootGo = GameObject.Find(RootName);
        if (rootGo == null) return; // nothing built yet; Build() covers fresh scenes
        Transform root = rootGo.transform;

        float longZ = CorridorLength + 8f;
        SetBox("Floor", new Vector3(0f, -0.25f, CorridorMidZ), new Vector3(CorridorWidth + 1f, 0.5f, longZ));
        SetBox("Wall_Left", new Vector3(-(CorridorWidth / 2f + 0.25f), 2f, CorridorMidZ), new Vector3(0.5f, CorridorHeight + 0.5f, longZ));
        SetBox("Wall_Right", new Vector3(CorridorWidth / 2f + 0.25f, 2f, CorridorMidZ), new Vector3(0.5f, CorridorHeight + 0.5f, longZ));
        SetBox("Ceiling", new Vector3(0f, CorridorHeight + 0.25f, CorridorMidZ), new Vector3(CorridorWidth + 1f, 0.5f, longZ));
        SetBox("FarWall", new Vector3(0f, 2f, FarZ), new Vector3(CorridorWidth + 1f, CorridorHeight + 1f, 0.5f));
        SetBox("GlowStrip_L", new Vector3(-2.9f, 2.6f, CorridorMidZ), new Vector3(0.1f, 0.15f, CorridorLength));
        SetBox("GlowStrip_R", new Vector3(2.9f, 2.6f, CorridorMidZ), new Vector3(0.1f, 0.15f, CorridorLength));
        SetBox("GoalPad", new Vector3(0f, 0.02f, GoalZ), new Vector3(3.5f, 0.04f, 3f));

        // Floor stripes: keep existing, extend to the new far end.
        Material stripeMat = MakeEmissive(new Color(0.3f, 0.3f, 0.35f), Color.white * 0.4f);
        for (int i = 0; i < StripeCount; i++)
        {
            var s = GameObject.Find($"Stripe_{i:00}");
            if (s == null)
            {
                s = AddBox(root, $"Stripe_{i:00}", new Vector3(0f, 0.01f, 2f + i * 4f),
                    new Vector3(CorridorWidth, 0.02f, 0.3f), stripeMat, true);
                s.GetComponent<BoxCollider>().enabled = false;
            }
            else
            {
                s.transform.position = new Vector3(0f, 0.01f, 2f + i * 4f);
            }
        }

        Move("EndBeacon", new Vector3(-2.4f, 1.5f, GoalZ));
        Move("SpawnGate_L", new Vector3(-2.9f, 1.5f, SpawnPos.z));
        Move("SpawnGate_R", new Vector3(2.9f, 1.5f, SpawnPos.z));
        Move("LaserSpawn", SpawnPos);
        Move("GoalTrigger", new Vector3(0f, 1.5f, GoalZ));
        Move("GreenLight_Goal", new Vector3(0f, 3f, GoalZ));
        // Red lights re-spaced along the long corridor (move existing, add missing).
        float[] redZ = new float[] { 12f, 32f, 52f, 72f, 92f, 112f, 132f };
        for (int li = 0; li < redZ.Length; li++)
        {
            string lightName = $"RedLight_{li + 1}";
            var existing = GameObject.Find(lightName);
            if (existing != null)
                existing.transform.position = new Vector3(0f, 3f, redZ[li]);
            else
                EnsureLight(root, lightName, new Vector3(0f, 3f, redZ[li]), Color.red);
        }

        // Bonus panels: new spacing plus the late invulnerability save and a second wind sprint.
        MoveBonus("Bonus_Heal", root, new Vector3(0f, 0.12f, 15f),
            BonusPanel.BonusType.Heal, 6f, 0f, 25);
        MoveBonus("Bonus_Speed", root, new Vector3(-1.5f, 0.12f, 40f),
            BonusPanel.BonusType.SpeedBoost, 6f, 1.5f, 0);
        MoveBonus("Bonus_Shield", root, new Vector3(1.5f, 0.12f, 75f),
            BonusPanel.BonusType.DamageReduction, 8f, 0.5f, 0);
        MoveBonus("Bonus_Invuln", root, new Vector3(0f, 0.12f, 105f),
            BonusPanel.BonusType.Invulnerability, 3f, 0f, 0);
        MoveBonus("Bonus_Speed2", root, new Vector3(1.5f, 0.12f, 125f),
            BonusPanel.BonusType.SpeedBoost, 6f, 1.5f, 0);

        // Harder spawner tuning.
        var spawner = Object.FindAnyObjectByType<LaserSpawner>();
        if (spawner != null)
        {
            spawner.spawnInterval = 0.6f;
            spawner.laserSpeed = 20f;
            spawner.laserDamage = 34;
            spawner.cleanupZ = CleanupZ;
            spawner.corridorHalfWidth = 2.2f;
            spawner.maxActive = 14;
            spawner.chaserWeight = 45;
            spawner.sweeperWeight = 25;
            spawner.lowBarWeight = 30;
            spawner.rampOverTime = true;
            spawner.minInterval = 0.4f;
            spawner.rampEverySeconds = 10f;
            spawner.doubleChance = 0.6f;
            spawner.doubleAfterSeconds = 6f;
            spawner.tripleChance = 0.2f;
            spawner.tripleAfterSeconds = 20f;
            spawner.sweeperMinZ = 15f;
            spawner.sweeperMaxZ = 145f;
            spawner.homingStrength = 2.2f;
            spawner.homingAfterSeconds = 12f;
            spawner.speedJitter = 0.3f;
            if (spawner.spawnOrigin != null) spawner.spawnOrigin.position = SpawnPos;
        }
        Debug.Log("Laser corridor upgraded: longer run, faster lasers, double beams late.");
    }

    static void SetBox(string name, Vector3 pos, Vector3 size)
    {
        var go = GameObject.Find(name);
        if (go == null) return;
        go.transform.position = pos;
        go.transform.localScale = size;
    }

    static void Move(string name, Vector3 pos)
    {
        var go = GameObject.Find(name);
        if (go != null) go.transform.position = pos;
    }

    static void EnsureLight(Transform root, string name, Vector3 pos, Color c)
    {
        if (GameObject.Find(name) != null) return;
        AddPoint(root, name, pos, c, 12f);
    }

    static void MoveBonus(string name, Transform root, Vector3 pos, BonusPanel.BonusType type, float duration, float magnitude, int heal)
    {
        var go = GameObject.Find(name);
        if (go == null)
        {
            if (root != null) CreateBonusPanel(root, name, pos, type, duration, magnitude, heal);
            return;
        }
        go.transform.position = pos;
        var bp = go.GetComponent<BonusPanel>();
        if (bp != null)
        {
            bp.bonusType = type;
            bp.duration = duration;
            bp.magnitude = magnitude;
            bp.healAmount = heal;
            bp.RefreshAfterMove();
        }
    }

    static void EnsurePlayerAndCamera()
    {        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            player = new GameObject("Player");
            player.tag = "Player";
            player.transform.position = StartPos;
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.SetParent(player.transform, false);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);
            body.name = "Body";
            var cc = player.AddComponent<CharacterController>();
            cc.height = 2f; cc.radius = 0.4f; cc.center = new Vector3(0f, 1f, 0f);
            player.AddComponent<CustomPlayerController>();
            player.AddComponent<PlayerHealth>();
#if UNITY_EDITOR && ENABLE_INPUT_SYSTEM
            var input = player.AddComponent<PlayerInput>();
            input.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            input.defaultActionMap = "Player";
            input.notificationBehavior = PlayerNotifications.SendMessages;
#endif
        }
        else
        {
            player.transform.position = StartPos;
            if (player.GetComponent<CustomPlayerController>() == null)
                player.AddComponent<CustomPlayerController>();
            if (player.GetComponent<PlayerHealth>() == null)
                player.AddComponent<PlayerHealth>();
            if (player.GetComponent<CharacterController>() == null)
            {
                var cc = player.AddComponent<CharacterController>();
                cc.height = 2f; cc.radius = 0.4f; cc.center = new Vector3(0f, 1f, 0f);
            }
        }
        var ctrl = player.GetComponent<CustomPlayerController>();
        ctrl.SetSpawn(StartPos);
        ctrl.moveSpeed = 6f;

        var cam = Camera.main;
        if (cam == null)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = StartPos + new Vector3(0f, 5.2f, -8.2f);
        }
        var follow = cam.GetComponent<CameraFollow>();
        if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.target = player.transform;
        // First-person only: chase offset unused, head locked, no zoom-out.
        follow.firstPersonOnly = true;
        follow.freeLook = true;
        follow.lookSensitivity = 0.25f;
        follow.headHeight = 1.6f;
        follow.hidePlayerInFirstPerson = true;
        follow.lockCursorInFirstPerson = true;
        follow.minDistance = 0.5f;
        follow.maxDistance = 0.5f;
        follow.offset = new Vector3(0f, 5.2f, -8.2f);
        follow.focusOffset = new Vector3(0f, 1.1f, 3.5f);
        if (ctrl.cameraTransform == null) ctrl.cameraTransform = cam.transform;
    }

    static LaserCorridorUI EnsureUI()
    {
        EnsureEventSystem();
        var ui = Object.FindAnyObjectByType<LaserCorridorUI>();
        GameObject hud;
        if (ui != null && ui.gameObject.name == "GameHUD") hud = ui.gameObject;
        else
        {
            hud = GameObject.Find("GameHUD");
            if (hud == null) hud = CreateCanvas("GameHUD", 10);
            ui = hud.GetComponent<LaserCorridorUI>();
            if (ui == null) ui = hud.AddComponent<LaserCorridorUI>();
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        ui.healthText = EnsureText(hud.transform, "HealthText", font, 22, TextAnchor.UpperLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -12f), "HP: 100/100");
        ui.bonusText = EnsureText(hud.transform, "BonusText", font, 20, TextAnchor.UpperLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -44f), "");
        EnsureText(hud.transform, "Crosshair", font, 28, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, "+");
        // Hint removed: delete any stale HintText so it never reappears.
        var staleHint = hud.transform.Find("HintText");
        if (staleHint != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) Object.DestroyImmediate(staleHint.gameObject);
            else Object.Destroy(staleHint.gameObject);
#else
            Object.Destroy(staleHint.gameObject);
#endif
        }
        ui.hintText = null;
        ui.damageFlash = EnsureFlash(hud.transform);

        // End panels live under FinishHUD like the Traversal project.
        var finishHud = GameObject.Find("FinishHUD");
        if (finishHud == null) finishHud = CreateCanvas("FinishHUD", 20);
        ui.gameOverPanel = EnsurePanel(finishHud.transform, "GameOverPanel", new Color(0.5f, 0f, 0f, 0.55f));
        ui.gameOverText = EnsureText(ui.gameOverPanel.transform, "GameOverText", font, 48,
            TextAnchor.MiddleCenter, new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f), Vector2.zero,
            "GAME OVER");
        ui.retryButton = EnsureButton(ui.gameOverPanel.transform, "RetryButton", font, "RETRY (R)");
        ui.completePanel = EnsurePanel(finishHud.transform, "CompletePanel", new Color(0f, 0.35f, 0f, 0.55f));
        ui.completeText = EnsureText(ui.completePanel.transform, "CompleteText", font, 48,
            TextAnchor.MiddleCenter, new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f), Vector2.zero,
            "CORRIDOR CLEARED!");
        ui.playAgainButton = EnsureButton(ui.completePanel.transform, "PlayAgainButton", font, "PLAY AGAIN (R)");
        ui.gameOverPanel.SetActive(false);
        ui.completePanel.SetActive(false);
        ui.EnsureRuntimeHud();
        return ui;
    }

    static void EnsureManager(LaserSpawner spawner, LaserCorridorUI ui)
    {
        var gm = GameObject.Find("GameManager");
        if (gm == null) gm = new GameObject("GameManager");
        var mgr = gm.GetComponent<LaserCorridorManager>();
        if (mgr == null) mgr = gm.AddComponent<LaserCorridorManager>();
        var player = GameObject.FindGameObjectWithTag("Player");
        mgr.player = player != null ? player.GetComponent<CustomPlayerController>() : null;
        mgr.playerHealth = player != null ? player.GetComponent<PlayerHealth>() : null;
        mgr.spawner = spawner;
        mgr.ui = ui;
        mgr.startPosition = StartPos;
        ui.manager = mgr;
        ui.health = mgr.playerHealth;
        var mainCam = Camera.main;
        if (mainCam != null) ui.follow = mainCam.GetComponent<CameraFollow>();
        foreach (var panel in Object.FindObjectsByType<BonusPanel>())
            panel.ui = ui;
        var goal = Object.FindAnyObjectByType<LaserGoal>();
        if (goal != null) goal.manager = mgr;
    }

    // ---- UI helpers ----

    static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }

    static GameObject CreateCanvas(string name, int sorting)
    {
        var go = new GameObject(name);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sorting;
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    static Text EnsureText(Transform parent, string name, Font font, int size,
        TextAnchor align, Vector2 anchorMin, Vector2 anchorMax, Vector2 offset, string content)
    {
        var t = parent.Find(name);
        Text text;
        if (t != null) text = t.GetComponent<Text>();
        else
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            text = go.AddComponent<Text>();
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.anchoredPosition = offset;
            rt.sizeDelta = new Vector2(600f, 60f);
        }
        text.font = font;
        text.fontSize = size;
        text.alignment = align;
        text.color = Color.white;
        text.text = content;
        var rect = text.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin; rect.anchorMax = anchorMax;
        return text;
    }

    static Image EnsureFlash(Transform parent)
    {
        var t = parent.Find("DamageFlash");
        Image img;
        if (t != null) img = t.GetComponent<Image>();
        else
        {
            var go = new GameObject("DamageFlash");
            go.transform.SetParent(parent, false);
            img = go.AddComponent<Image>();
            img.color = new Color(1f, 0f, 0f, 0f);
            img.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
        return img;
    }

    static GameObject EnsurePanel(Transform parent, string name, Color color)
    {
        var t = parent.Find(name);
        GameObject go = t != null ? t.gameObject : new GameObject(name);
        if (t == null) go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        if (img == null) img = go.AddComponent<Image>();
        img.color = color;
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return go;
    }

    static Button EnsureButton(Transform parent, string name, Font font, string label)
    {
        var t = parent.Find(name);
        Button btn;
        if (t != null) btn = t.GetComponent<Button>();
        else
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.75f, 0.35f);
            btn = go.AddComponent<Button>();
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.3f); rt.anchorMax = new Vector2(0.5f, 0.3f);
            rt.sizeDelta = new Vector2(300f, 70f);
            rt.anchoredPosition = Vector2.zero;
            var lt = new GameObject("Label");
            lt.transform.SetParent(go.transform, false);
            var txt = lt.AddComponent<Text>();
            txt.font = font; txt.fontSize = 24; txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white; txt.text = label;
            var lrt = lt.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
        }
        var lbl = btn.transform.Find("Label")?.GetComponent<Text>();
        if (lbl != null) lbl.text = label;
        return btn;
    }

    static Material MakeLit(Color color, float metallic, float smoothness)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var mat = new Material(shader != null ? shader : Shader.Find("Standard"));
        mat.color = color;
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        return mat;
    }

    static Material MakeEmissive(Color albedo, Color emission)
    {
        var mat = MakeLit(albedo, 0f, 0.5f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", emission);
        return mat;
    }
}
