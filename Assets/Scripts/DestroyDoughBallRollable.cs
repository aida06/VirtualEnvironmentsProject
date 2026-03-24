using UnityEngine;
using Ubiq.Spawning;

public class DestoryDoughBallRollable : MonoBehaviour
{
    [Header("Result")]
    public GameObject flattenedDoughPrefab;

    [Header("Rolling Settings")]
    public float requiredRollingTime = 3f;

    [Header("Audio")]
    public AudioClip rollingSound;

    private float rollingProgress = 0f;

    private bool isFlattened = false;
    private bool flattenMessageSent = false;
    private bool flattenApplied = false;

    private AudioSource audioSource;
    private NetworkSpawnManager networkSpawnManager;
    private NetworkedDestroyable networkedDestroyable;

    private void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = rollingSound;
        audioSource.loop = true;
        audioSource.playOnAwake = false;

        networkedDestroyable = GetComponent<NetworkedDestroyable>();

        if (networkedDestroyable == null)
        {
            Debug.LogWarning("[DestoryDoughBallRollable] NetworkedDestroyable not found on DoughBall.");
        }
    }

    public void SetNetworkSpawnManager(NetworkSpawnManager manager)
    {
        networkSpawnManager = manager;
    }

    public void AddRollingProgress(float amount)
    {
        if (isFlattened || flattenApplied)
            return;

        if (audioSource != null && rollingSound != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }

        rollingProgress += amount;

        if (rollingProgress >= requiredRollingTime)
        {
            TrySendFlatten();
        }
    }

    public bool IsFlattened()
    {
        return isFlattened;
    }

    private void TrySendFlatten()
    {
        if (isFlattened || flattenApplied || flattenMessageSent)
            return;

        flattenMessageSent = true;

        Vector3 spawnPosition = transform.position;
        Quaternion spawnRotation = transform.rotation;

        if (networkedDestroyable != null)
        {
            networkedDestroyable.SendFlatten(spawnPosition, spawnRotation);
        }
        else
        {
            Debug.LogWarning("[DestoryDoughBallRollable] Cannot send flatten message because NetworkedDestroyable is missing.");
        }

        // 不要本地立刻执行，等网络消息回来再统一执行
        // ApplyFlattenFromNetwork(spawnPosition, spawnRotation);
    }

    public void ApplyFlattenFromNetwork(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        if (flattenApplied || isFlattened)
            return;

        flattenApplied = true;
        isFlattened = true;

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        if (flattenedDoughPrefab == null)
        {
            Debug.LogWarning("[DestoryDoughBallRollable] Flattened Dough Prefab is not assigned.");
            return;
        }

        if (networkSpawnManager == null)
        {
            Debug.LogWarning("[DestoryDoughBallRollable] NetworkSpawnManager is not assigned.");
            return;
        }

        GameObject spawned = networkSpawnManager.SpawnWithPeerScope(flattenedDoughPrefab);
        if (spawned != null)
        {
            spawned.transform.position = spawnPosition;
            spawned.transform.rotation = spawnRotation;
        }

        Destroy(gameObject);
    }
}