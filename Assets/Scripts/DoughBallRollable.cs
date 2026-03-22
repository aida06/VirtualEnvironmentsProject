using UnityEngine;

public class DoughBallRollable : MonoBehaviour
{
    [Header("Rolling Settings")]
    public float requiredRollingTime = 3f;

    private float rollingProgress = 0f;
    private bool isFlattened = false;

    private NetworkedStateObject stateObject;
    private DoughBallPlacement placement;

    private void Awake()
    {
        stateObject = GetComponentInParent<NetworkedStateObject>();
        placement = GetComponentInParent<DoughBallPlacement>();

        Debug.Log($"[DoughBallRollable] stateObject found = {stateObject != null}");
        Debug.Log($"[DoughBallRollable] placement found = {placement != null}");
    }

    public void AddRollingProgress(float amount)
    {
        if (isFlattened)
            return;

        if (placement != null && !placement.IsPlacedOnTable())
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

        if (stateObject != null)
        {
            stateObject.SetStateAndPose(1, transform.position, transform.rotation);
            Debug.Log("[DoughBallRollable] Flatten success, switched to state 1");
        }
        else
        {
            Debug.LogWarning("NetworkedStateObject not found.");
        }
    }
}