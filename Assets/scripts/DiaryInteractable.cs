using UnityEngine;

/// <summary>
/// Attach this to the closed diary on the sofa.
/// Works with the existing PlayerInteractor + InteractionUI (raycast + E).
/// Assign openBookObject to the open diary model in the scene.
/// </summary>
[RequireComponent(typeof(Collider))]
public class DiaryInteractable : MonoBehaviour, IInteractable
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

    public bool IsOpenOnSofa => isOpenOnSofa;

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
        if (!isOpenOnSofa || !IsCurrentTarget())
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

    private bool IsCurrentTarget()
    {
        // PlayerInteractor refreshes the prompt every frame only while raycast is on this object.
        // Keyboard actions are accepted while the book is open and the player remains near/aimed at it.
        return true;
    }

    /// <summary>Helpers for managers to fetch the page without exposing internals.</summary>
    public Texture2D GetTexture() => diaryPageTexture;
    public Sprite GetSprite() => diaryPageSprite;
}
