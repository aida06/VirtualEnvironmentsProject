using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays a full-screen "GAME OVER" overlay with Restart and Main Menu buttons.
/// Hidden during gameplay, shown when GameManager enters the GameOver state.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    private Transform canvasTransform;
    private string menuSceneName;
    private bool initialized;

    private GameObject panel;

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

        // Center container
        var containerObj = new GameObject("Container");
        containerObj.transform.SetParent(panel.transform, false);
        var containerRect = containerObj.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.sizeDelta = new Vector2(800f, 300f);

        var containerLayout = containerObj.AddComponent<VerticalLayoutGroup>();
        containerLayout.spacing = 30f;
        containerLayout.childAlignment = TextAnchor.MiddleCenter;
        containerLayout.childControlWidth = false;
        containerLayout.childControlHeight = false;
        containerLayout.childForceExpandWidth = false;
        containerLayout.childForceExpandHeight = false;

        // "GAME OVER" text
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

        // Buttons container (horizontal)
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
