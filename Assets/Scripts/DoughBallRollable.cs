using UnityEngine;

public class DoughBallRollable : MonoBehaviour
{
    [Header("Rolling Settings")]
    public float requiredRollingTime = 3f;

    private float rollingProgress = 0f;
    private bool isFlattened = false;

    private NetworkedStateObject stateObject;

    private void Start()
    {
        stateObject = GetComponentInParent<NetworkedStateObject>();
    }

    public void AddRollingProgress(float amount)
    {
        if (isFlattened) return;

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
        if (isFlattened) return;

        isFlattened = true;

        if (stateObject != null)
        {
            stateObject.SetState(1);
        }
        else
        {
            Debug.LogWarning("NetworkedStateObject not found in parent.");
        }
    }
}