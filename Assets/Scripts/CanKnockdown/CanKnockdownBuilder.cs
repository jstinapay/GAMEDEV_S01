using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// One-click builder for the carnival booth.
/// Menu: CanKnockdown / Build Carnival Booth (3 balls, 6 cans).
/// Idempotent: deletes the old CanKnockdownLevel root and rebuilds.
/// Manager.Awake calls EnsureLevelInPlace() so a blank scene also self-heals at runtime.
/// Uses only built-in primitives + URP/Lit materials. No paid assets.
/// </summary>
public static class CanKnockdownBuilder
{
    const string RootName = "CanKnockdownLevel";
    static readonly Vector3 StackPlatformPos = new Vector3(0f, 0.9f, 4f);
    static readonly Vector3 StackPlatformSize = new Vector3(2.4f, 0.2f, 1.2f);
    static readonly Vector3 ThrowPointPos = new Vector3(0f, 1.45f, -2.5f);
    static readonly Vector3 CameraPos = new Vector3(0f, 2.0f, -4.2f);
    static readonly Vector3 CameraLookAt = new Vector3(0f, 1.2f, 4f);

#if UNITY_EDITOR
    [MenuItem("CanKnockdown/Build Carnival Booth (3 balls, 6 cans)")]
    public static void BuildFromMenu()
    {
        BuildLevel();
        SuppressConflictingLevels();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("CanKnockdown built: press Play. Tune via CanKnockdownManager / ThrowController / CanStackManager. TIP: use CanKnockdown / New CanKnockdown Scene for a clean scene without the Laser Corridor.");
    }

    [MenuItem("CanKnockdown/New CanKnockdown Scene (clean, no Laser Corridor)")]
    public static void NewSceneFromMenu()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuildLevel();
        SuppressConflictingLevels();
        const string path = "Assets/Scenes/CanKnockdown.unity";
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), path);
        Debug.Log($"Saved {path}. Press Play.");
    }
