using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

/// <summary>
/// 10-minute escape timer. Win = gate opens in time. Lose = clock hits zero.
/// Pauses while the diary reader or journal is open (player is locked then).
/// </summary>
public class GameTimer : MonoBehaviour
{
    public static GameTimer Instance { get; private set; }

    [Header("Time")]
    [Tooltip("Seconds to escape. 600 = 10 minutes.")]
    public float totalSeconds = 600f;

    [Header("End Messages")]
    public string winTitle = "YOU ESCAPED!";
    public string loseTitle = "TIME'S UP!";

    public float Remaining { get; private set; }
    public bool IsFinished { get; private set; }
    public bool Won { get; private set; }

    private GameObject root;
    private Text timerText;
    private Image timerPanel;
    private GameObject endPanel;
    private Text endTitle;
    private Text endSubtitle;
    private Text endStats;
    private GameObject keepExploringBtn;

    private readonly System.Collections.Generic.List<MonoBehaviour> lockedScripts = new System.Collections.Generic.List<MonoBehaviour>();
    private bool gateWarningShown;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Remaining = totalSeconds;
        BuildUI();
        UpdateTimerLabel();
    }

    void Update()
    {
        if (IsFinished)
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
                Restart();
            return;
        }

        // Win the moment the gate opens.
        if (PedestalGate.Instance != null && PedestalGate.Instance.IsOpen)
        {
            Finish(true);
            return;
        }

        // Diagnosable setup: without this component the win panel can never trigger.
        if (PedestalGate.Instance == null && !gateWarningShown)
        {
            gateWarningShown = true;
            Debug.LogWarning("[GameTimer] No PedestalGate in the scene. Attach PedestalGate to the exit door or the win panel will never show.");
        }

        // Fair pause: player can't act while reading or in the journal.
        bool paused = (DiaryUIManager.Instance != null && DiaryUIManager.Instance.IsOpen)
                   || (CollectiblesManager.Instance != null && CollectiblesManager.Instance.JournalOpen);

        if (!paused)
        {
            Remaining -= Time.deltaTime;
            if (Remaining <= 0f)
            {
                Remaining = 0f;
                UpdateTimerLabel();
                Finish(false);
                return;
            }
        }

        UpdateTimerLabel();
    }

    private void UpdateTimerLabel()
    {
        if (timerText == null) return;

        int total = Mathf.CeilToInt(Remaining);
        int mins = total / 60;
        int secs = total % 60;
        timerText.text = mins.ToString("00") + ":" + secs.ToString("00");

        bool danger = Remaining <= 60f && !IsFinished;
        timerText.color = danger ? new Color(1f, 0.3f, 0.25f) : Color.white;
        if (timerPanel != null)
            timerPanel.color = danger ? new Color(0.35f, 0.05f, 0.05f, 0.8f) : new Color(0f, 0f, 0f, 0.65f);
    }

    private void Finish(bool won)
    {
        IsFinished = true;
        Won = won;

        float used = totalSeconds - Remaining;
        int uMins = Mathf.FloorToInt(used / 60f);
        int uSecs = Mathf.FloorToInt(used % 60f);

        endTitle.text = won ? winTitle : loseTitle;
        endTitle.color = won ? new Color(0.5f, 1f, 0.55f) : new Color(1f, 0.35f, 0.3f);
        endSubtitle.text = won
            ? "Escaped with " + TimerString(Remaining) + " left  (finished in " + uMins.ToString("00") + ":" + uSecs.ToString("00") + ")"
            : "The door stays shut...  (Enter = try again)";

        int placed = QuestInventory.Instance != null ? QuestInventory.Instance.PlacedCount() : 0;
        int total = QuestInventory.Instance != null ? QuestInventory.Instance.totalItems : 4;
        int diaries = CollectiblesManager.Instance != null ? CollectiblesManager.Instance.collected.Count : 0;
        endStats.text = "Ritual items placed: " + placed + "/" + total + "   •   Diaries found: " + diaries;

        keepExploringBtn.SetActive(won);
        endPanel.SetActive(true);

        LockPlayer(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private string TimerString(float t)
    {
        int total = Mathf.CeilToInt(t);
        return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
    }

    public void KeepExploring()
    {
        if (endPanel != null) endPanel.SetActive(false);
        LockPlayer(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LockPlayer(bool lockIt)
    {
        if (lockIt)
        {
            lockedScripts.Clear();
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                player = GameObject.Find("PlayerCapsule");
            if (player != null)
            {
                foreach (var mb in player.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    string tn = mb.GetType().Name;
                    if (tn.Contains("Controller") || tn.Contains("Input") || tn.Contains("Look") || tn.Contains("Move"))
                    {
                        if (mb.enabled) { mb.enabled = false; lockedScripts.Add(mb); }
                    }
                }
            }
        }
        else
        {
            foreach (var mb in lockedScripts)
                if (mb != null) mb.enabled = true;
            lockedScripts.Clear();
        }
    }

    public void Restart()
    {
        // Fresh run: clear cross-scene state, then reload.
        if (QuestInventory.Instance != null) QuestInventory.Instance.ResetRun();
        if (CollectiblesManager.Instance != null) CollectiblesManager.Instance.ResetRun();
        TorchPickup.ResetRun();
        if (DiaryUIManager.Instance != null && DiaryUIManager.Instance.IsOpen)
            DiaryUIManager.Instance.Close();

        Remaining = totalSeconds;
        IsFinished = false;
        Won = false;
        if (endPanel != null) endPanel.SetActive(false);
        UpdateTimerLabel();

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ---------- UI (built in code, no scene setup) ----------
    private void EnsureEventSystem()
    {
        // Project runs on the new Input System package only. The old
        // StandaloneInputModule throws InvalidOperationException here, so
        // create or upgrade to InputSystemUIInputModule instead.
        EventSystem eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            var es = new GameObject("EventSystem");
            es.transform.SetParent(transform, false);
            eventSystem = es.AddComponent<EventSystem>();
        }

        StandaloneInputModule oldModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (oldModule != null)
            Destroy(oldModule);

        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
    }

    private void BuildUI()
    {
        EnsureEventSystem();

        root = new GameObject("GameTimerUI");
        root.transform.SetParent(transform, false);

        var canvasGo = new GameObject("TimerCanvas");
        canvasGo.transform.SetParent(root.transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        // Top-center timer pill
        var pillGo = new GameObject("TimerPill");
        pillGo.transform.SetParent(canvasGo.transform, false);
        timerPanel = pillGo.AddComponent<Image>();
        timerPanel.color = new Color(0f, 0f, 0f, 0.65f);
        var pillRt = pillGo.GetComponent<RectTransform>();
        pillRt.anchorMin = new Vector2(1f, 1f); pillRt.anchorMax = new Vector2(1f, 1f);
        pillRt.pivot = new Vector2(1f, 1f);
        pillRt.sizeDelta = new Vector2(300, 88);
        pillRt.anchoredPosition = new Vector2(-24f, -16f);

        var labelGo = new GameObject("TimerLabel");
        labelGo.transform.SetParent(pillGo.transform, false);
        timerText = labelGo.AddComponent<Text>();
        timerText.fontSize = 56;
        timerText.alignment = TextAnchor.MiddleCenter;
        timerText.color = Color.white;
        timerText.fontStyle = FontStyle.Bold;
        timerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero; labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero; labelRt.offsetMax = Vector2.zero;

        // End screen (own top canvas)
        var endCanvasGo = new GameObject("EndCanvas");
        endCanvasGo.transform.SetParent(root.transform, false);
        var endCanvas = endCanvasGo.AddComponent<Canvas>();
        endCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        endCanvas.sortingOrder = 300;
        var endScaler = endCanvasGo.AddComponent<CanvasScaler>();
        endScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        endScaler.referenceResolution = new Vector2(1920, 1080);
        endCanvasGo.AddComponent<GraphicRaycaster>();

        endPanel = new GameObject("EndPanel");
        endPanel.transform.SetParent(endCanvasGo.transform, false);
        var dim = endPanel.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.8f);
        FullStretch(endPanel);

        var boxGo = new GameObject("EndBox");
        boxGo.transform.SetParent(endPanel.transform, false);
        var box = boxGo.AddComponent<Image>();
        box.color = new Color(0.1f, 0.09f, 0.08f, 0.98f);
        var boxRt = boxGo.GetComponent<RectTransform>();
        boxRt.anchorMin = new Vector2(0.5f, 0.5f); boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        boxRt.pivot = new Vector2(0.5f, 0.5f);
        boxRt.sizeDelta = new Vector2(700, 500);

        endTitle = MakeLabel(boxGo.transform, 52, new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(660, 90));
        endSubtitle = MakeLabel(boxGo.transform, 26, new Vector2(0.5f, 1f), new Vector2(0, -140), new Vector2(660, 80));
        endStats = MakeLabel(boxGo.transform, 24, new Vector2(0.5f, 1f), new Vector2(0, -205), new Vector2(660, 50));

        MakeButton(boxGo.transform, "PLAY AGAIN (Enter)", new Vector2(0.5f, 0f), new Vector2(0, 120), () => Restart());

        keepExploringBtn = MakeButton(boxGo.transform, "KEEP EXPLORING", new Vector2(0.5f, 0f), new Vector2(0, 50), () => KeepExploring());

        endPanel.SetActive(false);
    }

    private Text MakeLabel(Transform parent, int size, Vector2 anchor, Vector2 offset, Vector2 delta)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.fontSize = size;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.fontStyle = FontStyle.Bold;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.resizeTextForBestFit = true;
        t.resizeTextMinSize = 16;
        t.resizeTextMaxSize = size;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = delta;
        rt.anchoredPosition = offset;
        return t;
    }

    private GameObject MakeButton(Transform parent, string label, Vector2 anchor, Vector2 offset, UnityEngine.Events.UnityAction onClick)
    {
        var bGo = new GameObject("Btn_" + label);
        bGo.transform.SetParent(parent, false);
        var rt = bGo.AddComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(360, 52);
        rt.anchoredPosition = offset;
        var img = bGo.AddComponent<Image>();
        img.color = new Color(0.25f, 0.22f, 0.15f, 0.95f);
        var btn = bGo.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        var tGo = new GameObject("Label");
        tGo.transform.SetParent(bGo.transform, false);
        var t = tGo.AddComponent<Text>();
        t.text = label;
        t.fontSize = 24;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var trt = tGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        return bGo;
    }

    private void FullStretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }
}
