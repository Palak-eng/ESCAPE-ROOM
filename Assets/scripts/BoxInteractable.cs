using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Attach to the CLOSED box. O swaps closed <-> open box (never hinted).
/// E is reserved for everything else. The closed box's collider stays the
/// interaction target; the open box's colliders are disabled so they never
/// steal the raycast (except the photo inside, which keeps its own).
/// </summary>
[RequireComponent(typeof(Collider))]
public class BoxInteractable : MonoBehaviour, IInteractable, IDeferTargeting
{
    [Header("Box Models")]
    [Tooltip("Closed box model. Defaults to this object.")]
    public GameObject closedBoxObject;

    [Tooltip("Open box model shown after pressing E (e.g. box_full_open).")]
    public GameObject openBoxObject;

    [Header("Open Key")]
    [Tooltip("O opens/closes drawers and boxes. Never shown in hints; the side legend teaches it.")]
    public Key openKey = Key.O;

    public bool IsOpen { get; private set; }

    /// <summary>While open, the invisible closed shell defers to contents (photo).</summary>
    public bool DeferTargeting => IsOpen;

    private Renderer[] closedRenderers;

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col == null)
            gameObject.AddComponent<BoxCollider>();

        if (closedBoxObject == null)
            closedBoxObject = gameObject;

        // Convenience: going by the scene names.
        if (openBoxObject == null && gameObject.name.ToLower().Contains("closed"))
            openBoxObject = GameObject.Find("box_full_open");

        closedRenderers = closedBoxObject.GetComponentsInChildren<Renderer>(true);

        if (openBoxObject != null)
        {
            // The slot may point at a child mesh (e.g. the lid) rather than the box root.
            // Hiding only a child would leave the photo's branch visible and collectible.
            // The box instance root is parentless in the scene, so climbing to the top
            // safely selects the whole open box. If already the root, this is a no-op.
            if (openBoxObject.transform.parent != null)
                openBoxObject = openBoxObject.transform.root.gameObject;

            // Keep the photo (or other contents) interactive, disable the box shell colliders.
            foreach (Collider c in openBoxObject.GetComponentsInChildren<Collider>(true))
            {
                if (c.GetComponentInParent<CollectibleItem>() == null)
                    c.enabled = false;
            }
            openBoxObject.SetActive(false);
        }
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (!kb[openKey].wasPressedThisFrame) return;

        // Only when the player is actually aiming at this box.
        if (PlayerInteractor.Instance == null) return;
        if (PlayerInteractor.Instance.CurrentTarget != (IInteractable)this) return;

        if (DiaryUIManager.Instance != null && DiaryUIManager.Instance.IsOpen)
            return;
        if (CollectiblesManager.Instance != null && CollectiblesManager.Instance.JournalOpen)
            return;

        IsOpen = !IsOpen;
        SetClosedVisible(!IsOpen);
        if (openBoxObject != null)
            openBoxObject.SetActive(IsOpen);
    }

    public string GetInteractionPrompt()
    {
        // Open key is never hinted (side legend teaches O).
        return null;
    }

    public void OnInteract()
    {
        // E is reserved for interactables (diary, torch, pickups, pedestals).
    }

    private void SetClosedVisible(bool visible)
    {
        if (closedRenderers == null) return;
        foreach (Renderer r in closedRenderers)
            if (r != null)
                r.enabled = visible;
    }
}
