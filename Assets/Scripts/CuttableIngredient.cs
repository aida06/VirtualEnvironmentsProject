using UnityEngine;

public class CuttableIngredient : MonoBehaviour
{
    [Header("Cut Result")]
    public GameObject cutResultPrefab;

    [Header("Cut Settings")]
    public int requiredCutCount = 3;

    private int currentCutCount = 0;
    private bool isCutComplete = false;

    public void AddCut()
    {
        if (isCutComplete)
            return;

        currentCutCount++;

        if (currentCutCount >= requiredCutCount)
        {
            CompleteCut();
        }
    }

    public bool IsCutComplete()
    {
        return isCutComplete;
    }

    private void CompleteCut()
    {
        if (isCutComplete)
            return;

        isCutComplete = true;

        if (cutResultPrefab == null)
        {
            Debug.LogWarning("Cut result prefab not assigned.");
            return;
        }

        Vector3 spawnPosition = transform.position + Vector3.up * 0.02f;
        Quaternion spawnRotation = transform.rotation;

        GameObject result = Instantiate(cutResultPrefab, spawnPosition, spawnRotation);

        Rigidbody resultRb = result.GetComponent<Rigidbody>();
        if (resultRb != null)
        {
            resultRb.linearVelocity = Vector3.zero;
            resultRb.angularVelocity = Vector3.zero;
        }

        Destroy(gameObject);
    }
}
