using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// A single order card in the order list. Shows a recipe sprite icon, recipe name,
/// ingredient icons (loaded from Resources/UI/Icons/), and a countdown timer bar.
///
/// If a sprite PNG has not been imported yet, a gray placeholder is shown and a
/// warning is logged to the Console with the expected file path.
/// </summary>
public class OrderCardUI : MonoBehaviour
{
    public bool IsExpired { get; private set; }
    public string RecipeName { get; private set; }

    private float totalTime;
    private float remainingTime;
    private Image timerBarFill;
    private RectTransform cardRect;

    private const string ICON_PATH = "UI/Icons/";

    /// <summary>
    /// Factory method. Creates and returns a fully built order card.
    /// </summary>
    public static OrderCardUI Create(
        Transform parent,
        string recipeName,
        string iconSpriteName,
        string[] ingredientSpriteNames,
        float timeLimit)
    {
        // ---- Root card ----
        var cardObj = new GameObject("OrderCard_" + recipeName);
        cardObj.transform.SetParent(parent, false);
        var card = cardObj.AddComponent<OrderCardUI>();
        card.RecipeName = recipeName;
        card.totalTime = timeLimit;
        card.remainingTime = timeLimit;

        card.cardRect = cardObj.GetComponent<RectTransform>();
        if (card.cardRect == null) card.cardRect = cardObj.AddComponent<RectTransform>();
        card.cardRect.sizeDelta = new Vector2(150f, 240f);

        // Card background
        var bgImage = cardObj.AddComponent<Image>();
        bgImage.color = new Color(0.18f, 0.18f, 0.22f, 0.92f);

        // Vertical layout for all sections
        var vLayout = cardObj.AddComponent<VerticalLayoutGroup>();
        vLayout.padding = new RectOffset(8, 8, 6, 6);
        vLayout.spacing = 4f;
        vLayout.childAlignment = TextAnchor.UpperCenter;
        vLayout.childControlWidth = true;
        vLayout.childControlHeight = true;
        vLayout.childForceExpandWidth = true;
        vLayout.childForceExpandHeight = false;

        // ---- 1. Recipe icon (sprite) ----
        var iconObj = new GameObject("RecipeIcon");
        iconObj.transform.SetParent(cardObj.transform, false);
        var iconImage = iconObj.AddComponent<Image>();
        LoadSpriteInto(iconImage, iconSpriteName);
        iconImage.preserveAspect = true;
        var iconLE = iconObj.AddComponent<LayoutElement>();
        iconLE.preferredHeight = 80f;
        iconLE.preferredWidth = 80f;

        // ---- 2. Recipe name ----
        var nameObj = new GameObject("RecipeName");
        nameObj.transform.SetParent(cardObj.transform, false);
        var nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = recipeName;
        nameText.fontSize = 14f;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = Color.white;
        nameText.fontStyle = FontStyles.Bold;
        nameText.enableWordWrapping = false;
        var nameLE = nameObj.AddComponent<LayoutElement>();
        nameLE.preferredHeight = 18f;

        // ---- 3. Ingredient icons (2x2 grid) ----
        var ingGrid = new GameObject("Ingredients");
        ingGrid.transform.SetParent(cardObj.transform, false);
        ingGrid.AddComponent<RectTransform>();

        var gridLayout = ingGrid.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(44f, 44f);
        gridLayout.spacing = new Vector2(4f, 4f);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 2;
        gridLayout.childAlignment = TextAnchor.UpperCenter;
        gridLayout.padding = new RectOffset(4, 4, 2, 2);

        var ingLE = ingGrid.AddComponent<LayoutElement>();
        ingLE.preferredHeight = 96f; // 2 rows × 44 + spacing + padding

        foreach (var ingName in ingredientSpriteNames)
        {
            var ingObj = new GameObject("Ing_" + ingName);
            ingObj.transform.SetParent(ingGrid.transform, false);

            var ingImage = ingObj.AddComponent<Image>();
            LoadSpriteInto(ingImage, ingName);
            ingImage.preserveAspect = true;
        }

        // ---- 5. Timer bar ----
        var timerBgObj = new GameObject("TimerBarBg");
        timerBgObj.transform.SetParent(cardObj.transform, false);
        var timerBgImage = timerBgObj.AddComponent<Image>();
        timerBgImage.color = new Color(0.25f, 0.25f, 0.25f, 1f);
        var timerBgLE = timerBgObj.AddComponent<LayoutElement>();
        timerBgLE.preferredHeight = 10f;

        // Timer fill (child of background, stretches to fill)
        var timerFillObj = new GameObject("TimerBarFill");
        timerFillObj.transform.SetParent(timerBgObj.transform, false);
        card.timerBarFill = timerFillObj.AddComponent<Image>();
        card.timerBarFill.color = Color.green;
        card.timerBarFill.type = Image.Type.Filled;
        card.timerBarFill.fillMethod = Image.FillMethod.Horizontal;
        card.timerBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        card.timerBarFill.fillAmount = 1f;

        var fillRect = timerFillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        return card;
    }

    // -----------------------------------------------------------------
    // Sprite helper
    // -----------------------------------------------------------------

    /// <summary>
    /// Loads a sprite from Resources/UI/Icons/{spriteName}.
    /// Falls back to a gray placeholder if the file has not been imported yet.
    /// </summary>
    private static void LoadSpriteInto(Image image, string spriteName)
    {
        var sprite = Resources.Load<Sprite>(ICON_PATH + spriteName);
        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Simple;
        }
        else
        {
            // Gray placeholder — replace once you import the real PNG icon
            image.color = new Color(0.55f, 0.55f, 0.55f, 0.8f);
            Debug.LogWarning(
                $"[OrderCardUI] Sprite not found: Resources/{ICON_PATH}{spriteName}\n" +
                $"  -> Place your PNG at: Assets/Resources/UI/Icons/{spriteName}.png\n" +
                $"  -> Then set Texture Type to 'Sprite (2D and UI)' in Import Settings.");
        }
    }

    // -----------------------------------------------------------------
    // Runtime
    // -----------------------------------------------------------------

    private void Update()
    {
        if (IsExpired) return;
        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        remainingTime -= Time.deltaTime;
        float ratio = Mathf.Clamp01(remainingTime / totalTime);

        // Timer bar fill & color (green -> yellow -> red)
        timerBarFill.fillAmount = ratio;
        timerBarFill.color = ratio > 0.5f
            ? Color.Lerp(Color.yellow, Color.green, (ratio - 0.5f) * 2f)
            : Color.Lerp(Color.red, Color.yellow, ratio * 2f);

        // Gentle pulse when time is running low
        if (ratio < 0.25f)
        {
            float pulse = 1f + Mathf.Sin(Time.time * 8f) * 0.03f;
            cardRect.localScale = Vector3.one * pulse;
        }

        if (remainingTime <= 0f)
        {
            IsExpired = true;
        }
    }
}
