using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Visual overlay for the tutorial: a dark mask with a circular spotlight cutout,
/// an arrow pointing at the target, instruction text, and a step counter.
/// Created and driven by TutorialManager.
/// </summary>
public class TutorialOverlay : MonoBehaviour
{
    private Canvas overlayCanvas;
    private RawImage maskImage;
    private Material maskMaterial;
    private RectTransform arrowRect;
    private Image arrowImage;
    private TextMeshProUGUI instructionText;
    private TextMeshProUGUI stepCounterText;
    private RectTransform instructionPanel;

    private Camera mainCam;
    private int totalSteps;

    // Cached shader property IDs
    private static readonly int CenterProp = Shader.PropertyToID("_Center");
    private static readonly int RadiusProp = Shader.PropertyToID("_Radius");

    public void Initialize(int totalStepCount)
    {
        totalSteps = totalStepCount;
        mainCam = Camera.main;
        CreateOverlayCanvas();
        CreateMask();
        CreateArrow();
        CreateInstructionPanel();
        CreateStepCounter();
    }

    private void CreateOverlayCanvas()
    {
        var canvasObj = new GameObject("TutorialOverlayCanvas");
        overlayCanvas = canvasObj.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 20; // above game UI (sortingOrder = 10)

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();
    }

    private void CreateMask()
    {
        var maskObj = new GameObject("SpotlightMask");
        maskObj.transform.SetParent(overlayCanvas.transform, false);

        maskImage = maskObj.AddComponent<RawImage>();
        var rect = maskObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // Load or find the SpotlightMask shader
        var shader = Shader.Find("UI/SpotlightMask");
        if (shader == null)
        {
            Debug.LogError("[TutorialOverlay] Shader 'UI/SpotlightMask' not found! Falling back to solid overlay.");
            maskImage.color = new Color(0, 0, 0, 0.6f);
            return;
        }

        maskMaterial = new Material(shader);
        maskMaterial.SetColor("_OverlayColor", new Color(0, 0, 0, 0.65f));
        maskMaterial.SetVector(CenterProp, new Vector4(0.5f, 0.5f, 0, 0));
        maskMaterial.SetFloat(RadiusProp, 0.10f);
        maskMaterial.SetFloat("_Feather", 0.025f);
        maskImage.material = maskMaterial;
        maskImage.color = Color.white; // let the shader handle colors
    }

    private void CreateArrow()
    {
        var arrowObj = new GameObject("Arrow");
        arrowObj.transform.SetParent(overlayCanvas.transform, false);

        arrowImage = arrowObj.AddComponent<Image>();
        var arrowSprite = Resources.Load<Sprite>("UI/Icons/icon_arrow");
        if (arrowSprite != null)
        {
            arrowImage.sprite = arrowSprite;
            arrowImage.type = Image.Type.Simple;
            arrowImage.preserveAspect = true;
            arrowImage.color = Color.white; // use the sprite's own colors
        }
        else
        {
            arrowImage.color = new Color(1f, 0.85f, 0.2f, 0.95f); // gold fallback
            Debug.LogWarning("[TutorialOverlay] icon_arrow not found. Place PNG at Assets/Resources/UI/Icons/icon_arrow.png");
        }

        arrowRect = arrowObj.GetComponent<RectTransform>();
        arrowRect.sizeDelta = new Vector2(80f, 80f);
        arrowRect.pivot = new Vector2(0.5f, 0f);
    }

    private void CreateInstructionPanel()
    {
        // Background panel
        var panelObj = new GameObject("InstructionPanel");
        panelObj.transform.SetParent(overlayCanvas.transform, false);

        var panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);

        instructionPanel = panelObj.GetComponent<RectTransform>();
        instructionPanel.anchorMin = new Vector2(0.5f, 1f);
        instructionPanel.anchorMax = new Vector2(0.5f, 1f);
        instructionPanel.pivot = new Vector2(0.5f, 1f);
        instructionPanel.anchoredPosition = new Vector2(0f, -20f); // top of screen
        instructionPanel.sizeDelta = new Vector2(600f, 70f);

        // Instruction text
        var textObj = new GameObject("InstructionText");
        textObj.transform.SetParent(panelObj.transform, false);
        instructionText = textObj.AddComponent<TextMeshProUGUI>();
        instructionText.text = "";
        instructionText.fontSize = 26f;
        instructionText.alignment = TextAlignmentOptions.Center;
        instructionText.color = Color.white;
        instructionText.fontStyle = FontStyles.Bold;
        instructionText.enableWordWrapping = true;

        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(15, 8);
        textRect.offsetMax = new Vector2(-15, -8);
    }

    private void CreateStepCounter()
    {
        var counterObj = new GameObject("StepCounter");
        counterObj.transform.SetParent(overlayCanvas.transform, false);
        stepCounterText = counterObj.AddComponent<TextMeshProUGUI>();
        stepCounterText.fontSize = 20f;
        stepCounterText.alignment = TextAlignmentOptions.Center;
        stepCounterText.color = new Color(0.8f, 0.8f, 0.8f);

        var rect = counterObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -95f); // below instruction panel
        rect.sizeDelta = new Vector2(200f, 30f);
    }

    /// <summary>
    /// Updates the spotlight position, arrow, and instruction text for the current step.
    /// Called every frame by TutorialManager.
    /// </summary>
    public void UpdateTarget(Vector3 worldPosition, string instruction, int currentStep)
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        // Convert world position to viewport (0-1) coordinates for the shader
        Vector3 viewportPos = mainCam.WorldToViewportPoint(worldPosition);

        // Clamp to screen if object is behind camera
        if (viewportPos.z < 0)
        {
            viewportPos.x = 1f - viewportPos.x;
            viewportPos.y = 1f - viewportPos.y;
        }
        viewportPos.x = Mathf.Clamp(viewportPos.x, 0.05f, 0.95f);
        viewportPos.y = Mathf.Clamp(viewportPos.y, 0.05f, 0.95f);

        // Update shader spotlight center
        if (maskMaterial != null)
        {
            maskMaterial.SetVector(CenterProp, new Vector4(viewportPos.x, viewportPos.y, 0, 0));
        }

        // Update arrow position and rotation
        Vector3 screenPos = mainCam.WorldToScreenPoint(worldPosition);
        // Convert screen position to canvas coordinates
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            overlayCanvas.transform as RectTransform,
            new Vector2(screenPos.x, screenPos.y),
            null, // ScreenSpaceOverlay uses null camera
            out Vector2 canvasPos
        );

        // Position arrow slightly below the target
        arrowRect.anchoredPosition = canvasPos + new Vector2(0f, -100f);

        // Instruction text
        instructionText.text = instruction;
        stepCounterText.text = $"Step {currentStep} / {totalSteps}";
    }

    /// <summary>
    /// Hides the entire overlay (called when tutorial ends).
    /// </summary>
    public void Hide()
    {
        if (overlayCanvas != null)
            overlayCanvas.gameObject.SetActive(false);
    }

    /// <summary>
    /// Returns the overlay canvas transform (for parenting the NEXT button).
    /// </summary>
    public Transform GetCanvasTransform()
    {
        return overlayCanvas?.transform;
    }
}
