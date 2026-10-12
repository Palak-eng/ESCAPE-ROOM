using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Raycast Settings")]
    public float interactDistance = 3f;
    [SerializeField] private Camera playerCamera;
    public LayerMask interactableLayers = ~0;

    private IInteractable currentTarget;

    void Awake()
    {
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

        bool hasHit = Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactableLayers, QueryTriggerInteraction.Collide);
        Debug.DrawRay(ray.origin, ray.direction * interactDistance, hasHit ? Color.green : Color.red);

        // Check for IInteractable on hit object or its parents
        IInteractable interactable = null;
        if (hasHit)
        {
            interactable = hit.collider.GetComponentInParent<IInteractable>();
        }

        // Handle UI Prompt
        if (interactable != currentTarget)
        {
            currentTarget = interactable;
            if (currentTarget != null && InteractionUI.Instance != null)
            {
                InteractionUI.Instance.ShowPrompt(currentTarget.GetInteractionPrompt());
            }
            else if (InteractionUI.Instance != null)
            {
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
            Debug.Log("<color=green>[PlayerInteractor]</color> Interacted with: " + hit.collider.gameObject.name);
            currentTarget.OnInteract();
        }
        else if (hasHit)
        {
            Debug.Log("<color=yellow>[PlayerInteractor]</color> Looked at: " + hit.collider.gameObject.name + " (Not interactable)");
        }
        else
        {
            Debug.Log("<color=yellow>[PlayerInteractor]</color> Pressed [E], but no object was in range (" + interactDistance + "m).");
        }
    }
}
