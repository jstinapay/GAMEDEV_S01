using UnityEngine;
using Unity.Cinemachine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Roblox-style scroll zoom for the Starter Assets Third Person rig (Cinemachine 3).
/// Scroll out = third person, scroll fully in = first person (meshes hidden).
/// Attach to the PlayerFollowCamera object next to CinemachineThirdPersonFollow.
/// </summary>
[RequireComponent(typeof(CinemachineThirdPersonFollow))]
public class RobloxCameraZoom : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("Player root used to hide meshes in first person. Auto-found from camera Follow if empty.")]
    public Transform playerRoot;

    [Header("Zoom")]
    public float minDistance = 0.4f;
    public float maxDistance = 8f;
    [Tooltip("Distance change per scroll unit. Scroll up zooms in.")]
    public float scrollSensitivity = 0.006f;
    public float smoothSpeed = 10f;

    [Header("First Person")]
    [Tooltip("At or below this distance the view counts as first person and meshes hide.")]
    public float firstPersonThreshold = 0.9f;
    [Tooltip("Hide player meshes when in first person so you don't see inside the head.")]
    public bool hideMeshesInFirstPerson = true;

    public bool IsFirstPerson { get; private set; }
    public float CurrentDistance { get; private set; }

    CinemachineThirdPersonFollow follow;
    Renderer[] playerRenderers;
    float targetDistance;
    bool meshesHidden;

    void Awake()
    {
        follow = GetComponent<CinemachineThirdPersonFollow>();
        if (playerRoot == null)
        {
            var vcam = GetComponent<CinemachineCamera>();
            if (vcam != null && vcam.Follow != null)
            {
                var cc = vcam.Follow.GetComponentInParent<CharacterController>();
                playerRoot = cc != null ? cc.transform : vcam.Follow.root;
            }
        }
    }

    void Start()
    {
        targetDistance = Mathf.Clamp(follow.CameraDistance, minDistance, maxDistance);
        CurrentDistance = targetDistance;
        if (playerRoot != null)
            playerRenderers = playerRoot.GetComponentsInChildren<Renderer>(true);
        ApplyFirstPersonState(true);
    }

    void Update()
    {
        float scroll = ReadScroll();
        if (Mathf.Abs(scroll) > Mathf.Epsilon)
        {
            targetDistance = Mathf.Clamp(targetDistance - scroll * scrollSensitivity, minDistance, maxDistance);
        }

        CurrentDistance = Mathf.Lerp(CurrentDistance, targetDistance,
            1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));
        follow.CameraDistance = CurrentDistance;

        IsFirstPerson = CurrentDistance <= firstPersonThreshold;
        ApplyFirstPersonState(false);
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

    void ApplyFirstPersonState(bool force)
    {
        bool shouldHide = hideMeshesInFirstPerson && IsFirstPerson;
        if (!force && shouldHide == meshesHidden)
            return;
        meshesHidden = shouldHide;
        if (playerRenderers == null)
            return;
        foreach (var r in playerRenderers)
        {
            if (r != null)
                r.enabled = !shouldHide;
        }
    }

    public void ResetZoom()
    {
        targetDistance = Mathf.Clamp(4f, minDistance, maxDistance);
    }
}
