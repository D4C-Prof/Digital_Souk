using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Contrôleur arcade simple pour véhicule avec rotation réaliste des roues (cylindres) sur l'axe X.
/// </summary>
public class SimpleArcadeCarMouves : MonoBehaviour
{
    public enum WheelRotationAxis
    {
        [Tooltip("Axe X de la voiture (transform.right) - Rotation réaliste autour de l'essieu")]
        Car_X_Axis,
        [Tooltip("Axe X local du cylindre (Vector3.right, Space.Self)")]
        Local_X_Axis,
        [Tooltip("Axe Y local du cylindre")]
        Local_Y_Axis,
        [Tooltip("Axe Z local du cylindre")]
        Local_Z_Axis
    }

    [Header("Drive State")]
    [Tooltip("Ne bouge que lorsque vrai. Géré automatiquement par CarEnterExitUIBuilder.")]
    public bool isBeingDriven = false;

    [Header("Speed & Steering")]
    public float speed = 15f;
    public float turnSpeed = 90f;

    [Header("Control Inversions")]
    [Tooltip("Inverse la direction : A tourne à gauche, D tourne à droite")]
    public bool invertSteering = true;

    [Tooltip("Inverse la marche avant/arrière")]
    public bool invertDrive = false;

    [Tooltip("Inverse le sens de roulement visuel des roues")]
    public bool reverseWheelRoll = true;

    [Header("Wheel Cylinders (Roues)")]
    public Transform frontCylinder;
    public Transform rearCylinder;

    [Tooltip("Axe de rotation pour faire rouler les cylindres. Choisissez l'axe X pour un roulement naturel.")]
    public WheelRotationAxis rotationAxis = WheelRotationAxis.Car_X_Axis;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Démarre toujours désactivé tant que le joueur n'est pas au volant
        isBeingDriven = false;
        enabled = false;
    }

    private void Start()
    {
        isBeingDriven = false;
        enabled = false;
    }

    private void FixedUpdate()
    {
        // Ne se déplace que si le joueur conduit ce véhicule précis
        if (!isBeingDriven) return;

        float move = 0f;
        float steer = 0f;

        // Entrées clavier (WASD / Touches fléchées)
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) move += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) move -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) steer += 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) steer -= 1f;
        }

        // Entrées manette (Gamepad)
        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.leftStick.ReadValue();
            if (Mathf.Abs(stick.y) > 0.15f) move = stick.y;
            if (Mathf.Abs(stick.x) > 0.15f) steer = stick.x;
        }

        // Inversions si configurées
        if (invertDrive) move = -move;
        if (invertSteering) steer = -steer;

        // Déplacement avant / arrière le long de l'axe avant du véhicule (axe Z)
        Vector3 movement = transform.forward * move * speed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);

        // Rotation directionnelle (virage gauche / droite autour de Y)
        Quaternion turn = Quaternion.Euler(0f, steer * turnSpeed * Time.fixedDeltaTime, 0f);
        rb.MoveRotation(rb.rotation * turn);

        // Rotation des cylindres (roues) sur l'axe X pour un roulement réaliste
        if (Mathf.Abs(move) > 0.01f)
        {
            float rollDirection = reverseWheelRoll ? -1f : 1f;
            float rollAngle = move * speed * 200f * rollDirection * Time.fixedDeltaTime;

            switch (rotationAxis)
            {
                case WheelRotationAxis.Car_X_Axis:
                    // Tourne autour de l'axe X du véhicule (l'essieu des roues dans l'espace mondial)
                    Vector3 carXAxis = transform.right;
                    if (frontCylinder != null) frontCylinder.Rotate(carXAxis, rollAngle, Space.World);
                    if (rearCylinder != null) rearCylinder.Rotate(carXAxis, rollAngle, Space.World);
                    break;

                case WheelRotationAxis.Local_X_Axis:
                    // Tourne autour de l'axe X local du cylindre
                    if (frontCylinder != null) frontCylinder.Rotate(Vector3.right * rollAngle, Space.Self);
                    if (rearCylinder != null) rearCylinder.Rotate(Vector3.right * rollAngle, Space.Self);
                    break;

                case WheelRotationAxis.Local_Y_Axis:
                    if (frontCylinder != null) frontCylinder.Rotate(Vector3.up * rollAngle, Space.Self);
                    if (rearCylinder != null) rearCylinder.Rotate(Vector3.up * rollAngle, Space.Self);
                    break;

                case WheelRotationAxis.Local_Z_Axis:
                    if (frontCylinder != null) frontCylinder.Rotate(Vector3.forward * rollAngle, Space.Self);
                    if (rearCylinder != null) rearCylinder.Rotate(Vector3.forward * rollAngle, Space.Self);
                    break;
            }
        }
    }
}