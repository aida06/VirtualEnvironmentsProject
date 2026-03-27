using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Ubiq.Spawning;
using Ubiq.Rooms;

/// <summary>
/// 物品箱：XR 交互触发后生成物品。
///
/// 联机模式：通过 NetworkSpawnManager.SpawnWithRoomScope() 生成，
///   所有玩家都会同步看到该物体出现。
///   前提：itemPrefab 必须已注册在 NetworkSpawnManager 的 PrefabCatalogue 里。
///
/// 单机回退：找不到 NetworkSpawnManager 时，回退到本地 Instantiate。
/// </summary>
public class ItemBox : MonoBehaviour
{
    [Header("Item Settings")]
    public GameObject itemPrefab;
    public Transform spawnPoint;

    [Header("XR Interaction")]
    public XRSimpleInteractable interactable;

    private NetworkSpawnManager spawnManager;
    private RoomClient roomClient;

    // 记录「本次我触发的生成」标志，用于在 OnSpawned 回调中定位该物体并设置位置
    private bool pendingPositioning = false;
    private Vector3 pendingPos;
    private Quaternion pendingRot;

    private void Reset()
    {
        interactable = GetComponent<XRSimpleInteractable>();
    }

    private void Start()
    {
        // 找到场景中的 NetworkSpawnManager（挂在 NetworkScene 下）
        spawnManager = NetworkSpawnManager.Find(this);
        roomClient = RoomClient.Find(this);

        if (spawnManager != null)
        {
            spawnManager.OnSpawned.AddListener(OnNetworkSpawned);
        }
    }

    private void OnDestroy()
    {
        if (spawnManager != null)
            spawnManager.OnSpawned.RemoveListener(OnNetworkSpawned);
    }

    private void OnEnable()
    {
        if (interactable != null)
            interactable.activated.AddListener(OnActivated);
    }

    private void OnDisable()
    {
        if (interactable != null)
            interactable.activated.RemoveListener(OnActivated);
    }

    private void OnActivated(ActivateEventArgs args)
    {
        SpawnItem();
    }

    public GameObject SpawnItem()
    {
        if (itemPrefab == null)
        {
            Debug.LogWarning("ItemBox: itemPrefab is not assigned.");
            return null;
        }

        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position + Vector3.up * 0.5f;
        Quaternion rot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        if (spawnManager != null && roomClient != null)
        {
            // 联机模式：记录目标位置，等待 OnSpawned 回调
            pendingPos = pos;
            pendingRot = rot;
            pendingPositioning = true;
            Debug.Log($"[ItemBox] SpawnWithRoomScope: {itemPrefab.name}");
            spawnManager.SpawnWithRoomScope(itemPrefab);
            return null; // 实际 GameObject 通过 OnSpawned 处理
        }
        else
        {
            Debug.Log($"[ItemBox] Fallback Instantiate: {itemPrefab.name} (spawnManager={spawnManager}, roomClient={roomClient})");
            // 单机回退
            return Instantiate(itemPrefab, pos, rot);
        }
    }

    /// <summary>
    /// NetworkSpawnManager 在任意玩家生成物体后，会在所有 Peer 上触发此回调。
    /// 只有「发起生成的那个 ItemBox」才负责定位该物体。
    /// </summary>
    private void OnNetworkSpawned(GameObject go, IRoom room, IPeer peer, NetworkSpawnOrigin origin)
    {
        Debug.Log($"[ItemBox] OnNetworkSpawned fired: go.name={go.name}, myPrefab={itemPrefab?.name}, pending={pendingPositioning}");

        // 用 prefab 名字匹配，确保是本 ItemBox 负责的那类物体
        if (itemPrefab != null && go.name != itemPrefab.name + "(Clone)")
        {
            Debug.Log($"[ItemBox] Name mismatch: go.name={go.name} expected {itemPrefab.name}(Clone)");
            return;
        }

        // 只处理本机触发的生成（pendingPositioning 仅在本机发起 Spawn 时为 true）
        if (!pendingPositioning)
        {
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
        go.transform.SetPositionAndRotation(pendingPos, pendingRot);
        if (rb != null) { rb.isKinematic = false; rb.Sleep(); }
        go.GetComponent<NetworkedGrabbable>()?.ClaimPositionAuthority();
        Debug.Log($"[ItemBox] Positioned {go.name} at {pendingPos}");
    }
}
