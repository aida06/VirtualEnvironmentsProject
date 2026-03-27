using System.Collections;
using UnityEngine;
using Ubiq.Spawning;
using Ubiq.Rooms;

public class CuttableIngredient : MonoBehaviour
{
    [Header("Cut Result")]
    public GameObject cutResultPrefab;

    [Header("Cut Settings")]
    public int requiredCutCount = 3;

    [Header("Audio")]
    public AudioClip cutSound;

    private int currentCutCount = 0;
    private bool isCutComplete = false;
    private AudioSource audioSource;

    private NetworkSpawnManager spawnManager;
    private RoomClient roomClient;
    private bool pendingSpawn = false;
    private Vector3 pendingPos;
    private Quaternion pendingRot;

    private void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
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

    public void AddCut()
    {
        if (isCutComplete) return;
        currentCutCount++;
        if (cutSound != null) audioSource.PlayOneShot(cutSound);
        if (currentCutCount >= requiredCutCount) CompleteCut();
    }

    public bool IsCutComplete() => isCutComplete;

    private void CompleteCut()
    {
        if (isCutComplete) return;
        isCutComplete = true;

        if (cutResultPrefab == null)
        {
            Debug.LogWarning("CuttableIngredient: cutResultPrefab is not assigned.");
            return;
        }

        pendingPos = transform.position + Vector3.up * 0.02f;
        pendingRot = transform.rotation;

        if (spawnManager != null && roomClient != null && roomClient.JoinedRoom)
        {
            pendingSpawn = true;
            spawnManager.SpawnWithRoomScope(cutResultPrefab);
            StartCoroutine(DespawnSelf());
        }
        else
        {
            var result = Instantiate(cutResultPrefab, pendingPos, pendingRot);
            var rb = result.GetComponent<Rigidbody>();
            if (rb != null) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }

    private void OnNetworkSpawned(GameObject go, IRoom room, IPeer peer, NetworkSpawnOrigin origin)
    {
        if (cutResultPrefab != null && go.name != cutResultPrefab.name + "(Clone)") return;

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
        if (rb != null) { rb.isKinematic = false; rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
        go.GetComponent<NetworkedGrabbable>()?.ClaimPositionAuthority();
    }

    private IEnumerator DespawnSelf()
    {
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
