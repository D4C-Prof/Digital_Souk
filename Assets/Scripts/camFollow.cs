using UnityEngine;
using UnityEngine.InputSystem;

public class camFollow : MonoBehaviour
{

    [Header("Target")]
    
    public GameObject plyr;

    [Tooltip("Offset applied to the target's position (e.g. height to look at Pacman's center).")]
    public Vector3 targetOffset = new Vector3(0f, 0.5f, 0f);

    [Header("Mouse Orbit / Rotation")]
    [Tooltip("Enables mouse control over the camera rotation.")]
    public bool enableMouseControl = true;

    [Tooltip("Horizontal look sensitivity.")]
    public float mouseSensitivityX = 2.5f;

    [Tooltip("Vertical look sensitivity.")]
    public float mouseSensitivityY = 2.0f;

    [Tooltip("Inverts the vertical mouse pitch input.")]
    public bool invertY = false;

    [Tooltip("Minimum vertical angle (looking up at target).")]
    public float minPitch = -15f;

    [Tooltip("Maximum vertical angle (looking down at target from above).")]
    public float maxPitch = 80f;

    [Tooltip("If true, only orbits when holding the Right Mouse Button. If false, mouse always controls the camera.")]
    public bool requireRightClick = false;

    [Header("Cursor Settings")]
    [Tooltip("Automatically locks and hides the cursor when playing. Press Esc to unlock, click window to lock again.")]
    public bool lockCursor = true;

    [Tooltip("Key used to unlock the cursor.")]
    public Key unlockKey = Key.Escape;

    [Header("Distance & Zoom")]
    [Tooltip("Distance from Pacman.")]
    public float distance = 6f;

    [Tooltip("Minimum zoom distance.")]
    public float minDistance = 2f;

    [Tooltip("Maximum zoom distance.")]
    public float maxDistance = 15f;

    [Tooltip("Scroll wheel zoom sensitivity.")]
    public float zoomSensitivity = 2f;

    [Tooltip("Enables zoom with the mouse scroll wheel.")]
    public bool enableZoom = true;

    [Header("Follow Smoothing")]
    [Range(0.01f, 30f)]
    [Tooltip("Smooth speed for following the target position. Lower = smoother/lazy follow, higher = snappier.")]
    public float smoothSpeed = 10f;

    [Header("Height & Ground Protection")]
    [Tooltip("Prevents the camera from clipping into the ground.")]
    public bool preventGroundClipping = true;

    [Tooltip("Minimum allowed world Y position for the camera.")]
    public float minY = 0.5f;

    [Header("Teleport Handling")]
    [Tooltip("If the target moves more than this distance in a single frame, camera treats it as a teleport and shifts instantly.")]
    public float teleportThreshold = 2.0f;

    [Header("Wall Collision")]
    public bool preventWallClipping = true;
    public float collisionRadius = 0.25f;
    public float collisionPadding = 0.1f;
    public LayerMask collisionLayers = ~0;

    // Internal state
    private float yaw = 0f;
    private float pitch = 25f;
    private InputAction lookAction;
    private Vector3 lastTargetPosition;
    private bool hasLastTargetPosition = false;
    private Transform target;

    private void Start()
    {
        // 1. Auto-find Pacman if not assigned
            target = plyr.transform;

            if (target == null)
            {
                Debug.LogWarning("cameraFollows: Could not find 'player'. Please assign the Target in the Inspector.");
            }

        // 2. Lock cursor if enabled
        if (lockCursor && !requireRightClick)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // 3. Try to bind Look action from InputSystem if available
        try
        {
            lookAction = InputSystem.actions?.FindAction("Look");
        }
        catch
        {
            // InputSystem.actions might not be initialized or present
        }

        // 4. Initialize Yaw and Pitch from current camera orientation
        Vector3 currentAngles = transform.eulerAngles;
        yaw = currentAngles.y;
        pitch = currentAngles.x;
        if (pitch > 180f) pitch -= 360f;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // 5. Initialize distance and snap to target's position
        if (target != null)
        {
            Vector3 targetCenter = target.position + targetOffset;
            float initialDist = Vector3.Distance(transform.position, targetCenter);
            if (initialDist > 0.5f)
            {
                distance = Mathf.Clamp(initialDist, minDistance, maxDistance);
            }

            Quaternion initialRot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 desiredPos = targetCenter - (initialRot * Vector3.forward * distance);
            if (preventGroundClipping && desiredPos.y < minY)
            {
                desiredPos.y = minY;
            }
            transform.position = desiredPos;
            transform.rotation = initialRot;
            lastTargetPosition = target.position;
            hasLastTargetPosition = true;
        }
    }

