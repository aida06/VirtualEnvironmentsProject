using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Ubiq.Spawning;

public class ItemBox : MonoBehaviour
{
    [Header("Item Settings")]
    public GameObject itemPrefab;
    public Transform spawnPoint;

    [Header("XR Interaction")]
    public XRSimpleInteractable interactable;

    [Header("Networking")]
    public NetworkSpawnManager networkSpawnManager;

    private void Reset()
    {
        interactable = GetComponent<XRSimpleInteractable>();
    }

    private void Awake()
    {
        if (networkSpawnManager == null)
        {
            networkSpawnManager = NetworkSpawnManager.Find(this);
        }
    }

    private void OnEnable()
    {
        if (interactable != null)
        {
            interactable.activated.AddListener(OnActivated);
        }
    }

    private void OnDisable()
    {
        if (interactable != null)
        {
            interactable.activated.RemoveListener(OnActivated);
        }
    }

    private void OnActivated(ActivateEventArgs args)
    {
        SpawnItem();
    }

    public GameObject SpawnItem()
    {
        if (itemPrefab == null)
        {
            Debug.LogWarning($"{name}: Item Prefab is not assigned.");
            return null;
        }

        if (networkSpawnManager == null)
        {
            Debug.LogWarning($"{name}: NetworkSpawnManager not found.");
            return null;
        }

        Vector3 spawnPosition = spawnPoint != null
            ? spawnPoint.position
            : transform.position + Vector3.up * 0.5f;

        Quaternion spawnRotation = spawnPoint != null
            ? spawnPoint.rotation
            : Quaternion.identity;

        // 先用 PeerScope，方便立刻拿到生成对象并设置位置
        GameObject spawned = networkSpawnManager.SpawnWithPeerScope(itemPrefab);

        if (spawned != null)
        {
            spawned.transform.position = spawnPosition;
            spawned.transform.rotation = spawnRotation;
        }

        return spawned;
    }
}