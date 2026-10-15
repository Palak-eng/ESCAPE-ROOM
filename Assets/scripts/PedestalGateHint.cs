using UnityEngine;

/// <summary>
/// Put this on a trigger collider around the four pedestals/gate area.
/// Shows the story hint while the player stands near/passes through.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PedestalGateHint : MonoBehaviour
{
    [TextArea]
    public string message = "The door will open only when you find 4 collectibles.";

    [Tooltip("When ON, the hint shows live progress, e.g. 2/4 placed.")]
    public bool showProgress = true;

    [Tooltip("Keep the message visible for this many seconds after player leaves the trigger.")]
    public float hideDelay = 1.5f;

    private float hideAt;
    private bool playerInside;
    private static PedestalGateHint activeHint;

    void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void Update()
    {
        if (!playerInside && hideAt > 0f && Time.time >= hideAt)
        {
            hideAt = 0f;
            if (InteractionUI.Instance != null)
                InteractionUI.Instance.HidePrompt();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
            return;

        playerInside = true;
        activeHint = this;
        hideAt = 0f;
        if (InteractionUI.Instance != null)
            InteractionUI.Instance.ShowPrompt(CurrentMessage());
    }

    private string CurrentMessage()
    {
        if (!showProgress || QuestInventory.Instance == null)
            return message;

        int placed = QuestInventory.Instance.PlacedCount();
        int total = QuestInventory.Instance.totalItems;

        if (PedestalGate.Instance != null && PedestalGate.Instance.IsOpen)
            return "The door stands open. You are free.";

        if (placed >= total)
            return message;

        return message + " (" + placed + "/" + total + " placed)";
    }

    void OnTriggerStay(Collider other)
    {
        // Intentionally empty. Showing every physics frame can stack duplicate-looking prompts.
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
            return;

        playerInside = false;
        if (activeHint == this)
            hideAt = Time.time + hideDelay;
    }

    private bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player") || other.GetComponentInParent<PlayerInteractor>() != null || other.name.Contains("Player");
    }
}
