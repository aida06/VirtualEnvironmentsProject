using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Step-by-step tutorial controller. Guides the player through the pizza-making process
/// with a spotlight overlay highlighting each target object.
///
/// SETUP: Attach to a GameObject in the Tutorial scene alongside GameManager (with isTutorialMode = true).
/// The TutorialManager automatically finds target objects by name and creates the overlay + NEXT button.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    [System.Serializable]
    public class TutorialStep
    {
        public string targetObjectName;
        public string instruction;
        [HideInInspector] public GameObject targetObject;
    }

    private TutorialStep[] steps;
    private int currentStepIndex;
    private TutorialOverlay overlay;
    private GameObject nextButton;
    private bool tutorialActive;

    private void Start()
    {
        // Only run in tutorial mode
        if (GameManager.Instance == null || !GameManager.Instance.IsTutorialMode)
        {
            enabled = false;
            return;
        }

        DefineSteps();
        ResolveTargets();
        CreateOverlay();
        CreateNextButton();
        CreateFrozenOrderCard();

        currentStepIndex = 0;
        tutorialActive = true;
    }

    private void DefineSteps()
    {
        steps = new TutorialStep[]
        {
            new TutorialStep
            {
                targetObjectName = "BoxWithDough.002",
                instruction = "Pick up dough from the box"
            },
            new TutorialStep
            {
                targetObjectName = "RollingPin",
                instruction = "Use the rolling pin to flatten the dough"
            },
            new TutorialStep
            {
                targetObjectName = "BoxWithSausages.002",
                instruction = "Pick up a sausage from the box"
            },
            new TutorialStep
            {
                targetObjectName = "CuttingTable",
                instruction = "Use the knife to cut the sausage"
            },
            new TutorialStep
            {
                targetObjectName = "BoxWithCheese.002",
                instruction = "Pick up cheese from the box"
            },
            new TutorialStep
            {
                targetObjectName = "RollingTable",
                instruction = "Combine dough, sausage, and cheese to assemble pizza"
            },
            new TutorialStep
            {
                targetObjectName = "SauceLocation",
                instruction = "Squeeze sauce onto the pizza"
            },
            new TutorialStep
            {
                targetObjectName = "PlateSpawn",
                instruction = "Place the sauced pizza on a plate"
            },
            new TutorialStep
            {
                targetObjectName = "Oven.004",
                instruction = "Put the plated pizza in the oven to bake"
            },
            new TutorialStep
            {
                targetObjectName = "WallFood.005",
                instruction = "Deliver the cooked pizza to the serving window"
            }
        };
    }

    private void ResolveTargets()
    {
        foreach (var step in steps)
        {
            step.targetObject = GameObject.Find(step.targetObjectName);
            if (step.targetObject == null)
            {
                Debug.LogWarning($"[TutorialManager] Target object '{step.targetObjectName}' not found in scene.");
            }
        }
    }

    private void CreateOverlay()
    {
        var overlayGO = new GameObject("TutorialOverlay");
        overlayGO.transform.SetParent(transform);
        overlay = overlayGO.AddComponent<TutorialOverlay>();
        overlay.Initialize(steps.Length);
    }

    private void CreateNextButton()
    {
        var canvasTransform = overlay.GetCanvasTransform();
        if (canvasTransform == null) return;

        // Button panel
        nextButton = new GameObject("NextButton");
        nextButton.transform.SetParent(canvasTransform, false);

        var btnImage = nextButton.AddComponent<Image>();
        btnImage.color = new Color(0.22f, 0.70f, 0.35f, 1f);

        var btn = nextButton.AddComponent<Button>();
        btn.targetGraphic = btnImage;
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.30f, 0.82f, 0.45f, 1f);
        colors.pressedColor = new Color(0.15f, 0.50f, 0.25f, 1f);
        btn.colors = colors;
        btn.onClick.AddListener(AdvanceStep);

        var btnRect = nextButton.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 1f);
        btnRect.anchorMax = new Vector2(0.5f, 1f);
        btnRect.pivot = new Vector2(0.5f, 1f);
        btnRect.anchoredPosition = new Vector2(0f, -130f); // below step counter
        btnRect.sizeDelta = new Vector2(200f, 60f);

        // Button label
        var textObj = new GameObject("Label");
        textObj.transform.SetParent(nextButton.transform, false);
        var text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "NEXT";
        text.fontSize = 28f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.fontStyle = FontStyles.Bold;
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// Creates a frozen (non-expiring) pizza order card at the top-left of the game UI canvas.
    /// Ingredients: dough + sausage + cheese, matching the tutorial recipe.
    /// </summary>
    private void CreateFrozenOrderCard()
    {
        if (GameManager.Instance == null || GameManager.Instance.UICanvas == null) return;

        // Create a container panel anchored to top-left (same style as OrderManager's panel)
        var panelObj = new GameObject("TutorialOrderPanel");
        panelObj.transform.SetParent(GameManager.Instance.UICanvas.transform, false);

        var panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(20f, -20f);
        panelRect.sizeDelta = new Vector2(200f, 260f);

        // Create the order card as a child — use float.MaxValue so the timer never expires
        OrderCardUI.Create(
            panelObj.transform,
            "Pizza",
            "icon_pizza",
            new[] { "icon_dough", "icon_sausage", "icon_cheese" },
            float.MaxValue
        );
    }

    private void Update()
    {
        if (!tutorialActive) return;
        if (currentStepIndex >= steps.Length) return;

        var step = steps[currentStepIndex];
        if (step.targetObject != null)
        {
            // Use the target object's world position (center of its bounds if available)
            Vector3 targetPos = step.targetObject.transform.position;
            var renderer = step.targetObject.GetComponent<Renderer>();
            if (renderer != null)
                targetPos = renderer.bounds.center;

            overlay.UpdateTarget(targetPos, step.instruction, currentStepIndex + 1);
        }
        else
        {
            // Fallback: show instruction at screen center
            Vector3 fallbackPos = Camera.main != null
                ? Camera.main.transform.position + Camera.main.transform.forward * 3f
                : Vector3.zero;
            overlay.UpdateTarget(fallbackPos, step.instruction, currentStepIndex + 1);
        }
    }

    private void AdvanceStep()
    {
        currentStepIndex++;

        if (currentStepIndex >= steps.Length)
        {
            CompleteTutorial();
        }
    }

    private void CompleteTutorial()
    {
        tutorialActive = false;

        // Hide overlay and NEXT button
        overlay.Hide();
        if (nextButton != null) nextButton.SetActive(false);

        // Award score for the pizza delivery
        ScoreManager.Instance?.AddScore(50);

        // Trigger tutorial completion (shows TutorialGameOverUI)
        GameManager.Instance?.TutorialComplete();
    }
}
