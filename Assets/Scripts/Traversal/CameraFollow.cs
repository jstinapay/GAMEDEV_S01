using UnityEngine;

/// <summary>Chase camera: low, close, biased to look ahead down-course.</summary>
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 5.2f, -8.2f);
    [Tooltip("Where the camera looks, relative to the player (push +Z to see ahead).")]
    public Vector3 focusOffset = new Vector3(0f, 1.1f, 3.5f);
    public float smoothSpeed = 6f;

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));
        transform.LookAt(target.position + focusOffset);
    }
}
