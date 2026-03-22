using UnityEngine;
using System.Collections;

public class DeliveryCounter : MonoBehaviour
{
    [Header("设置")]
    public float deliveryTime = 3f;

    void OnTriggerEnter(Collider other)
    {
        // 检测盘子进入
        PlateController plate = other.GetComponent<PlateController>();
        if (plate == null) return;

        // 检查盘子上有没有烤熟的披萨
        foreach (Transform pizza in plate.GetSnappedPizzas())
        {
            if (pizza.gameObject.name == "PizzaMushroomsBaked" ||
                pizza.gameObject.name == "PizzaPepperoniBaked")
            {
                Debug.Log("收到披萨: " + pizza.gameObject.name);
                StartCoroutine(DeliverAfterDelay(plate, pizza));
                break;
            }
        }
    }

    IEnumerator DeliverAfterDelay(PlateController plate, Transform pizza)
    {
        yield return new WaitForSeconds(deliveryTime);

        if (pizza != null)
        {
            plate.Detach(pizza);
            Destroy(pizza.gameObject);

            // Award points and complete the oldest order
            if (GameManager.Instance != null)
                GameManager.Instance.CompleteOldestOrder();
        }
    }
}