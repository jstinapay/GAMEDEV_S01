using UnityEngine;

/// <summary>Continuous rotation for platforms (required).</summary>
public class RotatingPlatform : MonoBehaviour
{
    [Header("Rotation")]
    public Vector3 rotationAxis = Vector3.up;
    public float degreesPerSecond = 60f;

    /// <summary>Angle rotated last frame (used for rider carry).</summary>
    public float LastAngleDelta { get; private set; }

    void Update()
    {
        float angle = degreesPerSecond * Time.deltaTime;
        transform.Rotate(rotationAxis.normalized, angle, Space.World);
        LastAngleDelta = angle;
    }
}
