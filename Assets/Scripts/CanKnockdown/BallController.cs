using UnityEngine;

/// <summary>
/// Ball lifetime and impact feedback. Thrown by ThrowController via Rigidbody.AddForce.
/// Cleans itself up after lifetime seconds or when it falls out of the booth.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BallController : MonoBehaviour
{
    [Tooltip("Seconds before auto-despawn. Prevents stray balls piling up.")]
    [SerializeField] float lifetime = 6f;
    [Tooltip("Y below which the ball is destroyed (missed everything).")]
    [SerializeField] float killY = -5f;
    [Tooltip("Min impact speed to play a thud.")]
    [SerializeField] float minImpactSpeed = 1.5f;

    [Tooltip("Balloon Rush shot id. -1 = not a rush shot (knockdown ignores it).")]
    public int shotSeq = -1;

    float age;
    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        age = 0f;
        if (rb == null) rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // A ball riding the slingshot pouch is kinematic: it must not age out.
        if (rb != null && rb.isKinematic) return;
        age += Time.deltaTime;
        if (age >= lifetime || transform.position.y < killY)
            Destroy(gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.relativeVelocity.magnitude < minImpactSpeed) return;
        // Ball-vs-can thud is handled here; can-vs-can clank is in CanController.
        // Throttle inside audio manager prevents spam on multi-can hits.
        if (CanKnockdownAudio.Instance != null)
            CanKnockdownAudio.Instance.PlayBallHit(collision.relativeVelocity.magnitude);
    }

    void OnDestroy()
    {
        // Rush only: a despawned ball that never popped breaks the streak.
        if (shotSeq >= 0 && BalloonRushManager.Instance != null)
            BalloonRushManager.Instance.NoteBallGone(shotSeq);
    }
}
