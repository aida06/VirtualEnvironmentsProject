using UnityEngine;

public class DoughBallRollable : MonoBehaviour
{
    [Header("Result")]
    public GameObject flattenedDoughPrefab;

    [Header("Rolling Settings")]
    public float requiredRollingTime = 3f;

    private float rollingProgress = 0f;
    private bool isFlattened = false;

    public void AddRollingProgress(float amount)
    {
        if (isFlattened)
            return;

        rollingProgress += amount;

        if (rollingProgress >= requiredRollingTime)
        {
            Flatten();
        }
    }

    public bool IsFlattened()
    {
        return isFlattened;
    }

    private void Flatten()
    {
        if (isFlattened)
            return;

        isFlattened = true;

        if (flattenedDoughPrefab == null)
        {
            Debug.LogWarning("Flattened Dough Prefab is not assigned.");
            return;
        }

        Vector3 spawnPosition = transform.position;
        Quaternion spawnRotation = transform.rotation;

        Instantiate(flattenedDoughPrefab, spawnPosition, spawnRotation);
        Destroy(gameObject);
    }
}