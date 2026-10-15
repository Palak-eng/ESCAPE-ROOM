using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Attach to a drawer. O toggles it open / closed with a slide animation (never hinted).
/// E is reserved for everything else. Optionally reveals a hidden object (e.g. the key).
/// </summary>
[RequireComponent(typeof(Collider))]
public class DrawerInteractable : MonoBehaviour, IInteractable
{
    [Header("Drawer")]
    [Tooltip("The part that slides. Leave empty to slide this object itself. Drag the visible drawer mesh here if this object is just a marker.")]
    public Transform movingPart;

    [Tooltip("Slide direction in the moving part's local space. Flip the sign if it slides the wrong way.")]
    public Vector3 openDirectionLocal = new Vector3(0f, 0f, 1f);

    public float openDistance = 0.35f;
    public float animDuration = 0.5f;

    [Header("Hidden Contents")]
    [Tooltip("Optional: object hidden inside the drawer (e.g. the key). Made visible the first time the drawer opens.")]
    public GameObject revealOnOpen;

    [Header("Open Key")]
    [Tooltip("O opens/closes drawers and boxes. Never shown in hints; the side legend teaches it.")]
    public Key openKey = Key.O;

    public bool IsOpen { get; private set; }

    private Vector3 closedLocalPos;
    private bool animating;

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col == null)
            gameObject.AddComponent<BoxCollider>();

        if (movingPart == null)
            movingPart = transform;

        closedLocalPos = movingPart.localPosition;

        if (revealOnOpen != null)
            revealOnOpen.SetActive(false);
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (!kb[openKey].wasPressedThisFrame) return;

        // Only when the player is actually aiming at this drawer.
        if (PlayerInteractor.Instance == null) return;
        if (PlayerInteractor.Instance.CurrentTarget != (IInteractable)this) return;

        // Don't steal input while diary reader or journal is open.
        if (DiaryUIManager.Instance != null && DiaryUIManager.Instance.IsOpen) return;
        if (CollectiblesManager.Instance != null && CollectiblesManager.Instance.JournalOpen) return;

        Toggle();
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

    private void Toggle()
    {
        if (animating) return;

        if (IsOpen)
            StartCoroutine(MoveDrawer(closedLocalPos, false));
        else
            StartCoroutine(MoveDrawer(closedLocalPos + openDirectionLocal.normalized * openDistance, true));
    }

    private IEnumerator MoveDrawer(Vector3 target, bool opening)
    {
        animating = true;

        Vector3 from = movingPart.localPosition;
        float t = 0f;
        while (t < animDuration)
        {
            t += Time.deltaTime;
            movingPart.localPosition = Vector3.Lerp(from, target, Mathf.Clamp01(t / animDuration));
            yield return null;
        }
        movingPart.localPosition = target;

        IsOpen = opening;
        animating = false;

        if (opening && revealOnOpen != null && !revealOnOpen.activeSelf)
            revealOnOpen.SetActive(true);
    }
}
