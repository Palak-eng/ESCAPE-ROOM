using UnityEngine;

/// <summary>
/// Attach to the torch object in the scene.
/// Player looks at it and presses E, then this torch follows the player camera and its light turns on.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TorchPickup : MonoBehaviour, IInteractable
{
    public string prompt = "Press E to collect the torch";
    public string collectedMessage = "Torch collected";

    [Header("Carry Setup")]
    [Tooltip("Optional: where the torch appears relative to camera after pickup.")]
    public Vector3 carriedLocalPosition = new Vector3(0.32f, -0.12f, 0.55f);

    [Tooltip("Optional: rotate the carried torch in the player's hand.")]
    public Vector3 carriedLocalEulerAngles = new Vector3(10f, -8f, 0f);

    [Tooltip("Leave at (0,0,0) to keep the torch's original visible world size after pickup.")]
    public Vector3 carriedLocalScale = Vector3.zero;

    [Header("Torch Light")]
    [Tooltip("Drag your Point Light here. It will stay off until pickup, then turn on and follow the player with the torch.")]
    public Light torchLight;

    [Tooltip("If no Torch Light is assigned, script creates a fallback light.")]
    public bool createLightIfMissing = true;

    public float lightRange = 20f;
    public float lightIntensity = 2.5f;
    public float spotAngle = 55f;
    public Color lightColor = new Color(1f, 0.86f, 0.62f);

    [Header("Optional")]
    public bool disableColliderAfterPickup = true;

    private static bool hasTorch;
    private Vector3 originalWorldScale;

    /// <summary>Allows the torch to be collected again after Play Again.</summary>
    public static void ResetRun() { hasTorch = false; }

    /// <summary>True once the player carries the torch (used for effects like the diary glow).</summary>
    public static bool HasTorch => hasTorch;

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col == null)
            col = gameObject.AddComponent<BoxCollider>();
        col.isTrigger = false;
        originalWorldScale = transform.lossyScale;

        if (torchLight == null)
            torchLight = GetComponentInChildren<Light>(true);

        if (torchLight != null)
            torchLight.enabled = false;
    }

    public string GetInteractionPrompt()
    {
        return hasTorch ? "Torch already collected" : prompt;
    }

    public void OnInteract()
    {
        if (hasTorch)
            return;

        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[TorchPickup] No MainCamera found. Make sure player camera is tagged MainCamera.");
            return;
        }

        hasTorch = true;
        AttachTorchToPlayer(cam.transform);

        if (InteractionUI.Instance != null)
            InteractionUI.Instance.ShowPrompt(collectedMessage);
    }

    private void AttachTorchToPlayer(Transform cameraTransform)
    {
        transform.SetParent(cameraTransform, false);
        transform.localPosition = carriedLocalPosition;
        transform.localRotation = Quaternion.Euler(carriedLocalEulerAngles);
        transform.localScale = carriedLocalScale == Vector3.zero ? GetLocalScaleForWorldSize(originalWorldScale, cameraTransform) : carriedLocalScale;

        if (disableColliderAfterPickup)
        {
            foreach (Collider col in GetComponentsInChildren<Collider>())
                col.enabled = false;
        }

        if (torchLight == null && createLightIfMissing)
        {
            GameObject lightObject = new GameObject("TorchLight");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.localPosition = Vector3.zero;
            lightObject.transform.localRotation = Quaternion.identity;
            torchLight = lightObject.AddComponent<Light>();
            torchLight.type = LightType.Spot;
        }

        if (torchLight == null)
            return;

        torchLight.gameObject.SetActive(true);
        torchLight.range = lightRange;
        torchLight.intensity = lightIntensity;
        if (torchLight.type == LightType.Spot)
            torchLight.spotAngle = spotAngle;
        torchLight.color = lightColor;
        // No shadows on the hand torch: the drawer/cabinet lips would otherwise
        // throw the interiors you are trying to search into darkness.
        torchLight.shadows = LightShadows.None;
        torchLight.enabled = true;

        DontDestroyOnLoad(gameObject);
    }

    private Vector3 GetLocalScaleForWorldSize(Vector3 targetWorldScale, Transform parent)
    {
        Vector3 parentScale = parent.lossyScale;
        return new Vector3(
            parentScale.x != 0f ? targetWorldScale.x / parentScale.x : targetWorldScale.x,
            parentScale.y != 0f ? targetWorldScale.y / parentScale.y : targetWorldScale.y,
            parentScale.z != 0f ? targetWorldScale.z / parentScale.z : targetWorldScale.z
        );
    }
}
