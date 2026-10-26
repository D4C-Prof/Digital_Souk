using UnityEngine;
using UnityEngine.InputSystem;

public class movement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 20;
    public float jumpForce = 8;

    [Header("Camera-Relative Movement")]
    [Tooltip("If true, movement aligns with the camera's view direction (W moves forward into screen).")]
    public bool moveRelativeToCamera = true;

    [Tooltip("Optional camera transform reference. If empty, automatically uses the Main Camera.")]
    public Transform cameraTransform;

    private Rigidbody rb;
    private InputAction moveAction;
    private InputAction jumpAction;
    private Vector2 moveInput;
    private bool isGranded;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // Freeze physics rotation to stop the sphere from rolling and tumbling
        rb.freezeRotation = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        moveAction = InputSystem.actions?.FindAction("Move");
        jumpAction = InputSystem.actions?.FindAction("Jump");

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        isGranded = true;
    }

    void Update()
    {
        if (moveAction != null)
        {
            moveInput = moveAction.ReadValue<Vector2>();
        }

        if (jumpAction != null && jumpAction.WasPressedThisFrame())
        {
            if (isGranded)
            {
                Vector3 v = rb.linearVelocity;
                // Clamp jump velocity to realistic arcade jump (7-8 m/s) in case inspector has old 999
                float actualJump = (jumpForce > 30f) ? 8f : Mathf.Max(jumpForce, 5f);
                v.y = actualJump;
                rb.linearVelocity = v;
                isGranded = false;
            }
        }

        // Face movement direction without rolling
        Vector3 moveDir = GetMoveDirection();
        if (moveDir.sqrMagnitude > 0.01f)
        {
            transform.forward = Vector3.Slerp(transform.forward, moveDir, Time.deltaTime * 15f);
        }
    }

    private void FixedUpdate()
    {
        Vector3 moveDir = GetMoveDirection();
        Vector3 velocity = rb.linearVelocity;
        velocity.x = moveDir.x * speed * Time.fixedDeltaTime;
        velocity.z = moveDir.z * speed * Time.fixedDeltaTime;

        // Ground detection: cast down 1.25m and ignore player's own collider
        RaycastHit[] hits = Physics.RaycastAll(transform.position, Vector3.down, 1.25f, ~0, QueryTriggerInteraction.Ignore);
        bool grounded = false;
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider.transform.root != transform.root)
            {
                grounded = true;
                break;
            }
        }
        isGranded = grounded;

        // Prevent floating when running into obstacles; apply downward gravity when in air
        if (grounded && velocity.y <= 0.1f)
        {
            velocity.y = 0f;
        }
        else if (!grounded)
        {
            // Crisp downward gravity so player quickly returns to the ground
            velocity.y += Physics.gravity.y * 2.5f * Time.fixedDeltaTime;
            if (velocity.y < -30f) velocity.y = -30f;
        }

        rb.linearVelocity = velocity;
    }

    private Vector3 GetMoveDirection()
    {
        if (moveRelativeToCamera)
        {
            Transform cam = cameraTransform != null ? cameraTransform : (Camera.main != null ? Camera.main.transform : null);
            if (cam != null)
            {
                Vector3 camForward = cam.forward;
                Vector3 camRight = cam.right;
                camForward.y = 0f;
                camRight.y = 0f;
                camForward.Normalize();
                camRight.Normalize();

                Vector3 direction = (camForward * moveInput.y + camRight * moveInput.x);
                if (direction.sqrMagnitude > 1f) direction.Normalize();
                return direction;
            }
        }

        return new Vector3(moveInput.x, 0f, moveInput.y);
    }

    private void OnCollisionEnter(Collision collision)
    {
        isGranded = true;
    }

    private void OnCollisionStay(Collision collision)
    {
        isGranded = true;
    }

    private void OnCollisionExit(Collision collision)
    {
    }
}
