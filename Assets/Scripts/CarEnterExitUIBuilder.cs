using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

public class CarEnterExitUIBuilder : MonoBehaviour
{
    [Header("Car Movement Script")]
    public SimpleArcadeCarMouves carController;

    [Header("Positions & Camera")]
    public Transform exitPoint;
    public SmoothCarCamera carCameraFollow;

    [Header("UI Toolkit Reference")]
    public UIDocument DriveUi;

    [Header("Interaction Radius (Fallback & Dual-Detection)")]
    [Tooltip("Maximum distance from player to car to interact (meters)")]
    public float interactionDistance = 8.0f;

    private VisualElement drivePromptElement;
    private VisualElement exitPromptElement;

    private GameObject playerObject;
    private bool isPlayerInRange = false;
    private bool isDriving = false;

    private void Awake()
    {
        AutoWireReferences();
    }

    private void Start()
    {
        AutoWireReferences();
        InitializeUI();

        // Ensure car controls and chase camera start disabled
        if (carController != null)
        {
            carController.isBeingDriven = false;
            carController.enabled = false;
        }
        if (carCameraFollow != null) carCameraFollow.enabled = false;
    }

    public void AutoWireReferences()
    {
        if (carController == null)
        {
            carController = GetComponent<SimpleArcadeCarMouves>() 
                         ?? GetComponentInParent<SimpleArcadeCarMouves>() 
                         ?? GetComponentInChildren<SimpleArcadeCarMouves>()
                         ?? Object.FindFirstObjectByType<SimpleArcadeCarMouves>();
        }

        if (exitPoint == null)
        {
            Transform foundExit = transform.Find("ExitPoint") ?? transform.parent?.Find("ExitPoint");
            if (foundExit != null) exitPoint = foundExit;
            else if (carController != null)
            {
                exitPoint = carController.transform.Find("ExitPoint");
            }
        }

        if (carCameraFollow == null)
        {
            carCameraFollow = Object.FindFirstObjectByType<SmoothCarCamera>();
        }

        if (DriveUi == null)
        {
            DriveUi = GetComponent<UIDocument>() 
                   ?? GetComponentInChildren<UIDocument>() 
                   ?? GetComponentInParent<UIDocument>();

            if (DriveUi == null)
            {
                GameObject promptGO = GameObject.Find("DrivePrompt");
                if (promptGO != null) DriveUi = promptGO.GetComponent<UIDocument>();
            }
            if (DriveUi == null)
            {
                DriveUi = Object.FindFirstObjectByType<UIDocument>();
            }
        }
    }

    private void InitializeUI()
    {
        if (DriveUi == null)
        {
            AutoWireReferences();
            if (DriveUi == null) return;
        }

        var root = DriveUi.rootVisualElement;
        if (root == null) return;

        drivePromptElement = root.Q<VisualElement>("DrivePrompt");
        exitPromptElement = root.Q<VisualElement>("ExitPrompt");

        SetVisible(drivePromptElement, false);
        SetVisible(exitPromptElement, false);
    }

