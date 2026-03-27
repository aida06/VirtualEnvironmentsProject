using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Visual overlay for the tutorial: instruction text and step counter.
/// Created and driven by TutorialManager.
/// </summary>
public class TutorialOverlay : MonoBehaviour
{
    private Canvas overlayCanvas;
    private TextMeshProUGUI instructionText;
    private TextMeshProUGUI stepCounterText;

    private int totalSteps;

    public void Initialize(int totalStepCount)
    {
        totalSteps = totalStepCount;
        CreateOverlayCanvas();
        CreateInstructionPanel();
        CreateStepCounter();
    }

    private void CreateOverlayCanvas()
    {
        var canvasObj = new GameObject("TutorialOverlayCanvas");
        overlayCanvas = canvasObj.AddComponent<Canvas>();

        // WorldSpace + VR follow — slightly closer than game UI so overlay renders in front
        VRUIAnchor.Setup(overlayCanvas, dist: 2.4f, sortOrder: 20);
    }

    private void CreateInstructionPanel()
    {
        // Background panel
        var panelObj = new GameObject("InstructionPanel");
        panelObj.transform.SetParent(overlayCanvas.transform, false);

        var panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);

        var panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0f, -15f); // top of screen
        panelRect.sizeDelta = new Vector2(600f, 70f);

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
        rect.anchoredPosition = new Vector2(0f, -90f); // below instruction panel
        rect.sizeDelta = new Vector2(200f, 30f);
    }

    /// <summary>
    /// Updates the instruction text for the current step.
    /// Called every frame by TutorialManager.
    /// </summary>
    public void UpdateTarget(Vector3 worldPosition, string instruction, int currentStep)
    {
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