#endif

    /// <summary>Runtime-safe entry: builds only if the level root is missing.</summary>
    public static void EnsureLevelInPlace()
    {
        if (GameObject.Find(RootName) == null)
            BuildLevel();
        else
            EnsureManagersWired();
        EnsureRushObjects();
        SuppressConflictingLevels();
    }

    /// <summary>
    /// Creates the Balloon Rush trio (manager + spawner + UI host) when missing,
    /// so older built scenes pick up the mode at Play without a rebuild.
    /// The rush UI builds its own canvas in Awake.
    /// </summary>
    public static void EnsureRushObjects()
    {
        if (Object.FindAnyObjectByType<BalloonRushManager>() == null)
        {
            var go = new GameObject("BalloonRush");
            go.AddComponent<BalloonRushManager>();
        }
        if (Object.FindAnyObjectByType<BalloonSpawner>() == null)
        {
            var go = new GameObject("BalloonSpawner");
            go.AddComponent<BalloonSpawner>();
        }
        if (Object.FindAnyObjectByType<BalloonRushUI>() == null)
        {
            var go = new GameObject("BalloonRushUI");
            go.AddComponent<BalloonRushUI>();
        }
    }

    /// <summary>
    /// Deactivates Laser Corridor leftovers (level root, spawner manager, player,
    /// HUDs, FPS camera script) so they can't fight the knockdown game for the
    /// camera, cursor, or UI. Name-based, so no compile dependency on those scripts.
    /// Runs at Play (via EnsureLevelInPlace) and in the Build menu.
    /// </summary>
    public static void SuppressConflictingLevels()
    {
        foreach (string name in new[]
        {
            "LaserCorridorLevel", // corridor geometry + stripes + glow
            "GameManager",        // LaserCorridorManager lives here (ours is CanKnockdownManager)
            "Player",             // corridor FPS capsule sitting in the throw lane
            "GameHUD", "FinishHUD",
            "LaserSpawn", "Lasers", "GoalTrigger",
        })
        {
            var go = GameObject.Find(name);
            if (go != null && go.activeSelf)
                go.SetActive(false);
        }
        // Corridor forces first-person cursor lock via CameraFollow: switch it off.
        var follow = Object.FindAnyObjectByType<CameraFollow>();
        if (follow != null) follow.enabled = false;
    }

    public static void BuildLevel()
    {
        var old = GameObject.Find(RootName);
        if (old != null) DestroyRoot(old);

        var root = new GameObject(RootName);

        Material woodMat = MakeLit(new Color(0.55f, 0.35f, 0.18f), 0f, 0.4f);
        Material floorMat = MakeLit(new Color(0.16f, 0.12f, 0.14f), 0f, 0.5f);
        Material backMat = MakeLit(new Color(0.45f, 0.08f, 0.15f), 0f, 0.5f);
        Material redMat = MakeLit(new Color(0.85f, 0.1f, 0.15f), 0f, 0.5f);
        Material whiteMat = MakeLit(Color.white, 0f, 0.5f);
        Material goldMat = MakeEmissive(new Color(0.9f, 0.7f, 0.2f), new Color(0.9f, 0.7f, 0.2f) * 1.2f);

        // Floor + back wall (booth backdrop).
        AddBox(root.transform, "Floor", new Vector3(0f, -0.25f, 2f),
            new Vector3(24f, 0.5f, 24f), floorMat);
        AddBox(root.transform, "BackWall", new Vector3(0f, 3f, 8.5f),
            new Vector3(24f, 7f, 0.5f), backMat);
        AddBox(root.transform, "SideCurtain_L", new Vector3(-5f, 2.5f, 4f),
            new Vector3(0.4f, 5f, 8f), redMat);
        AddBox(root.transform, "SideCurtain_R", new Vector3(5f, 2.5f, 4f),
            new Vector3(0.4f, 5f, 8f), redMat);

        // Striped awning over the booth (alternating red/white).
        for (int i = 0; i < 8; i++)
        {
            AddBox(root.transform, $"Awning_{i}",
                new Vector3(-3.5f + i * 1f, 4.6f, 4f),
                new Vector3(1f, 0.15f, 3f), i % 2 == 0 ? redMat : whiteMat);
        }
        AddBox(root.transform, "AwningSign", new Vector3(0f, 5.2f, 4f),
            new Vector3(6f, 0.9f, 0.3f), goldMat);

        // Booth counter = can platform. BoxCollider kept (cans + balls collide).
        var platform = AddBox(root.transform, "BoothCounter", StackPlatformPos, StackPlatformSize, woodMat);
        var platCol = platform.GetComponent<BoxCollider>();
        var platPhys = new PhysicsMaterial("PlatformPhys")
        {
            dynamicFriction = 0.6f,
            staticFriction = 0.6f,
            bounciness = 0.05f
        };
        platCol.sharedMaterial = platPhys;

        // String lights: emissive spheres, no colliders. Only 2 real point lights for perf.
        for (int i = 0; i < 7; i++)
        {
            var bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulb.name = $"Bulb_{i}";
            bulb.transform.SetParent(root.transform, false);
            bulb.transform.position = new Vector3(-3f + i * 1f, 4.2f, 3f);
            bulb.transform.localScale = Vector3.one * 0.15f;
            bulb.GetComponent<MeshRenderer>().sharedMaterial = goldMat;
            DestroyColliderIfEditMode(bulb.GetComponent<SphereCollider>());
        }

        AddLights(root.transform);
        EnsureCamera();
        EnsureThrowPoint(root.transform);
        BuildSlingshot(root.transform, woodMat);
        DressCarnival(root.transform);
        ApplyAtmosphere();

        // Managers.
        var ambienceGo = new GameObject("Ambience");
        ambienceGo.AddComponent<CanKnockdownAmbience>();
        var stackGo = new GameObject("CanStack");
        var stack = stackGo.AddComponent<CanStackManager>();
        stackGo.transform.position = Vector3.zero;

        var throwGo = new GameObject("Thrower");
        var thrower = throwGo.AddComponent<ThrowController>();
        throwGo.AddComponent<LineRenderer>();

        var gmGo = GameObject.Find("CanKnockdownManager");
        if (gmGo == null) gmGo = new GameObject("CanKnockdownManager");
        var gm = gmGo.GetComponent<CanKnockdownManager>();
        if (gm == null) gm = gmGo.AddComponent<CanKnockdownManager>();

        var audioGo = GameObject.Find("KnockdownAudio");
        if (audioGo == null) audioGo = new GameObject("KnockdownAudio");
        if (audioGo.GetComponent<CanKnockdownAudio>() == null)
            audioGo.AddComponent<CanKnockdownAudio>();

        EnsureRushObjects();

        // Build the 6-can pyramid now (StackManager.Start would also do it).
        stack.BuildStack();

        var ui = BuildUI();
        WireManager(gm, stack, thrower, ui);
    }

    static void EnsureManagersWired()
    {
        var gm = Object.FindAnyObjectByType<CanKnockdownManager>();
        if (gm == null) return;
        // Serialized fields are private; wiring happens via Awake auto-find.
        // This just guarantees the UI canvas exists in older builds.
        if (Object.FindAnyObjectByType<CanKnockdownUI>() == null)
        {
            var ui = BuildUI();
            var stack = Object.FindAnyObjectByType<CanStackManager>();
            var thrower = Object.FindAnyObjectByType<ThrowController>();
            WireManager(gm, stack, thrower, ui);
        }
    }

    static void DestroyRoot(GameObject go)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) Object.DestroyImmediate(go);
        else Object.Destroy(go);
