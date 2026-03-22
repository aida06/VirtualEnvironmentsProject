using UnityEngine;
using System.Collections.Generic;

public class PlateController : MonoBehaviour
{
    [Header("吸附设置")]
    public string pizzaTag = "Pizza";
    public float snapDistance = 0.5f;
    public Vector3 snapOffset = Vector3.zero;

    private Dictionary<Transform, Vector3> snappedPizzas = new Dictionary<Transform, Vector3>();

    void Update()
    {
        GameObject[] pizzas = GameObject.FindGameObjectsWithTag(pizzaTag);
        foreach (GameObject pizzaObj in pizzas)
        {
            Transform pizza = pizzaObj.transform;

            if (!snappedPizzas.ContainsKey(pizza))
            {
                float dist = Vector3.Distance(pizza.position, transform.position);
                if (dist <= snapDistance)
                {
                    SnapPizzaPublic(pizza);
                }
            }
        }

        foreach (var kvp in snappedPizzas)
        {
            kvp.Key.position = transform.position + transform.TransformDirection(kvp.Value);
        }
    }

    public void SnapPizzaPublic(Transform pizza)
    {
        Vector3 offset = snapOffset;
        snappedPizzas.Add(pizza, offset);
    }

    public List<Transform> GetSnappedPizzas()
    {
        return new List<Transform>(snappedPizzas.Keys);
    }

    public void Detach(Transform pizza)
    {
        if (snappedPizzas.ContainsKey(pizza))
            snappedPizzas.Remove(pizza);
    }

    public void DetachAll()
    {
        snappedPizzas.Clear();
    }
}