using UnityEngine;
using System.Collections;

/// <summary>
/// Attach this script to WallFood.005 (the delivery point).
/// It auto-adds a trigger BoxCollider if none exists.
/// When a Plate carrying a baked pizza enters the trigger zone,
/// both the Plate and Pizza are destroyed and the player earns points.
/// </summary>
public class DeliveryCounter : MonoBehaviour
{
    [Header("Settings")]
    public float deliveryTime = 3f;
    public Vector3 triggerSize = new Vector3(0.3f, 0.3f, 0.3f);

    private bool isProcessing = false; // guard against duplicate deliveries

    private void Awake()
    {
        // Auto-add a trigger collider if this object has none
        Collider existingCollider = GetComponent<Collider>();
        if (existingCollider == null)
        {
            var box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = triggerSize;
            Debug.Log($"[DeliveryCounter] Auto-added trigger BoxCollider (size {triggerSize}) to {gameObject.name}");
        }
        else if (!existingCollider.isTrigger)
        {
            // If a collider exists but is not a trigger, add a separate trigger collider
            var box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = triggerSize;
            Debug.Log($"[DeliveryCounter] Added extra trigger BoxCollider to {gameObject.name}");
        }
    }

    private bool TryStartDelivery(Collider other)
    {
        if (isProcessing) return false;

        PlateController plate = other.GetComponent<PlateController>();
        if (plate == null) plate = other.GetComponentInParent<PlateController>();
        if (plate == null) return false;

        foreach (Transform pizza in plate.GetSnappedPizzas())
        {
            string pizzaName = pizza.gameObject.name;
            int score = GetPizzaScore(pizzaName);
            string recipe = GetRecipeName(pizzaName);
            if (score > 0)
            {
                Debug.Log($"[DeliveryCounter] Valid pizza found: {pizzaName} → {recipe} (+{score})");
                isProcessing = true;
                StartCoroutine(DeliverAfterDelay(plate, pizza, score, recipe));
                return true;
            }
        }
        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryStartDelivery(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryStartDelivery(other);
    }

    /// <summary>
    /// Returns the score for a recognized pizza name, or 0 if not recognized.
    /// </summary>
    private int GetPizzaScore(string pizzaName)
    {
        if (pizzaName.Contains("PizzaPepperoniBaked")) return 50;
        if (pizzaName.Contains("PizzaMushroomBaked"))  return 40;
        return 0;
    }

    /// <summary>
    /// Maps a baked pizza model name to the corresponding order recipe name.
    /// </summary>
    private string GetRecipeName(string pizzaName)
    {
        if (pizzaName.Contains("PizzaPepperoniBaked")) return "Pizza-Sausage";
        if (pizzaName.Contains("PizzaMushroomBaked"))  return "Pizza-Mushroom";
        return null;
    }

    IEnumerator DeliverAfterDelay(PlateController plate, Transform pizza, int score, string recipeName)
    {
        yield return new WaitForSeconds(deliveryTime);

        if (pizza != null)
        {
            plate.Detach(pizza);
            Destroy(pizza.gameObject);
        }

        if (plate != null)
            Destroy(plate.gameObject);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.CompleteOldestOrder(score, recipeName);
            Debug.Log($"[DeliveryCounter] +{score} points, removed order: {recipeName}");
        }

        isProcessing = false;
    }
}
