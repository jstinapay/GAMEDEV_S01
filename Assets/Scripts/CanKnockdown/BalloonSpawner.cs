using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns Balloon Pop Rush targets in the booth lane and ramps difficulty over
/// the round: faster cruise, shorter intervals, more active balloons, richer
/// golden odds late. All bounded by the Inspector min/max values.
/// </summary>
public class BalloonSpawner : MonoBehaviour
{
    [Header("Types (points, colors, speeds, weights)")]
    [SerializeField] BalloonTypeConfig[] typeConfigs = new BalloonTypeConfig[]
    {
        new BalloonTypeConfig { type = BalloonType.Standard, points = 100, bonusAmmo = 0, color = new Color(0.9f, 0.16f, 0.2f), speed = 1.2f, scale = 1f, spawnWeight = 70f },
        new BalloonTypeConfig { type = BalloonType.Golden, points = 300, bonusAmmo = 0, color = new Color(1f, 0.76f, 0.15f), speed = 2f, scale = 1.12f, spawnWeight = 12f },
        new BalloonTypeConfig { type = BalloonType.Bonus, points = 200, bonusAmmo = 1, color = new Color(0.1f, 0.75f, 0.7f), speed = 0.8f, scale = 1f, spawnWeight = 18f },
    };

    [Header("Spawn area (visible booth lane)")]
    [SerializeField] float minX = -5f;
    [SerializeField] float maxX = 5f;
    [SerializeField] float minY = 1.2f;
    [SerializeField] float maxY = 3.8f;
    [SerializeField] float laneZ = 3.6f;
    [SerializeField] float laneDepth = 0.8f;

    [Header("Pacing")]
    [SerializeField] float startInterval = 1.6f;
    [SerializeField] float minInterval = 0.7f;
    [SerializeField] int startMaxActive = 4;
    [SerializeField] int maxActive = 7;

    [Header("Difficulty ramp (multipliers at round end)")]
    [SerializeField] float endSpeedMult = 1.5f;
    [SerializeField] float endGoldenBonus = 10f;

    [Header("Safety")]
    [SerializeField] float minSeparation = 1.4f;
    [SerializeField] float balloonLife = 30f;
    [SerializeField] float despawnMargin = 1.2f;

    readonly List<BalloonController> active = new List<BalloonController>();
    BalloonRushManager rush;
    float spawnTimer;
    bool running;

    public int ActiveCount
    {
        get
        {
            active.RemoveAll(b => b == null);
            return active.Count;
        }
    }

    void Awake()
    {
        rush = Object.FindAnyObjectByType<BalloonRushManager>();
    }

    /// <summary>Begins spawning with an immediate opener balloon.</summary>
    public void StartSpawning()
    {
        running = true;
        spawnTimer = 0f;
        TrySpawn(); // no empty-lane wait at GO
    }

    public void StopSpawning() { running = false; }

    /// <summary>Removes every live balloon (round end / mode switch).</summary>
    public void ClearAll()
    {
        running = false;
        foreach (var b in active)
            if (b != null) Destroy(b.gameObject);
        active.Clear();
    }

    /// <summary>Called by a balloon leaving the lane or popping.</summary>
    public void Release(BalloonController balloon)
    {
        active.Remove(balloon);
        if (balloon != null) Destroy(balloon.gameObject);
    }

