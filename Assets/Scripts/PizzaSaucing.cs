using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Ubiq.Spawning;
using Ubiq.Rooms;

public class PizzaSaucing : MonoBehaviour
{
    [Header("完成后生成的撒酱Pizza Prefab")]
    public GameObject saucedPizzaPrefab;

    [Header("进度条")]
    public Slider progressSlider;
    public GameObject progressCanvas;

    private bool isCompleted = false;

    private NetworkSpawnManager spawnManager;
    private RoomClient roomClient;
    private bool pendingSpawn = false;
    private Vector3 pendingPos;

    void Awake()
    {
        if (progressSlider != null)
        {
            progressSlider.transform.parent.gameObject.SetActive(false);
            progressSlider.value = 0f;
            var rt = progressSlider.GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = new Vector2(150f, rt.sizeDelta.y);
        }
        else
        {
            Debug.LogError("PizzaSaucing: progressSlider 未赋值！", gameObject);
        }
    }

    private void Start()
    {
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

    public void ShowProgress()
    {
        if (progressSlider != null && !isCompleted)
            progressSlider.transform.parent.gameObject.SetActive(true);
    }

    public void HideProgress()
    {
        if (progressSlider != null)
            progressSlider.transform.parent.gameObject.SetActive(false);
    }

    public void UpdateProgress(float progress)
    {
        if (isCompleted) return;
        if (progressSlider != null) progressSlider.value = progress;
        if (progress >= 1f) CompleteSaucing();
    }

    void CompleteSaucing()
    {
        if (isCompleted) return;
        isCompleted = true;

        if (progressSlider != null)
            progressSlider.transform.parent.gameObject.SetActive(false);

        if (saucedPizzaPrefab == null)
        {
            Debug.LogError("saucedPizzaPrefab 未赋值！", gameObject);
            return;
        }

        pendingPos = transform.position + Vector3.up * 0.05f;

        if (spawnManager != null && roomClient != null && roomClient.JoinedRoom)
        {
            pendingSpawn = true;
            spawnManager.SpawnWithRoomScope(saucedPizzaPrefab);
            StartCoroutine(DespawnSelf());
        }
        else
        {
            Instantiate(saucedPizzaPrefab, pendingPos, Quaternion.identity);
            gameObject.SetActive(false); // 立即从物理/渲染移除，防止同帧被其他系统重复检测
            Destroy(gameObject);
        }
    }

    private void OnNetworkSpawned(GameObject go, IRoom room, IPeer peer, NetworkSpawnOrigin origin)
    {
        if (saucedPizzaPrefab != null && go.name != saucedPizzaPrefab.name + "(Clone)") return;

        if (!pendingSpawn)
        {
            // 接收方：冻结物理，等待 BroadcastInitialPosition 到达后由 ProcessMessage 解冻
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
        go.transform.SetPositionAndRotation(pendingPos, Quaternion.identity);
        if (rb != null) { rb.isKinematic = false; rb.Sleep(); }
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
