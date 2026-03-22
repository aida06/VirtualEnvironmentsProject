using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays the main game countdown timer at the top-right corner of the screen.
/// Changes color and pulses when time is running low.
/// </summary>
public class CountdownTimerUI : MonoBehaviour
{
    private Transform canvasTransform;
    private bool initialized;

    private TextMeshProUGUI timerText;
    private Image panelImage;
    private float totalDuration;
    private bool isFrozen;

    public void Initialize(Transform canvas)
    {
        canvasTransform = canvas;
        isFrozen = GameManager.Instance != null && GameManager.Instance.IsTutorialMode;
        totalDuration = GameManager.Instance != null ? GameManager.Instance.RemainingTime : 120f;
        CreateTimerUI();

        // If frozen, set initial display immediately
        if (isFrozen)
        {
            float frozen = totalDuration;
            int m = Mathf.FloorToInt(frozen / 60f);
            int s = Mathf.FloorToInt(frozen % 60f);
            timerText.text = $"{m:00}:{s:00}";
            timerText.color = Color.white;
        }

        initialized = true;
    }

    private void CreateTimerUI()
    {
        // Timer panel - top right
        var panelObj = new GameObject("TimerPanel");
        panelObj.transform.SetParent(canvasTransform, false);

        panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0.15f, 0.15f, 0.2f, 0.85f);

        var panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-20f, -20f);
        panelRect.sizeDelta = new Vector2(130f, 60f);

        // Clock icon text
        var iconObj = new GameObject("ClockIcon");
        iconObj.transform.SetParent(panelObj.transform, false);
        var iconText = iconObj.AddComponent<TextMeshProUGUI>();
        iconText.text = "TIME";
        iconText.fontSize = 14f;
        iconText.alignment = TextAlignmentOptions.Center;
        iconText.color = new Color(0.7f, 0.7f, 0.7f);
        var iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.6f);
        iconRect.anchorMax = new Vector2(1f, 1f);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;

        // Timer text
        var textObj = new GameObject("TimerText");
        textObj.transform.SetParent(panelObj.transform, false);
        timerText = textObj.AddComponent<TextMeshProUGUI>();
        timerText.text = "02:00";
        timerText.fontSize = 32f;
        timerText.alignment = TextAlignmentOptions.Center;
        timerText.color = Color.white;
        timerText.fontStyle = FontStyles.Bold;
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 0.65f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    private void Update()
    {
        if (!initialized || GameManager.Instance == null) return;

        // In tutorial mode the timer is frozen — no updates needed
        if (isFrozen) return;

        float remaining = GameManager.Instance.RemainingTime;
        int minutes = Mathf.FloorToInt(remaining / 60f);
        int seconds = Mathf.FloorToInt(remaining % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";

        float ratio = remaining / totalDuration;

        // Color transitions: white -> yellow (at 25%) -> red (at 10%)
        if (ratio > 0.25f)
        {
            timerText.color = Color.white;
            panelImage.color = new Color(0.15f, 0.15f, 0.2f, 0.85f);
        }
        else if (ratio > 0.10f)
        {
            timerText.color = Color.Lerp(Color.yellow, Color.white, (ratio - 0.10f) / 0.15f);
            panelImage.color = new Color(0.25f, 0.20f, 0.10f, 0.90f);
        }
        else
        {
            timerText.color = Color.red;
            panelImage.color = new Color(0.30f, 0.10f, 0.10f, 0.92f);

            // Pulse effect when critically low
            float scale = 1f + Mathf.Sin(Time.time * 6f) * 0.05f;
            timerText.transform.localScale = Vector3.one * scale;
        }
    }
}
