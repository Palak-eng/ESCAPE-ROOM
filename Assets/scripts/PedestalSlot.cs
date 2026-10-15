using System.Collections;
using UnityEngine;

/// <summary>
/// Attach to each pedestal. Player looks at an empty pedestal and presses E to place
/// the item they are currently holding. Quest items sink the pedestal; anything else
/// is rejected with "This is not the right item."
/// </summary>
[RequireComponent(typeof(Collider))]
public class PedestalSlot : MonoBehaviour, IInteractable
{
    [Header("Placement")]
    [Tooltip("Where the placed collectible sits. Defaults to this pedestal.")]
    public Transform itemAnchor;

    [Tooltip("Fallback height above the pedestal base when the mesh top can't be measured. Normally the top is measured from the pedestal mesh automatically.")]
    public float placeHeight = 1.0f;

    [Tooltip("Extra lift above the measured pedestal top surface.")]
    public float topLift = 0.14f;

    [Header("Sink Animation")]
    public float sinkDistance = 0.45f;
    public float sinkDuration = 0.8f;

    [Header("Audio")]
    [Tooltip("Drag Assets/music/rumble_sound.mp3 here. Plays from the pedestal while it sinks.")]
    public AudioClip rumbleSound;

    [Range(0f, 1f)]
    public float rumbleVolume = 1f;

    [Header("Prompts")]
    public string promptEmpty = "Press E to place";
    public string promptFilled = "A collectible is already placed here";
    public string promptNoItems = "Pick up an item, then press E here";
    public string promptWrongItem = "This is not the right item.";

    public bool IsFilled { get; private set; }

    private Vector3 startLocalPosition;
    private AudioSource rumbleSource;

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) gameObject.AddComponent<BoxCollider>();

        // The anchor must ride the pedestal down. Anything outside this object
        // would leave the placed item floating behind, so fall back to the pedestal.
        if (itemAnchor == null || (itemAnchor != transform && !itemAnchor.IsChildOf(transform)))
        {
            if (itemAnchor != null)
                Debug.LogWarning("[PedestalSlot] Item Anchor on '" + gameObject.name + "' is outside the pedestal and would not sink with it. Using the pedestal itself.", this);
            itemAnchor = transform;
        }
        startLocalPosition = transform.localPosition;

        rumbleSource = GetComponent<AudioSource>();
        if (rumbleSource == null)
            rumbleSource = gameObject.AddComponent<AudioSource>();
        rumbleSource.playOnAwake = false;
        rumbleSource.spatialBlend = 1f;
        rumbleSource.minDistance = 1f;
        rumbleSource.maxDistance = 18f;
    }

    public string GetInteractionPrompt()
    {
        if (IsFilled) return promptFilled;

        QuestInventory inv = QuestInventory.Instance;
        QuestInventory.Entry held = inv != null ? inv.HeldEntry() : null;
        if (held == null) return promptNoItems;

        return promptEmpty + " " + held.displayName;
    }

    public void OnInteract()
    {
        if (IsFilled) return;

        QuestInventory inv = QuestInventory.Instance;
        if (inv == null) return;

        QuestInventory.Entry held = inv.HeldEntry();
        if (held == null) return;

        if (!held.isQuestItem || held.placed)
        {
            string wrongName = held.displayName;
            // Wrong object: hands learn it, and it goes back to its original position.
            inv.ReturnHeldToOrigin();
            if (InteractionUI.Instance != null)
                InteractionUI.Instance.ShowPrompt(promptWrongItem + " (" + wrongName + " returned)");
            return;
        }

        Place(inv, held);
    }

    private void Place(QuestInventory inv, QuestInventory.Entry entry)
    {
        IsFilled = true;
        entry.placed = true;
        inv.ClearHeld();

        GameObject item = entry.worldObject;
        if (item != null)
        {
            item.SetActive(true);

            foreach (Collider c in item.GetComponentsInChildren<Collider>(true))
                c.enabled = false;

            Vector3 worldScale = item.transform.lossyScale;
            // Measure first: after parenting, the item's own mesh would pollute the bounds.
            Vector3 targetLocal = PlacementLocalPos(item);
            item.transform.SetParent(itemAnchor, false);
            item.transform.localRotation = Quaternion.identity;
            item.transform.localScale = LocalScaleForWorldSize(worldScale, itemAnchor);
            item.transform.localPosition = targetLocal;
        }

        if (InteractionUI.Instance != null)
            InteractionUI.Instance.ShowPrompt(entry.displayName + " placed");

        StartCoroutine(SinkRoutine());
    }

    private IEnumerator SinkRoutine()
    {
        PlayRumble();

        Vector3 from = transform.localPosition;
        Vector3 to = startLocalPosition + Vector3.down * sinkDistance;

        float t = 0f;
        while (t < sinkDuration)
        {
            t += Time.deltaTime;
            transform.localPosition = Vector3.Lerp(from, to, Mathf.Clamp01(t / sinkDuration));
            yield return null;
        }
        transform.localPosition = to;
    }

    private void PlayRumble()
    {
        if (rumbleSound == null || rumbleSource == null) return;
        rumbleSource.clip = rumbleSound;
        rumbleSource.volume = rumbleVolume;
        rumbleSource.Play();
    }

    /// <summary>
    /// Local position on the anchor that sits centred on the pedestal MESH
    /// (not the object root, which can sit offset from the visible column),
    /// resting the item's bottom on the top surface. Falls back to placeHeight
    /// when no mesh renderer exists.
    /// </summary>
    private Vector3 PlacementLocalPos(GameObject item)
    {
        Renderer ped = GetComponentInChildren<Renderer>();
        if (ped == null)
            return new Vector3(0f, placeHeight, 0f);

        Bounds pb = ped.bounds;
        float y = pb.max.y + topLift;

        // Models are usually centre-pivoted: lift by the item's half height so
        // its bottom rests on the surface instead of sinking halfway in.
        Renderer[] itemRs = item.GetComponentsInChildren<Renderer>(true);
        if (itemRs != null && itemRs.Length > 0)
        {
            Bounds ib = itemRs[0].bounds;
            for (int i = 1; i < itemRs.Length; i++)
                ib.Encapsulate(itemRs[i].bounds);
            y += ib.extents.y;
        }

        Vector3 worldTarget = new Vector3(pb.center.x, y, pb.center.z);
        return itemAnchor.InverseTransformPoint(worldTarget);
    }

    private Vector3 LocalScaleForWorldSize(Vector3 targetWorldScale, Transform parent)
    {
        Vector3 p = parent.lossyScale;
        return new Vector3(
            p.x != 0f ? targetWorldScale.x / p.x : targetWorldScale.x,
            p.y != 0f ? targetWorldScale.y / p.y : targetWorldScale.y,
            p.z != 0f ? targetWorldScale.z / p.z : targetWorldScale.z);
    }
}
