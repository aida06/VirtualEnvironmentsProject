using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections.Generic;

public class PlateController : MonoBehaviour
{
    [Header("吸附设置")]
    public string pizzaTag = "Pizza";
    public float snapDistance = 0.5f;
    public Vector3 snapOffset = Vector3.zero;

    private Dictionary<Transform, Vector3> snappedPizzas = new Dictionary<Transform, Vector3>();

    // 缓存 FindGameObjectsWithTag 结果，避免每帧全场搜索
    private GameObject[] cachedPizzas = new GameObject[0];
    private float cacheTimer = 0f;
    private const float CACHE_INTERVAL = 0.1f; // 每秒刷新 10 次

    void Update()
    {
        // 定时刷新 Pizza 列表
        cacheTimer -= Time.deltaTime;
        if (cacheTimer <= 0f)
        {
            cachedPizzas = GameObject.FindGameObjectsWithTag(pizzaTag);
            cacheTimer = CACHE_INTERVAL;
        }

        // 检测新的吸附（未被吸附、在范围内、且未被玩家手持）
        foreach (var pizzaObj in cachedPizzas)
        {
            if (pizzaObj == null) continue;
            Transform pizza = pizzaObj.transform;
            if (snappedPizzas.ContainsKey(pizza)) continue;
            // 正在被抓取时不吸附，避免 kinematic 冲突导致弹飞
            var grab = pizzaObj.GetComponent<XRGrabInteractable>();
            if (grab != null && grab.isSelected) continue;
            if (Vector3.Distance(pizza.position, transform.position) <= snapDistance)
                SnapPizzaPublic(pizza);
        }

        // 每帧更新已吸附 pizza 的位置（跟随盘子），同时清理已被 Despawn 销毁的 null 条目
        List<Transform> nullKeys = null;
        foreach (var kvp in snappedPizzas)
        {
            if (kvp.Key == null)
            {
                if (nullKeys == null) nullKeys = new List<Transform>();
                nullKeys.Add(kvp.Key);
            }
            else
            {
                // 正在被抓取时跳过位置覆盖，否则会与 XR 抓取产生位置冲突
                var grab = kvp.Key.GetComponent<XRGrabInteractable>();
                if (grab != null && grab.isSelected) continue;
                kvp.Key.position = transform.position + transform.TransformDirection(kvp.Value);
            }
        }
        if (nullKeys != null)
            foreach (var key in nullKeys) snappedPizzas.Remove(key);
    }

    public void SnapPizzaPublic(Transform pizza)
    {
        if (snappedPizzas.ContainsKey(pizza)) return;
        snappedPizzas.Add(pizza, snapOffset);

        // 吸附时设为 kinematic，防止物理引擎和 NetworkedGrabbable 远端插值干扰
        var rb = pizza.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        pizza.GetComponent<NetworkedGrabbable>()?.SetSnapped(true);
    }

    public List<Transform> GetSnappedPizzas()
    {
        return new List<Transform>(snappedPizzas.Keys);
    }

    public void Detach(Transform pizza)
    {
        if (!snappedPizzas.ContainsKey(pizza)) return;
        snappedPizzas.Remove(pizza);

        pizza.GetComponent<NetworkedGrabbable>()?.SetSnapped(false);

        var rb = pizza.GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = false; rb.Sleep(); }
    }

    public void DetachAll()
    {
        foreach (var pizza in snappedPizzas.Keys)
        {
            if (pizza == null) continue;
            pizza.GetComponent<NetworkedGrabbable>()?.SetSnapped(false);
            var rb = pizza.GetComponent<Rigidbody>();
            if (rb != null) { rb.isKinematic = false; rb.Sleep(); }
        }
        snappedPizzas.Clear();
    }
}
