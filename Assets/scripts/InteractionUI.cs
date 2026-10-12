using UnityEngine;
using TMPro;

public class InteractionUI : MonoBehaviour
{
    public static InteractionUI Instance { get; private set; }

    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private GameObject promptPanel;

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
        if (promptPanel != null)
        {
            promptPanel.SetActive(false);
        }
        else if (promptText != null)
        {
            promptText.gameObject.SetActive(false);
        }
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

        // Create Center Reticle Dot
        GameObject dotGo = new GameObject("Reticle");
        dotGo.transform.SetParent(canvasGo.transform, false);
        UnityEngine.UI.Image dot = dotGo.AddComponent<UnityEngine.UI.Image>();
        dot.color = new Color(1f, 1f, 1f, 0.5f);
        RectTransform dotRect = dotGo.GetComponent<RectTransform>();
        dotRect.sizeDelta = new Vector2(4, 4);

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
        promptText = textGo.AddComponent<TextMeshProUGUI>();
        promptText.fontSize = 32;
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.color = Color.white;
        promptText.fontStyle = FontStyles.Bold;
        promptText.outlineWidth = 0.18f;
        promptText.outlineColor = Color.black;

        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.offsetMin = new Vector2(18, 8);
        textRect.offsetMax = new Vector2(-18, -8);

        promptPanel = panelGo;
    }
}