#else
        Object.Destroy(go);
#endif
    }

    static void DestroyColliderIfEditMode(Component col)
    {
        if (col == null) return;
#if UNITY_EDITOR
        if (!Application.isPlaying) Object.DestroyImmediate(col);
        else Object.Destroy(col);
#else
        Object.Destroy(col);
#endif
    }

    static void WireManager(CanKnockdownManager gm, CanStackManager stack, ThrowController thrower, CanKnockdownUI ui)
    {
        // Private [SerializeField] refs can't be set from outside without reflection;
        // Awake() auto-finds them, so just make sure all pieces exist.
        if (gm == null) return;
        var so = new { gm, stack, thrower, ui };
        _ = so; // documented no-op: Awake resolves these via FindAnyObjectByType
    }

    // ---- pieces ----

    /// <summary>Wooden Y-fork slingshot at the throw point. Tips are band anchors.</summary>
    static void BuildSlingshot(Transform parent, Material woodMat)
    {
        var fork = new GameObject("Slingshot");
        fork.transform.SetParent(parent, false);

        // Trunk: radius 0.09, height 1.1 (Unity cylinder is 2m tall, 1m diameter).
        // Decorative only: no collider, so fired balls can never clip the fork.
        var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        trunk.transform.SetParent(fork.transform, false);
        trunk.transform.position = new Vector3(ThrowPointPos.x, 0.55f, ThrowPointPos.z);
        trunk.transform.localScale = new Vector3(0.18f, 0.55f, 0.18f);
        trunk.GetComponent<MeshRenderer>().sharedMaterial = woodMat;
        DestroyColliderIfEditMode(trunk.GetComponent<CapsuleCollider>());

        // Prongs lean out to tips at pouch height.
        Vector3 tipL = new Vector3(ThrowPointPos.x - 0.26f, ThrowPointPos.y + 0.17f, ThrowPointPos.z);
        Vector3 tipR = new Vector3(ThrowPointPos.x + 0.26f, ThrowPointPos.y + 0.17f, ThrowPointPos.z);
        Vector3 baseTop = new Vector3(ThrowPointPos.x, 1.05f, ThrowPointPos.z);
        AddProng(fork.transform, "ProngL", baseTop, tipL, woodMat);
        AddProng(fork.transform, "ProngR", baseTop, tipR, woodMat);

        AddTip(fork.transform, "ForkTipL", tipL);
        AddTip(fork.transform, "ForkTipR", tipR);
    }

    static void AddProng(Transform parent, string name, Vector3 from, Vector3 to, Material mat)
    {
        var prong = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        prong.name = name;
        prong.transform.SetParent(parent, false);
        Vector3 mid = (from + to) * 0.5f;
        Vector3 axis = to - from;
        prong.transform.position = mid;
        prong.transform.localScale = new Vector3(0.14f, axis.magnitude * 0.5f, 0.14f);
        prong.transform.rotation = Quaternion.FromToRotation(Vector3.up, axis.normalized);
        prong.GetComponent<MeshRenderer>().sharedMaterial = mat;
        DestroyColliderIfEditMode(prong.GetComponent<CapsuleCollider>());
    }

    static void AddTip(Transform parent, string name, Vector3 pos)
    {
        var existing = GameObject.Find(name);
        if (existing != null)
        {
            existing.transform.position = pos;
            return;
        }
        var tip = new GameObject(name);
        tip.transform.SetParent(parent, false);
        tip.transform.position = pos;
    }

    /// <summary>
    /// Carnival dressing: stage discs, counter stripes + trim, carpet runner,
    /// bunting rows, balloon clusters, gold star studs, second bulb row, and a
    /// stone base for the slingshot. All decorative (colliders stripped) so the
    /// physics lane only ever touches floor, platform, cans, and balls.
    /// </summary>
    static void DressCarnival(Transform root)
    {
        Material creamMat = MakeLit(new Color(1f, 0.93f, 0.80f), 0f, 0.5f);
        Material redMat = MakeLit(new Color(0.85f, 0.12f, 0.18f), 0f, 0.45f);
        Material tealMat = MakeLit(new Color(0.10f, 0.62f, 0.68f), 0f, 0.5f);
        Material woodDark = MakeLit(new Color(0.36f, 0.22f, 0.11f), 0f, 0.4f);
        Material stoneMat = MakeLit(new Color(0.22f, 0.22f, 0.27f), 0f, 0.6f);
        Material goldGlow = MakeEmissive(new Color(0.95f, 0.72f, 0.2f), new Color(0.95f, 0.72f, 0.2f) * 1.4f);
        Material redGlow = MakeEmissive(new Color(0.9f, 0.12f, 0.16f), new Color(0.9f, 0.12f, 0.16f) * 1.6f);

        // Stage discs ground the booth (visual only; the floor keeps colliding).
        AddDecor(root, PrimitiveType.Cylinder, "StageRed", new Vector3(0f, 0.02f, 4f),
            new Vector3(6.8f, 0.02f, 6.8f), redMat);
        AddDecor(root, PrimitiveType.Cylinder, "StageCream", new Vector3(0f, 0.045f, 4f),
            new Vector3(5.8f, 0.02f, 5.8f), creamMat);

        // Counter front stripes + gold trim draw the eye to the can stack.
        for (int i = 0; i < 7; i++)
        {
            AddBox(root, $"CounterStripe_{i}", new Vector3(-0.9f + i * 0.3f, 0.9f, 3.37f),
                new Vector3(0.3f, 0.5f, 0.06f), i % 2 == 0 ? creamMat : redMat);
        }
        AddBox(root, "CounterTrim", new Vector3(0f, 0.99f, 3.37f),
            new Vector3(2.5f, 0.07f, 0.07f), goldGlow);

        // Carpet runner leads from the slingshot to the booth (colliders stripped:
        // the floor handles rolling; the carpet is a 2cm visual skin).
        for (int i = 0; i < 8; i++)
        {
            var tile = AddBox(root, $"Carpet_{i}", new Vector3(0f, 0.012f, -2.1f + i * 0.73f),
                new Vector3(1.6f, 0.02f, 0.75f), i % 2 == 0 ? redMat : creamMat);
            DestroyColliderIfEditMode(tile.GetComponent<BoxCollider>());
        }

        // Bunting rows sagging overhead (rope + hanging diamond flags).
        BuntingRow(root, 2.0f, redMat, creamMat, tealMat, woodDark);
        BuntingRow(root, 6.0f, tealMat, goldGlow, redMat, woodDark);

        // Balloon clusters flanking the booth.
        BalloonCluster(root, new Vector3(-4.2f, 0f, 5.5f), redMat, tealMat, creamMat, woodDark);
        BalloonCluster(root, new Vector3(4.2f, 0f, 5.5f), tealMat, creamMat, redMat, woodDark);

        // Gold star studs on the back wall frame the stack (z in front of the wall face).
        for (int i = 0; i < 3; i++)
        {
            AddDiamond(root, $"StarL_{i}", new Vector3(-2.2f, 2.2f + i * 0.8f, 8.2f), 0.2f, goldGlow);
            AddDiamond(root, $"StarR_{i}", new Vector3(2.2f, 2.2f + i * 0.8f, 8.2f), 0.2f, goldGlow);
        }
        AddDiamond(root, "StarTop_0", new Vector3(-1f, 6f, 8.2f), 0.24f, goldGlow);
        AddDiamond(root, "StarTop_1", new Vector3(0f, 6.2f, 8.2f), 0.3f, goldGlow);
        AddDiamond(root, "StarTop_2", new Vector3(1f, 6f, 8.2f), 0.24f, goldGlow);

        // Second bulb row along the awning front, alternating gold/red.
        for (int i = 0; i < 9; i++)
        {
            var bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulb.name = $"BulbFront_{i}";
            bulb.transform.SetParent(root, false);
            bulb.transform.position = new Vector3(-4f + i * 1f, 4.5f, 2.55f);
            bulb.transform.localScale = Vector3.one * 0.13f;
            bulb.GetComponent<MeshRenderer>().sharedMaterial = i % 2 == 0 ? goldGlow : redGlow;
            DestroyColliderIfEditMode(bulb.GetComponent<SphereCollider>());
        }

        // Stone base grounds the slingshot prop.
        AddDecor(root, PrimitiveType.Cylinder, "SlingBase", new Vector3(ThrowPointPos.x, 0.06f, ThrowPointPos.z),
            new Vector3(1f, 0.06f, 1f), stoneMat);
    }

    static void BuntingRow(Transform root, float z, Material a, Material b, Material c, Material ropeMat)
    {
        var rope = AddBox(root, $"BuntingRope_{z}", new Vector3(0f, 4.32f, z),
            new Vector3(9f, 0.03f, 0.03f), ropeMat);
        DestroyColliderIfEditMode(rope.GetComponent<BoxCollider>());
        Material[] mats = { a, b, c };
        for (int i = 0; i < 13; i++)
        {
            float x = -4.2f + i * 0.7f;
            float y = 4.32f - 0.45f * (1f - (x / 4.5f) * (x / 4.5f)) - 0.16f;
            var flag = AddBox(root, $"Bunting_{z}_{i}", new Vector3(x, y, z),
                new Vector3(0.16f, 0.24f, 0.02f), mats[i % 3]);
            flag.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
            DestroyColliderIfEditMode(flag.GetComponent<BoxCollider>());
        }
    }

    static void BalloonCluster(Transform root, Vector3 basePos, Material a, Material b, Material c, Material stickMat)
    {
        var stick = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stick.name = "BalloonStick";
        stick.transform.SetParent(root, false);
        stick.transform.position = basePos + new Vector3(0f, 1.1f, 0f);
        stick.transform.localScale = new Vector3(0.08f, 1.1f, 0.08f);
        stick.GetComponent<MeshRenderer>().sharedMaterial = stickMat;
        DestroyColliderIfEditMode(stick.GetComponent<CapsuleCollider>());
        Material[] mats = { a, b, c };
        Vector3[] offs = { new Vector3(-0.28f, 2.45f, 0f), new Vector3(0.28f, 2.45f, 0f), new Vector3(0f, 2.75f, 0f) };
        for (int i = 0; i < 3; i++)
        {
            var bal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bal.name = $"Balloon_{i}";
            bal.transform.SetParent(root, false);
            bal.transform.position = basePos + offs[i];
            bal.transform.localScale = new Vector3(0.56f, 0.66f, 0.56f);
            bal.GetComponent<MeshRenderer>().sharedMaterial = mats[i];
            DestroyColliderIfEditMode(bal.GetComponent<SphereCollider>());
        }
    }

    static void AddDiamond(Transform root, string name, Vector3 pos, float size, Material mat)
    {
        var d = AddBox(root, name, pos, new Vector3(size, size, 0.06f), mat);
        d.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
        DestroyColliderIfEditMode(d.GetComponent<BoxCollider>());
    }

    /// <summary>Decorative primitive with its collider stripped (never touches physics).</summary>
    static GameObject AddDecor(Transform parent, PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        DestroyColliderIfEditMode(go.GetComponent<Collider>());
        return go;
    }

    /// <summary>Warm night-fair atmosphere: subtle depth fog + warm ambient lift.</summary>
    static void ApplyAtmosphere()
    {
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.10f, 0.05f, 0.14f);
        RenderSettings.fogStartDistance = 14f;
        RenderSettings.fogEndDistance = 34f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.38f, 0.30f, 0.40f);
    }

    static GameObject AddBox(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = size;
        if (mat != null) go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    static void AddLights(Transform parent)
    {
        var dir = new GameObject("Sun");
        dir.transform.SetParent(parent, false);
        dir.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        var sun = dir.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.1f;
        sun.color = new Color(1f, 0.95f, 0.85f);
        sun.shadows = LightShadows.Soft;

        AddPoint(parent, "BoothLight", new Vector3(0f, 3.5f, 3f), new Color(1f, 0.8f, 0.5f), 12f, 30f);
        AddPoint(parent, "PlayerLight", new Vector3(0f, 3f, -2f), new Color(0.72f, 0.84f, 1f), 10f, 15f);
        // Hero spot: a warm pool of light on the can stack pulls the eye to the target.
        var spotGo = new GameObject("StackSpot");
        spotGo.transform.SetParent(parent, false);
        spotGo.transform.position = new Vector3(0f, 5.4f, 1.2f);
        spotGo.transform.LookAt(new Vector3(0f, 1.2f, 4f));
        var spot = spotGo.AddComponent<Light>();
        spot.type = LightType.Spot;
        spot.color = new Color(1f, 0.86f, 0.66f);
        spot.intensity = 60f;
        spot.range = 14f;
        spot.spotAngle = 30f;
    }

    static void AddPoint(Transform parent, string name, Vector3 pos, Color c, float range, float intensity)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = c;
        l.range = range;
        l.intensity = intensity;
    }

    static void EnsureCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
        }
        cam.transform.position = CameraPos;
        cam.transform.LookAt(CameraLookAt);
        cam.fieldOfView = 60f;
        cam.backgroundColor = new Color(0.08f, 0.05f, 0.12f);
    }

    static void EnsureThrowPoint(Transform parent)
    {
        var tp = GameObject.Find("ThrowPoint");
        if (tp == null)
        {
            tp = new GameObject("ThrowPoint");
            tp.transform.position = ThrowPointPos;
        }
        else
        {
            tp.transform.position = ThrowPointPos;
        }
    }

    // ---- UI (TextMeshPro) ----

    static CanKnockdownUI BuildUI()
    {
        EnsureEventSystem();
        var existing = Object.FindAnyObjectByType<CanKnockdownUI>();
        if (existing != null) return existing;

        var hud = new GameObject("CanKnockdownHUD");
        var canvas = hud.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = hud.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        hud.AddComponent<GraphicRaycaster>();
        var ui = hud.AddComponent<CanKnockdownUI>();

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        Font legacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // HUD root (score/throws/hint/notification/popup).
        var hudRoot = new GameObject("HudRoot");
        hudRoot.transform.SetParent(hud.transform, false);
        var hudRt = hudRoot.AddComponent<RectTransform>();
        hudRt.anchorMin = Vector2.zero; hudRt.anchorMax = Vector2.one;
        hudRt.offsetMin = Vector2.zero; hudRt.offsetMax = Vector2.zero;

        TMP_Text score = MakeTMP(hudRoot.transform, "ScoreText", font, legacyFont, 42,
            TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(220f, -50f), "SCORE: 0");
        TMP_Text throws = MakeTMP(hudRoot.transform, "ThrowsText", font, legacyFont, 42,
            TextAlignmentOptions.TopRight, new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-220f, -50f), "BALLS: 3/3");
        TMP_Text notif = MakeTMP(hudRoot.transform, "NotifyText", font, legacyFont, 36,
            TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -130f), "");
        notif.gameObject.SetActive(false);
        AddOutline(notif, 0.18f, new Color32(60, 5, 12, 255));
        AddChip(hudRoot.transform, notif, 560f, 86f, 0.62f).SetActive(false);
        TMP_Text hint = MakeTMP(hudRoot.transform, "HintText", font, legacyFont, 26,
            TextAlignmentOptions.Center, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 60f), "Grab the ball, pull back to aim  â€¢  Release to fire");
        TMP_Text popup = MakeTMP(hudRoot.transform, "ScorePopup", font, legacyFont, 72,
            TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 80f), "");
        popup.color = Color.yellow;
        popup.gameObject.SetActive(false);
        AddOutline(popup, 0.14f, new Color32(60, 30, 0, 255));
        AddChip(hudRoot.transform, popup, 440f, 130f, 0.55f).SetActive(false);
        TMP_Text cross = MakeTMP(hud.transform, "Crosshair", font, legacyFont, 48,
            TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, "+");
        cross.raycastTarget = false;
        cross.gameObject.SetActive(false); // slingshot aims via the pouch, not a cursor
        BuildPowerBar(hudRoot.transform);

        // Main menu panel.
        var menu = MakePanel(hud.transform, "MainMenuPanel", new Color(0.1f, 0.02f, 0.08f, 0.88f));
        MakeTMP(menu.transform, "TitleText", font, legacyFont, 110,
            TextAlignmentOptions.Center, new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.75f),
            Vector2.zero, "CAN KNOCKDOWN");
        MakeTMP(menu.transform, "SubtitleText", font, legacyFont, 30,
            TextAlignmentOptions.Center, new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.65f),
            Vector2.zero, "Knock down all 6 cans with 3 balls!");
        Button play = MakeButton(menu.transform, "PlayButton", font, legacyFont, "CAN KNOCKDOWN",
            new Vector2(0.5f, 0.52f), new Vector2(340f, 80f));
        Button rush = MakeButton(menu.transform, "RushButton", font, legacyFont, "BALLOON POP RUSH",
            new Vector2(0.5f, 0.42f), new Vector2(340f, 80f));
        MakeTMP(menu.transform, "RushDesc", font, legacyFont, 24,
            TextAlignmentOptions.Center, new Vector2(0.5f, 0.355f), new Vector2(0.5f, 0.355f),
            Vector2.zero, "60s - 12 balls - moving targets! Gold = 300, teal = 200 + ball.");
        Button quit = MakeButton(menu.transform, "QuitButton", font, legacyFont, "QUIT",
            new Vector2(0.5f, 0.22f), new Vector2(280f, 70f));

        // Round-over panel.
        var over = MakePanel(hud.transform, "RoundOverPanel", new Color(0.02f, 0.1f, 0.05f, 0.9f));
        MakeTMP(over.transform, "RoundOverTitle", font, legacyFont, 72,
            TextAlignmentOptions.Center, new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.7f),
            Vector2.zero, "ROUND COMPLETE!");
        TMP_Text finalScore = MakeTMP(over.transform, "FinalScoreText", font, legacyFont, 54,
            TextAlignmentOptions.Center, new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.58f),
            Vector2.zero, "FINAL SCORE: 0");
        TMP_Text summary = MakeTMP(over.transform, "SummaryText", font, legacyFont, 32,
            TextAlignmentOptions.Center, new Vector2(0.5f, 0.45f), new Vector2(0.5f, 0.45f),
            Vector2.zero, "");
        summary.rectTransform.sizeDelta = new Vector2(900f, 200f);
        Button again = MakeButton(over.transform, "PlayAgainButton", font, legacyFont, "PLAY AGAIN (R)",
            new Vector2(0.5f, 0.28f), new Vector2(340f, 80f));
        Button menuBtn = MakeButton(over.transform, "MenuButton", font, legacyFont, "MAIN MENU",
            new Vector2(0.5f, 0.18f), new Vector2(340f, 70f));
        over.SetActive(false);

        // Inject into private [SerializeField] fields via reflection (keeps inspector clean,
        // avoids a public-field API while letting the builder fully wire a fresh scene).
        Set(ui, "scoreText", score);
        Set(ui, "throwsText", throws);
        Set(ui, "notificationText", notif);
        Set(ui, "hintText", hint);
        Set(ui, "crosshairText", cross);
        Set(ui, "popupText", popup);
        Set(ui, "mainMenuPanel", menu);
        Set(ui, "roundOverPanel", over);
        Set(ui, "finalScoreText", finalScore);
        Set(ui, "summaryText", summary);
        Set(ui, "playButton", play);
        Set(ui, "rushButton", rush);
        Set(ui, "quitButton", quit);
        Set(ui, "playAgainButton", again);
        Set(ui, "menuButton", menuBtn);
        Set(ui, "hudRoot", hudRoot);
        var title = menu.transform.Find("TitleText")?.GetComponent<TMP_Text>();
        Set(ui, "titleText", title);

        PolishUI(hudRoot.transform, score, throws, notif, popup, menu, play, over);

        return ui;
    }

    /// <summary>
    /// Carnival UI styling: dark chips behind score/throws for readability, poster
    /// shadows on display text, gold hero PLAY button, gold round-over title.
    /// Pure presentation — no wiring changes.
    /// </summary>
    static void PolishUI(Transform hudRoot, TMP_Text score, TMP_Text throws,
        TMP_Text notif, TMP_Text popup, GameObject menu, Button play, GameObject over)
    {
        if (score != null) { AddChip(hudRoot, score); AddShadow(score.gameObject, new Vector2(2f, -2f)); }
        if (throws != null) { AddChip(hudRoot, throws); AddShadow(throws.gameObject, new Vector2(2f, -2f)); }
        if (notif != null) AddShadow(notif.gameObject, new Vector2(2f, -2f));
        if (popup != null) AddShadow(popup.gameObject, new Vector2(3f, -3f));

        var title = menu.transform.Find("TitleText")?.GetComponent<TMP_Text>();
        if (title != null)
        {
            title.color = new Color(1f, 0.94f, 0.80f);
            AddShadow(title.gameObject, new Vector2(4f, -4f));
        }
        var subtitle = menu.transform.Find("SubtitleText")?.GetComponent<TMP_Text>();
        if (subtitle != null) subtitle.color = new Color(1f, 0.80f, 0.30f);

        if (play != null)
        {
            var img = play.GetComponent<Image>();
            if (img != null) img.color = new Color(0.95f, 0.70f, 0.14f);
            var label = play.transform.Find("Label")?.GetComponent<TMP_Text>();
            if (label != null)
            {
                label.color = new Color(0.28f, 0.12f, 0.05f);
                label.fontStyle = FontStyles.Bold;
                label.fontSize = 34;
            }
        }

        var overTitle = over.transform.Find("RoundOverTitle")?.GetComponent<TMP_Text>();
        if (overTitle != null)
        {
            overTitle.color = new Color(1f, 0.82f, 0.25f);
            AddShadow(overTitle.gameObject, new Vector2(3f, -3f));
        }
    }

    static GameObject AddChip(Transform hudRoot, TMP_Text text, float w = 400f, float h = 78f, float alpha = 0.45f)
    {
        var chip = new GameObject(text.name + "_Chip");
        chip.transform.SetParent(hudRoot, false);
        var img = chip.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, alpha);
        img.raycastTarget = false;
        var rt = chip.GetComponent<RectTransform>();
        var src = text.rectTransform;
        rt.anchorMin = src.anchorMin; rt.anchorMax = src.anchorMax; rt.pivot = src.pivot;
        rt.anchoredPosition = src.anchoredPosition;
        rt.sizeDelta = new Vector2(w, h);
        chip.transform.SetAsFirstSibling();
        return chip;
    }

    /// <summary>Dark SDF outline so text reads over bright booth colors. Extra padding prevents edge clipping.</summary>
    static void AddOutline(TMP_Text tmp, float width, Color color)
    {
        tmp.outlineWidth = width;
        tmp.outlineColor = color;
        tmp.extraPadding = true;
    }

    static void AddShadow(GameObject go, Vector2 distance)
    {
        var sh = go.AddComponent<Shadow>();
        sh.effectColor = new Color(0f, 0f, 0f, 0.6f);
        sh.effectDistance = distance;
    }

    static void Set(Object target, string field, Object value)
    {
        var f = target.GetType().GetField(field,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (f != null) f.SetValue(target, value);
    }

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

    static TMP_Text MakeTMP(Transform parent, string name, TMP_FontAsset font, Font legacy,
        int size, TextAlignmentOptions align, Vector2 anchor, Vector2 pivot, Vector2 offset, string text)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.color = Color.white;
        tmp.text = text;
        tmp.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = pivot;
        rt.anchoredPosition = offset;
        rt.sizeDelta = new Vector2(700f, 80f);
        return tmp;
    }

    /// <summary>Bottom-center power meter, shown only while stretching the slingshot.</summary>
    static void BuildPowerBar(Transform hudRoot)
    {
        if (hudRoot.Find("PowerBarBg") != null) return;
        var bg = new GameObject("PowerBarBg");
        bg.transform.SetParent(hudRoot, false);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.55f);
        bgImg.raycastTarget = false;
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0.5f, 0f); bgRt.anchorMax = new Vector2(0.5f, 0f);
        bgRt.sizeDelta = new Vector2(340f, 30f);
        bgRt.anchoredPosition = new Vector2(0f, 110f);

        var fillGo = new GameObject("PowerBar");
        fillGo.transform.SetParent(bg.transform, false);
        var fill = fillGo.AddComponent<Image>();
        fill.color = new Color(1f, 0.8f, 0.15f);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.fillAmount = 0f;
        fill.raycastTarget = false;
        var fillRt = fillGo.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(4f, 4f); fillRt.offsetMax = new Vector2(-4f, -4f);
        bg.SetActive(false);
    }

    static GameObject MakePanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return go;
    }

    static Button MakeButton(Transform parent, string name, TMP_FontAsset font, Font legacy,
        string label, Vector2 anchor, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.85f, 0.25f, 0.15f);
        var btn = go.AddComponent<Button>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;

        var lgo = new GameObject("Label");
        lgo.transform.SetParent(go.transform, false);
        var tmp = lgo.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.fontSize = 30;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.text = label;
        tmp.raycastTarget = false;
        var lrt = lgo.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
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
