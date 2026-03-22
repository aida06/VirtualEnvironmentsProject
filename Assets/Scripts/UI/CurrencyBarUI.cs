using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays a currency bar at the top-right of the screen, to the left of the countdown timer.
/// Shows a coin icon (icon_coin) and the current score. Updates in real time.
/// </summary>
public class CurrencyBarUI : MonoBehaviour
{
    private TextMeshProUGUI scoreText;
    private bool initialized;

    private const string ICON_PATH = "UI/Icons/";

    public void Initialize(Transform canvasTransform)
    {
        CreateCurrencyBar(canvasTransform);

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreChanged += OnScoreChanged;

        initialized = true;
    }

    private void OnDestroy()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreChanged -= OnScoreChanged;
    }

    private void OnScoreChanged(int newScore)
    {
        if (scoreText != null)
            scoreText.text = newScore.ToString();
    }

    private void CreateCurrencyBar(Transform canvasTransform)
    {
        // Panel - positioned to the left of the timer panel (timer is at -20, -20 with width 130)
        var panelObj = new GameObject("CurrencyBar");
        panelObj.transform.SetParent(canvasTransform, false);

        var panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0.15f, 0.15f, 0.2f, 0.85f);

        var panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-160f, -20f); // 10px gap left of timer
        panelRect.sizeDelta = new Vector2(130f, 60f);

        // Horizontal layout: [icon] [score]
        var hLayout = panelObj.AddComponent<HorizontalLayoutGroup>();
        hLayout.padding = new RectOffset(8, 8, 8, 8);
        hLayout.spacing = 6f;
        hLayout.childAlignment = TextAnchor.MiddleCenter;
        hLayout.childControlWidth = false;
        hLayout.childControlHeight = false;
        hLayout.childForceExpandWidth = false;
        hLayout.childForceExpandHeight = false;

        // Coin icon
        var iconObj = new GameObject("CoinIcon");
        iconObj.transform.SetParent(panelObj.transform, false);
        var iconImage = iconObj.AddComponent<Image>();
        var sprite = Resources.Load<Sprite>(ICON_PATH + "icon_coin");
        if (sprite != null)
        {
            iconImage.sprite = sprite;
            iconImage.type = Image.Type.Simple;
        }
        else
        {
            iconImage.color = new Color(1f, 0.85f, 0.2f, 1f); // gold fallback
            Debug.LogWarning("[CurrencyBarUI] icon_coin not found. Place PNG at Assets/Resources/UI/Icons/icon_coin.png");
        }
        iconImage.preserveAspect = true;
        var iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(36f, 36f);

        // Score text
        var textObj = new GameObject("ScoreText");
        textObj.transform.SetParent(panelObj.transform, false);
        scoreText = textObj.AddComponent<TextMeshProUGUI>();
        scoreText.text = "0";
        scoreText.fontSize = 28f;
        scoreText.alignment = TextAlignmentOptions.MidlineLeft;
        scoreText.color = Color.white;
        scoreText.fontStyle = FontStyles.Bold;
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(70f, 40f);
    }
}
