using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Spawns lasers at the far end of the corridor at fixed intervals.
/// Guarantees fairness: one narrow vertical beam at a time at a random
/// lane, so strafing left/right always avoids it. Multiple lasers coexist.
/// </summary>
public class LaserSpawner : MonoBehaviour
{
    [Header("Spawn")]
    public GameObject laserPrefab;
    public Transform spawnOrigin; // far end marker; uses own transform if null
    public Transform laserContainer; // keeps hierarchy clean
    public float spawnInterval = 2f;
    public float laserSpeed = 10f;
    public int laserDamage = 20;
    public float cleanupZ = -6f;

    [Header("Fairness")]
    public float corridorHalfWidth = 2.2f;

    [Header("Multi-axis mix (spawn weights)")]
    [Tooltip("Vertical beams rushing down the corridor (strafe to dodge).")]
    public int chaserWeight = 50;
    [Tooltip("Vertical beams sweeping wall-to-wall ahead of you (time your advance).")]
    public int sweeperWeight = 25;
    [Tooltip("Low bars rushing down the corridor (JUMP over them).")]
    public int lowBarWeight = 25;
    public float sweeperMinZ = 15f;
    public float sweeperMaxZ = 145f;
    public float lowBarY = 0.45f;
    public int maxActive = 5;
    [Tooltip("Beam size: narrow (dodge sideways) and tall (jump won't trivialize).")]
    public Vector3 laserSize = new Vector3(0.35f, 2.6f, 0.35f);

    [Header("Double beams (late-run pressure)")]
    [Range(0f, 1f)]
    [Tooltip("Chance per spawn to fire TWO beams in different lanes (one gap always remains).")]
    public float doubleChance;
    public float doubleAfterSeconds = 15f;

    [Header("Surge waves + homing (all-out mode)")]
    [Range(0f, 1f)]
    [Tooltip("Chance per spawn for chaser + low bar (+ sometimes sweeper). Jump AND strafe.")]
    public float tripleChance;
    public float tripleAfterSeconds = 20f;
    [Tooltip("Chaser sideways tracking in m/s once the run heats up.")]
    public float homingStrength;
    public float homingAfterSeconds = 12f;
    [Tooltip("Random speed multiplier applied per laser, e.g. 0.3 = +/-30%.")]
    public float speedJitter = 0.3f;

    [Header("Telegraphs (fairness readability)")]
    [Tooltip("Flash a warning marker at the spawn spot shortly before each beam appears.")]
    public bool telegraph = true;
    public float telegraphTime = 0.45f;

    struct PendingSpawn { public int kind; public int lane; public float z; public float side; }
    bool telegraphing;

    [Header("Difficulty ramp (optional)")]
    public bool rampOverTime;
    public float minInterval = 1.2f;
    public float rampEverySeconds = 20f;

    bool spawning;
    float timer;
    float runTime;
    int spawnCount;
    readonly List<GameObject> active = new List<GameObject>();

    void Awake()
    {
        if (spawnOrigin == null) spawnOrigin = transform;
        if (laserContainer == null)
        {
            var holder = new GameObject("Lasers");
            laserContainer = holder.transform;
        }
    }

    void Update()
    {
        if (!spawning) return;
        runTime += Time.deltaTime;
        timer -= Time.deltaTime;
        Prune();
        if (timer <= 0f && !telegraphing && active.Count < maxActive)
        {
            StartCoroutine(TelegraphThenSpawn());
        }
    }

    IEnumerator TelegraphThenSpawn()
    {
        telegraphing = true;
        var pending = PickSpawns();
        var markers = new List<GameObject>();
        if (telegraph && telegraphTime > 0f)
        {
            foreach (var p in pending)
            {
                Vector3 mpos;
                Vector3 mscl;
                MarkerPose(p, out mpos, out mscl);
                markers.Add(MakeMarker(mpos, mscl));
            }
            yield return new WaitForSeconds(telegraphTime);
        }
        foreach (var m in markers) if (m != null) Destroy(m);
        foreach (var p in pending) ExecuteSpawn(p);
        float interval = spawnInterval;
        if (rampOverTime)
        {
            float steps = Mathf.Floor(runTime / Mathf.Max(1f, rampEverySeconds));
            interval = Mathf.Max(minInterval, spawnInterval - steps * 0.15f);
        }
        timer = interval;
        telegraphing = false;
    }

