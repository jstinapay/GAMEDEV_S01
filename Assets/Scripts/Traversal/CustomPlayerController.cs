using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Custom CharacterController movement + jump (double-credit: no Starter Assets).
/// Camera-relative WASD (falls back to world-aligned), manual gravity, platform-riding via mover deltas.
/// Works with PlayerInput Send Messages (OnMove / OnJump / OnSprint).
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class CustomPlayerController : MonoBehaviour
{
    [Header("Move")]
    public float moveSpeed = 6f;
    public float sprintMultiplier = 1.5f;
    public float rotationSpeed = 12f;
    [Tooltip("When assigned, WASD is relative to this camera's facing. Auto-finds MainCamera.")]
    public Transform cameraTransform;

    [Header("Jump / Gravity")]
    public float jumpHeight = 2f;
    public float gravity = -20f;
    public float groundedStickForce = -2f;

    [Header("Spawn")]
    public Vector3 spawnPoint = new Vector3(0f, 2f, 0f);

    CharacterController controller;
    Vector2 moveInput;
    bool sprintHeld;
    float verticalVelocity;
    bool locked;
    Vector3 platformCarry;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (spawnPoint == Vector3.zero)
            spawnPoint = transform.position;
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        if (locked)
        {
            // Keep grounded while locked so finish pose is stable.
            verticalVelocity = groundedStickForce;
            controller.Move(new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime);
            return;
        }

        bool grounded = controller.isGrounded;
        if (grounded && verticalVelocity < 0f)
            verticalVelocity = groundedStickForce;

        float speed = moveSpeed * (sprintHeld ? sprintMultiplier : 1f);
        Vector3 wishDir = new Vector3(moveInput.x, 0f, moveInput.y);
        if (wishDir.sqrMagnitude > 1f) wishDir.Normalize();
        // Camera-relative movement so W follows your view. Falls back to world-aligned.
        if (cameraTransform != null)
        {
            Vector3 camFwd = cameraTransform.forward;
            camFwd.y = 0f;
            if (camFwd.sqrMagnitude > 0.001f)
            {
                camFwd.Normalize();
                float camYaw = Mathf.Atan2(camFwd.x, camFwd.z) * Mathf.Rad2Deg;
                wishDir = Quaternion.Euler(0f, camYaw, 0f) * new Vector3(moveInput.x, 0f, moveInput.y);
                if (wishDir.sqrMagnitude > 1f) wishDir.Normalize();
            }
        }

        // Ride moving / rotating platforms standing underneath us.
        Vector3 carry = ConsumePlatformCarry();

        Vector3 horizontal = wishDir * speed;
        verticalVelocity += gravity * Time.deltaTime;
        Vector3 motion = (horizontal + carry) * Time.deltaTime;
        motion.y = verticalVelocity * Time.deltaTime;
        // Add platform delta as positional offset (frame-rate independent).
        motion += platformCarry;
        platformCarry = Vector3.zero;

        controller.Move(motion);

        if (wishDir.sqrMagnitude > 0.001f)
        {
            Quaternion target = Quaternion.LookRotation(wishDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, rotationSpeed * Time.deltaTime);
        }
    }

    Vector3 ConsumePlatformCarry()
    {
        // Velocity-style carry is folded into platformCarry (positional) by the
        // platform callbacks below; this keeps CharacterController stable.
        return Vector3.zero;
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Only ride when landing on top of something.
        if (hit.normal.y < 0.5f) return;

        var mover = hit.collider.GetComponent<MovingPlatform>();
        if (mover != null)
            platformCarry += mover.Delta;

        var spinner = hit.collider.GetComponent<RotatingPlatform>();
        if (spinner != null)
        {
            // Rotate rider around platform center by the same frame angle.
            Vector3 center = spinner.transform.position;
            Quaternion rot = Quaternion.AngleAxis(spinner.LastAngleDelta, spinner.rotationAxis);
            Vector3 offset = transform.position - center;
            Vector3 rotated = rot * offset;
            platformCarry += rotated - offset;
        }
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (locked || !value.isPressed) return;
        if (controller != null && controller.isGrounded)
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }

    public void OnSprint(InputValue value)
    {
        sprintHeld = value.isPressed;
    }

    public void Respawn()
    {
        controller.enabled = false;
        transform.position = spawnPoint;
        verticalVelocity = 0f;
        moveInput = Vector2.zero;
        controller.enabled = true;
    }

    public void SetSpawn(Vector3 pos) { spawnPoint = pos; }

    public void Lock()
    {
        locked = true;
        moveInput = Vector2.zero;
    }

    public void Unlock() { locked = false; }
}
