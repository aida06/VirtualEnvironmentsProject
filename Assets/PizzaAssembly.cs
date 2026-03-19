using System.Collections.Generic;
using UnityEngine;

[System.Serializable]  // 👈 这个必须有，不然Inspector不显示
public class PizzaRecipe
{
    public string recipeName;

    [Header("需要的配料Tag")]
    public string[] requiredIngredients;

    [Header("生成的Pizza")]
    public GameObject resultPrefab;
}

public class PizzaAssembly : MonoBehaviour
{
    [Header("所有配方")]
    public List<PizzaRecipe> recipes = new List<PizzaRecipe>();

    private HashSet<string> currentIngredients = new HashSet<string>();
    private List<GameObject> ingredientObjects = new List<GameObject>();

    private HashSet<string> validIngredientTags = new HashSet<string> { "sausage", "cheese", "mushroom" };

    void OnTriggerEnter(Collider other)
{
    if (!validIngredientTags.Contains(other.tag)) return;

    string tag = other.tag;

    if (!currentIngredients.Contains(tag))
    {
        currentIngredients.Add(tag);
        ingredientObjects.Add(other.gameObject);
        Debug.Log("加入配料: " + tag);
    }

    TryAssemble();
}

    void OnTriggerExit(Collider other)
    {
        string tag = other.tag;

        if (currentIngredients.Contains(tag))
        {
            currentIngredients.Remove(tag);
            ingredientObjects.Remove(other.gameObject);

            Debug.Log("移除配料: " + tag);
        }
    }

    void TryAssemble()
    {
        foreach (var recipe in recipes)
        {
            if (MatchRecipe(recipe))
            {
                AssemblePizza(recipe);
                return;
            }
        }
    }

    bool MatchRecipe(PizzaRecipe recipe)
    {
        foreach (string ingredient in recipe.requiredIngredients)
        {
            if (!currentIngredients.Contains(ingredient))
            {
                return false;
            }
        }
        return true;
    }

    void AssemblePizza(PizzaRecipe recipe)
    {
        if (recipe.resultPrefab == null)
        {
            Debug.LogError("配方 " + recipe.recipeName + " 没有 prefab！");
            return;
        }

        Debug.Log("生成Pizza: " + recipe.recipeName);

        GameObject pizza = Instantiate(
            recipe.resultPrefab,
            transform.position + Vector3.up * 0.05f,
            Quaternion.identity
        );

        pizza.name = recipe.recipeName;

        foreach (var obj in ingredientObjects)
        {
            Destroy(obj);
        }

        ingredientObjects.Clear();
        currentIngredients.Clear();

        Destroy(gameObject);
    }
}