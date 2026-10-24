
using UnityEngine;
using UnityEngine.AI;
using TMPro;

public class AdminAreaGuard : MonoBehaviour
{
    public NavMeshAgent admin;
    public Transform player;
    public Transform exitPoint;
    public TMP_Text warningText;

    public float catchDistance = 1.2f;
    public float pushSpeed = 2.5f;

    private Vector3 startPosition;
    private bool inside = false;
    private bool pushing = false;

    private CharacterController cc;
    private Rigidbody rb;

    void Start()
    {
        startPosition = admin.transform.position;

        cc = player.GetComponent<CharacterController>();
        rb = player.GetComponent<Rigidbody>();

        if (warningText != null)
            warningText.gameObject.SetActive(false);
    }

    bool IsPlayer(Collider other)
    {
        return other.transform == player ||
               other.transform.IsChildOf(player);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;

        inside = true;
        pushing = false;

        if (warningText != null)
        {
            warningText.text = "STOP! Workers only!";
            warningText.gameObject.SetActive(true);
        }
    }

    void Update()
    {
        if (!inside || !admin.isOnNavMesh)
            return;

        // STEP 1: CHASE
        if (!pushing)
        {
            admin.isStopped = false;
            admin.SetDestination(player.position);

            Vector3 gap = player.position - admin.transform.position;
            gap.y = 0f;

            if (gap.magnitude <= catchDistance)
            {
                pushing = true;
                admin.ResetPath();
            }
        }

        // STEP 2: PUSH
        // CharacterController or simple Transform movement
        if (pushing && (cc != null || rb == null))
        {
            Push(Time.deltaTime);
        }
    }

    void FixedUpdate()
    {
        // Rigidbody movement
        if (inside && pushing && cc == null && rb != null)
        {
            Push(Time.fixedDeltaTime);
        }
    }

    void Push(float deltaTime)
    {
        Vector3 direction = exitPoint.position - player.position;
        direction.y = 0f;

        if (direction.magnitude < 0.2f)
        {
            ReturnHome();
            return;
        }

        Vector3 movement = Vector3.ClampMagnitude(
            direction, pushSpeed * deltaTime
        );

        // Move the player
        if (cc != null)
        {
            Vector3 before = player.position;
            cc.Move(movement);
            movement = player.position - before;
            movement.y = 0f;
        }
        else if (rb != null)
        {
            rb.MovePosition(rb.position + movement);
        }
        else
        {
            player.position += movement;
        }

        // Guard moves WITH the player
        admin.Move(movement);
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other)) return;

        ReturnHome();
    }

    void ReturnHome()
    {
        inside = false;
        pushing = false;

        if (warningText != null)
            warningText.gameObject.SetActive(false);

        if (admin.isOnNavMesh)
        {
            admin.isStopped = false;
            admin.SetDestination(startPosition);
        }
    }
}
