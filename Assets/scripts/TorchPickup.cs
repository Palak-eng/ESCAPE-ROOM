using UnityEngine;

/// <summary>
/// Attach to the torch object in the scene.
/// Player looks at it and presses E, then a carried light is attached to the camera.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TorchPickup : MonoBehaviour, IInteractable
{
    public string prompt = "Press E to collect the torch";
    public string collectedMessage = "Torch collected";

    [Header("Torch Light")]
    public float lightRange = 12f;
    public float lightIntensity = 2.5f;
    public float spotAngle = 55f;
    public Color lightColor = new Color(1f, 0.86f, 0.62f);

    [Header("Optional")]
    public bool hideWorldTorchAfterPickup = true;

    private static bool hasTorch;

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = false;
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
        CreateCarriedTorch(cam.transform);

        if (InteractionUI.Instance != null)
            InteractionUI.Instance.ShowPrompt(collectedMessage);

        if (hideWorldTorchAfterPickup)
            gameObject.SetActive(false);
    }

    private void CreateCarriedTorch(Transform cameraTransform)
    {
        GameObject existing = GameObject.Find("PlayerCarriedTorchLight");
        if (existing != null)
            return;

        GameObject lightObject = new GameObject("PlayerCarriedTorchLight");
        lightObject.transform.SetParent(cameraTransform, false);
        lightObject.transform.localPosition = new Vector3(0.25f, -0.2f, 0.35f);
        lightObject.transform.localRotation = Quaternion.identity;

        Light torchLight = lightObject.AddComponent<Light>();
        torchLight.type = LightType.Spot;
        torchLight.range = lightRange;
        torchLight.intensity = lightIntensity;
        torchLight.spotAngle = spotAngle;
        torchLight.color = lightColor;
        torchLight.shadows = LightShadows.Soft;

        DontDestroyOnLoad(lightObject);
    }
}
