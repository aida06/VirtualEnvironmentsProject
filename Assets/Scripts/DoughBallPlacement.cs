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

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (mainCollider == null)
        {
            mainCollider = GetComponent<Collider>();
        }
    }

    private void Update()
    {
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
        if (placedOnTable)
            return;

        if (collision.collider.CompareTag(tableTag))
        {
            tableContactTime = Time.time;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (placedOnTable)
            return;

        if (collision.collider.CompareTag(tableTag))
        {
            tableContactTime = -1f;
        }
    }

    private void PlaceOnTable()
    {
        placedOnTable = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (mainCollider != null)
        {
            mainCollider.enabled = false;
        }
    }

    public bool IsPlacedOnTable()
    {
        return placedOnTable;
    }
}