    List<PendingSpawn> PickSpawns()
    {
        var list = new List<PendingSpawn>();
        bool surge = runTime >= tripleAfterSeconds && tripleChance > 0f
            && active.Count + 2 < maxActive && Random.value < tripleChance;
        bool dbl = !surge && runTime >= doubleAfterSeconds && doubleChance > 0f
            && active.Count + 1 < maxActive && Random.value < doubleChance;
        if (surge)
        {
            list.Add(new PendingSpawn { kind = 0, lane = Random.Range(0, 3) });
            spawnCount++;
            list.Add(new PendingSpawn { kind = 2 });
            if (Random.value < 0.5f) list.Add(SweeperRequest());
        }
        else if (dbl)
        {
            int a = spawnCount % 3;
            list.Add(new PendingSpawn { kind = 0, lane = a });
            spawnCount++;
            int b = (a + 1 + Random.Range(0, 2)) % 3;
            list.Add(new PendingSpawn { kind = 0, lane = b });
            spawnCount++;
        }
        else
        {
            list.Add(PickSingle());
        }
        return list;
    }

    PendingSpawn PickSingle()
    {
        int total = Mathf.Max(1, chaserWeight + sweeperWeight + lowBarWeight);
        int roll = Random.Range(0, total);
        if (roll < chaserWeight)
        {
            int lane = spawnCount % 3;
            spawnCount++;
            return new PendingSpawn { kind = 0, lane = lane };
        }
        if (roll < chaserWeight + sweeperWeight) return SweeperRequest();
        return new PendingSpawn { kind = 2 };
    }

    PendingSpawn SweeperRequest()
    {
        return new PendingSpawn
        {
            kind = 1,
            z = Random.Range(Mathf.Min(sweeperMinZ, sweeperMaxZ), Mathf.Max(sweeperMinZ, sweeperMaxZ)),
            side = (Random.value < 0.5f ? -1f : 1f)
        };
    }

    void ExecuteSpawn(PendingSpawn p)
    {
        switch (p.kind)
        {
            case 0: SpawnAtLane(p.lane); break;
            case 1: SpawnSweeper(p.z, p.side); break;
            default: SpawnLowBar(); break;
        }
    }

    void MarkerPose(PendingSpawn p, out Vector3 pos, out Vector3 scl)
    {
        if (p.kind == 1)
        {
            pos = new Vector3(p.side * (corridorHalfWidth + 1f), 1.5f, p.z);
            scl = new Vector3(0.6f, 2.6f, 0.6f);
        }
        else if (p.kind == 2)
        {
            pos = new Vector3(0f, lowBarY, spawnOrigin.position.z);
            scl = new Vector3(corridorHalfWidth * 2f + 1.2f, 0.5f, 0.5f);
        }
        else
        {
            float[] lanes = { -corridorHalfWidth, 0f, corridorHalfWidth };
            pos = new Vector3(lanes[Mathf.Clamp(p.lane, 0, 2)], 1.5f, spawnOrigin.position.z);
            scl = new Vector3(0.5f, 2.6f, 0.5f);
        }
    }

    static Material markerMat;
    GameObject MakeMarker(Vector3 pos, Vector3 scl)
    {
        if (markerMat == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            markerMat = new Material(shader != null ? shader : Shader.Find("Standard"));
            markerMat.color = new Color(1f, 0.6f, 0f);
            markerMat.EnableKeyword("_EMISSION");
            markerMat.SetColor("_EmissionColor", new Color(1f, 0.45f, 0f) * 2.5f);
        }
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "SpawnTelegraph";
        go.transform.position = pos;
        go.transform.localScale = scl;
        go.GetComponent<MeshRenderer>().sharedMaterial = markerMat;
        Destroy(go.GetComponent<BoxCollider>());
        return go;
    }

    public void StartSpawning()
    {
        spawning = true;
        timer = 0.5f; // small grace so player sees first telegraph
        runTime = 0f;
    }

    /// <summary>Live laser count, for HUD debug readout.</summary>
    public int ActiveCount
    {
        get
        {
            Prune();
            return active.Count;
        }
    }

    public void StopSpawning() { spawning = false; }

    public void ResetSpawner()
    {
        ClearAll();
        runTime = 0f;
        timer = 0.5f;
        spawnCount = 0;
    }

    public void ClearAll()
    {
        Prune();
        foreach (var go in active)
            if (go != null) Destroy(go);
        active.Clear();
        // Also clear stray lasers parented under container (e.g. after domain reload).
        if (laserContainer != null)
        {
            for (int i = laserContainer.childCount - 1; i >= 0; i--)
                Destroy(laserContainer.GetChild(i).gameObject);
        }
    }

