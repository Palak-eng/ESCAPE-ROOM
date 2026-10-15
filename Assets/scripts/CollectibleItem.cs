using UnityEngine;

/// <summary>
/// Attach to anything the player can lift. E picks it up into the one-item hand
/// (torch is carried separately). The object is hidden (not destroyed) so quest
/// items can be placed on pedestals later.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CollectibleItem : MonoBehaviour, IInteractable
{
    [Header("Identity")]
    public string itemId = "alarm_clock";
    public string displayName = "Alarm Clock";

    [Header("Type")]
    [Tooltip("ON for the 4 ritual items (alarm clock, book, photograph, key) — these fit the pedestals. OFF for decorative liftables (vase, cup, toy book...): the player can pick them up, but pedestals will reject them.")]
    public bool isQuestItem = true;

    [Header("Difficulty")]
    [Tooltip("OFF = no pickup hint; the player must discover the item and press E themselves. Diary keeps its own hint.")]
    public bool showHint = false;

    private bool collected;

    void Awake()
    {
        FitColliderToMesh();
    }

    /// <summary>
    /// Sizes a BoxCollider to the visible mesh, so aiming works no matter where
    /// the mesh sits relative to the object origin. Fixes hand-edited colliders
    /// that are offset, negative, or room-sized. Runs for hidden items too, the
    /// moment their container (drawer / box) reveals them.
    /// </summary>
    private void FitColliderToMesh()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null)
            box = gameObject.AddComponent<BoxCollider>();

        if (renderers == null || renderers.Length == 0)
            return; // no mesh: keep whatever collider exists

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);

        if (b.size == Vector3.zero)
            return;

        Vector3 lossy = transform.lossyScale;
        Vector3 localSize = new Vector3(
            lossy.x != 0f ? b.size.x / Mathf.Abs(lossy.x) : b.size.x,
            lossy.y != 0f ? b.size.y / Mathf.Abs(lossy.y) : b.size.y,
            lossy.z != 0f ? b.size.z / Mathf.Abs(lossy.z) : b.size.z);

        // Minimum grab size (in local units) so tiny items like the key stay hittable.
        Vector3 minLocal = new Vector3(
            lossy.x != 0f ? 0.12f / Mathf.Abs(lossy.x) : 0.12f,
            lossy.y != 0f ? 0.12f / Mathf.Abs(lossy.y) : 0.12f,
            lossy.z != 0f ? 0.12f / Mathf.Abs(lossy.z) : 0.12f);
        localSize = new Vector3(
            Mathf.Max(localSize.x * 1.15f, minLocal.x),
            Mathf.Max(localSize.y * 1.15f, minLocal.y),
            Mathf.Max(localSize.z * 1.15f, minLocal.z));

        box.center = transform.InverseTransformPoint(b.center);
        box.size = localSize;
        box.isTrigger = false;
        box.enabled = true;
    }

    public string GetInteractionPrompt()
    {
        if (collected || (QuestInventory.Instance != null && QuestInventory.Instance.Has(itemId)))
            return showHint ? displayName + " collected" : null;

        // Hands-full is state feedback, not a discovery hint: always show it,
        // otherwise the player gets no response and thinks pickup is broken.
        if (QuestInventory.Instance != null && QuestInventory.Instance.HandsFull())
            return "Hands full: holding " + QuestInventory.Instance.HeldEntry().displayName;

        // No hint: null tells PlayerInteractor / InteractionUI to show nothing.
        // E pickup still works when the player aims at the item.
        if (!showHint)
            return null;

        return "Press E to collect: " + displayName;
    }

    public void OnInteract()
    {
        Collect();
    }

    private void Collect()
    {
        if (collected) return;
        if (QuestInventory.Instance != null && QuestInventory.Instance.Has(itemId)) return;

        // Only one thing in hand. Torch is carried separately and never blocks this.
        if (QuestInventory.Instance != null && QuestInventory.Instance.HandsFull())
        {
            if (InteractionUI.Instance != null)
                InteractionUI.Instance.ShowPrompt("Hands full: holding " + QuestInventory.Instance.HeldEntry().displayName);
            return;
        }

        collected = true;

        if (QuestInventory.Instance != null)
        {
            QuestInventory.Instance.Add(itemId, displayName, gameObject, isQuestItem);
            QuestInventory.Instance.Hold(itemId);
        }

        if (InteractionUI.Instance != null)
            InteractionUI.Instance.ShowPrompt("Collected " + displayName);

        gameObject.SetActive(false);
    }

    /// <summary>Called when the item is sent back to its original spot. Makes it collectible again.</summary>
    public void ResetForReturn()
    {
        collected = false;
    }
}
