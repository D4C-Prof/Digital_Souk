using UnityEngine;

public class SellerInteraction : MonoBehaviour
{
    [Header("References")]
    public GameObject shopMenu;      // The shop UI panel (drag it here)
    public GameObject interactHint;  // Optional: "Press E" text (can be left empty)

    private bool playerInRange = false;  // Is the player close to the seller?

    void Start()
    {
        // Make sure the menu is closed at the start
        if (shopMenu != null) shopMenu.SetActive(false);
        if (interactHint != null) interactHint.SetActive(false);
    }

    void Update()
    {
        // Open / close the shop when pressing E near the seller
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            bool isOpen = shopMenu.activeSelf;
            shopMenu.SetActive(!isOpen);   // toggle open/close
        }
    }

    // Player enters the seller's trigger zone
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            if (interactHint != null) interactHint.SetActive(true);
        }
    }

    // Player leaves the zone -> close everything
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            shopMenu.SetActive(false);
            if (interactHint != null) interactHint.SetActive(false);
        }
    }
}