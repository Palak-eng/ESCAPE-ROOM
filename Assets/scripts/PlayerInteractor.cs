using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Raycast Settings")]
    public float interactDistance = 3f;
    [SerializeField] private Camera playerCamera;
    public LayerMask interactableLayers = ~0;

    private IInteractable currentTarget;

    public static PlayerInteractor Instance { get; private set; }

    public IInteractable CurrentTarget => currentTarget;

    void Awake()
    {
        Instance = this;

        if (InteractionUI.Instance == null)
        {
            GameObject ui = new GameObject("InteractionUI");
            ui.AddComponent<InteractionUI>();
        }

        FindCamera();
    }

    private void FindCamera()
    {
        if (playerCamera == null)
        {
            playerCamera = GetComponent<Camera>();
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }
        }
    }

    void Update()
    {
        // Don't raycast / interact while reading diary or journal is open
        if (DiaryUIManager.Instance != null && DiaryUIManager.Instance.IsOpen)
            return;
        if (CollectiblesManager.Instance != null && CollectiblesManager.Instance.JournalOpen)
            return;
        if (DiaryUIManager.Instance != null && DiaryUIManager.Instance.ShouldIgnoreInteract())
            return;

        if (playerCamera == null)
        {
            FindCamera();
        }

        Transform camTransform = playerCamera != null ? playerCamera.transform : transform;
        Ray ray = new Ray(camTransform.position, camTransform.forward);

        // Penetrating raycast: check every hit along the ray (nearest first) and take
        // the first INTERACTABLE one. Furniture/shelves in front no longer hide items.
        RaycastHit[] hits = Physics.RaycastAll(ray, interactDistance, interactableLayers, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        bool hasHit = hits.Length > 0;
        RaycastHit hit = hasHit ? hits[0] : new RaycastHit();
        Debug.DrawRay(ray.origin, ray.direction * interactDistance, hasHit ? Color.green : Color.red);

        // Check for IInteractable on hit object or its parents.
        // Hits that defer (invisible closed shells used as close handles) yield to
        // visible interactables behind them, but the first one stays as fallback
        // so the hidden object remains closable/targetable.
        IInteractable interactable = null;
        string hitName = null;
        IInteractable hiddenFallback = null;
        RaycastHit hiddenFallbackHit = new RaycastHit();
        string hiddenFallbackName = null;
        foreach (RaycastHit h in hits)
        {
            if (h.collider == null) continue;
            if (hitName == null) hitName = h.collider.gameObject.name;
            IInteractable found = h.collider.GetComponentInParent<IInteractable>();
            if (found == null) continue;
            if (found is IDeferTargeting deferring && deferring.DeferTargeting)
            {
                if (hiddenFallback == null)
                {
                    hiddenFallback = found;
                    hiddenFallbackHit = h;
                    hiddenFallbackName = h.collider.gameObject.name;
                }
                continue;
            }
            interactable = found;
            hit = h;
            hitName = h.collider.gameObject.name;
            break;
        }
        if (interactable == null && hiddenFallback != null)
        {
            interactable = hiddenFallback;
            hit = hiddenFallbackHit;
            hitName = hiddenFallbackName;
        }

        // Handle UI Prompt (null / empty prompt = show nothing, e.g. hidden pickups)
        if (interactable != currentTarget)
        {
            currentTarget = interactable;
            if (InteractionUI.Instance != null)
            {
                if (currentTarget != null)
                    InteractionUI.Instance.ShowPrompt(currentTarget.GetInteractionPrompt());
                else
                    InteractionUI.Instance.HidePrompt();
            }
        }
        else if (currentTarget != null && InteractionUI.Instance != null)
        {
            // Refresh prompt (e.g. "Press E to open" -> "Press E to read again" after collect)
            InteractionUI.Instance.ShowPrompt(currentTarget.GetInteractionPrompt());
        }

        // Handle Interaction Input [E]
        if (Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame)
            return;

        if (currentTarget != null)
        {
            Debug.Log("<color=green>[PlayerInteractor]</color> Interacted with: " + hitName);
            currentTarget.OnInteract();
        }
        else if (hasHit)
        {
            Debug.Log("<color=yellow>[PlayerInteractor]</color> Looked at: " + hitName + " (Not interactable)");
        }
        else
        {
            Debug.Log("<color=yellow>[PlayerInteractor]</color> Pressed [E], but no object was in range (" + interactDistance + "m).");
        }
    }
}
