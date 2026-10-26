using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Caméra de poursuite fluide pour véhicule.
/// Conserve fidèlement la position et l'angle de vue de la caméra extérieure (Photo 1),
/// sans saut de rotation brutal à 90°, avec suivi fluide et contrôle d'orbite à la souris.
/// </summary>
public class SmoothCarCamera : MonoBehaviour
{
    [Header("Caméras")]
    [Tooltip("Script qui suit le joueur à pied (ex: camFollow).")]
    public MonoBehaviour playerCamFollow;

    [Header("Cible à suivre")]
    [Tooltip("La voiture à suivre")]
    public Transform target;

    [Header("Distance & Centrage")]
    [Tooltip("Distance par défaut par rapport au véhicule")]
    [Range(4f, 35f)]
    public float distance = 12f;

    [Tooltip("Distance minimale de zoom")]
    public float minDistance = 6f;

    [Tooltip("Distance maximale de zoom")]
    public float maxDistance = 30f;

    [Tooltip("Sensibilité du zoom à la molette")]
    public float zoomSensitivity = 2f;

    [Tooltip("Active le zoom avec la molette de la souris")]
    public bool enableZoom = true;

    [Tooltip("Décalage manuel supplémentaire du point visé")]
    public Vector3 targetOffset = Vector3.zero;

    [Header("Contrôle Souris / Orbite")]
    [Tooltip("Permet de regarder librement autour de la voiture avec la souris")]
    public bool enableMouseControl = true;
    public float mouseSensitivityX = 2.5f;
    public float mouseSensitivityY = 2.0f;
    public float minPitch = -15f;
    public float maxPitch = 80f;

    [Header("Fluidité")]
    [Tooltip("Vitesse de suivi de la caméra")]
    public float smoothSpeed = 10f;

    [Header("Protection du Sol")]
    public bool preventGroundClipping = true;
    public float minY = 0.5f;

    // État interne
    private float yaw = 0f;
    private float pitch = 20f;
    private InputAction lookAction;

    private void Awake()
    {
        if (playerCamFollow == null)
        {
            playerCamFollow = GetComponent<camFollow>();
        }
    }

    private void OnEnable()
    {
        if (playerCamFollow == null)
        {
            playerCamFollow = GetComponent<camFollow>();
        }

        if (playerCamFollow != null) 
            playerCamFollow.enabled = false;

        // Liaison de l'action Look si disponible
        try
        {
            lookAction = InputSystem.actions?.FindAction("Look");
        }
        catch { }

        // 1. Conserve STRICTEMENT l'angle et l'orientation actuelle de la caméra (Photo 1)
        // Aucun saut ou rotation à 90 degrés !
        Vector3 currentAngles = transform.eulerAngles;
        yaw = currentAngles.y;
        pitch = currentAngles.x;
        if (pitch > 180f) pitch -= 360f;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // 2. Conserve la distance actuelle par rapport au centre du véhicule
        if (target != null)
        {
            Vector3 carCenter = GetTargetCenter();
            float currentDist = Vector3.Distance(transform.position, carCenter);
            if (currentDist > 2f)
            {
                distance = Mathf.Clamp(currentDist, minDistance, maxDistance);
            }
        }
    }

    private void OnDisable()
    {
        if (playerCamFollow != null) 
            playerCamFollow.enabled = true;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // 1. Zoom molette
        HandleZoom();

        // 2. Contrôle de la vue à la souris (orbite libre comme à pied)
        HandleMouseLook();

        // 3. Calcul de la position souhaitée basée sur l'angle actuel (comme Photo 1)
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 carCenter = GetTargetCenter();
        Vector3 desiredPosition = carCenter - (rotation * Vector3.forward * distance);

        if (preventGroundClipping && desiredPosition.y < minY)
        {
            desiredPosition.y = minY;
        }

        // 4. Suivi fluide sans secousse
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // 5. Orientation fluide
        transform.rotation = rotation;
    }

    /// <summary>
    /// Calcule le centre 3D réel du véhicule à partir de ses meshes enfants (châssis, cabine, roues).
    /// </summary>
    public Vector3 GetTargetCenter()
    {
        if (target == null) return transform.position;

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers != null && renderers.Length > 0)
        {
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] is MeshRenderer || renderers[i] is SkinnedMeshRenderer)
                {
                    b.Encapsulate(renderers[i].bounds);
                }
            }
            return b.center + target.rotation * targetOffset;
        }

        return target.position + (Vector3.up * 1.8f) + target.rotation * targetOffset;
    }

    private void HandleMouseLook()
    {
        if (!enableMouseControl) return;

        Vector2 lookDelta = Vector2.zero;

        if (lookAction != null && lookAction.enabled)
        {
            lookDelta = lookAction.ReadValue<Vector2>();
        }
        else if (Mouse.current != null)
        {
            lookDelta = Mouse.current.delta.ReadValue();
        }

        if (lookDelta.sqrMagnitude > 0.001f)
        {
            yaw += lookDelta.x * mouseSensitivityX * 0.1f;
            pitch -= lookDelta.y * mouseSensitivityY * 0.1f;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }
    }

    private void HandleZoom()
    {
        if (!enableZoom) return;

        float scrollDelta = 0f;
        if (Mouse.current != null)
        {
            scrollDelta = Mouse.current.scroll.ReadValue().y;
        }

        if (Mathf.Abs(scrollDelta) > 0.01f)
        {
            float zoomChange = (scrollDelta / 120f) * zoomSensitivity;
            distance = Mathf.Clamp(distance - zoomChange, minDistance, maxDistance);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (target != null)
        {
            Vector3 center = GetTargetCenter();
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(center, 0.6f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, center);
        }
    }
}