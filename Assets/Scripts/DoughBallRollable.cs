using System.Collections;
using UnityEngine;
using Ubiq.Spawning;
using Ubiq.Rooms;

public class DoughBallRollable : MonoBehaviour
{
    [Header("Result")]
    public GameObject flattenedDoughPrefab;

    [Header("Rolling Settings")]
    public float requiredRollingTime = 3f;

    [Header("Audio")]
    public AudioClip rollingSound;

    private float rollingProgress = 0f;
    private bool isFlattened = false;
    private AudioSource audioSource;

    private NetworkSpawnManager spawnManager;
    private RoomClient roomClient;
    private bool pendingSpawn = false;
    private Vector3 pendingPos;
    private Quaternion pendingRot;

    private void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = rollingSound;
        audioSource.loop = true;
        audioSource.playOnAwake = false;

        spawnManager = NetworkSpawnManager.Find(this);
        roomClient = RoomClient.Find(this);
        if (spawnManager != null)
            spawnManager.OnSpawned.AddListener(OnNetworkSpawned);
    }

    private void OnDestroy()
    {
        if (spawnManager != null)
            spawnManager.OnSpawned.RemoveListener(OnNetworkSpawned);
    }

    public void AddRollingProgress(float amount)
    {
        if (isFlattened) return;
        if (!audioSource.isPlaying) audioSource.Play();
        rollingProgress += amount;
        if (rollingProgress >= requiredRollingTime) Flatten();
    }

    public bool IsFlattened() => isFlattened;

    private void Flatten()
    {
        if (isFlattened) return;
        isFlattened = true;
        audioSource.Stop();

        if (flattenedDoughPrefab == null)
        {
            Debug.LogWarning("DoughBallRollable: flattenedDoughPrefab is not assigned.");
            return;
        }

        pendingPos = transform.position;
        pendingRot = transform.rotation;

        if (spawnManager != null)
        {
            pendingSpawn = true;
            spawnManager.SpawnWithRoomScope(flattenedDoughPrefab);
            // 推迟一帧销毁，确保 OnNetworkSpawned 先执行完毕
            StartCoroutine(DespawnSelf());
        }
        else
        {
            Instantiate(flattenedDoughPrefab, pendingPos, pendingRot);
            Destroy(gameObject);
        }
    }

    private void OnNetworkSpawned(GameObject go, IRoom room, IPeer peer, NetworkSpawnOrigin origin)
    {
        if (flattenedDoughPrefab != null && go.name != flattenedDoughPrefab.name + "(Clone)") return;

        if (!pendingSpawn)
        {
            if (origin == NetworkSpawnOrigin.Remote)
            {
                var rbOther = go.GetComponent<Rigidbody>();
                if (rbOther != null) rbOther.isKinematic = true;
            }
            return;
        }

        pendingSpawn = false;
        var rb = go.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        go.transform.SetPositionAndRotation(pendingPos, pendingRot);
        if (rb != null) { rb.isKinematic = false; rb.Sleep(); }
        go.GetComponent<NetworkedGrabbable>()?.ClaimPositionAuthority();
    }

    private IEnumerator DespawnSelf()
    {
        // 等待 OnNetworkSpawned 确认（pendingSpawn 清零）后再销毁，
        // 避免异步回调在销毁后才触发导致面饼无人定位
        float timeout = 3f;
        while (pendingSpawn && timeout > 0f)
        {
            yield return null;
            timeout -= Time.deltaTime;
        }
        if (spawnManager != null) spawnManager.Despawn(gameObject);
        var ng = GetComponent<NetworkedGrabbable>();
        if (ng != null) ng.BroadcastDestroy();
        else Destroy(gameObject);
    }
}
