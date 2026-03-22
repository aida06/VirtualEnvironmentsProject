using UnityEngine;

public class DoughBallPlacement : MonoBehaviour
{
    [Header("Table Detection")]
    public string tableTag = "Table";

    [Header("Settle Settings")]
    public float settleDelay = 0.15f;

    [Header("Main Collider")]
    public Collider mainCollider;

    private Rigidbody rb;
    private bool placedOnTable = false;
    private float tableContactTime = -1f;

    private NetworkedStateObject stateObject;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        stateObject = GetComponent<NetworkedStateObject>();

        if (mainCollider == null)
        {
            mainCollider = GetComponent<Collider>();
        }
    }

    private void Update()
    {
        // 非 owner 不参与放置逻辑
        if (stateObject != null && !stateObject.IsOwner())
            return;

        if (!placedOnTable && tableContactTime > 0f)
        {
            if (Time.time - tableContactTime >= settleDelay)
            {
                PlaceOnTable();
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 非 owner 不参与放置逻辑
        if (stateObject != null && !stateObject.IsOwner())
            return;

        if (placedOnTable)
            return;

        if (collision.collider.CompareTag(tableTag))
        {
            tableContactTime = Time.time;
            Debug.Log("[Placement] Table contact started");
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        // 非 owner 不参与放置逻辑
        if (stateObject != null && !stateObject.IsOwner())
            return;

        if (placedOnTable)
            return;

        if (collision.collider.CompareTag(tableTag))
        {
            tableContactTime = -1f;
            Debug.Log("[Placement] Table contact cancelled");
        }
    }

    private void PlaceOnTable()
    {
        // 双保险
        if (stateObject != null && !stateObject.IsOwner())
            return;

        placedOnTable = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // 先不要关主 collider，避免 XR / 联机同步异常
         if (mainCollider != null)
         {
             mainCollider.enabled = false;
         }

        // 放下后，把当前 root 的最终位置再同步一次
        if (stateObject != null)
        {
            stateObject.SetStateAndPose(
                stateObject.currentState,
                stateObject.transform.position,
                stateObject.transform.rotation
            );
        }

        Debug.Log($"[Placement] Placed on table at {transform.position}");
    }

    public bool IsPlacedOnTable()
    {
        return placedOnTable;
    }

    public void SetPlacedOnTable(bool value)
    {
        placedOnTable = value;
    }
}