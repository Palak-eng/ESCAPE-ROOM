using UnityEngine;

public class InteractionUI : MonoBehaviour
{
    public static InteractionUI Instance { get; private set; }

    [Header("UI Reference")]
    [SerializeField] private UnityEngine.UI.Text promptText;
    [SerializeField] private GameObject promptPanel;

    private string currentMessage;
    private bool promptVisible;
    private GUIStyle fallbackStyle;

    private UnityEngine.UI.Text legendText;
    private RectTransform legendBox;
    private bool diaryKnown;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Auto-create minimal HUD if not manually assigned in Inspector
        if (promptText == null)
        {
            CreateAutoHUD();
        }

        HidePrompt();
    }

    public void ShowPrompt(string message)
    {
        // Null / empty means "show nothing" (used by hidden pickups).
        if (string.IsNullOrEmpty(message))
        {
            HidePrompt();
            return;
        }

        currentMessage = message;
        promptVisible = true;

        if (promptText != null)
        {
            promptText.text = message;
        }

        if (promptPanel != null)
        {
            promptPanel.SetActive(true);
        }
        else if (promptText != null)
        {
            promptText.gameObject.SetActive(true);
        }
    }

    public void HidePrompt()
    {
        promptVisible = false;

        if (promptPanel != null)
        {
            promptPanel.SetActive(false);
        }
        else if (promptText != null)
        {
            promptText.gameObject.SetActive(false);
        }
    }

    void OnGUI()
    {
        // Fallback only. If the normal Canvas prompt exists, do not draw a second prompt.
        if (promptPanel != null || promptText != null)
            return;

        if (!promptVisible || string.IsNullOrEmpty(currentMessage))
            return;

        if (fallbackStyle == null)
        {
            fallbackStyle = new GUIStyle(GUI.skin.box);
            fallbackStyle.alignment = TextAnchor.MiddleCenter;
            fallbackStyle.fontSize = 28;
            fallbackStyle.fontStyle = FontStyle.Bold;
            fallbackStyle.normal.textColor = Color.white;
            fallbackStyle.wordWrap = true;
            fallbackStyle.padding = new RectOffset(18, 18, 10, 10);
        }

        float width = Mathf.Min(820f, Screen.width - 80f);
        float height = 70f;
        Rect rect = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.68f, width, height);

        Color oldColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.82f);
        GUI.Box(rect, GUIContent.none);
        GUI.color = oldColor;
        GUI.Box(rect, currentMessage, fallbackStyle);
    }

    private void CreateAutoHUD()
    {
        // Create Canvas
        GameObject canvasGo = new GameObject("InteractionCanvas");
        canvasGo.transform.SetParent(transform);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Center reticle: aim sprite if available, else the plain dot.
        Texture aimTex = Resources.Load<Texture>("aim");
        if (aimTex != null)
        {
            GameObject retGo = new GameObject("Reticle");
            retGo.transform.SetParent(canvasGo.transform, false);
            UnityEngine.UI.RawImage ret = retGo.AddComponent<UnityEngine.UI.RawImage>();
            ret.texture = aimTex;
            ret.color = Color.white;
            RectTransform retRect = retGo.GetComponent<RectTransform>();
            retRect.anchorMin = new Vector2(0.5f, 0.5f);
            retRect.anchorMax = new Vector2(0.5f, 0.5f);
            retRect.pivot = new Vector2(0.5f, 0.5f);
            // Keep the sprite's own aspect (aim.png is 16:9); a square box would squash it.
            float retH = 30f;
            float retW = aimTex.height > 0 ? retH * ((float)aimTex.width / aimTex.height) : retH;
            retRect.sizeDelta = new Vector2(retW, retH);
            retRect.anchoredPosition = Vector2.zero;
        }
        else
        {
            GameObject dotGo = new GameObject("Reticle");
            dotGo.transform.SetParent(canvasGo.transform, false);
            UnityEngine.UI.Image dot = dotGo.AddComponent<UnityEngine.UI.Image>();
            dot.color = new Color(1f, 1f, 1f, 0.5f);
            RectTransform dotRect = dotGo.GetComponent<RectTransform>();
            dotRect.sizeDelta = new Vector2(4, 4);
        }

        // Create visible prompt panel
        GameObject panelGo = new GameObject("PromptPanel");
        panelGo.transform.SetParent(canvasGo.transform, false);
        UnityEngine.UI.Image panel = panelGo.AddComponent<UnityEngine.UI.Image>();
        panel.color = new Color(0f, 0f, 0f, 0.72f);
        RectTransform panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.28f);
        panelRect.anchorMax = new Vector2(0.5f, 0.28f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(760, 78);

        // Create Prompt Text
        GameObject textGo = new GameObject("PromptText");
        textGo.transform.SetParent(panelGo.transform, false);
        promptText = textGo.AddComponent<UnityEngine.UI.Text>();
        promptText.fontSize = 32;
        promptText.alignment = TextAnchor.MiddleCenter;
        promptText.color = Color.white;
        promptText.fontStyle = FontStyle.Bold;
        promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        promptText.resizeTextForBestFit = true;
        promptText.resizeTextMinSize = 18;
        promptText.resizeTextMaxSize = 34;

        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.offsetMin = new Vector2(18, 8);
        textRect.offsetMax = new Vector2(-18, -8);

        promptPanel = panelGo;

        // Persistent side legend: E = interact, O = open. Always visible.
        // The T : Diary line appears only after the first diary is collected.
        GameObject legendGo = new GameObject("ControlsLegend");
        legendGo.transform.SetParent(canvasGo.transform, false);
        var legendBg = legendGo.AddComponent<UnityEngine.UI.Image>();
        legendBg.color = new Color(0f, 0f, 0f, 0.55f);
        legendBox = legendGo.GetComponent<RectTransform>();
        RectTransform legendRt = legendBox;
        legendRt.anchorMin = new Vector2(1f, 0.5f);
        legendRt.anchorMax = new Vector2(1f, 0.5f);
        legendRt.pivot = new Vector2(1f, 0.5f);
        legendRt.sizeDelta = new Vector2(250, 84);
        legendRt.anchoredPosition = new Vector2(-16f, 0f);

        GameObject legendTextGo = new GameObject("LegendText");
        legendTextGo.transform.SetParent(legendGo.transform, false);
        legendText = legendTextGo.AddComponent<UnityEngine.UI.Text>();
        legendText.text = "E : Interact\nO : Open";
        legendText.fontSize = 26;
        legendText.alignment = TextAnchor.MiddleCenter;
        legendText.color = new Color(1f, 0.92f, 0.6f);
        legendText.fontStyle = FontStyle.Bold;
        legendText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RectTransform legendTextRt = legendTextGo.GetComponent<RectTransform>();
        legendTextRt.anchorMin = Vector2.zero;
        legendTextRt.anchorMax = Vector2.one;
        legendTextRt.offsetMin = new Vector2(10, 6);
        legendTextRt.offsetMax = new Vector2(-10, -6);
    }

    void Update()
    {
        // Reveal the T : Diary legend line once the first diary is collected.
        bool hasDiary = CollectiblesManager.Instance != null && CollectiblesManager.Instance.collected.Count > 0;
        if (hasDiary == diaryKnown) return;
        diaryKnown = hasDiary;

        if (legendText != null)
            legendText.text = hasDiary ? "E : Interact\nO : Open\nT : Diary" : "E : Interact\nO : Open";
        if (legendBox != null)
            legendBox.sizeDelta = new Vector2(250, hasDiary ? 128 : 84);
    }
}