    void Update()
    {
        if (!running || rush == null) return;
        active.RemoveAll(b => b == null);
        float progress = rush.RoundProgress();
        int cap = Mathf.RoundToInt(Mathf.Lerp(startMaxActive, maxActive, progress));
        if (active.Count >= cap) return;
        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f) return;
        spawnTimer = Mathf.Lerp(startInterval, minInterval, progress);
        TrySpawn();
    }

    float SpeedMult() => rush != null ? Mathf.Lerp(1f, endSpeedMult, rush.RoundProgress()) : 1f;

    void TrySpawn()
    {
        BalloonMove pattern = (BalloonMove)Random.Range(0, 4);
        Vector3 pos = SpawnPos(pattern);
        // Don't stack spawns on top of live balloons.
        for (int attempt = 0; attempt < 4; attempt++)
        {
            bool clear = true;
            foreach (var b in active)
            {
                if (b == null) continue;
                if (Vector3.Distance(b.transform.position, pos) < minSeparation) { clear = false; break; }
            }
            if (clear) break;
            pos = SpawnPos(pattern);
            if (attempt == 3) return; // lane crowded: skip this tick
        }

        BalloonTypeConfig cfg = PickType();
        var go = BuildBalloon(cfg);
        go.transform.position = pos;
        var bc = go.GetComponent<BalloonController>();
        Vector3 dir = MoveDir(pattern);
        bc.Launch(this, cfg.type, cfg.points, cfg.bonusAmmo, dir,
            cfg.speed * SpeedMult(), balloonLife,
            new Vector3(minX - despawnMargin, 0.4f, laneZ - laneDepth - despawnMargin),
            new Vector3(maxX + despawnMargin, 5f, laneZ + laneDepth + despawnMargin));
        active.Add(bc);
    }

    Vector3 SpawnPos(BalloonMove pattern)
    {
        float z = laneZ + Random.Range(-laneDepth * 0.5f, laneDepth * 0.5f);
        switch (pattern)
        {
            case BalloonMove.RightLeft:
                return new Vector3(maxX + 0.5f, Random.Range(minY, maxY), z);
            case BalloonMove.FloatUp:
                return new Vector3(Random.Range(minX, maxX), 0.5f, z);
            case BalloonMove.Diagonal:
                return new Vector3(-maxX - 0.5f, 0.6f, z);
            default: // LeftRight
                return new Vector3(minX - 0.5f, Random.Range(minY, maxY), z);
        }
    }

    static Vector3 MoveDir(BalloonMove pattern)
    {
        switch (pattern)
        {
            case BalloonMove.RightLeft: return Vector3.left;
            case BalloonMove.FloatUp: return Vector3.up;
            case BalloonMove.Diagonal: return new Vector3(0.8f, 0.6f, 0f).normalized;
            default: return Vector3.right;
        }
    }

    BalloonTypeConfig PickType()
    {
        float goldBonus = rush != null ? endGoldenBonus * rush.RoundProgress() : 0f;
        float total = 0f;
        foreach (var c in typeConfigs)
            total += c.spawnWeight + (c.type == BalloonType.Golden ? goldBonus : 0f);
        float roll = Random.Range(0f, Mathf.Max(0.01f, total));
        foreach (var c in typeConfigs)
        {
            roll -= c.spawnWeight + (c.type == BalloonType.Golden ? goldBonus : 0f);
            if (roll <= 0f) return c;
        }
        return typeConfigs.Length > 0 ? typeConfigs[0]
            : new BalloonTypeConfig { type = BalloonType.Standard, points = 100, speed = 1.2f, scale = 1f, color = Color.red };
    }

    GameObject BuildBalloon(BalloonTypeConfig cfg)
    {
        float s = Mathf.Max(0.5f, cfg.scale);
        var go = new GameObject($"Balloon_{cfg.type}");
        go.transform.localScale = new Vector3(0.38f * s, 0.46f * s, 0.38f * s);

        var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.name = "Body";
        body.transform.SetParent(go.transform, false);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = Vector3.one;
        var rend = body.GetComponent<MeshRenderer>();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        mat.color = cfg.color;
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.7f);
        if (cfg.type == BalloonType.Golden)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", cfg.color * 0.55f);
        }
        rend.sharedMaterial = mat;
        // Generous trigger pop zone (slightly larger than the visual).
        var trigger = body.GetComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 0.9f;

        // Knot below the body.
        var knot = GameObject.CreatePrimitive(PrimitiveType.Cube);
        knot.name = "Knot";
        knot.transform.SetParent(go.transform, false);
        knot.transform.localPosition = new Vector3(0f, -0.55f, 0f);
        knot.transform.localScale = new Vector3(0.18f, 0.14f, 0.18f);
        knot.GetComponent<MeshRenderer>().sharedMaterial = mat;
        Destroy(knot.GetComponent<BoxCollider>());

        // String hanging down.
        var str = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        str.name = "String";
        str.transform.SetParent(go.transform, false);
        str.transform.localPosition = new Vector3(0f, -1.15f, 0f);
        str.transform.localScale = new Vector3(0.05f, 0.6f, 0.05f);
        str.GetComponent<MeshRenderer>().sharedMaterial = mat;
        Destroy(str.GetComponent<CapsuleCollider>());

        // Bonus marker: white band so the reward reads before the shot.
        if (cfg.type == BalloonType.Bonus)
        {
            var band = GameObject.CreatePrimitive(PrimitiveType.Cube);
            band.name = "Band";
            band.transform.SetParent(go.transform, false);
            band.transform.localPosition = Vector3.zero;
            band.transform.localScale = new Vector3(1.02f, 0.28f, 1.02f);
            var bandRend = band.GetComponent<MeshRenderer>();
            bandRend.sharedMaterial = new Material(mat) { color = Color.white };
            Destroy(band.GetComponent<BoxCollider>());
        }

        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        go.AddComponent<BalloonController>();
        return go;
    }
}
