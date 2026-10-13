using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds, tracks, and resets the 3-2-1 pyramid (6 cans).
/// When canPrefab is null, a procedural can is built from primitives
/// so the game runs with zero manual prefab setup.
/// </summary>
public class CanStackManager : MonoBehaviour
{
    [Header("Stack (3 balls / 6 cans default)")]
    [Tooltip("Optional can prefab. Leave empty for auto-built procedural cans.")]
    [SerializeField] GameObject canPrefab;
    [Tooltip("Platform the cans sit on. Auto-found by name/tag when empty.")]
    [SerializeField] Transform platform;
    [Tooltip("Center of the stack on the platform top.")]
    [SerializeField] Vector3 stackCenter = new Vector3(0f, 1f, 4f);
    [SerializeField] float canHeight = 0.48f;
    [SerializeField] float canDiameter = 0.3f;
    [Tooltip("Center-to-center gap. Kept slightly wider than the diameter so fresh cans never spawn touching.")]
    [SerializeField] float spacing = 0.34f;
    [Tooltip("Extra lift per row so stacked rows never interpenetrate on build.")]
    [SerializeField] float rowLift = 0.003f;
    [Tooltip("Park fresh cans asleep so the pyramid can't jitter apart before the first shot.")]
    [SerializeField] bool sleepInitially = true;

    [Header("Physics")]
    [SerializeField] float canMass = 0.4f;
    [SerializeField] float canDrag = 0.05f;
    [SerializeField] float canAngularDrag = 0.3f;

    readonly List<CanController> cans = new List<CanController>();
    readonly List<Color> canColors = new List<Color>
    {
        new Color(0.9f, 0.15f, 0.2f),   // carnival red
        new Color(1f, 0.75f, 0.1f),     // mustard yellow
        new Color(0.15f, 0.55f, 0.95f), // booth blue
        new Color(0.2f, 0.75f, 0.3f),   // green
        new Color(0.95f, 0.4f, 0.8f),   // pink
        new Color(1f, 0.5f, 0.1f),      // orange
    };

    public int TotalCans => cans.Count;
    public IReadOnlyList<CanController> Cans => cans;

    void Awake()
    {
        ResolvePlatform();
    }

    void Start()
    {
        // Builder normally pre-builds the stack; rebuild here only if empty
        // (e.g. manager dropped into a blank scene at runtime).
        if (cans.Count == 0)
            BuildStack();
    }

    void ResolvePlatform()
    {
        if (platform != null) return;
        var byName = GameObject.Find("BoothCounter");
        if (byName != null) platform = byName.transform;
    }

    /// <summary>Clears old cans and arranges a fresh 3-2-1 pyramid.</summary>
    public void BuildStack()
    {
        ClearStack();
        ResolvePlatform();

        float topY = platform != null
            ? platform.position.y + platform.localScale.y * 0.5f
            : stackCenter.y;
        float baseY = topY + canHeight * 0.5f + 0.005f;
        float z = platform != null ? platform.position.z : stackCenter.z;

        // Bottom: 3, middle: 2, top: 1. Small inward offsets keep it stable.
        var offsets = new Vector2[]
        {
            new Vector2(-spacing, 0f), new Vector2(0f, 0f), new Vector2(spacing, 0f),
            new Vector2(-spacing * 0.5f, canHeight), new Vector2(spacing * 0.5f, canHeight),
            new Vector2(0f, canHeight * 2f),
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            int row = i < 3 ? 0 : (i < 5 ? 1 : 2);
            Vector3 pos = new Vector3(offsets[i].x, baseY + offsets[i].y + row * rowLift, z);
            GameObject go = canPrefab != null
                ? Instantiate(canPrefab, pos, Quaternion.identity, transform)
                : BuildProceduralCan(i);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.identity;
            go.name = $"Can_{i}";

            var rb = go.GetComponent<Rigidbody>();
            ConfigureCanRigidbody(rb);
            EnsureCanFriction(go);

            var cc = go.GetComponent<CanController>();
            if (cc == null) cc = go.AddComponent<CanController>();
            cc.CaptureInitial();
            // Park asleep: only a real impact wakes the pyramid.
            if (sleepInitially && rb != null && !rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.Sleep();
            }
            cans.Add(cc);
        }
    }

