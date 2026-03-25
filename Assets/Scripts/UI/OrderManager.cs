using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the order list displayed horizontally at the top-left of the screen.
/// Orders spawn at intervals with random ingredient combinations.
/// Pizza = dough + meat (fixed).
/// Burger = bread + meat + random subset of {lettuce, tomato}.
/// </summary>
public class OrderManager : MonoBehaviour
{
    [System.Serializable]
    public class RecipeData
    {
        public string name;
        public string iconSpriteName;
        public string[] requiredIngredients;
        public string[] optionalIngredients;
    }

    private static readonly RecipeData[] Recipes =
    {
        new RecipeData
        {
            name = "Pizza-Sausage",
            iconSpriteName = "icon_pizza",
            requiredIngredients = new[] { "icon_dough", "icon_sausage","icon_cheese" }
        },
        new RecipeData
        {
            name = "Pizza-Mushroom",
            iconSpriteName = "icon_pizza",
            requiredIngredients = new[] { "icon_dough", "icon_mushroom","icon_cheese" }
        }
    };

    private Transform canvasTransform;
    private float spawnInterval;
    private float orderTimeLimit;
    private int maxOrders;
    private bool initialized;

    private GameObject orderListPanel;
    private readonly List<OrderCardUI> activeOrders = new List<OrderCardUI>();
    private float spawnTimer;

    public void Initialize(Transform canvas, float interval, float timeLimit, int maxActive)
    {
        canvasTransform = canvas;
        spawnInterval = interval;
        orderTimeLimit = timeLimit;
        maxOrders = maxActive;

        CreateOrderListPanel();
        SpawnOrder(); // spawn first order immediately
        spawnTimer = spawnInterval;
        initialized = true;
    }

    private void Update()
    {
        if (!initialized) return;
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        // Spawn new orders at intervals
        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f && activeOrders.Count < maxOrders)
        {
            SpawnOrder();
            spawnTimer = spawnInterval;
        }

        // Remove expired orders and deduct points (iterate backwards for safe removal)
        for (int i = activeOrders.Count - 1; i >= 0; i--)
        {
            if (activeOrders[i] == null || activeOrders[i].IsExpired)
            {
                // Deduct 10 points for each expired order
                if (activeOrders[i] != null && activeOrders[i].IsExpired)
                {
                    ScoreManager.Instance?.AddScore(-20);
                    Debug.Log($"[OrderManager] Order expired: {activeOrders[i].RecipeName} (-20 points)");
                }
                RemoveOrder(i);
            }
        }
    }

    private void CreateOrderListPanel()
    {
        orderListPanel = new GameObject("OrderListPanel");
        orderListPanel.transform.SetParent(canvasTransform, false);

        var rect = orderListPanel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(20f, -20f);
        rect.sizeDelta = new Vector2(900f, 185f);

        var layout = orderListPanel.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.padding = new RectOffset(5, 5, 5, 5);

        var fitter = orderListPanel.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void SpawnOrder()
    {
        int recipeIndex = Random.Range(0, Recipes.Length);
        var recipe = Recipes[recipeIndex];

        // Build ingredient list: all required + random optional
        var ingredients = new List<string>(recipe.requiredIngredients);
        // foreach (var opt in recipe.optionalIngredients)
        // {
        //     if (Random.value > 0.35f) // ~65% chance to include each optional
        //         ingredients.Add(opt);
        // }

        var card = OrderCardUI.Create(
            orderListPanel.transform,
            recipe.name,
            recipe.iconSpriteName,
            ingredients.ToArray(),
            orderTimeLimit
        );
        activeOrders.Add(card);
    }

    private void RemoveOrder(int index)
    {
        if (index < 0 || index >= activeOrders.Count) return;
        var card = activeOrders[index];
        activeOrders.RemoveAt(index);
        if (card != null && card.gameObject != null)
            Destroy(card.gameObject);
    }

    /// <summary>
    /// Completes and removes the oldest (first) active order.
    /// </summary>
    public void CompleteOldestOrder()
    {
        if (activeOrders.Count > 0)
            RemoveOrder(0);
    }

    /// <summary>
    /// Completes and removes the oldest order matching the given recipe name.
    /// Returns true if a matching order was found and removed.
    /// </summary>
    public bool CompleteOldestOrderByType(string recipeName)
    {
        for (int i = 0; i < activeOrders.Count; i++)
        {
            if (activeOrders[i] != null && activeOrders[i].RecipeName == recipeName)
            {
                Debug.Log($"[OrderManager] Completed order: {recipeName} (index {i})");
                RemoveOrder(i);
                return true;
            }
        }
        Debug.Log($"[OrderManager] No matching order found for: {recipeName}");
        return false;
    }

    /// <summary>
    /// Completes and removes a specific order by index.
    /// </summary>
    public void CompleteOrder(int index)
    {
        RemoveOrder(index);
    }
}
