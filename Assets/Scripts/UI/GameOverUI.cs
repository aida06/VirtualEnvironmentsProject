using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays a full-screen "GAME OVER" overlay with animated score roll-up,
/// Restart/Main Menu buttons, and sound effects (time_out + stage_clear).
/// Hidden during gameplay, shown on GameOver state.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    private Transform canvasTransform;
    private string menuSceneName;
    private bool initialized;

    private GameObject panel;
    private TextMeshProUGUI scoreText;

    public void Initialize(Transform canvas, string mainMenuScene)
    {
        canvasTransform = canvas;
        menuSceneName = mainMenuScene;
        CreateGameOverUI();
        panel.SetActive(false);

        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged += OnGameStateChanged;

        initialized = true;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnGameStateChanged;
    }

    private void OnGameStateChanged(GameManager.GameState state)
    {
        panel.SetActive(state == GameManager.GameState.GameOver);

        if (state == GameManager.GameState.GameOver)
        {
            int finalScore = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;

            // Play time-out sound
            GameManager.Instance?.PlaySound("Audio/time_out");

            // Start score roll-up animation (from 0 to finalScore over ~4 seconds)
            StartCoroutine(AnimateScore(finalScore));
        }
    }

    private IEnumerator AnimateScore(int targetScore)
    {
        // Small delay before starting the roll-up
        yield return new WaitForSeconds(0.5f);

        // Play stage-clear sound at the start of the roll-up
        GameManager.Instance?.PlaySound("Audio/stage_clear");

        float duration = 4f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Ease-out cubic curve for dramatic deceleration
            t = 1f - Mathf.Pow(1f - t, 3f);
            int displayScore = Mathf.RoundToInt(Mathf.Lerp(0, targetScore, t));
            if (scoreText != null) scoreText.text = displayScore.ToString();
            yield return null;
        }

        // Ensure final value is exact
        if (scoreText != null) scoreText.text = targetScore.ToString();
    }

    private void CreateGameOverUI()
    {
        // Full-screen overlay panel
        panel = new GameObject("GameOverPanel");
        panel.transform.SetParent(canvasTransform, false);

        var panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.75f);

        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // ---- Center container (title + score + buttons) ----
        var containerObj = new GameObject("Container");
        containerObj.transform.SetParent(panel.transform, false);
        var containerRect = containerObj.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.sizeDelta = new Vector2(800f, 380f);

        var containerLayout = containerObj.AddComponent<VerticalLayoutGroup>();
        containerLayout.spacing = 25f;
        containerLayout.childAlignment = TextAnchor.MiddleCenter;
        containerLayout.childControlWidth = false;
        containerLayout.childControlHeight = false;
        containerLayout.childForceExpandWidth = false;
        containerLayout.childForceExpandHeight = false;

        // 1. "GAME OVER" text
        var titleObj = new GameObject("GameOverText");
        titleObj.transform.SetParent(containerObj.transform, false);
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "GAME OVER";
        titleText.fontSize = 72f;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        titleText.fontStyle = FontStyles.Bold;
        titleText.enableWordWrapping = false;
        titleText.overflowMode = TextOverflowModes.Overflow;
        var titleLayout = titleObj.AddComponent<LayoutElement>();
        titleLayout.preferredWidth = 800f;
        titleLayout.preferredHeight = 90f;

        // 2. Score display row: [icon_coin] [score number]
        var scoreRow = new GameObject("ScoreRow");
        scoreRow.transform.SetParent(containerObj.transform, false);

        var scoreRowLayout = scoreRow.AddComponent<HorizontalLayoutGroup>();
        scoreRowLayout.spacing = 10f;
        scoreRowLayout.childAlignment = TextAnchor.MiddleCenter;
        scoreRowLayout.childControlWidth = false;
        scoreRowLayout.childControlHeight = false;
        scoreRowLayout.childForceExpandWidth = false;
        scoreRowLayout.childForceExpandHeight = false;

        var scoreRowLE = scoreRow.AddComponent<LayoutElement>();
        scoreRowLE.preferredWidth = 300f;
        scoreRowLE.preferredHeight = 50f;

        // Coin icon
        var coinObj = new GameObject("CoinIcon");
        coinObj.transform.SetParent(scoreRow.transform, false);
        var coinImage = coinObj.AddComponent<Image>();
        var coinSprite = Resources.Load<Sprite>("UI/Icons/icon_coin");
        if (coinSprite != null)
        {
            coinImage.sprite = coinSprite;
            coinImage.type = Image.Type.Simple;
        }
        else
        {
            coinImage.color = new Color(1f, 0.85f, 0.2f, 1f);
        }
        coinImage.preserveAspect = true;
        var coinRect = coinObj.GetComponent<RectTransform>();
        coinRect.sizeDelta = new Vector2(40f, 40f);

        // Score number (starts at 0, animated on game over)
        var scoreObj = new GameObject("FinalScore");
        scoreObj.transform.SetParent(scoreRow.transform, false);
        scoreText = scoreObj.AddComponent<TextMeshProUGUI>();
        scoreText.text = "0";
        scoreText.fontSize = 42f;
        scoreText.alignment = TextAlignmentOptions.MidlineLeft;
        scoreText.color = new Color(1f, 0.85f, 0.3f);
        scoreText.fontStyle = FontStyles.Bold;
        var scoreRect = scoreObj.GetComponent<RectTransform>();
        scoreRect.sizeDelta = new Vector2(200f, 50f);

        // 3. Buttons container (horizontal)
        var btnContainer = new GameObject("Buttons");
        btnContainer.transform.SetParent(containerObj.transform, false);
        var btnContainerRect = btnContainer.AddComponent<RectTransform>();
        btnContainerRect.sizeDelta = new Vector2(420f, 70f);
        var btnLayout = btnContainer.AddComponent<HorizontalLayoutGroup>();
        btnLayout.spacing = 20f;
        btnLayout.childAlignment = TextAnchor.MiddleCenter;
        btnLayout.childControlWidth = false;
        btnLayout.childControlHeight = false;
        btnLayout.childForceExpandWidth = false;
        btnLayout.childForceExpandHeight = false;
        var btnContainerLayoutElem = btnContainer.AddComponent<LayoutElement>();
        btnContainerLayoutElem.preferredWidth = 420f;
        btnContainerLayoutElem.preferredHeight = 70f;

        // Restart button
        CreateButton(btnContainer.transform, "RESTART", new Color(0.20f, 0.65f, 0.32f), () =>
        {
            GameManager.Instance?.RestartGame();
        });

        // Main Menu button
        CreateButton(btnContainer.transform, "MAIN MENU", new Color(0.25f, 0.45f, 0.75f), () =>
        {
            GameManager.Instance?.GoToMainMenu();
        });
    }

    private void CreateButton(Transform parent, string label, Color color, UnityEngine.Events.UnityAction onClick)
    {
        var btnObj = new GameObject("Btn_" + label);
        btnObj.transform.SetParent(parent, false);

        var btnImage = btnObj.AddComponent<Image>();
        btnImage.color = color;

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImage;
        var colors = btn.colors;
        colors.highlightedColor = color * 1.2f;
        colors.pressedColor = color * 0.7f;
        btn.colors = colors;
        btn.onClick.AddListener(onClick);

        var btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.sizeDelta = new Vector2(200f, 60f);

        // Button label
        var textObj = new GameObject("Label");
        textObj.transform.SetParent(btnObj.transform, false);
        var text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 24f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.fontStyle = FontStyles.Bold;
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }
}
