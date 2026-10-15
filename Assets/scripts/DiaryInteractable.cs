using UnityEngine;

/// <summary>
/// Attach this to the closed diary on the sofa.
/// Works with the existing PlayerInteractor + InteractionUI (raycast + E).
/// Assign openBookObject to the open diary model in the scene.
/// </summary>
[RequireComponent(typeof(Collider))]
public class DiaryInteractable : MonoBehaviour, IInteractable, IDeferTargeting
{
    [Header("Diary Identity")]
    [Tooltip("Unique id, e.g. diary_blackwood_1. Used by Collectibles journal.")]
    public string diaryId = "diary_1";

    [Tooltip("Shown in reading UI header and journal.")]
    public string diaryTitle = "Old Diary";

    [Header("Diary Page (HD)")]
    [Tooltip("Drag Assets/images/diary_image_without_background.png here. Works as Texture2D, no Sprite conversion needed.")]
    public Texture2D diaryPageTexture;

    [Tooltip("Optional: if you set image to Sprite type, assign Sprite here instead.")]
    public Sprite diaryPageSprite;

    [Header("Sofa Book Models")]
    [Tooltip("Closed book model that is visible before interaction. Defaults to this object.")]
    public GameObject closedBookObject;

    [Tooltip("Open book model that appears on sofa after pressing E.")]
    public GameObject openBookObject;

    [Header("Prompts")]
    public string promptBeforeCollect = "Press E to interact with the book";
    public string promptWhileOpen = "Press R to read in full mode  |  Esc to add to collection";
    public string promptAfterCollect = "Press E to read again";

    private bool isOpenOnSofa;
    private Renderer[] closedRenderers;
    private Collider[] openColliders;

    [Header("Range")]
    [Tooltip("R / Esc only work while the player is within this distance of the book.")]
    public float sofaInteractRange = 4f;

    private Transform playerTransform;

    public bool IsOpenOnSofa => isOpenOnSofa;

    /// <summary>While open on the sofa (renderers off), defer to anything visible behind.</summary>
    public bool DeferTargeting => isOpenOnSofa;

    void Reset()
    {
        // Auto-generate a stable id from object name if left default
        if (diaryId == "diary_1" || string.IsNullOrEmpty(diaryId))
            diaryId = gameObject.name.ToLower().Replace(" ", "_").Replace("(", "").Replace(")", "") + "_" + GetInstanceID();
    }

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col == null)
            gameObject.AddComponent<BoxCollider>();

        if (closedBookObject == null)
            closedBookObject = gameObject;

        if (openBookObject == null && gameObject.name == "Diary_Closed")
            openBookObject = GameObject.Find("diary_open");

        closedRenderers = closedBookObject.GetComponentsInChildren<Renderer>(true);

        FitColliderToMesh();

        if (openBookObject != null)
        {
            openColliders = openBookObject.GetComponentsInChildren<Collider>(true);
            foreach (Collider openCollider in openColliders)
                if (openCollider != null)
                    openCollider.enabled = false;
            openBookObject.SetActive(false);
        }

        if (string.IsNullOrEmpty(diaryId))
            diaryId = gameObject.name + "_" + GetInstanceID();
    }

    void Update()
    {
        if (!isOpenOnSofa)
            return;

        if (!IsPlayerInRange())
            return;

        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null)
            return;

        if (kb.rKey.wasPressedThisFrame)
        {
            if (DiaryUIManager.Instance != null)
                DiaryUIManager.Instance.OpenDiary(diaryId, diaryTitle, diaryPageTexture, diaryPageSprite, this);
        }
        else if (kb.escapeKey.wasPressedThisFrame)
        {
            AddToCollectionAndReset();
        }
    }

    public string GetInteractionPrompt()
    {
        if (isOpenOnSofa)
            return promptWhileOpen;
        if (CollectiblesManager.Instance != null && CollectiblesManager.Instance.Has(diaryId))
            return promptAfterCollect;
        return promptBeforeCollect;
    }

    public void OnInteract()
    {
        if (DiaryUIManager.Instance != null && DiaryUIManager.Instance.IsOpen)
            return;

        if (!isOpenOnSofa)
        {
            OpenOnSofa();
            return;
        }

        if (DiaryUIManager.Instance != null)
            DiaryUIManager.Instance.OpenDiary(diaryId, diaryTitle, diaryPageTexture, diaryPageSprite, this);
        else
            Debug.LogWarning("[Diary] DiaryUIManager not in scene. Create one: GameObject -> Create Empty -> Add DiaryUIManager.");
    }

    public void AddToCollectionAndReset()
    {
        if (CollectiblesManager.Instance != null)
            CollectiblesManager.Instance.Collect(this);

        isOpenOnSofa = false;
        SetClosedVisible(true);
        if (openBookObject != null)
            openBookObject.SetActive(false);
    }

    private void OpenOnSofa()
    {
        isOpenOnSofa = true;
        SetClosedVisible(false);
        if (openBookObject != null)
            openBookObject.SetActive(true);
        else if (DiaryUIManager.Instance != null)
            DiaryUIManager.Instance.OpenDiary(diaryId, diaryTitle, diaryPageTexture, diaryPageSprite, this);
    }

    private void SetClosedVisible(bool visible)
    {
        foreach (Renderer renderer in closedRenderers)
            if (renderer != null)
                renderer.enabled = visible;
    }

    private bool IsPlayerInRange()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                player = GameObject.Find("PlayerCapsule");
            if (player != null)
                playerTransform = player.transform;
            else
                return true;
        }

        float dist = Vector3.Distance(playerTransform.position, transform.position);
        return dist <= sofaInteractRange;
    }

    /// <summary>
    /// Sizes the BoxCollider to the closed book mesh, overwriting hand-dragged
    /// monsters (a 100m diary collider once shadowed the whole room).
    /// </summary>
    private void FitColliderToMesh()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null || closedRenderers == null || closedRenderers.Length == 0)
            return;

        Bounds b = new Bounds();
        bool init = false;
        foreach (Renderer r in closedRenderers)
        {
            if (r == null) continue;
            if (!init) { b = r.bounds; init = true; }
            else b.Encapsulate(r.bounds);
        }
        if (!init || b.size == Vector3.zero)
            return;

        Vector3 lossy = transform.lossyScale;
        Vector3 localSize = new Vector3(
            lossy.x != 0f ? b.size.x / Mathf.Abs(lossy.x) : b.size.x,
            lossy.y != 0f ? b.size.y / Mathf.Abs(lossy.y) : b.size.y,
            lossy.z != 0f ? b.size.z / Mathf.Abs(lossy.z) : b.size.z);
        localSize = new Vector3(
            Mathf.Max(localSize.x * 1.15f, 0.12f),
            Mathf.Max(localSize.y * 1.15f, 0.12f),
            Mathf.Max(localSize.z * 1.15f, 0.12f));

        box.center = transform.InverseTransformPoint(b.center);
        box.size = localSize;
        box.enabled = true;
    }

    /// <summary>Helpers for managers to fetch the page without exposing internals.</summary>
    public Texture2D GetTexture() => diaryPageTexture;
    public Sprite GetSprite() => diaryPageSprite;
}
