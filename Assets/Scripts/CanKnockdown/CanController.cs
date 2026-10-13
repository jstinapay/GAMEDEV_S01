using UnityEngine;

/// <summary>
/// Tracks a single can: knockdown state, initial transform, and collision SFX.
/// Knockdown = tilted past tiltThreshold OR fell below fallYThreshold.
/// Each can scores only once (scored flag, cleared on reset).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CanController : MonoBehaviour
{
    [Header("Knockdown detection")]
    [Tooltip("Tilt in degrees from upright that counts as knocked down.")]
    [SerializeField] float tiltThreshold = 35f;
    [Tooltip("World Y below which the can counts as knocked down (falling off platform). 0 = auto from spawn.")]
    [SerializeField] float fallYThreshold = 0f;
    [Tooltip("If true, auto-set fall threshold from spawn Y on capture.")]
    [SerializeField] bool autoFallThreshold = true;

    [Header("Collision SFX")]
    [Tooltip("Min impact speed to play a clank. Prevents spam on tiny touches.")]
    [SerializeField] float minImpactSpeed = 1.2f;

    Rigidbody rb;
    Vector3 initialPos;
    Quaternion initialRot;
    bool scored;
    float cosTilt;

    /// <summary>True after GameManager has awarded points for this can.</summary>
    public bool Scored => scored;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        cosTilt = Mathf.Cos(tiltThreshold * Mathf.Deg2Rad);
        // If placed by StackManager, it calls CaptureInitial after positioning.
        // Capture here as fallback so manually placed cans still work.
        if (initialPos == Vector3.zero && transform.position != Vector3.zero)
            CaptureInitial();
    }

    /// <summary>Records spawn transform. Called by CanStackManager after arranging.</summary>
    public void CaptureInitial()
    {
        initialPos = transform.position;
        initialRot = transform.rotation;
        cosTilt = Mathf.Cos(tiltThreshold * Mathf.Deg2Rad);
        if (autoFallThreshold || fallYThreshold <= 0f)
            fallYThreshold = initialPos.y - 0.15f;
    }

    /// <summary>Pure check, no side effects. Manager decides scoring.</summary>
    public bool IsKnockedDown()
    {
        // Tilt check: dot(up, worldUp) drops as can tips over.
        if (Vector3.Dot(transform.up, Vector3.up) < cosTilt)
            return true;
        // Fell off platform / floor check.
        if (transform.position.y < fallYThreshold)
            return true;
        return false;
    }

    public void MarkScored() { scored = true; }

    /// <summary>Restores original position/rotation/velocities for a new round.</summary>
    public void ResetCan()
    {
        transform.position = initialPos;
        transform.rotation = initialRot;
        scored = false;
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            // Leave asleep: collision wakes cans automatically, and starting
            // asleep stops the pyramid from jittering apart before the first shot.
            if (!rb.isKinematic)
                rb.Sleep();
        }
    }

    /// <summary>True when slow enough to consider settled.</summary>
    public bool IsSettled(float threshold)
    {
        if (rb == null) return true;
        if (rb.IsSleeping()) return true;
        return rb.linearVelocity.sqrMagnitude < threshold * threshold
            && rb.angularVelocity.sqrMagnitude < threshold * threshold;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.relativeVelocity.magnitude < minImpactSpeed) return;
        if (CanKnockdownAudio.Instance != null)
            CanKnockdownAudio.Instance.PlayCanHit(collision.relativeVelocity.magnitude);
    }
}