    void Prune()
    {
        for (int i = active.Count - 1; i >= 0; i--)
            if (active[i] == null) active.RemoveAt(i);
    }

    float SpeedMult()
    {
        return 1f + (speedJitter > 0f ? Random.Range(-speedJitter, speedJitter) : 0f);
    }

    void SpawnAtLane(int lane)
    {
        // Cycle lanes with jitter: guarantees no full-wall unavoidable spawn.
        float[] lanes = { -corridorHalfWidth, 0f, corridorHalfWidth };
        float x = lanes[Mathf.Clamp(lane, 0, lanes.Length - 1)] + Random.Range(-0.4f, 0.4f);
        x = Mathf.Clamp(x, -corridorHalfWidth, corridorHalfWidth);

        Vector3 pos = spawnOrigin.position;
        pos.x = x;
        GameObject go = InstantiateLaser(pos);
        var laser = go.GetComponent<Laser>();
        if (laser == null) laser = go.AddComponent<Laser>();
        laser.SetSize(laserSize);
        laser.speed = laserSpeed * SpeedMult();
        laser.damage = laserDamage;
        laser.cleanupByX = false;
        laser.cleanupZ = cleanupZ;
        laser.direction = Vector3.back;
        laser.homingStrength = runTime >= homingAfterSeconds ? homingStrength : 0f;

        active.Add(go);
    }

    void SpawnSweeper(float z, float side)
    {
        // Wall-to-wall beam ahead of the player: time your advance, don't stand in its path.
        float sx = side * (corridorHalfWidth + 1f);
        GameObject go = InstantiateLaser(new Vector3(sx, 1.5f, z));
        var laser = go.GetComponent<Laser>();
        if (laser == null) laser = go.AddComponent<Laser>();
        laser.SetSize(laserSize);
        laser.speed = laserSpeed * 0.85f * SpeedMult();
        laser.damage = laserDamage;
        laser.direction = sx < 0f ? Vector3.right : Vector3.left;
        laser.cleanupByX = true;
        laser.cleanupX = corridorHalfWidth + 1.4f;

        active.Add(go);
    }

    void SpawnLowBar()
    {
        // Full-width low bar rushing at you: JUMP it (jump height 2m clears it easily).
        Vector3 pos = spawnOrigin.position;
        pos.x = 0f;
        pos.y = lowBarY;
        GameObject go = InstantiateLaser(pos);
        var laser = go.GetComponent<Laser>();
        if (laser == null) laser = go.AddComponent<Laser>();
        laser.SetSize(new Vector3(corridorHalfWidth * 2f + 1.2f, 0.35f, 0.35f));
        laser.speed = laserSpeed * SpeedMult();
        laser.damage = laserDamage;
        laser.cleanupByX = false;
        laser.cleanupZ = cleanupZ;
        laser.direction = Vector3.back;

        active.Add(go);
    }

    GameObject InstantiateLaser(Vector3 pos)
    {
        if (laserPrefab != null)
        {
            return Instantiate(laserPrefab, pos, Quaternion.identity,
                laserContainer != null ? laserContainer : null);
        }
        return CreateFallbackLaser(pos);
    }

    GameObject CreateFallbackLaser(Vector3 pos)
    {
        // Works with zero prefab setup: bright red emissive beam.
        var go = new GameObject("Laser");
        go.transform.position = pos;
        go.transform.localScale = laserSize;
        var filter = go.AddComponent<MeshFilter>();
        filter.mesh = CreateBoxMesh();
        var rend = go.AddComponent<MeshRenderer>();
        rend.material = MakeLaserMaterial();
        var col = go.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = Vector3.one;
        var parent = laserContainer != null ? laserContainer : null;
        go.transform.SetParent(parent, true);
        return go;
    }

    static Mesh boxMeshCache;
    static Mesh CreateBoxMesh()
    {
        if (boxMeshCache != null) return boxMeshCache;
        var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        boxMeshCache = tmp.GetComponent<MeshFilter>().sharedMesh;
        Object.Destroy(tmp);
        return boxMeshCache;
    }

    static Material MakeLaserMaterial()
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var mat = new Material(shader != null ? shader : Shader.Find("Standard"));
        mat.color = Color.red;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", Color.red * 3f);
        return mat;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 p = (spawnOrigin != null ? spawnOrigin.position : transform.position);
        Gizmos.DrawWireCube(p, new Vector3(corridorHalfWidth * 2f + 1f, 3f, 0.5f));
    }
#endif
}
