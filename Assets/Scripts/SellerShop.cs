using UnityEngine;
using UnityEngine.UIElements;   // UI Toolkit 

public class SellerShop : MonoBehaviour
{
    [Header("References")]
    public UIDocument shopUI;             // The UIDocument object (drag it here)
    public Transform player;              // The player (found automatically if tagged "Player")

    [Header("Settings")]
    public float interactDistance = 3f;   // How close the player must be

    // Names of the items = names of the buttons in the UXML
    private string[] items =
    {
        "Melon", "Apple", "Pear", "Orange",
        "Lettuce", "Potato", "Onion", "Carrot"
    };

    private VisualElement shopPanel;      // The shop window
    private Label hint;                   // The "Press E" message
    private bool isOpen = false;          // Is the shop open?
    private CursorLockMode previousLockMode;

void Start()
{
    // Find the player if not assigned
    if (player == null)
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
        else Debug.LogWarning("No player found: give your player the 'Player' tag!");
    }

    // Get the UI elements from the UXML by their name
    VisualElement root = shopUI.rootVisualElement;
    shopPanel = root.Q<VisualElement>("ShopPanel");
    hint = root.Q<Label>("Hint");

    // Show a clear error if a name doesn't match
    if (shopPanel == null || hint == null)
    {
        Debug.LogError("ShopPanel or Hint not found in the UXML: check the names!");
        return;
    }

    // Hide everything at the start
    shopPanel.style.display = DisplayStyle.None;
    hint.style.display = DisplayStyle.None;

    // Link each button to its click event
    foreach (string item in items)
    {
        string itemName = item; // local copy (needed for the click event)
        Button button = root.Q<Button>(itemName);

        if (button == null)
        {
            Debug.LogError("Button not found in the UXML: " + itemName);
            continue;
        }

        button.clicked += () => BuyItem(itemName);
    }
}

    void Update()
    {
        if (player == null || shopPanel == null) return;
        if (player == null) return;

        // Is the player close enough to the seller?
        bool isNear = Vector3.Distance(player.position, transform.position) <= interactDistance;

        // Show the hint only when near and the shop is closed
        hint.style.display = (isNear && !isOpen) ? DisplayStyle.Flex : DisplayStyle.None;

        // Press E near the seller -> open / close
        if (isNear && UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame)    
        {
            SetShopOpen(!isOpen);
        }

        // Player walked away -> close
        if (!isNear && isOpen)
        {
            SetShopOpen(false);
        }
    }


    void LateUpdate()
{
    if (isOpen)
    {
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
    }
}


    // Open or close the shop
    void SetShopOpen(bool open)
    {
        isOpen = open;
        shopPanel.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;

        if (open)
        {
            // Free the mouse so we can click
            // (UnityEngine.Cursor written in full to avoid the conflict with UIElements.Cursor)
            previousLockMode = UnityEngine.Cursor.lockState;
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
        }
        else
        {
            // Restore the cursor as it was
            UnityEngine.Cursor.lockState = previousLockMode;
            UnityEngine.Cursor.visible = (previousLockMode != CursorLockMode.Locked);
        }
    }

    // Called when an item button is clicked
    void BuyItem(string itemName)
    {
        Debug.Log(itemName + " bought!");
    }
}