using System.Collections;
using UnityEngine;
using Ubiq.Spawning;
using Ubiq.Rooms;

/// <summary>
/// 盘子生成器：当盘子被拿走后自动补货。
///
/// 联机模式：通过房主（LocalIsHost）调用 SpawnWithRoomScope，
///   确保双端都能看到新盘子。非房主不主动生成，只通过 OnNetworkSpawned 追踪。
/// 单机回退：找不到 SpawnManager 时使用本地 Instantiate。
/// </summary>
public class PlateSpawner : MonoBehaviour
{
    [Header("设置")]
    public GameObject platePrefab;
    public float spawnDistance = 0.3f;
    public float checkDelay = 0.5f;

    private GameObject currentPlate;
    private float timer = 0f;
    private bool waiting = false;
    private bool pendingPositioning = false;

    private NetworkSpawnManager spawnManager;
    private RoomClient roomClient;

    private IEnumerator Start()
    {
        spawnManager = NetworkSpawnManager.Find(this);
        roomClient = RoomClient.Find(this);

        if (spawnManager != null)
            spawnManager.OnSpawned.AddListener(OnNetworkSpawned);

        if (spawnManager != null && roomClient != null && roomClient.JoinedRoom)
        {
            // 等待房主选举完成，最多等 5 秒；超时则视为单机/未入房间模式直接生成
            float elapsed = 0f;
            while (!NetworkedGameState.HostDecided && elapsed < 5f)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }
            // 已选出房主且本端为非房主：等待房主通过 OnNetworkSpawned 同步盘子
            if (NetworkedGameState.HostDecided && !NetworkedGameState.LocalIsHost)
                yield break;
            SpawnPlate();
        }
        else
        {
            SpawnPlate(); // 单机回退
        }
    }

    private void OnDestroy()
    {
        if (spawnManager != null)
            spawnManager.OnSpawned.RemoveListener(OnNetworkSpawned);
    }

    private void Update()
    {
        // 联机模式只有房主负责补货；host 未决定（超时回退）时本端也负责
        if (spawnManager != null && roomClient != null && roomClient.JoinedRoom &&
            NetworkedGameState.HostDecided && !NetworkedGameState.LocalIsHost)
            return;

        bool plateGone = !pendingPositioning &&
                         (currentPlate == null ||
                          Vector3.Distance(currentPlate.transform.position, transform.position) > spawnDistance);

        if (plateGone && !waiting)
        {
            waiting = true;
            timer = 0f;
        }

        if (waiting)
        {
            timer += Time.deltaTime;
            if (timer >= checkDelay)
            {
                waiting = false;
                SpawnPlate();
            }
        }
    }

    private void SpawnPlate()
    {
        if (platePrefab == null) return;

        if (spawnManager != null && roomClient != null && roomClient.JoinedRoom && NetworkedGameState.LocalIsHost)
        {
            pendingPositioning = true;
            spawnManager.SpawnWithRoomScope(platePrefab);
        }
        else
        {
            currentPlate = Instantiate(platePrefab, transform.position, transform.rotation);
        }
    }

    private void OnNetworkSpawned(GameObject go, IRoom room, IPeer peer, NetworkSpawnOrigin origin)
    {
        if (platePrefab == null || go.name != platePrefab.name + "(Clone)") return;

        if (!pendingPositioning)
        {
            // 接收方（非房主）：冻结物理，等待房主的 BroadcastInitialPosition 同步位置
            if (origin == NetworkSpawnOrigin.Remote)
            {
                var rbOther = go.GetComponent<Rigidbody>();
                if (rbOther != null) rbOther.isKinematic = true;
            }
            return;
        }

        pendingPositioning = false;
        var rb = go.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        go.transform.SetPositionAndRotation(transform.position, transform.rotation);
        if (rb != null) { rb.isKinematic = false; rb.Sleep(); }
        go.GetComponent<NetworkedGrabbable>()?.ClaimPositionAuthority();
        currentPlate = go;
    }
}