    private void OnEnable()
    {
        // When switching back from car camera, preserve current camera orientation seamlessly
        Vector3 currentAngles = transform.eulerAngles;
        yaw = currentAngles.y;
        pitch = currentAngles.x;
        if (pitch > 180f) pitch -= 360f;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        if (target != null)
        {
            Vector3 targetCenter = target.position + targetOffset;
            float initialDist = Vector3.Distance(transform.position, targetCenter);
            if (initialDist > 0.5f)
            {
                distance = Mathf.Clamp(initialDist, minDistance, maxDistance);
            }
            lastTargetPosition = target.position;
            hasLastTargetPosition = true;
        }
    }

    private void Update()
    {
        HandleCursorLock();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Detect teleportation jump if target moved abruptly
        if (hasLastTargetPosition)
        {
            Vector3 targetDelta = target.position - lastTargetPosition;
            if (targetDelta.sqrMagnitude > teleportThreshold * teleportThreshold)
            {
                // Instantly offset camera by targetDelta so there is no lag or lerp through walls
                transform.position += targetDelta;
            }
        }
        lastTargetPosition = target.position;
        hasLastTargetPosition = true;

        HandleInput();

        // Calculate rotation and desired position
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 targetCenter = target.position + targetOffset;
        Vector3 desiredPosition = targetCenter -
            (rotation * Vector3.forward * distance);

        // Ground clipping protection
        if (preventGroundClipping && desiredPosition.y < minY)
        {
            desiredPosition.y = minY;
        }

        // Wall collision protection
        bool wallBlocked = false;

        if (preventWallClipping)
        {
            Vector3 direction = desiredPosition - targetCenter;
            float desiredDistance = direction.magnitude;

            if (desiredDistance > 0.001f)
            {
                direction /= desiredDistance;

                if (Physics.SphereCast(
                    targetCenter,
                    collisionRadius,
                    direction,
                    out RaycastHit hit,
                    desiredDistance,
                    collisionLayers,
                    QueryTriggerInteraction.Ignore))
                {
                    desiredPosition = targetCenter + direction *
                        Mathf.Max(0.1f, hit.distance - collisionPadding);

                    wallBlocked = true;
                }
            }
        }

        // Smooth follow, but immediately correct wall penetration
        if (wallBlocked)
        {
            transform.position = desiredPosition;
        }
        else
        {
            float lerpFactor = smoothSpeed <= 1f
                ? (1f - Mathf.Pow(
                    1f - Mathf.Clamp(smoothSpeed, 0.01f, 0.99f),
                    Time.deltaTime * 60f))
                : (1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));

            transform.position = Vector3.Lerp(
                transform.position,
                desiredPosition,
                lerpFactor);
        }

        // Orient camera towards target
        transform.rotation = rotation;
    }

    /// <summary>
    /// Instantly shifts camera position when Pacman teleports so the camera follows with zero delay.
    /// </summary>
    public void OnTargetTeleported(Vector3 positionDelta)
    {
        transform.position += positionDelta;
        if (target != null)
        {
            lastTargetPosition = target.position;
            hasLastTargetPosition = true;
        }
    }

    private void HandleCursorLock()
    {
        if (requireRightClick)
        {
            if (Mouse.current != null)
            {
                if (Mouse.current.rightButton.wasPressedThisFrame)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                else if (Mouse.current.rightButton.wasReleasedThisFrame)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }
            return;
        }

        if (!lockCursor) return;

        // Escape unlocks cursor
        if (Keyboard.current != null && Keyboard.current[unlockKey].wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Clicking game window locks cursor again
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void HandleInput()
    {
        if (!enableMouseControl) return;

        bool canRotate = !requireRightClick || (Mouse.current != null && Mouse.current.rightButton.isPressed);

        if (canRotate)
        {
            Vector2 lookDelta = GetLookDelta();

            yaw += lookDelta.x * mouseSensitivityX * 0.1f;

            float pitchDelta = invertY ? lookDelta.y : -lookDelta.y;
            pitch += pitchDelta * mouseSensitivityY * 0.1f;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        if (enableZoom)
        {
            float scroll = GetScrollDelta();
            if (Mathf.Abs(scroll) > 0.01f)
            {
                distance -= scroll * 0.01f * zoomSensitivity;
                distance = Mathf.Clamp(distance, minDistance, maxDistance);
            }
        }
    }

    private Vector2 GetLookDelta()
    {
        // 1. Try look action from InputSystem
        if (lookAction != null && lookAction.enabled)
        {
            Vector2 val = lookAction.ReadValue<Vector2>();
            if (val.sqrMagnitude > 0.0001f)
                return val;
        }

        // 2. Direct mouse delta fallback
        if (Mouse.current != null)
        {
            return Mouse.current.delta.ReadValue();
        }

        return Vector2.zero;
    }

    private float GetScrollDelta()
    {
        if (Mouse.current != null)
        {
            return Mouse.current.scroll.ReadValue().y;
        }
        return 0f;
    }
}
