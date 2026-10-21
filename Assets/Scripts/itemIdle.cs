using UnityEngine;

/// <summary>
/// Controls the idle animation for consumable items (e.g. coins, pellets, power-ups).
/// Rotates the object around the default (world) Y axis while maintaining a 30-degree Z-axis tilt,
/// and creates a smooth up-and-down floating bob using a sine wave function.
/// All key variables can be customized in the Unity Inspector.
/// </summary>
public class itemIdle : MonoBehaviour
{
    public enum SpinMode
    {
        [Tooltip("Rotates around the default world Y axis (0, 1, 0) while maintaining the tilt.")]
        WorldY,
        [Tooltip("Rotates around the object's local Y axis.")]
        LocalY
    }

    [Header("Spin / Rotation Settings")]
    [Tooltip("Enable or disable the spinning rotation.")]
    public bool enableSpin = true;

    [Tooltip("Rotation speed in degrees per second.")]
    public float spinSpeed = 90f;

    [Tooltip("Tilt angle around the Z axis in degrees (default is 30 degrees).")]
    public float tiltAngleZ = 30f;

    [Tooltip("Optional tilt angle around the X axis in degrees.")]
    public float tiltAngleX = 0f;

    [Tooltip("Rotation mode: WorldY spins around the default vertical axis; LocalY spins around the object's own axis.")]
    public SpinMode spinMode = SpinMode.WorldY;

    [Tooltip("If true, automatically reads the initial tilt angles from the Transform on Start.")]
    public bool readInitialTiltFromTransform = false;

    [Header("Float / Bobbing Settings")]
    [Tooltip("Enable or disable the vertical floating/bobbing motion.")]
    public bool enableFloat = true;

    [Tooltip("Height of the float bob (maximum distance above and below starting position).")]
    public float floatAmplitude = 0.15f;

    [Tooltip("Frequency/speed of the sine wave bobbing.")]
    public float floatFrequency = 3f;

    [Tooltip("Starting phase offset in radians for the sine function.")]
    public float phaseOffset = 0f;

    [Tooltip("If true, assigns a random initial phase so multiple consumables don't bob in exact unison.")]
    public bool randomizeInitialPhase = false;

    [Tooltip("If true, uses local position (recommended for prefabs and parented objects).")]
    public bool useLocalPosition = true;

    // Internal state tracking
    private Vector3 startPosition;
    private float currentYaw;

    private void Start()
    {
        // Cache the initial resting position
        startPosition = useLocalPosition ? transform.localPosition : transform.position;

        // Optionally read initial tilt from object's current transform
        if (readInitialTiltFromTransform)
        {
            Vector3 euler = useLocalPosition ? transform.localEulerAngles : transform.eulerAngles;
            tiltAngleX = euler.x;
            tiltAngleZ = euler.z;
        }

        // Initialize current yaw from the transform's initial Y rotation for seamless start
        currentYaw = (useLocalPosition ? transform.localEulerAngles : transform.eulerAngles).y;

        // Optionally randomize phase so instances are staggered
        if (randomizeInitialPhase)
        {
            phaseOffset = Random.Range(0f, Mathf.PI * 2f);
        }
    }

    private void Update()
    {
        HandleSpin();
        HandleFloat();
    }

    /// <summary>
    /// Handles continuous spinning around the chosen Y axis while preserving tilt.
    /// </summary>
    private void HandleSpin()
    {
        if (!enableSpin) return;

        // Increment rotation yaw angle based on speed and delta time
        currentYaw = (currentYaw + spinSpeed * Time.deltaTime) % 360f;

        Quaternion tilt = Quaternion.Euler(tiltAngleX, 0f, tiltAngleZ);
        Quaternion yaw = Quaternion.Euler(0f, currentYaw, 0f);

        if (spinMode == SpinMode.WorldY)
        {
            // Multiplying yaw on the left rotates around the default world vertical axis (Vector3.up),
            // while preserving the tilt orientation relative to the vertical axis.
            transform.rotation = yaw * tilt;
        }
        else
        {
            // Local Y rotation spins around the object's own tilted axis
            transform.rotation = tilt * yaw;
        }
    }

    /// <summary>
    /// Handles smooth vertical bobbing using the sine function.
    /// </summary>
    private void HandleFloat()
    {
        if (!enableFloat) return;

        // Calculate vertical offset using sine function
        float yOffset = Mathf.Sin((Time.time * floatFrequency) + phaseOffset) * floatAmplitude;

        Vector3 newPos = new Vector3(startPosition.x, startPosition.y + yOffset, startPosition.z);

        if (useLocalPosition)
        {
            transform.localPosition = newPos;
        }
        else
        {
            transform.position = newPos;
        }
    }

    /// <summary>
    /// Helper method to reset or update the resting position at runtime or from other scripts.
    /// </summary>
    public void SetStartPosition(Vector3 newPos)
    {
        startPosition = newPos;
    }

    /// <summary>
    /// Context menu option in the Unity Inspector to quickly re-anchor the resting position.
    /// </summary>
    [ContextMenu("Reset Base Position")]
    public void ResetBasePosition()
    {
        startPosition = useLocalPosition ? transform.localPosition : transform.position;
    }
}
