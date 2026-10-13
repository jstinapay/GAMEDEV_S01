using UnityEngine;

/// <summary>Balloon Pop Rush movement styles. Small set, smooth and predictable.</summary>
public enum BalloonMove { LeftRight, RightLeft, FloatUp, Diagonal }

/// <summary>
/// A single poppable balloon: glossy body + knot + string, kinematic drift along
/// a pattern with a gentle sine sway, trigger pop on ball contact. Scores once.
/// Owned by BalloonSpawner (movement bounds, release) and BalloonRushManager (points).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BalloonController : MonoBehaviour
{
    public BalloonType type;
    public int points;
    public int bonusAmmo;

    BalloonSpawner home;
    Vector3 cruiseDir = Vector3.right;
    float cruiseSpeed = 1.2f;
    float swayAmp = 0.25f;
    float swayFreq = 1.1f;
    Vector3 swayAxis = Vector3.up;
    float age;
    float maxLife = 30f;
    Vector3 minBounds = new Vector3(-6f, 0.4f, 2.5f);
    Vector3 maxBounds = new Vector3(6f, 5f, 5f);
    bool popped;
    Rigidbody rb;

    /// <summary>Called by the spawner right after creation. Starts drifting.</summary>
    public void Launch(BalloonSpawner owner, BalloonType kind, int pts, int ammo,
        Vector3 direction, float speed, float life, Vector3 minB, Vector3 maxB)
    {
        home = owner;
        type = kind;
        points = pts;
        bonusAmmo = ammo;
        cruiseDir = direction.normalized;
        cruiseSpeed = speed;
        maxLife = life;
        minBounds = minB;
        maxBounds = maxB;
        // Sway perpendicular to travel so motion stays readable, never random.
        swayAxis = Mathf.Abs(direction.y) > 0.7f ? Vector3.right : Vector3.up;
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    void FixedUpdate()
    {
        if (popped || rb == null) return;
        age += Time.fixedDeltaTime;
        Vector3 step = cruiseDir * cruiseSpeed
            + swayAxis * (Mathf.Sin(age * swayFreq) * swayAmp);
        rb.MovePosition(rb.position + step * Time.fixedDeltaTime);
    }

    void Update()
    {
        if (popped) return;
        Vector3 p = transform.position;
        if (age >= maxLife
            || p.x < minBounds.x || p.x > maxBounds.x
            || p.y < minBounds.y || p.y > maxBounds.y
            || p.z < minBounds.z || p.z > maxBounds.z)
            Release(silent: true);
    }

    void OnTriggerEnter(Collider other)
    {
        if (popped) return;
        var ball = other.GetComponent<BallController>();
        if (ball == null) return; // only real projectiles pop balloons
        popped = true;
        int seq = ball.shotSeq;
        Vector3 pos = transform.position;
        BalloonType kind = type;
        int pts = points;
        int ammo = bonusAmmo;
        Release(silent: true);
        if (BalloonRushManager.Instance != null)
            BalloonRushManager.Instance.OnBalloonPopped(kind, pts, ammo, seq, pos);
    }

    void Release(bool silent)
    {
        _ = silent;
        if (home != null) home.Release(this);
        else Destroy(gameObject);
    }
}
