using UnityEngine;
using UnityEngine.InputSystem;

public class movement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 20;
    public float jumpForce = 10;

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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // Freeze physics rotation to stop the sphere from rolling and tumbling
        rb.freezeRotation = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        isGranded = false;
    }

    // Update is called once per frame
    void Update()
    {
        moveInput = moveAction.ReadValue<Vector2>();

        if (jumpAction.IsPressed())
        {
            Debug.Log("Jump Pressed");
        }
        if (jumpAction.WasPressedThisFrame())
        {
            Debug.Log("Jump Pressed this frame");
            if (isGranded)
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
        if (jumpAction.WasReleasedThisFrame())
        {
            Debug.Log("Jump Released this frame");
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
        GameObject other = collision.gameObject;

        if (!other.name.Equals("Plane"))
            Debug.Log("OnCollisionEnter");

        if (other.name.Equals("Plane"))
            isGranded = true;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!collision.gameObject.name.Equals("Plane"))
            Debug.Log("OnCollisionStay");
    }

    private void OnCollisionExit(Collision collision)
    {
        if (!collision.gameObject.name.Equals("Plane"))
            Debug.Log("OnCollisionExit");

        if (collision.gameObject.name.Equals("Plane"))
            isGranded = false;
    }
}
