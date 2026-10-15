using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Keeps collected diaries + journal UI (press J to open, click entry to re-read).
/// Persists across scenes. Auto-creates UI.
/// </summary>
public class CollectiblesManager : MonoBehaviour
{
    public static CollectiblesManager Instance { get; private set; }

    [System.Serializable]
    public class CollectedDiary
    {
        public string id;
        public string title;
        public Texture2D pageTexture;
        public Sprite pageSprite;
    }

    public List<CollectedDiary> collected = new List<CollectedDiary>();
    public bool JournalOpen { get; private set; }

    private GameObject root;
    private Transform listParent;
    private Text countText;
    private Text toastText;
    private float toastUntil;

    private static Font UiFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUI();
        root.SetActive(false);
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && (kb.jKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame || kb.cKey.wasPressedThisFrame))
        {
            // don't toggle journal while fullscreen reading (R/Esc handles that)
            if (DiaryUIManager.Instance != null && DiaryUIManager.Instance.IsOpen)
                return;
            // No diaries yet: don't open an empty journal.
            if (collected.Count == 0)
            {
                ShowToast("Find the diary first");
                return;
            }
            ToggleJournal();
        }

        // T opens the diary page itself, fullscreen, skipping the journal list.
        if (kb != null && kb.tKey.wasPressedThisFrame)
        {
            if (DiaryUIManager.Instance == null)
                return;
            if (DiaryUIManager.Instance.IsOpen)
                return;
            if (collected.Count == 0)
            {
                ShowToast("Find the diary first");
                return;
            }
            if (JournalOpen)
                ToggleJournal(false);

            CollectedDiary d = collected[collected.Count - 1];
            DiaryUIManager.Instance.OpenDiary(d.id, d.title, d.pageTexture, d.pageSprite);
        }

        if (kb != null && kb.escapeKey.wasPressedThisFrame && JournalOpen)
            ToggleJournal(false);

        if (toastText != null)
            toastText.gameObject.SetActive(Time.time < toastUntil);

        if (JournalOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public bool Has(string id) => collected.Exists(d => d.id == id);

    public void Collect(DiaryInteractable diary)
    {
        if (Has(diary.diaryId))
            return;
        collected.Add(new CollectedDiary
        {
            id = diary.diaryId,
            title = diary.diaryTitle,
            pageTexture = diary.GetTexture(),
            pageSprite = diary.GetSprite()
        });
        RefreshList();
        ShowToast("Collected: " + diary.diaryTitle + "  (press J to open journal)");
    }

    /// <summary>Clears the whole run (used by Play Again).</summary>
    public void ResetRun()
    {
        collected.Clear();
        if (JournalOpen) ToggleJournal(false);
        toastUntil = 0f;
        if (toastText != null) toastText.gameObject.SetActive(false);
        RefreshList();
    }

    public void ToggleJournal() => ToggleJournal(!JournalOpen);

    public void ToggleJournal(bool open)
    {
        JournalOpen = open;
        root.SetActive(open);
        if (open)
        {
            RefreshList();
            // pause world interaction by freeing cursor; player movement stays but raycast prompt hidden
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (InteractionUI.Instance != null) InteractionUI.Instance.HidePrompt();
        }
        else if (DiaryUIManager.Instance == null || !DiaryUIManager.Instance.IsOpen)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void ReadAgain(string id)
    {
        var d = collected.Find(x => x.id == id);
        if (d == null || DiaryUIManager.Instance == null) return;
        ToggleJournal(false);
        DiaryUIManager.Instance.OpenDiary(d.id, d.title, d.pageTexture, d.pageSprite);
    }

    private void ShowToast(string msg)
    {
        if (toastText == null) return;
        toastText.text = msg;
        toastUntil = Time.time + 3f;
        toastText.gameObject.SetActive(true);
    }

    private void RefreshList()
    {
        if (listParent == null) return;
        foreach (Transform c in listParent) Destroy(c.gameObject);

        countText.text = "Collectibles  (" + collected.Count + ")   -   T / J to close, click to re-read";

        if (collected.Count == 0)
        {
            var empty = new GameObject("Empty");
            empty.transform.SetParent(listParent, false);
            var t = empty.AddComponent<Text>();
            t.text = "No diaries yet. Find the books on the sofa and press E.";
            t.fontSize = 24; t.color = new Color(1, 1, 1, 0.7f);
            t.alignment = TextAnchor.MiddleCenter;
            t.font = UiFont;
            var rt = empty.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1); rt.sizeDelta = new Vector2(0, 60);
            return;
        }

        foreach (var d in collected)
        {
            var bGo = new GameObject("Entry_" + d.id);
            bGo.transform.SetParent(listParent, false);
            var rt = bGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1); rt.sizeDelta = new Vector2(0, 60);
            var img = bGo.AddComponent<Image>();
            img.color = new Color(0.2f, 0.17f, 0.12f, 0.95f);
            var btn = bGo.AddComponent<Button>();
            string captured = d.id;
            btn.onClick.AddListener(() => ReadAgain(captured));

            var tGo = new GameObject("Label");
            tGo.transform.SetParent(bGo.transform, false);
            var t = tGo.AddComponent<Text>();
            t.text = d.title + "   -   click to read";
            t.fontSize = 26; t.color = Color.white;
            t.alignment = TextAnchor.MiddleLeft;
            t.font = UiFont;
            var trt = tGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(20, 0); trt.offsetMax = Vector2.zero;
        }
    }

    private void BuildUI()
    {
        root = new GameObject("CollectiblesUI");
        root.transform.SetParent(transform, false);

        var canvasGo = new GameObject("JournalCanvas");
        canvasGo.transform.SetParent(root.transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 190;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        var dimGo = new GameObject("Dim");
        dimGo.transform.SetParent(canvasGo.transform, false);
        var dim = dimGo.AddComponent<Image>();
        dim.color = new Color(0, 0, 0, 0.6f);
        FullStretch(dimGo);

        var panel = new GameObject("JournalPanel");
        panel.transform.SetParent(canvasGo.transform, false);
        var pImg = panel.AddComponent<Image>();
        pImg.color = new Color(0.1f, 0.09f, 0.07f, 0.98f);
        var pRt = panel.GetComponent<RectTransform>();
        pRt.anchorMin = new Vector2(0.5f, 0.5f); pRt.anchorMax = new Vector2(0.5f, 0.5f);
        pRt.pivot = new Vector2(0.5f, 0.5f); pRt.sizeDelta = new Vector2(800, 600);

        var headerGo = new GameObject("Header");
        headerGo.transform.SetParent(panel.transform, false);
        countText = headerGo.AddComponent<Text>();
        countText.fontSize = 26; countText.alignment = TextAnchor.MiddleCenter;
        countText.color = new Color(1f, 0.9f, 0.6f);
        countText.font = UiFont;
        var hRt = headerGo.GetComponent<RectTransform>();
        hRt.anchorMin = new Vector2(0, 1); hRt.anchorMax = new Vector2(1, 1);
        hRt.pivot = new Vector2(0.5f, 1); hRt.sizeDelta = new Vector2(0, 60);
        hRt.anchoredPosition = new Vector2(0, -10);

        var listGo = new GameObject("List");
        listGo.transform.SetParent(panel.transform, false);
        listParent = listGo.transform;
        var lRt = listGo.AddComponent<RectTransform>();
        lRt.anchorMin = new Vector2(0, 0); lRt.anchorMax = new Vector2(1, 1);
        lRt.pivot = new Vector2(0.5f, 0.5f);
        lRt.offsetMin = new Vector2(30, 30); lRt.offsetMax = new Vector2(-30, -80);
        var layout = listGo.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 10; layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
        layout.childControlHeight = false;

        // toast (bottom center, always on top, outside journal root so it shows anytime)
        var toastGo = new GameObject("Toast");
        toastGo.transform.SetParent(canvasGo.transform, false);
        toastText = toastGo.AddComponent<Text>();
        toastText.fontSize = 26; toastText.alignment = TextAnchor.MiddleCenter;
        toastText.color = new Color(1f, 0.95f, 0.7f);
        toastText.fontStyle = FontStyle.Bold;
        toastText.font = UiFont;
        var tRt = toastGo.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0.5f, 0.15f); tRt.anchorMax = new Vector2(0.5f, 0.15f);
        tRt.pivot = new Vector2(0.5f, 0.5f); tRt.sizeDelta = new Vector2(1200, 60);
        toastGo.SetActive(false);
        // Toast must live even when journal closed: move it out so hiding root doesn't kill it.
        // Instead we keep a separate always-on canvas object for toast:
        toastGo.transform.SetParent(transform, false);
        var toastCanvasGo = new GameObject("ToastCanvas");
        toastCanvasGo.transform.SetParent(transform, false);
        var tc = toastCanvasGo.AddComponent<Canvas>();
        tc.renderMode = RenderMode.ScreenSpaceOverlay; tc.sortingOrder = 210;
        var ts = toastCanvasGo.AddComponent<CanvasScaler>();
        ts.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        ts.referenceResolution = new Vector2(1920, 1080);
        toastCanvasGo.AddComponent<GraphicRaycaster>();
        toastGo.transform.SetParent(toastCanvasGo.transform, false);
        tRt.anchorMin = new Vector2(0.5f, 0.12f); tRt.anchorMax = new Vector2(0.5f, 0.12f);
    }

    private void FullStretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }
}
