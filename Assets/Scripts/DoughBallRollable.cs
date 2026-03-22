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
        {
            Debug.Log("[DoughBallRollable] Not placed on table yet, skip rolling.");
            return;
        }

        rollingProgress += amount;
        Debug.Log($"[DoughBallRollable] rollingProgress = {rollingProgress:F2} / {requiredRollingTime}");

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

        if (stateObject != null && !stateObject.hasOwner)
        {
            stateObject.ClaimOwnership();
        }

        if (stateObject != null && !stateObject.IsOwner())
        {
            Debug.Log("[DoughBallRollable] Not owner, ignore flatten.");
            return;
        }

        isFlattened = true;

        if (stateObject != null)
        {
            stateObject.SetStateAndPose(
                1,
                stateObject.transform.position,
                stateObject.transform.rotation
            );

            Debug.Log("[DoughBallRollable] Flatten success, switched to state 1");
        }
        else
        {
            Debug.LogWarning("NetworkedStateObject not found.");
        }
    }
}