using Ubiq.Spawning;
using UnityEngine;

public class DoughBallRollable1 : MonoBehaviour
{
    [Header("Result")]
    public GameObject flattenedDoughPrefab;

    [Header("Rolling Settings")]
    public float requiredRollingTime = 3f;

    [Header("Networking")]
    public NetworkSpawnManager networkSpawnManager;

    private float rollingProgress = 0f;
    private bool isFlattened = false;

    private void Awake()
    {
        if (networkSpawnManager == null)
        {
            networkSpawnManager = NetworkSpawnManager.Find(this);
        }

        Debug.Log("[DoughBallRollable1] Awake, networkSpawnManager = " +
            (networkSpawnManager != null ? networkSpawnManager.name : "null"));
    }

    public void SetNetworkSpawnManager(NetworkSpawnManager manager)
    {
        networkSpawnManager = manager;
    }

    public void AddRollingProgress(float amount)
    {
        if (isFlattened)
            return;

        rollingProgress += amount;
        Debug.Log("[DoughBallRollable1] rollingProgress = " + rollingProgress);

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
        Debug.Log("[DoughBallRollable1] Flatten called on " + gameObject.name);

        if (flattenedDoughPrefab == null)
        {
            Debug.LogWarning("[DoughBallRollable1] Flattened Dough Prefab is not assigned.");
            return;
        }

        Debug.Log("[DoughBallRollable1] flattenedDoughPrefab = " + flattenedDoughPrefab.name);

        if (networkSpawnManager == null)
        {
            Debug.LogWarning("[DoughBallRollable1] NetworkSpawnManager not found.");
            return;
        }

        Vector3 spawnPosition = transform.position;
        Quaternion spawnRotation = transform.rotation;

        Debug.Log("[DoughBallRollable1] Trying network spawn at " + spawnPosition);

        GameObject spawned = networkSpawnManager.SpawnWithPeerScope(flattenedDoughPrefab);

        if (spawned != null)
        {
            Debug.Log("[DoughBallRollable1] Spawn success: " + spawned.name);
            spawned.transform.position = spawnPosition;
            spawned.transform.rotation = spawnRotation;
        }
        else
        {
            Debug.LogWarning("[DoughBallRollable1] Spawn returned null.");
        }

        var destroyable = GetComponent<NetworkedDestroyable>();
        if (destroyable == null)
        {
            destroyable = GetComponentInParent<NetworkedDestroyable>();
        }

        if (destroyable != null)
        {
            Debug.Log("[DoughBallRollable1] Calling DestroyForAll on " + destroyable.gameObject.name);
            destroyable.DestroyForAll();
        }
        else
        {
            Debug.LogWarning("[DoughBallRollable1] NetworkedDestroyable not found.");
        }
    }
}