using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Classic slingshot with a forward rest pose: click the ball (or the pouch
/// area) to grab it, drag back to stretch the bands, release to fire. The pull
/// steers around the rest pose that already aims at the cans, so dragging down
/// lobs toward the stack and pull length sets power (minLaunchSpeed-maxLaunchSpeed).
/// A loaded ball sits in the pouch whenever the player is allowed to shoot.
/// One release = one impulse.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class ThrowController : MonoBehaviour
{
    [Header("Refs (auto-found if empty)")]
    [SerializeField] CanKnockdownManager manager;
    [Tooltip("Balloon Rush round. Auto-found. When it accepts shots, releases count there instead.")]
    [SerializeField] BalloonRushManager rushManager;
    [Tooltip("Pouch rest position. The loaded ball waits here.")]
    [SerializeField] Transform throwPoint;
    [SerializeField] GameObject ballPrefab;
    [SerializeField] Camera camera;
    [Tooltip("Band anchors. Auto-found as ForkTipL / ForkTipR when empty.")]
    [SerializeField] Transform forkTipL;
    [SerializeField] Transform forkTipR;
    [Tooltip("Optional power fill image. Auto-found as PowerBar when empty.")]
    [SerializeField] Image powerBar;

    [Header("Slingshot tuning")]
    [Tooltip("Max drag distance in meters. Longer pull = faster shot.")]
    [SerializeField] float maxPull = 1.3f;
    [Tooltip("Releases shorter than this snap back without firing.")]
    [SerializeField] float minPull = 0.18f;
    [SerializeField] float minLaunchSpeed = 9f;
    [SerializeField] float maxLaunchSpeed = 26f;
    [SerializeField] float ballMass = 1.4f;
    [SerializeField] float ballDiameter = 0.2f;
    [Tooltip("Seconds after release before the next ball loads.")]
    [SerializeField] float throwCooldown = 0.8f;
    [Tooltip("Grab radius in screen pixels around the loaded ball. Clicking inside grabs the pouch.")]
    [SerializeField] float grabRadiusPx = 110f;
    [Tooltip("Forward bias (meters): the pouch rest pose already aims at the cans and the pull steers around it. Bigger = flatter shots. Needed because a head-on camera's drag plane is nearly perpendicular to the lane, so raw pull vectors point almost straight up.")]
    [SerializeField] float forwardAim = 2.2f;
    [Tooltip("Safety net: release direction must point this far toward +z (the cans). The z-clamp keeps pulls behind rest, so this rarely triggers.")]
    [SerializeField] float minForwardDot = 0f;

    [Header("Trajectory preview")]
    [SerializeField] bool showTrajectory = true;
    [SerializeField] int trajectoryPoints = 60;
    [Tooltip("Sim step in seconds. Clamped to the physics timestep so the preview integrates exactly like the flight.")]
    [SerializeField] float trajectoryStep = 0.02f;

    LineRenderer trajectory;
    LineRenderer bandL;
    LineRenderer bandR;
    GameObject pouch;
    GameObject loadedBall;
    Rigidbody loadedRb;
    bool dragging;
    Vector3 grabOffset;
    Plane dragPlane;
    float lastThrowTime = -10f;

    void Awake()
    {
        ResolveRefs();
        trajectory = GetComponent<LineRenderer>();
        ConfigureLine(trajectory, Color.yellow, 0.03f, 0.015f);
        EnsureBands();
        EnsurePouch();
        HideCrosshair();
        SetPowerVisible(false);
    }

    void ResolveRefs()
    {
        if (manager == null) manager = Object.FindAnyObjectByType<CanKnockdownManager>();
        if (rushManager == null) rushManager = Object.FindAnyObjectByType<BalloonRushManager>();
        if (camera == null) camera = Camera.main;
        if (throwPoint == null)
        {
            var tp = GameObject.Find("ThrowPoint");
            if (tp != null) throwPoint = tp.transform;
        }
        if (forkTipL == null)
        {
            var l = GameObject.Find("ForkTipL");
            if (l != null) forkTipL = l.transform;
        }
        if (forkTipR == null)
        {
            var r = GameObject.Find("ForkTipR");
            if (r != null) forkTipR = r.transform;
        }
        if (powerBar == null)
        {
            var pb = GameObject.Find("PowerBar");
            if (pb != null) powerBar = pb.GetComponent<Image>();
        }
    }

    static void ConfigureLine(LineRenderer line, Color color, float w0, float w1)
    {
        if (line == null) return;
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.startWidth = w0;
        line.endWidth = w1;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard"));
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        else if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        line.material = mat;
        line.enabled = false;
    }

    void EnsureBands()
    {
        bandL = CreateBand("BandL", new Color(0.35f, 0.2f, 0.1f));
        bandR = CreateBand("BandR", new Color(0.35f, 0.2f, 0.1f));
    }

    LineRenderer CreateBand(string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.startWidth = 0.035f;
        lr.endWidth = 0.035f;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard"));
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        else if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        lr.material = mat;
        lr.enabled = false;
        return lr;
    }

    void EnsurePouch()
    {
        if (pouch != null) return;
        pouch = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pouch.name = "Pouch";
        pouch.transform.SetParent(transform, false);
        pouch.transform.localScale = new Vector3(0.16f, 0.08f, 0.16f);
        var rend = pouch.GetComponent<MeshRenderer>();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        mat.color = new Color(0.4f, 0.25f, 0.12f);
        rend.sharedMaterial = mat;
        var pouchCol = pouch.GetComponent<SphereCollider>();
        if (pouchCol != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) Object.DestroyImmediate(pouchCol);
            else Object.Destroy(pouchCol);
#else
            Object.Destroy(pouchCol);
#endif
        }
        pouch.SetActive(false);
    }

    void HideCrosshair()
    {
        var ui = Object.FindAnyObjectByType<CanKnockdownUI>();
        if (ui != null) ui.SetCrosshairVisible(false);
    }

    void Update()
    {
        ResolveRefs();
        if (manager == null || camera == null || throwPoint == null) return;

        bool playing = manager.State == CanKnockdownManager.GameState.Playing;
        bool rushing = rushManager != null && rushManager.AcceptsShots();
        bool cooldownOk = Time.time - lastThrowTime >= throwCooldown;

        // Keep a ball loaded while the player may shoot (either mode).
        if (loadedBall == null && cooldownOk && !dragging
            && ((playing && manager.ThrowsRemaining > 0) || rushing))
            LoadBall();

        if (dragging)
        {
            // Abort if the round ended mid-drag (e.g. last ball resolved early).
            if ((!playing && !rushing) || loadedBall == null)
            {
                dragging = false;
                HideAimHelpers();
            }
            else
            {
                UpdateDrag();
            }
        }
        else if ((playing || rushing) && loadedBall != null && cooldownOk && WasPressed() && !IsPointerOverUI() && PressedOnBall())
        {
            dragging = true;
            // Slingshot plane: faces the camera, pinned at the pouch rest point.
            // The ball can only move on this plane, inside maxPull of rest.
            dragPlane = new Plane(camera.transform.forward, throwPoint.position);
            grabOffset = PlaneHit() - loadedBall.transform.position;
        }

        UpdateBands();
    }

    /// <summary>
    /// True when the press hits the loaded ball directly or lands inside the
    /// grab radius around it on screen. Clicks elsewhere never start a drag.
    /// </summary>
    bool PressedOnBall()
    {
        Vector2 mouse = CurrentMousePosition();
        Ray ray = camera.ScreenPointToRay(mouse);
        if (Physics.Raycast(ray, out RaycastHit hit) && hit.transform == loadedBall.transform)
            return true;
        Vector3 screenPos = camera.WorldToScreenPoint(loadedBall.transform.position);
        if (screenPos.z <= 0f) return false; // behind the camera
        return Vector2.Distance(mouse, (Vector2)screenPos) <= grabRadiusPx;
    }

    // ---- dragging ----

    Vector3 PlaneHit()
    {
        Ray ray = camera.ScreenPointToRay(CurrentMousePosition());
        if (dragPlane.Raycast(ray, out float dist))
            return ray.GetPoint(dist);
        return throwPoint.position;
    }

    void UpdateDrag()
    {
        Vector3 want = PlaneHit() - grabOffset;
        Vector3 rest = throwPoint.position;
        // Never push past the rest point: the pouch only pulls back toward the camera.
        if (want.z > rest.z) want.z = rest.z;
        Vector3 offset = want - rest;
        if (offset.magnitude > maxPull)
            offset = offset.normalized * maxPull;
        Vector3 pulled = rest + offset;
        loadedBall.transform.position = pulled;

        // Slingshot with a forward rest pose: the pouch at rest already aims at
        // the cans, and the pull steers around that baseline. Pull direction
        // still opposes the launch (drag down = lob up, drag left = curve right)
        // and pull length sets power. A raw pull vector alone can't work here:
        // the head-on camera's drag plane is nearly perpendicular to the lane,
        // so raw pulls point ~84 degrees up and can never reach the cans.
        // Zero-length guard: below minPull the direction is ill-defined, so weak
        // releases snap back without firing.
        float pullLen = offset.magnitude;
        Vector3 planar = rest - pulled;
        Vector3 dir = (planar + Vector3.forward * forwardAim).normalized;
        float speed = PullToSpeed(pullLen);
        SetPowerVisible(true);
        if (powerBar != null)
        {
            float t = Mathf.Clamp01(pullLen / maxPull);
            powerBar.fillAmount = t;
            // Juice only: green -> gold -> red as power builds.
            powerBar.color = t < 0.5f
                ? Color.Lerp(new Color(0.35f, 0.85f, 0.4f), new Color(1f, 0.8f, 0.15f), t * 2f)
                : Color.Lerp(new Color(1f, 0.8f, 0.15f), new Color(0.95f, 0.25f, 0.15f), (t - 0.5f) * 2f);
        }

        if (showTrajectory && trajectory != null && pullLen > 0.01f)
        {
            trajectory.enabled = true;
            trajectory.positionCount = trajectoryPoints;
            SimulateArc(trajectory, pulled, dir * speed);
        }
        else if (trajectory != null)
        {
            trajectory.enabled = false;
        }

        if (WasReleased())
        {
            dragging = false;
            HideAimHelpers();
            if (pullLen >= minPull && dir.z >= minForwardDot)
                Fire(dir, speed);
            else
                loadedBall.transform.position = rest; // weak pull: snap back, free shot
        }
    }

    float PullToSpeed(float pullLen)
    {
        float t = Mathf.Clamp01(pullLen / maxPull);
        return Mathf.Lerp(minLaunchSpeed, maxLaunchSpeed, t);
    }

    /// <summary>
    /// Preview using the same numbers as the release: same start pos/velocity,
    /// same gravity, same semi-implicit Euler integration at the physics timestep,
    /// same linear damping as the loaded ball. That is why the line matches flight.
    /// </summary>
    void SimulateArc(LineRenderer lr, Vector3 p, Vector3 v)
    {
        float dt = Mathf.Clamp(trajectoryStep, 0.005f, Time.fixedDeltaTime);
        float damping = loadedRb != null ? loadedRb.linearDamping : 0f;
        float dragK = 1f / (1f + damping * dt);
        Vector3 gravity = Physics.gravity;
        for (int i = 0; i < trajectoryPoints; i++)
        {
            lr.SetPosition(i, p);
            v += gravity * dt;
            v *= dragK;
            p += v * dt;
            if (p.y < 0f)
            {
                for (int j = i + 1; j < trajectoryPoints; j++)
                    lr.SetPosition(j, p);
                break;
            }
        }
    }

    void HideAimHelpers()
    {
        if (trajectory != null) trajectory.enabled = false;
        SetPowerVisible(false);
    }

    void SetPowerVisible(bool visible)
    {
        if (powerBar == null) return;
        // Fill lives under a background bar; toggle the whole cluster.
        Transform bg = powerBar.transform.parent;
        if (bg != null && bg.name == "PowerBarBg")
            bg.gameObject.SetActive(visible);
        else
            powerBar.gameObject.SetActive(visible);
    }

    // ---- balls ----

    void LoadBall()
    {
        GameObject ball = ballPrefab != null
            ? Instantiate(ballPrefab, throwPoint.position, Quaternion.identity)
            : BuildDefaultBall();
        ball.transform.position = throwPoint.position;
        ball.name = $"KnockdownBall_{Time.frameCount}";
        if (ball.GetComponent<BallController>() == null)
            ball.AddComponent<BallController>();
        var rb = ball.GetComponent<Rigidbody>();
        if (rb == null) rb = ball.AddComponent<Rigidbody>();
        rb.mass = ballMass;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.isKinematic = true; // rides in the pouch until release (gravity off while held)
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        CanKnockdownFX.AddBallTrail(ball); // visual only: ribbon follows flight
        loadedBall = ball;
        loadedRb = rb;
    }

    /// <summary>Releases the pouch: exactly one impulse, then hands off to physics.</summary>
    void Fire(Vector3 dir, float speed)
    {
        if (loadedBall == null || loadedRb == null) return;
        // Detach first: even if something below throws, a second Fire can't re-impulse.
        Rigidbody rb = loadedRb;
        loadedBall = null;
        loadedRb = null;

        rb.isKinematic = false; // gravity + collisions resume for flight
        rb.linearVelocity = Vector3.zero; // no inherited velocity from aiming
        rb.angularVelocity = Vector3.zero;
        // Impulse scaled by mass so `speed` is the true release velocity in m/s.
        rb.AddForce(dir * (speed * rb.mass), ForceMode.Impulse);
        lastThrowTime = Time.time;

        if (CanKnockdownAudio.Instance != null)
            CanKnockdownAudio.Instance.PlayThrow();
        if (pouch != null) pouch.SetActive(false);

        // One throwing system, two modes: rush consumes ammo + tags the shot,
        // knockdown consumes a throw. Modes are mutually exclusive by state.
        if (rushManager != null && rushManager.AcceptsShots())
        {
            int seq = rushManager.OnShotFired();
            var bc = rb.GetComponent<BallController>();
            if (bc != null) bc.shotSeq = seq;
        }
        else if (manager != null)
        {
            manager.OnThrowUsed();
        }
    }

    GameObject BuildDefaultBall()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.transform.localScale = Vector3.one * ballDiameter;
        var rend = go.GetComponent<MeshRenderer>();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        mat.color = new Color(0.95f, 0.2f, 0.15f);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.85f);
        rend.sharedMaterial = mat;
        var col = go.GetComponent<SphereCollider>();
        var phys = new PhysicsMaterial("BallPhys") { bounciness = 0.25f, dynamicFriction = 0.3f };
        col.sharedMaterial = phys;
        if (go.GetComponent<BallController>() == null)
            go.AddComponent<BallController>();
        return go;
    }

    void UpdateBands()
    {
        Vector3 anchor = throwPoint != null ? throwPoint.position : transform.position;
        Vector3 ballPos = loadedBall != null ? loadedBall.transform.position : anchor;
        bool show = loadedBall != null;
        if (bandL != null)
        {
            bandL.enabled = show && forkTipL != null;
            if (bandL.enabled) { bandL.SetPosition(0, forkTipL.position); bandL.SetPosition(1, ballPos); }
        }
        if (bandR != null)
        {
            bandR.enabled = show && forkTipR != null;
            if (bandR.enabled) { bandR.SetPosition(0, forkTipR.position); bandR.SetPosition(1, ballPos); }
        }
        if (pouch != null)
        {
            pouch.SetActive(show);
            if (show)
            {
                pouch.transform.position = ballPos;
                pouch.transform.LookAt(camera != null ? camera.transform : transform);
            }
        }
    }

    /// <summary>Removes all live balls (round restart).</summary>
    public void ClearBalls()
    {
        dragging = false;
        loadedBall = null;
        loadedRb = null;
        HideAimHelpers();
        foreach (var bc in Object.FindObjectsByType<BallController>())
            if (bc != null) Destroy(bc.gameObject);
        foreach (var rb in Object.FindObjectsByType<Rigidbody>())
            if (rb != null && rb.gameObject.name.StartsWith("KnockdownBall"))
                Destroy(rb.gameObject);
        UpdateBands();
    }

    public void ResetCooldown() { lastThrowTime = -10f; }

    // ---- input ----

    bool WasPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null) return mouse.leftButton.wasPressedThisFrame;
#endif
        return Input.GetMouseButtonDown(0);
    }

    bool WasReleased()
    {
#if ENABLE_INPUT_SYSTEM
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null) return mouse.leftButton.wasReleasedThisFrame;
#endif
        return Input.GetMouseButtonUp(0);
    }

    Vector2 CurrentMousePosition()
    {
#if ENABLE_INPUT_SYSTEM
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null) return mouse.position.ReadValue();
#endif
        return (Vector2)Input.mousePosition;
    }

    static bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        return EventSystem.current.IsPointerOverGameObject();
    }
}
