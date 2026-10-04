using UnityEngine;

/// <summary>
/// A single moving laser beam. Travels from the far end of the corridor
/// toward the player and damages once on touch. Destroys itself after
/// passing the player or after its lifetime expires.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class Laser : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 10f;
    public Vector3 direction = Vector3.back; // -Z: far end -> player start; +/-X: wall sweepers
    public float cleanupZ = -6f;
    [Tooltip("Sweepers die by X position instead of Z.")]
    public bool cleanupByX;
    public float cleanupX = 3.6f;
    public float maxLifetime = 12f;

    [Header("Damage")]
    public int damage = 20;

    [Header("Homing (chasers track you while far - late dodge still works)")]
    [Tooltip("Sideways steer toward the player in m/s. 0 = straight.")]
    public float homingStrength;
    public Transform homingTarget;

    [Header("Visual pulse (optional)")]
    public bool pulse = true;
    public float pulseSpeed = 6f;
    public float pulseAmount = 0.08f;

    bool hasHit;
    float age;
    Vector3 baseScale;

    void Awake()
    {
        EnsurePhysicsSetup();
        baseScale = transform.localScale;
    }

    void OnValidate()
    {
        // Keep editor-time setup correct without forcing play-mode changes.
        var col = GetComponent<BoxCollider>();
        if (col != null) col.isTrigger = true;
    }

    void EnsurePhysicsSetup()
    {
        var col = GetComponent<BoxCollider>();
        col.isTrigger = true;
        // Kinematic rigidbody guarantees OnTriggerEnter fires reliably with a
        // CharacterController player and prevents any physical push.
        var rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    void Update()
    {
        if (homingStrength > 0f)
        {
            if (homingTarget == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) homingTarget = p.transform;
            }
            // Steer only while far: once it's close, the line is committed and you CAN dodge.
            if (homingTarget != null && !cleanupByX && transform.position.z - homingTarget.position.z > 5f)
            {
                float steer = Mathf.Clamp(homingTarget.position.x - transform.position.x, -1f, 1f);
                transform.Translate(new Vector3(steer * homingStrength * Time.deltaTime, 0f, 0f), Space.World);
            }
        }
        transform.Translate(direction.normalized * speed * Time.deltaTime, Space.World);
        age += Time.deltaTime;

        if (pulse)
        {
            float s = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            transform.localScale = new Vector3(baseScale.x, baseScale.y * s, baseScale.z);
        }

        bool done = age > maxLifetime;
        if (cleanupByX) done = done || Mathf.Abs(transform.position.x) > cleanupX;
        else done = done || transform.position.z < cleanupZ;
        if (done)
            Destroy(gameObject);
    }

    /// <summary>Resize after instantiate (keeps the pulse effect scaled correctly).</summary>
    public void SetSize(Vector3 s)
    {
        transform.localScale = s;
        baseScale = s;
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;
        if (!other.CompareTag("Player")) return;
        var health = other.GetComponent<PlayerHealth>();
        if (health == null) return;
        hasHit = true;
        health.TakeDamage(damage);
        SpawnHitBurst(transform.position, Color.red);
        // Destroy after hit so one laser can never multi-tick, even during i-frames.
        Destroy(gameObject);
    }

    static void SpawnHitBurst(Vector3 pos, Color color)
    {
        var go = new GameObject("LaserHitBurst");
        go.transform.position = pos;
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.startColor = color;
        main.startSpeed = 6f;
        main.startLifetime = 0.4f;
        main.startSize = 0.12f;
        main.maxParticles = 24;
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission;
        em.enabled = true;
        em.rateOverTime = 0f;
        em.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 20) });
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.4f;
        ps.Play();
        Object.Destroy(go, 1f);
    }
}
