using UnityEngine;

public class SauceBottle : MonoBehaviour
{
    [Header("参数")]
    public float detectionRadius = 0.4f;
    public float detectionHeight = 0.5f;
    public float totalAngleNeeded = 1080f;

    [Header("粒子效果")]
    public ParticleSystem sauceParticle;

    [Header("重置设置")]
    public float resetHeight = -0.5f;

    private float accumulatedAngle = 0f;
    private float lastAngle = 0f;
    private bool isTracking = false;
    private PizzaSaucing targetPizza = null;

    private Vector3 startPosition;
    private Quaternion startRotation;

    void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    void Update()
    {
        // 倒置检测（真机测试时取消注释）
        // bool bottleInverted = transform.up.y < 0;
        // if (!bottleInverted)
        // {
        //     StopSaucing();
        //     return;
        // }

        if (transform.position.y < resetHeight)
        {
            ResetBottle();
            return;
        }

        PizzaSaucing pizza = FindPizzaBelow();
        if (pizza == null)
        {
            StopSaucing();
            return;
        }

        if (!isTracking)
        {
            StartSaucing(pizza);
        }

        Vector3 offset = transform.position - targetPizza.transform.position;
        float currentAngle = Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;

        if (isTracking)
        {
            float delta = Mathf.DeltaAngle(lastAngle, currentAngle);
            accumulatedAngle += Mathf.Abs(delta);
        }

        lastAngle = currentAngle;

        float progress = Mathf.Clamp01(accumulatedAngle / totalAngleNeeded);
        targetPizza.UpdateProgress(progress);
    }

    void ResetBottle()
    {
        StopSaucing();
        transform.position = startPosition;
        transform.rotation = startRotation;
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    PizzaSaucing FindPizzaBelow()
{
    Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);
    Debug.Log("检测到碰撞体数量: " + hits.Length);
    foreach (var hit in hits)
    {
        Debug.Log("碰到了: " + hit.gameObject.name + " Tag: " + hit.tag);
        if (hit.CompareTag("pizza"))
        {
            float heightDiff = transform.position.y - hit.transform.position.y;
            Debug.Log("高度差: " + heightDiff);
            if (heightDiff > 0 && heightDiff < detectionHeight)
            {
                return hit.GetComponent<PizzaSaucing>();
            }
        }
    }
    return null;
}
    void StartSaucing(PizzaSaucing pizza)
    {
        isTracking = true;
        targetPizza = pizza;
        accumulatedAngle = 0f;
        Vector3 offset = transform.position - pizza.transform.position;
        lastAngle = Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;
        if (sauceParticle != null) sauceParticle.Play();
    }

    void StopSaucing()
    {
        isTracking = false;
        targetPizza = null;
        if (sauceParticle != null) sauceParticle.Stop();
    }
}