    private void Update()
    {
        // 1. Proximity detection (guarantees interaction even if physics triggers fail or miss)
        if (!isDriving)
        {
            if (playerObject == null || !playerObject.activeInHierarchy)
            {
                playerObject = GameObject.FindWithTag("Player");
                if (playerObject == null)
                {
                    movement mov = Object.FindFirstObjectByType<movement>();
                    if (mov != null) playerObject = mov.gameObject;
                }
                if (playerObject == null)
                {
                    playerObject = GameObject.Find("player") ?? GameObject.Find("Player");
                }
            }

            if (playerObject != null && playerObject.activeInHierarchy)
            {
                Vector3 carCenter = (carController != null) ? carController.transform.position : transform.position;
                float dist = Vector3.Distance(carCenter, playerObject.transform.position);
                bool inRange = dist <= interactionDistance;

                if (inRange != isPlayerInRange)
                {
                    isPlayerInRange = inRange;
                    if (drivePromptElement == null) InitializeUI();
                    SetVisible(drivePromptElement, isPlayerInRange);
                    if (isPlayerInRange)
                    {
                        Debug.Log($"<color=#4ade80>[Digital Souk]</color> Player entered car interaction range ({dist:F1}m). [E] To Drive prompt shown.");
                    }
                }
            }
        }

        // 2. Read Keyboard / Gamepad Inputs
        bool ePressed = false;
        bool fPressed = false;

        if (Keyboard.current != null)
        {
            ePressed = Keyboard.current.eKey.wasPressedThisFrame;
            fPressed = Keyboard.current.fKey.wasPressedThisFrame;
        }

        if (Gamepad.current != null)
        {
            if (Gamepad.current.buttonNorth.wasPressedThisFrame || Gamepad.current.buttonWest.wasPressedThisFrame)
            {
                ePressed = true;
                fPressed = true;
            }
        }

        // Enter car with [E]
        if (isPlayerInRange && !isDriving && ePressed)
        {
            EnterVehicle();
        }
        // Exit car with [F]
        else if (isDriving && fPressed)
        {
            ExitVehicle();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayerCollider(other) && !isDriving)
        {
            playerObject = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
            isPlayerInRange = true;

            if (drivePromptElement == null) InitializeUI();
            SetVisible(drivePromptElement, true);
            Debug.Log("<color=#4ade80>[Digital Souk]</color> Player triggered vehicle zone. [E] To Drive prompt shown.");
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (IsPlayerCollider(other) && !isDriving && !isPlayerInRange)
        {
            playerObject = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
            isPlayerInRange = true;

            if (drivePromptElement == null) InitializeUI();
            SetVisible(drivePromptElement, true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayerCollider(other) && !isDriving)
        {
            isPlayerInRange = false;
            SetVisible(drivePromptElement, false);
        }
    }

    private bool IsPlayerCollider(Collider col)
    {
        if (col == null) return false;
        if (col.CompareTag("Player")) return true;
        if (col.transform.root.CompareTag("Player")) return true;
        if (col.attachedRigidbody != null && col.attachedRigidbody.CompareTag("Player")) return true;
        if (col.GetComponentInParent<movement>() != null) return true;
        if (col.gameObject.name.ToLower().Contains("player")) return true;
        return false;
    }

    private void EnterVehicle()
    {
        isDriving = true;
        isPlayerInRange = false;

        // Swap UI prompts
        SetVisible(drivePromptElement, false);
        SetVisible(exitPromptElement, true);

        // Hide the player character
        if (playerObject != null)
        {
            playerObject.SetActive(false);
        }

        // Enable car movement
        if (carController != null)
        {
            carController.isBeingDriven = true;
            carController.enabled = true;
        }

        // Enable car chase camera
        if (carCameraFollow != null)
        {
            carCameraFollow.target = (carController != null ? carController.transform : this.transform);
            carCameraFollow.enabled = true;
        }
    }

    private void ExitVehicle()
    {
        isDriving = false;

        // Hide exit prompt
        SetVisible(exitPromptElement, false);

        // Disable car movement and zero residual velocity
        if (carController != null)
        {
            carController.isBeingDriven = false;
            carController.enabled = false;
            if (carController.TryGetComponent<Rigidbody>(out var carRb))
            {
                carRb.linearVelocity = Vector3.zero;
                carRb.angularVelocity = Vector3.zero;
            }
        }

        // Disable car camera
        if (carCameraFollow != null)
        {
            carCameraFollow.enabled = false;
        }

        // Respawn player cleanly standing beside car (elevated by 1.2m so capsule never sinks in ground)
        if (playerObject != null)
        {
            Vector3 spawnPos;
            Quaternion spawnRot;

            if (exitPoint != null)
            {
                spawnPos = exitPoint.position;
                spawnRot = exitPoint.rotation;
            }
            else
            {
                Transform carT = carController != null ? carController.transform : transform;
                spawnPos = carT.position - carT.right * 2.5f;
                spawnRot = carT.rotation;
            }

            // Offset upwards so center of player capsule sits above ground
            spawnPos.y += 1.2f;

            playerObject.transform.position = spawnPos;
            playerObject.transform.rotation = spawnRot;

            if (playerObject.TryGetComponent<Rigidbody>(out var playerRb))
            {
                playerRb.linearVelocity = Vector3.zero;
                playerRb.angularVelocity = Vector3.zero;
            }

            playerObject.SetActive(true);
        }
    }

    private void SetVisible(VisualElement element, bool visible)
    {
        if (element != null)
        {
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            element.style.visibility = visible ? Visibility.Visible : Visibility.Hidden;
        }
    }
}