    GameObject BuildProceduralCan(int colorIndex)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.transform.SetParent(transform, false);
        // Unity cylinder is 2m tall, 1m diameter: scale to canHeight/canDiameter.
        go.transform.localScale = new Vector3(canDiameter, canHeight * 0.5f, canDiameter);
        // The cylinder primitive ships with a CAPSULE collider (rounded ends), which
        // balances on rounded bottoms and topples the pyramid. Swap to a flat box:
        // local size (1,2,1) x scale (0.2,0.16,0.2) = world (0.2,0.32,0.2).
        var capsule = go.GetComponent<CapsuleCollider>();
        if (capsule != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) Object.DestroyImmediate(capsule);
            else Object.Destroy(capsule);
#else
            Object.Destroy(capsule);
#endif
        }
        var box = go.AddComponent<BoxCollider>();
        box.center = Vector3.zero;
        box.size = new Vector3(1f, 2f, 1f);

        var rend = go.GetComponent<MeshRenderer>();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        mat.color = canColors[colorIndex % canColors.Count];
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.5f);
        rend.sharedMaterial = mat;

        // White stripe for carnival look (child cube, no collider).
        var stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stripe.name = "Stripe";
        stripe.transform.SetParent(go.transform, false);
        stripe.transform.localScale = new Vector3(1.02f, 0.25f, 1.02f);
        stripe.transform.localPosition = Vector3.zero;
        var stripeRend = stripe.GetComponent<MeshRenderer>();
        var stripeMat = new Material(rend.sharedMaterial) { color = Color.white };
        stripeRend.sharedMaterial = stripeMat;
        var stripeCol = stripe.GetComponent<BoxCollider>();
        if (stripeCol != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) Object.DestroyImmediate(stripeCol);
            else Object.Destroy(stripeCol);
#else
            Object.Destroy(stripeCol);
#endif
        }

        go.AddComponent<CanController>();
        return go;
    }

    void ConfigureCanRigidbody(Rigidbody rb)
    {
        if (rb == null) return;
        rb.mass = canMass;
        rb.linearDamping = canDrag;
        rb.angularDamping = canAngularDrag;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.sleepThreshold = 0.005f;
    }

    /// <summary>Grippy, low-bounce contact so cans thud and topple instead of skating.</summary>
    static void EnsureCanFriction(GameObject go)
    {
        var phys = new PhysicsMaterial("CanPhys")
        {
            dynamicFriction = 0.5f,
            staticFriction = 0.5f,
            bounciness = 0.05f,
            frictionCombine = PhysicsMaterialCombine.Average,
            bounceCombine = PhysicsMaterialCombine.Minimum,
        };
        foreach (var col in go.GetComponentsInChildren<Collider>())
            if (col != null && col.sharedMaterial == null)
                col.sharedMaterial = phys;
    }

    void ClearStack()
    {
        foreach (var c in cans)
            if (c != null) DestroyCan(c.gameObject);
        cans.Clear();
        // Also clear orphans parented here (e.g. after domain reload).
        // Copy to array first: destroying while iterating the transform is unsafe.
        var orphans = new System.Collections.Generic.List<GameObject>();
        foreach (Transform child in transform)
            orphans.Add(child.gameObject);
        foreach (var o in orphans)
            DestroyCan(o);
    }

    static void DestroyCan(GameObject go)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) Object.DestroyImmediate(go);
        else Object.Destroy(go);
#else
        Object.Destroy(go);
#endif
    }

    /// <summary>Shows or hides the whole stack (Balloon Rush hides the cans).</summary>
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    /// <summary>Restores all cans to spawn pose for a new round.</summary>
    public void ResetStack()
    {
        if (cans.Count == 0)
        {
            BuildStack();
            return;
        }
        foreach (var c in cans)
            if (c != null) c.ResetCan();
    }

    public int CountKnockedDown()
    {
        int n = 0;
        foreach (var c in cans)
            if (c != null && c.IsKnockedDown()) n++;
        return n;
    }

    /// <summary>
    /// Returns cans knocked down since last call and marks them scored,
    /// so each can awards points exactly once.
    /// </summary>
    public List<CanController> CollectNewlyKnockedDown()
    {
        var fresh = new List<CanController>();
        foreach (var c in cans)
        {
            if (c == null || c.Scored) continue;
            if (c.IsKnockedDown())
            {
                c.MarkScored();
                fresh.Add(c);
            }
        }
        return fresh;
    }

    public bool AllSettled(float threshold)
    {
        foreach (var c in cans)
        {
            if (c == null) continue;
            if (!c.IsSettled(threshold)) return false;
        }
        return true;
    }
}
