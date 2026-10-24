using UnityEngine;

public class TeleportPlayer : MonoBehaviour
{
    [Header("Destination")]
    [Tooltip("Target destination to teleport to. Automatically finds 'teleport-toshell' if left empty.")]
    public Transform destination;

    [Tooltip("Height offset so player appears safely on top of the destination cube.")]
    public Vector3 spawnOffset = new Vector3(0f, 1f, 0f);

    private void Start()
    {
        // If no destination is assigned in Inspector, search for "teleport-toshell"
        if (destination == null)
        {
            GameObject target = GameObject.Find("teleport-toshell");
            if (target != null)
            {
                destination = target.transform;
            }
            else
            {
                Debug.LogWarning("TeleportPlayer: 'teleport-toshell' not found in scene!");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryTeleport(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryTeleport(collision.gameObject);
    }

    private void TryTeleport(GameObject obj)
    {
        // Check if the object entering is the Player
        if (obj.CompareTag("Player") || obj.GetComponent<movement>() != null)
        {
            if (destination != null)
            {
                Vector3 targetPos = destination.position + spawnOffset;

                // Temporarily disable CharacterController if present
                CharacterController cc = obj.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                // Move the player to the destination
                obj.transform.position = targetPos;

                // Reset physics velocity if using a Rigidbody so player doesn't slide/fly
                Rigidbody rb = obj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.position = targetPos;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                if (cc != null) cc.enabled = true;

                Debug.Log("Teleported player to: " + destination.name);
            }
        }
    }
}
