using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>Chase camera with Roblox-style scroll zoom and free look. Right-drag orbits, first person always looks.</summary>
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 5.2f, -8.2f);
    [Tooltip("Where the camera looks, relative to the player (push +Z to see ahead).")]
    public Vector3 focusOffset = new Vector3(0f, 1.1f, 3.5f);
    public float smoothSpeed = 6f;

    [Header("Free Look")]
    [Tooltip("Right-drag orbits in third person. First person always free-looks.")]
    public bool freeLook = true;
    public float lookSensitivity = 0.25f;
    public bool invertLookX;
    public bool invertLookY;
    public float minPitch = -60f;
    public float maxPitch = 75f;

    [Header("Roblox Zoom")]
    public float minDistance = 0.5f;
    public float maxDistance = 14f;
    public float scrollSensitivity = 0.006f;
    [Tooltip("At or below this distance the view counts as first person.")]
    public float firstPersonThreshold = 1f;
    public float headHeight = 1.6f;
    [Tooltip("Hide player meshes in first person so you don't see your back/face.")]
    public bool hidePlayerInFirstPerson = true;
    [Tooltip("Lock and hide the cursor in first person for true FPS look.")]
    public bool lockCursorInFirstPerson = true;

    public bool IsFirstPerson { get; private set; }

    Vector3 baseDir;
    float baseDist;
    float targetDist;
    float currentDist;
    float yaw;
    float pitch;
    Renderer[] playerRenderers;
    bool meshesHidden;

    void Start()
    {
        baseDist = Mathf.Max(offset.magnitude, 0.01f);
        baseDir = offset / baseDist;
        yaw = Mathf.Atan2(baseDir.x, -baseDir.z) * Mathf.Rad2Deg;
        pitch = Mathf.Asin(Mathf.Clamp(baseDir.y, -1f, 1f)) * Mathf.Rad2Deg;
        targetDist = Mathf.Clamp(baseDist, minDistance, maxDistance);
        currentDist = targetDist;
        if (target != null)
            playerRenderers = target.GetComponentsInChildren<Renderer>(true);
    }

    void LateUpdate()
    {
        if (target == null) return;

        float scroll = ReadScroll();
        // New Input System reports small values (~1 per notch), old reports ~120.
        if (Mathf.Abs(scroll) > 10f) scroll /= 120f;
        // Existing scenes serialized 0.006 for the old scale; treat tiny values as legacy.
        float step = scrollSensitivity < 0.1f ? 2f : scrollSensitivity;
        if (Mathf.Abs(scroll) > Mathf.Epsilon)
            targetDist = Mathf.Clamp(targetDist - scroll * step, minDistance, maxDistance);

        // Keyboard fallback for testing: - zooms out, = zooms in.
        float key = ReadKeyDir();
        if (Mathf.Abs(key) > Mathf.Epsilon)
            targetDist = Mathf.Clamp(targetDist + key * step * Time.deltaTime * 4f, minDistance, maxDistance);

        currentDist = Mathf.Lerp(currentDist, targetDist, 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));
        IsFirstPerson = currentDist <= firstPersonThreshold;
        ApplyFirstPersonMeshes();
        ApplyCursorState();
        UpdateFreeLook();

        Vector3 dir = OrbitDir();
        if (IsFirstPerson)
        {
            // True FPS view: position at the head, look along orbit direction.
            transform.position = target.position + Vector3.up * headHeight + dir * 0.1f;
            transform.rotation = Quaternion.LookRotation(-dir);
            return;
        }

        Vector3 desired = target.position + dir * currentDist;

        transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));
        transform.LookAt(target.position + focusOffset);
    }

    float ReadScroll()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            return Mouse.current.scroll.ReadValue().y;
        return 0f;
#else
        return Input.mouseScrollDelta.y * 120f;
#endif
    }

    // -1 = zoom in, +1 = zoom out, 0 = none. Lets you test without a mouse wheel.
    float ReadKeyDir()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null) return 0f;
        bool zoomin = Keyboard.current.equalsKey.isPressed || Keyboard.current.numpadPlusKey.isPressed;
        bool zoomOut = Keyboard.current.minusKey.isPressed || Keyboard.current.numpadMinusKey.isPressed;
#else
        bool zoomin = Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.KeypadPlus);
        bool zoomOut = Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus);
#endif
        if (zoomin && !zoomOut) return -1f;
        if (zoomOut && !zoomin) return 1f;
        return 0f;
    }

    void ApplyFirstPersonMeshes()
    {
        bool shouldHide = hidePlayerInFirstPerson && IsFirstPerson;
        if (shouldHide == meshesHidden) return;
        meshesHidden = shouldHide;
        if (playerRenderers == null)
        {
            if (target != null)
                playerRenderers = target.GetComponentsInChildren<Renderer>(true);
            if (playerRenderers == null) return;
        }
        foreach (var r in playerRenderers)
        {
            if (r != null)
                r.enabled = !shouldHide;
        }
    }

    void ApplyCursorState()
    {
        if (!Application.isPlaying) return;
        if (!lockCursorInFirstPerson) return;
        if (IsFirstPerson)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    Vector3 OrbitDir()
    {
        float p = pitch * Mathf.Deg2Rad;
        float y = yaw * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(y) * Mathf.Cos(p), Mathf.Sin(p), -Mathf.Cos(y) * Mathf.Cos(p));
    }

    void UpdateFreeLook()
    {
        if (!freeLook) return;
        Vector2 delta;
        bool dragging;
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null) return;
        delta = Mouse.current.delta.ReadValue();
        dragging = Mouse.current.rightButton.isPressed;
#else
        delta = new Vector2(Input.GetAxis("Mouse X") * 50f * Time.deltaTime, Input.GetAxis("Mouse Y") * 50f * Time.deltaTime);
        dragging = Input.GetMouseButton(1);
#endif
        if (IsFirstPerson || dragging)
        {
            yaw += (invertLookX ? delta.x : -delta.x) * lookSensitivity;
            pitch += (invertLookY ? delta.y : -delta.y) * lookSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }
    }
}
