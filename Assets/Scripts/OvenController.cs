using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections.Generic;
using Ubiq.Messaging;
using Ubiq.Spawning;
using Ubiq.Rooms;

[System.Serializable]
public class BakeRecipe
{
    public string rawPizzaName;
    public GameObject bakedPrefab;
}

public class OvenController : MonoBehaviour
{
    [Header("烘烤设置")]
    public float bakeTime = 3f;

    [Header("配方列表")]
    public List<BakeRecipe> bakeRecipes = new List<BakeRecipe>();

    [Header("进度条")]
    public Slider progressSlider;

    [Header("雾效果")]
    public ParticleSystem fogEffect;

    [Header("Audio")]
    public AudioClip bakingSound;

    private bool isBaking = false;
    private bool plateReady = false;
    private float bakeTimer = 0f;
    private GameObject currentPlate = null;
    private GameObject currentBakedPrefab = null;
    private AudioSource audioSource;

    private NetworkSpawnManager spawnManager;
    private RoomClient roomClient;
    private NetworkContext networkContext;

    // 非发起方用于近似显示进度条
    private bool isBakingRemote = false;
    private float remoteBakeTimer = 0f;
    private float remoteBakeTime = 0f;

    private struct OvenSyncMessage { public string type; public float bakeTime; }

    // 等待 OnNetworkSpawned 回调时缓存的上下文
    private bool pendingBakedSpawn = false;
    private Vector3 pendingBakedPos;
    private Quaternion pendingBakedRot;
    private Transform pendingRawPizzaTransform;   // 需要销毁的生 pizza
    private PlateController pendingPlateController;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;

        if (progressSlider != null)
        {
            progressSlider.gameObject.SetActive(false);
            progressSlider.value = 0f;
        }

        if (fogEffect != null) fogEffect.Stop();

        spawnManager = NetworkSpawnManager.Find(this);
        roomClient = RoomClient.Find(this);
        if (spawnManager != null)
            spawnManager.OnSpawned.AddListener(OnNetworkSpawned);

