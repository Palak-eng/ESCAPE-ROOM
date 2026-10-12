using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Raycast Settings")]
    public float interactDistance = 3f;
    [SerializeField] private Camera playerCamera;
    public LayerMask interactableLayers = ~0;

    void Awake()
    {
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
        if (playerCamera == null)
        {
            FindCamera();
        }

        Transform camTransform = playerCamera != null ? playerCamera.transform : transform;
        Ray ray = new Ray(camTransform.position, camTransform.forward);

        // Visual debug line in Scene view (Green = hit, Red = miss)
        bool hasHit = Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactableLayers, QueryTriggerInteraction.Collide);
        Debug.DrawRay(ray.origin, ray.direction * interactDistance, hasHit ? Color.green : Color.red);

        if (Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame)
            return;

        if (hasHit)
        {
            Debug.Log("<color=green>[PlayerInteractor]</color> Interacted with: " + hit.collider.gameObject.name);
        }
        else
        {
            Debug.Log("<color=yellow>[PlayerInteractor]</color> Pressed [E], but no object was in range (" + interactDistance + "m).");
        }
    }
}
