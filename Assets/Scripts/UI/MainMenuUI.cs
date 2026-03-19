using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Main menu screen with a "START GAME" button.
/// Creates its own Canvas. Drop this on any GameObject in your MainMenu scene.
/// Configure gameSceneName in the Inspector to match the name of your game scene
/// (e.g., "Demo" for the Ubiq Demo scene).
///
/// SETUP:
/// 1. Create a new scene (File > New Scene) and save it as "MainMenu".
/// 2. Create an empty GameObject, attach this script.
/// 3. Set "Game Scene Name" in the Inspector to your game scene name.
/// 4. Add both "MainMenu" and your game scene to Build Settings (File > Build Settings).
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string gameSceneName = "Demo";

    [Header("Visual Settings")]
    [SerializeField] private string gameTitle = "KITCHEN SIMULATOR";

    private void Start()
    {
        CreateMenuUI();
    }

    private void CreateMenuUI()
    {
        // Canvas
        var canvasObj = new GameObject("MainMenuCanvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // EventSystem
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            esObj.AddComponent<StandaloneInputModule>();
#endif
        }

        // Background
        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        var bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.12f, 0.12f, 0.18f, 1f);
        var bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Title
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(canvasObj.transform, false);
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = gameTitle;
        titleText.fontSize = 80f;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(1f, 0.85f, 0.3f);
        titleText.fontStyle = FontStyles.Bold;
        var titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.6f);
        titleRect.anchorMax = new Vector2(0.5f, 0.6f);
        titleRect.sizeDelta = new Vector2(900f, 120f);
        titleRect.anchoredPosition = Vector2.zero;

        // Subtitle
        var subObj = new GameObject("Subtitle");
        subObj.transform.SetParent(canvasObj.transform, false);
        var subText = subObj.AddComponent<TextMeshProUGUI>();
        subText.text = "A Cooperative Cooking Experience";
        subText.fontSize = 28f;
        subText.alignment = TextAlignmentOptions.Center;
        subText.color = new Color(0.7f, 0.7f, 0.75f);
        var subRect = subObj.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0.5f, 0.50f);
        subRect.anchorMax = new Vector2(0.5f, 0.50f);
        subRect.sizeDelta = new Vector2(600f, 50f);
        subRect.anchoredPosition = Vector2.zero;

        // Start button
        var btnObj = new GameObject("StartButton");
        btnObj.transform.SetParent(canvasObj.transform, false);
        var btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.22f, 0.70f, 0.35f, 1f);

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImage;
        var btnColors = btn.colors;
        btnColors.highlightedColor = new Color(0.30f, 0.82f, 0.45f, 1f);
        btnColors.pressedColor = new Color(0.15f, 0.50f, 0.25f, 1f);
        btn.colors = btnColors;
        btn.onClick.AddListener(OnStartGame);

        var btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.30f);
        btnRect.anchorMax = new Vector2(0.5f, 0.30f);
        btnRect.sizeDelta = new Vector2(320f, 80f);
        btnRect.anchoredPosition = Vector2.zero;

        // Button text
        var btnTextObj = new GameObject("Label");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        var btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
        btnText.text = "START GAME";
        btnText.fontSize = 36f;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.color = Color.white;
        btnText.fontStyle = FontStyles.Bold;
        var btnTextRect = btnTextObj.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.offsetMin = Vector2.zero;
        btnTextRect.offsetMax = Vector2.zero;
    }

    private void OnStartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }
}
