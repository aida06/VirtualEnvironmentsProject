using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Tutorial-specific completion screen. Replaces GameOverUI when in tutorial mode.
/// Shows "TUTORIAL COMPLETE", the final score, and a "START GAME" button that loads
/// the actual game scene (Demo).
/// </summary>
public class TutorialGameOverUI : MonoBehaviour
{
    private Transform canvasTransform;
    private string gameSceneName;
    private bool initialized;

    private GameObject panel;
    private TextMeshProUGUI scoreText;

    public void Initialize(Transform canvas, string targetGameScene)
    {
        canvasTransform = canvas;
        gameSceneName = targetGameScene;
        CreateUI();
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
            StartCoroutine(AnimateScore(finalScore));
        }
    }

    private System.Collections.IEnumerator AnimateScore(int targetScore)
    {
        yield return new WaitForSeconds(0.5f);
        GameManager.Instance?.PlaySound("Audio/stage_clear");

        float duration = 4f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = 1f - Mathf.Pow(1f - t, 3f);
            int displayScore = Mathf.RoundToInt(Mathf.Lerp(0, targetScore, t));
            if (scoreText != null) scoreText.text = displayScore.ToString();
            yield return null;
        }
        if (scoreText != null) scoreText.text = targetScore.ToString();
    }

    private void CreateUI()
    {
        // Full-screen overlay
        panel = new GameObject("TutorialCompletePanel");
        panel.transform.SetParent(canvasTransform, false);

        var panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.75f);

        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Center container
        var containerObj = new GameObject("Container");
        containerObj.transform.SetParent(panel.transform, false);
        var containerRect = containerObj.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.sizeDelta = new Vector2(800f, 350f);

        var containerLayout = containerObj.AddComponent<VerticalLayoutGroup>();
        containerLayout.spacing = 25f;
        containerLayout.childAlignment = TextAnchor.MiddleCenter;
        containerLayout.childControlWidth = false;
        containerLayout.childControlHeight = false;
        containerLayout.childForceExpandWidth = false;
        containerLayout.childForceExpandHeight = false;

        // 1. "TUTORIAL COMPLETE" title
        var titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(containerObj.transform, false);
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "TUTORIAL COMPLETE";
        titleText.fontSize = 64f;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(0.4f, 0.9f, 0.5f); // green tint
        titleText.fontStyle = FontStyles.Bold;
        titleText.enableWordWrapping = false;
        titleText.overflowMode = TextOverflowModes.Overflow;
        var titleLE = titleObj.AddComponent<LayoutElement>();
        titleLE.preferredWidth = 800f;
        titleLE.preferredHeight = 80f;

        // 2. Score row: [icon_coin] [score]
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

        // Score number
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

        // 3. "START GAME" button (single button, no Main Menu)
        var btnObj = new GameObject("Btn_StartGame");
        btnObj.transform.SetParent(containerObj.transform, false);

        var btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.22f, 0.70f, 0.35f, 1f);

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImage;
        var btnColors = btn.colors;
        btnColors.highlightedColor = new Color(0.30f, 0.82f, 0.45f, 1f);
        btnColors.pressedColor = new Color(0.15f, 0.50f, 0.25f, 1f);
        btn.colors = btnColors;
        btn.onClick.AddListener(() => SceneManager.LoadScene(gameSceneName));

        var btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.sizeDelta = new Vector2(260f, 65f);
        var btnLE = btnObj.AddComponent<LayoutElement>();
        btnLE.preferredWidth = 260f;
        btnLE.preferredHeight = 65f;

        // Button label
        var btnTextObj = new GameObject("Label");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        var btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
        btnText.text = "START GAME";
        btnText.fontSize = 28f;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.color = Color.white;
        btnText.fontStyle = FontStyles.Bold;
        var btnTextRect = btnTextObj.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.offsetMin = Vector2.zero;
        btnTextRect.offsetMax = Vector2.zero;
    }
}
