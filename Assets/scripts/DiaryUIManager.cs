using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Fullscreen book reader.
/// Flow: E on diary opens the model on sofa -> R opens HD clear view -> Esc closes and collects.
/// Auto-creates all UI, locks player movement + frees cursor while reading.
/// </summary>
public class DiaryUIManager : MonoBehaviour
{
    public static DiaryUIManager Instance { get; private set; }

    public bool IsOpen { get; private set; }
    public bool IsClearView { get; private set; }

    // Cooldown so the same E press that closes doesn't instantly re-interact
    public float ignoreInteractUntil;

    [Header("Current diary")]
    public string currentId;
    public string currentTitle;

    private GameObject root;
    private GameObject previewPanel;
    private GameObject clearPanel;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI hintText;
    private TextMeshProUGUI clearHintText;
    private RawImage previewRaw;
    private Image previewImg;
    private RawImage clearRaw;
    private Image clearImg;
    private DiaryInteractable worldDiarySource;

    // player lock
    private readonly List<MonoBehaviour> disabledScripts = new List<MonoBehaviour>();
    private GameObject playerObj;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUI();
        HideAll();
    }

    void Update()
    {
        if (!IsOpen) return;

        // Keep cursor free + hide world prompt while reading
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (InteractionUI.Instance != null) InteractionUI.Instance.HidePrompt();

        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.rKey.wasPressedThisFrame)
        {
            ToggleClearView();
        }
        else if (kb.eKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)
        {
            Close();
        }
    }

    public void OpenDiary(string id, string title, Texture2D tex, Sprite sprite, DiaryInteractable sourceDiary = null)
    {
        currentId = id;
        currentTitle = title;
        worldDiarySource = sourceDiary;

        bool hasSprite = sprite != null;
        bool hasTex = tex != null && !hasSprite;
        if (!hasTex && !hasSprite)
        {
            Debug.LogWarning("[DiaryUI] No page image assigned for '" + title + "'. Assign diaryPageTexture in DiaryInteractable.");
        }

        // Prefer Sprite when available because Unity preserves PNG transparency correctly for UI sprites.
        previewRaw.gameObject.SetActive(hasTex);
        previewImg.gameObject.SetActive(hasSprite);
        if (hasTex) { previewRaw.texture = tex; }
        if (hasSprite) { previewImg.sprite = sprite; previewImg.preserveAspect = true; }

        // clear-view images
        clearRaw.gameObject.SetActive(hasTex);
        clearImg.gameObject.SetActive(hasSprite);
        if (hasTex) { clearRaw.texture = tex; }
        if (hasSprite) { clearImg.sprite = sprite; clearImg.preserveAspect = true; }

        titleText.text = "OPEN  \u2014  " + title;
        IsOpen = true;
        IsClearView = true;
        root.SetActive(true);
        previewPanel.SetActive(false);
        clearPanel.SetActive(true);

        LockPlayer(true);
    }

    public void ToggleClearView()
    {
        IsClearView = !IsClearView;
        previewPanel.SetActive(!IsClearView);
        clearPanel.SetActive(IsClearView);
    }

    public void Close()
    {
        IsOpen = false;
        IsClearView = false;
        HideAll();
        LockPlayer(false);
        if (worldDiarySource != null)
        {
            worldDiarySource.AddToCollectionAndReset();
            worldDiarySource = null;
        }
        // re-lock cursor for FPS controller
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        ignoreInteractUntil = Time.time + 0.35f;
    }

    public bool ShouldIgnoreInteract() => Time.time < ignoreInteractUntil;

    private void HideAll()
    {
        if (root != null) root.SetActive(false);
    }

    // ---------- player lock (generic, works with StarterAssets FirstPerson) ----------
    private void LockPlayer(bool lockIt)
    {
        if (lockIt)
        {
            disabledScripts.Clear();
            playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null)
            {
                // fallback: StarterAssets names it PlayerCapsule
                playerObj = GameObject.Find("PlayerCapsule");
            }
            if (playerObj != null)
            {
                foreach (var mb in playerObj.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    string tn = mb.GetType().Name;
                    if (tn.Contains("Controller") || tn.Contains("Input") || tn.Contains("Look") || tn.Contains("Move"))
                    {
                        if (mb.enabled) { mb.enabled = false; disabledScripts.Add(mb); }
                    }
                }
            }
        }
        else
        {
            foreach (var mb in disabledScripts)
                if (mb != null) mb.enabled = true;
            disabledScripts.Clear();
        }
    }

    // ---------- auto UI ----------
    private void BuildUI()
    {
        root = new GameObject("DiaryUI");
        root.transform.SetParent(transform, false);

        var canvasGo = new GameObject("DiaryCanvas");
        canvasGo.transform.SetParent(root.transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        // dim background
        var dimGo = new GameObject("Dim");
        dimGo.transform.SetParent(canvasGo.transform, false);
        var dim = dimGo.AddComponent<Image>();
        dim.color = new Color(0, 0, 0, 0.75f);
        Stretch(dimGo);

        // ---- preview panel (book OPEN state) ----
        previewPanel = new GameObject("PreviewPanel");
        previewPanel.transform.SetParent(canvasGo.transform, false);
        var ppImg = previewPanel.AddComponent<Image>();
        ppImg.color = new Color(0.12f, 0.10f, 0.08f, 0.98f);
        RectTransform ppRt = previewPanel.GetComponent<RectTransform>();
        ppRt.anchorMin = new Vector2(0.5f, 0.5f); ppRt.anchorMax = new Vector2(0.5f, 0.5f);
        ppRt.pivot = new Vector2(0.5f, 0.5f);
        ppRt.sizeDelta = new Vector2(1100, 700);

        // title (OPEN stamp)
        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(previewPanel.transform, false);
        titleText = titleGo.AddComponent<TextMeshProUGUI>();
        titleText.fontSize = 34; titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(1f, 0.9f, 0.6f);
        titleText.fontStyle = FontStyles.Bold;
        var tRt = titleGo.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0, 1); tRt.anchorMax = new Vector2(1, 1);
        tRt.pivot = new Vector2(0.5f, 1); tRt.sizeDelta = new Vector2(0, 60);
        tRt.anchoredPosition = new Vector2(0, -10);

        // page image holder
        var pageHolder = new GameObject("PageHolder");
        pageHolder.transform.SetParent(previewPanel.transform, false);
        var phRt = pageHolder.AddComponent<RectTransform>();
        phRt.anchorMin = new Vector2(0.5f, 0.5f); phRt.anchorMax = new Vector2(0.5f, 0.5f);
        phRt.pivot = new Vector2(0.5f, 0.5f); phRt.sizeDelta = new Vector2(1000, 520);
        phRt.anchoredPosition = new Vector2(0, -10);

        var prvRawGo = new GameObject("PageRaw");
        prvRawGo.transform.SetParent(pageHolder.transform, false);
        previewRaw = prvRawGo.AddComponent<RawImage>();
        previewRaw.color = Color.white;
        Stretch(prvRawGo);

        var prvImgGo = new GameObject("PageSprite");
        prvImgGo.transform.SetParent(pageHolder.transform, false);
        previewImg = prvImgGo.AddComponent<Image>();
        previewImg.preserveAspect = true;
        Stretch(prvImgGo);

        // hint
        var hintGo = new GameObject("Hint");
        hintGo.transform.SetParent(previewPanel.transform, false);
        hintText = hintGo.AddComponent<TextMeshProUGUI>();
        hintText.fontSize = 24; hintText.alignment = TextAlignmentOptions.Center;
        hintText.color = Color.white;
        hintText.text = "Press R for clear HD view   \u2022   E / Esc to close   \u2022   Saved to Collectibles (J)";
        var hRt = hintGo.GetComponent<RectTransform>();
        hRt.anchorMin = new Vector2(0, 0); hRt.anchorMax = new Vector2(1, 0);
        hRt.pivot = new Vector2(0.5f, 0); hRt.sizeDelta = new Vector2(0, 50);
        hRt.anchoredPosition = new Vector2(0, 12);

        // ---- clear fullscreen panel (HD) ----
        clearPanel = new GameObject("ClearPanel");
        clearPanel.transform.SetParent(canvasGo.transform, false);
        var cpImg = clearPanel.AddComponent<Image>();
        cpImg.color = new Color(0, 0, 0, 0.95f);
        Stretch(clearPanel);

        var clrRawGo = new GameObject("ClearRaw");
        clrRawGo.transform.SetParent(clearPanel.transform, false);
        clearRaw = clrRawGo.AddComponent<RawImage>();
        var crRt = clrRawGo.GetComponent<RectTransform>();
        crRt.anchorMin = new Vector2(0.5f, 0.5f); crRt.anchorMax = new Vector2(0.5f, 0.5f);
        crRt.pivot = new Vector2(0.5f, 0.5f); crRt.sizeDelta = new Vector2(1600, 900);

        var clrImgGo = new GameObject("ClearSprite");
        clrImgGo.transform.SetParent(clearPanel.transform, false);
        clearImg = clrImgGo.AddComponent<Image>();
        clearImg.preserveAspect = true;
        var csRt = clrImgGo.GetComponent<RectTransform>();
        csRt.anchorMin = new Vector2(0.5f, 0.5f); csRt.anchorMax = new Vector2(0.5f, 0.5f);
        csRt.pivot = new Vector2(0.5f, 0.5f); csRt.sizeDelta = new Vector2(1600, 900);

        var chGo = new GameObject("ClearHint");
        chGo.transform.SetParent(clearPanel.transform, false);
        clearHintText = chGo.AddComponent<TextMeshProUGUI>();
        clearHintText.fontSize = 24; clearHintText.alignment = TextAlignmentOptions.Center;
        clearHintText.color = Color.white;
        clearHintText.text = "Press R to go back   \u2022   E / Esc to close";
        var chRt = chGo.GetComponent<RectTransform>();
        chRt.anchorMin = new Vector2(0.5f, 0); chRt.anchorMax = new Vector2(0.5f, 0);
        chRt.pivot = new Vector2(0.5f, 0); chRt.sizeDelta = new Vector2(1200, 50);
        chRt.anchoredPosition = new Vector2(0, 20);

        // close button (mouse friendly)
        MakeButton(canvasGo.transform, "Close (E)", new Vector2(1, 1), new Vector2(-120, -40), () => Close());
        MakeButton(canvasGo.transform, "HD View (R)", new Vector2(1, 1), new Vector2(-120, -100), () => ToggleClearView());
    }

    private void MakeButton(Transform parent, string label, Vector2 anchor, Vector2 offset, UnityEngine.Events.UnityAction onClick)
    {
        var bGo = new GameObject("Btn_" + label);
        bGo.transform.SetParent(parent, false);
        var rt = bGo.AddComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(180, 45); rt.anchoredPosition = offset;
        var img = bGo.AddComponent<Image>();
        img.color = new Color(0.25f, 0.22f, 0.15f, 0.95f);
        var btn = bGo.AddComponent<Button>();
        btn.onClick.AddListener(onClick);
        var tGo = new GameObject("Label");
        tGo.transform.SetParent(bGo.transform, false);
        var t = tGo.AddComponent<TextMeshProUGUI>();
        t.text = label; t.fontSize = 22; t.alignment = TextAlignmentOptions.Center; t.color = Color.white;
        Stretch(tGo);
    }

    private void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }
}
