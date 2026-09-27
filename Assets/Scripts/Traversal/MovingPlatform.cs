using UnityEngine;

/// <summary>Ping-pong mover for vertical (required) and horizontal (bonus) platforms.</summary>
public class MovingPlatform : MonoBehaviour
{
    public enum MoveAxis { Vertical, HorizontalX, HorizontalZ }

    [Header("Motion")]
    public MoveAxis axis = MoveAxis.Vertical;
    public float distance = 2f;
    public float speed = 1.2f;
    public float phaseOffset;

    Vector3 basePosition;
    Vector3 prevPosition;

    /// <summary>World-space displacement applied last frame (used for rider carry).</summary>
    public Vector3 Delta { get; private set; }

    void Start()
    {
        basePosition = transform.position;
        prevPosition = basePosition;
    }

    void Update()
    {
        float t = Mathf.PingPong(Time.time * speed + phaseOffset, distance * 2f) - distance;
        Vector3 target = basePosition;
        switch (axis)
        {
            case MoveAxis.Vertical: target.y += t; break;
            case MoveAxis.HorizontalX: target.x += t; break;
            case MoveAxis.HorizontalZ: target.z += t; break;
        }
        transform.position = target;
        Delta = target - prevPosition;
        prevPosition = target;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Vector3 b = Application.isPlaying ? basePosition : transform.position;
        Vector3 dir = axis == MoveAxis.Vertical ? Vector3.up : axis == MoveAxis.HorizontalX ? Vector3.right : Vector3.forward;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(b - dir * distance, b + dir * distance);
    }
#endif
}
