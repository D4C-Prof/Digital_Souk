
using UnityEngine;

public class AutomaticDoor : MonoBehaviour
{
    public Transform player;
    public Transform leftDoor;
    public Transform rightDoor;

    public float openDistance = 1.5f;
    public float speed = 3f;

    private Vector3 leftClosed;
    private Vector3 rightClosed;

    private Vector3 leftOpen;
    private Vector3 rightOpen;

    private bool playerNearby = false;

    void Start()
    {
        // Save original door positions
        leftClosed = leftDoor.localPosition;
        rightClosed = rightDoor.localPosition;

        // Calculate open positions
        leftOpen = leftClosed + Vector3.left * openDistance;
        rightOpen = rightClosed + Vector3.right * openDistance;
    }

    void Update()
    {
        // Choose open or closed positions
        Vector3 leftTarget = playerNearby ? leftOpen : leftClosed;
        Vector3 rightTarget = playerNearby ? rightOpen : rightClosed;

        // Move doors smoothly
        leftDoor.localPosition = Vector3.MoveTowards(
            leftDoor.localPosition,
            leftTarget,
            speed * Time.deltaTime
        );

        rightDoor.localPosition = Vector3.MoveTowards(
            rightDoor.localPosition,
            rightTarget,
            speed * Time.deltaTime
        );
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.transform == player ||
            other.transform.IsChildOf(player))
        {
            playerNearby = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.transform == player ||
            other.transform.IsChildOf(player))
        {
            playerNearby = false;
        }
    }
}
