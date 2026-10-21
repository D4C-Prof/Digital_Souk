using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(Collider))]
public class InteractionZoneTrigger : MonoBehaviour
{
    // Tag used to identify the player object
    [Tooltip("Tag assigned to the player GameObject.")]
    public string playerTag = "Player";

    // Reference to the GameObject that holds the UIDocument for the tooltip
    [Tooltip("GameObject containing the UIDocument component for the interaction tooltip.")]
    public GameObject interactionToolTip;

    private UIDocument uiDocument;

    private void Awake()
    {
        // Ensure the collider acts as a trigger so the player can pass through
        var col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // If the reference is not set in the inspector, try to find it by name
        if (interactionToolTip == null)
        {
            interactionToolTip = GameObject.Find("interactionToolTip");
        }

        if (interactionToolTip != null)
        {
            uiDocument = interactionToolTip.GetComponent<UIDocument>();
            // Hide the tooltip initially
            SetTooltipVisible(false);
        }
        else
        {
            Debug.LogWarning("InteractionZoneTrigger: Could not find GameObject named 'interactionToolTip'. Tooltip will not be shown.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            SetTooltipVisible(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            SetTooltipVisible(false);
        }
    }

    private void SetTooltipVisible(bool visible)
    {
        if (uiDocument != null)
        {
            // Enabling/disabling the UIDocument component toggles the UI visibility
            uiDocument.enabled = visible;
            // Alternative: manipulate root element directly
            // var root = uiDocument.rootVisualElement;
            // root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