        networkContext = NetworkScene.Register(this);
    }

    private void OnDestroy()
    {
        if (spawnManager != null)
            spawnManager.OnSpawned.RemoveListener(OnNetworkSpawned);
    }

    void Update()
    {
        // 本地烘烤（发起方）
        if (isBaking)
        {
            bakeTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(bakeTimer / bakeTime);
            if (progressSlider != null) progressSlider.value = progress;
            if (progress >= 1f) CompleteBaking();
        }

        // 远端烘烤进度条（接收方近似同步）
        if (isBakingRemote)
        {
            remoteBakeTimer += Time.deltaTime;
            float remoteProgress = Mathf.Clamp01(remoteBakeTimer / remoteBakeTime);
            if (progressSlider != null)
            {
                progressSlider.gameObject.SetActive(true);
                progressSlider.value = remoteProgress;
            }
            if (remoteProgress >= 1f) isBakingRemote = false;
        }
    }

    /// <summary>接收来自发起方的烘烤状态同步消息</summary>
    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<OvenSyncMessage>();
        if (msg.type == "startBaking" && !isBaking)
        {
            isBakingRemote = true;
            remoteBakeTimer = 0f;
            remoteBakeTime = msg.bakeTime;
        }
    }

    public void OnPlateDetected(GameObject plate)
    {
        if (isBaking) return;
        currentPlate = plate;
        plateReady = true;
        Debug.Log("盘子已放到挡板上: " + plate.name);
    }

    public void OnPlateRemoved()
    {
        if (isBaking) return;
        currentPlate = null;
        plateReady = false;
        currentBakedPrefab = null;
        Debug.Log("盘子已从挡板上移走");
    }

    public void OnButtonPressed()
    {
        if (isBaking) { Debug.Log("烤箱正在工作中..."); return; }
        if (!plateReady || currentPlate == null) { Debug.Log("挡板上没有盘子！"); return; }

        currentBakedPrefab = null;
        PlateController plateController = currentPlate.GetComponent<PlateController>();
        if (plateController != null)
        {
            foreach (Transform pizza in plateController.GetSnappedPizzas())
            {
                if (pizza == null) continue; // 跳过已被 Despawn 销毁的 null 条目
                Debug.Log("找到吸附的披萨: " + pizza.gameObject.name);
                foreach (var recipe in bakeRecipes)
                {
                    if (pizza.gameObject.name == recipe.rawPizzaName ||
                        pizza.gameObject.name == recipe.rawPizzaName + "(Clone)")
                    {
                        currentBakedPrefab = recipe.bakedPrefab;
                        break;
                    }
                }
                if (currentBakedPrefab != null) break;
            }
        }

        if (currentBakedPrefab == null) { Debug.Log("盘子上没有可以烘烤的披萨！"); return; }

        StartBaking();
    }

    void StartBaking()
    {
        isBaking = true;
        bakeTimer = 0f;

        if (networkContext.Scene != null)
            networkContext.SendJson(new OvenSyncMessage { type = "startBaking", bakeTime = bakeTime });

        if (bakingSound != null) { audioSource.clip = bakingSound; audioSource.Play(); }

        var grab = currentPlate.GetComponent<XRGrabInteractable>();
        if (grab != null) grab.enabled = false;

        if (fogEffect != null) fogEffect.Play();

        if (progressSlider != null) { progressSlider.gameObject.SetActive(true); progressSlider.value = 0f; }

        Debug.Log("烤箱启动！开始烘烤...");
    }

    void CompleteBaking()
    {
        isBaking = false;
        audioSource.Stop();

        if (progressSlider != null) progressSlider.gameObject.SetActive(false);
        if (fogEffect != null) { fogEffect.Stop(); fogEffect.Clear(); }

        if (currentPlate == null) { ResetState(); return; }

        PlateController plateController = currentPlate.GetComponent<PlateController>();
        if (plateController == null) { ResetState(); return; }

        // 找到生 pizza（跳过已被 Despawn 销毁的 null 条目）
        Transform pizzaChild = null;
        foreach (Transform pizza in plateController.GetSnappedPizzas())
        {
            if (pizza == null) continue;
            foreach (var recipe in bakeRecipes)
            {
                if (pizza.gameObject.name == recipe.rawPizzaName ||
                    pizza.gameObject.name == recipe.rawPizzaName + "(Clone)")
                { pizzaChild = pizza; break; }
            }
            if (pizzaChild != null) break;
        }

        if (pizzaChild == null) { ResetState(); return; }

        pendingBakedPos = pizzaChild.position;
        pendingBakedRot = pizzaChild.rotation;
        pendingRawPizzaTransform = pizzaChild;
        pendingPlateController = plateController;

        // 从盘子分离并销毁生 pizza
        plateController.Detach(pizzaChild);
        if (spawnManager != null) spawnManager.Despawn(pizzaChild.gameObject);
        else Destroy(pizzaChild.gameObject);

        // 生成烤熟 pizza
        if (spawnManager != null)
        {
            pendingBakedSpawn = true;
            spawnManager.SpawnWithRoomScope(currentBakedPrefab);
        }
        else
        {
            var baked = Instantiate(currentBakedPrefab, pendingBakedPos, pendingBakedRot);
            ApplyBakedSetup(baked, plateController);
        }

        var plateGrab = currentPlate.GetComponent<XRGrabInteractable>();
        if (plateGrab != null) plateGrab.enabled = true;

        ResetState();
    }

    private void OnNetworkSpawned(GameObject go, IRoom room, IPeer peer, NetworkSpawnOrigin origin)
    {
        // 用配方列表判断是否为本烤箱的烤熟产物
        // （不依赖 currentBakedPrefab，因为 ResetState 在回调前就已清空它）
        bool isBakedResult = false;
        foreach (var recipe in bakeRecipes)
        {
            if (recipe.bakedPrefab != null && go.name == recipe.bakedPrefab.name + "(Clone)")
            {
                isBakedResult = true;
                break;
            }
        }
        if (!isBakedResult) return;

        if (pendingBakedSpawn)
        {
            // 按下按钮的客户端：定位 + 完整设置
            pendingBakedSpawn = false;
            var rbBaked = go.GetComponent<Rigidbody>();
            if (rbBaked != null) rbBaked.isKinematic = true;
            go.transform.SetPositionAndRotation(pendingBakedPos, pendingBakedRot);
            var ng = go.GetComponent<NetworkedGrabbable>();
            ng?.SetNoAutoUnfreeze(true);  // 烤熟披萨始终保持 kinematic
            ng?.ClaimPositionAuthority();
            ApplyBakedSetup(go, pendingPlateController);
        }
        else
        {
            // 接收方：冻结，等待 BroadcastInitialPosition 定位后由 PlateController 自动吸附
            // SetNoAutoUnfreeze 确保 ProcessMessage 不会将 rb 解冻（baked pizza 必须保持 kinematic）
            go.GetComponent<NetworkedGrabbable>()?.SetNoAutoUnfreeze(true);
            ApplyBakedSetup(go, null);
            // 隐藏接收方进度条（发起方生成披萨即视为烘烤完成）
            isBakingRemote = false;
            if (progressSlider != null) progressSlider.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 烤熟后的物体设置：冻结物理、禁用碰撞和抓取，吸附到盘子上。
    /// </summary>
    private void ApplyBakedSetup(GameObject baked, PlateController plate)
    {
        var rb = baked.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        foreach (var col in baked.GetComponents<Collider>())
            col.enabled = false;

        var bakedGrab = baked.GetComponent<XRGrabInteractable>();
        if (bakedGrab != null) bakedGrab.enabled = false;

        if (plate != null)
            plate.SnapPizzaPublic(baked.transform);

        Debug.Log("烘烤完成！Pizza已变为: " + baked.name);
    }

    private void ResetState()
    {
        plateReady = false;
        currentPlate = null;
        currentBakedPrefab = null;
    }
}
