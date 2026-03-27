using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Ubiq.Messaging;
using Ubiq.Spawning;

[RequireComponent(typeof(XRGrabInteractable))]
public class NetworkedGrabbable : MonoBehaviour, INetworkSpawnable
{
    public NetworkId NetworkId { get; set; }

    [Header("Network Settings")]
    [Tooltip("Transform updates sent per second while object is held")]
    public int sendRate = 20;

    private NetworkContext context;
    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;

    private bool isHeldLocally = false;
    private float sendInterval;
    private float sendTimer;

    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool hasRemoteTarget = false;

    // 生成者调用 ClaimPositionAuthority() 后为 true。
    // 持有权威时忽略远端发来的 isHeld=false 消息（防止对端广播初始位置覆盖本端正确位置）。
    // 当远端玩家抓起物体时（isHeld=true），权威自动移交。
    private bool isPositionAuthority = false;

    // 当物体被 PlateController 吸附到盘子上时为 true。
    // 吸附期间由 PlateController 全权控制位置，忽略来自远端的位置更新。
    private bool isSnappedToPlate = false;

    // 设为 true 后，ProcessMessage 的 isHeld=false 路径不再修改 rb.isKinematic。
    // 用于烤熟的披萨：始终保持 kinematic，位置通过 PlateController 管理。
    private bool noAutoUnfreeze = false;
    public void SetNoAutoUnfreeze(bool value) => noAutoUnfreeze = value;

    /// <summary>
    /// 由 PlateController 在吸附/分离时调用。
    /// 吸附时停止接受远端位置更新，分离时恢复。
    /// </summary>
    public void SetSnapped(bool snapped)
    {
        isSnappedToPlate = snapped;
        if (snapped) hasRemoteTarget = false; // 立即停止 FixedUpdate 插值
    }

    private struct TransformMessage
    {
        public float px, py, pz;
        public float rx, ry, rz, rw;
        public bool isHeld;
        public bool destroy; // 显式销毁指令
    }

    /// <summary>
    /// 广播销毁指令到所有客户端并销毁本地对象。
    /// 比仅靠 SpawnManager.Despawn 更可靠，确保跨客户端同步销毁。
    /// </summary>
    public void BroadcastDestroy()
    {
        if (context.Scene != null)
            context.SendJson(new TransformMessage { destroy = true });
        Destroy(gameObject);
    }

    private void Start()
    {
        // NetworkId 由 NetworkSpawnManager 在生成时赋值。
        // 场景预置物未经过 SpawnManager，NetworkId 为 default（零值）。
        // 直接调用 NetworkScene.Register(this) 会触发异常（因为 GetNetworkId
        // 读到 default 导致 !id.Valid），所以场景预置物需要用
        // NetworkId.Create(this) 生成基于场景图路径的稳定确定性 ID。
        context = (NetworkId != default)
            ? NetworkScene.Register(this, NetworkId)
            : NetworkScene.Register(this, NetworkId.Create(this));

        grabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        sendInterval = 1f / Mathf.Max(1, sendRate);

        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);

        StartCoroutine(BroadcastInitialPosition());
    }

    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }

    /// <summary>
    /// 由生成脚本（ItemBox、DoughBallRollable 等）在定位完成后调用。
    /// 声明本客户端是该物体位置的权威方。
    /// </summary>
    public void ClaimPositionAuthority()
    {
        isPositionAuthority = true;
    }

    private void Update()
    {
        if (isHeldLocally)
        {
            sendTimer -= Time.deltaTime;
            if (sendTimer <= 0f)
            {
                SendTransform(isHeld: true);
                sendTimer = sendInterval;
            }
        }
    }

    private void FixedUpdate()
    {
        // 用 rb.MovePosition 插值，让物理引擎感知运动轨迹，
        // 避免 transform.position 赋值穿透桌面碰撞体
        if (!isHeldLocally && hasRemoteTarget && !isSnappedToPlate && rb != null && rb.isKinematic)
        {
            rb.MovePosition(Vector3.Lerp(rb.position, targetPosition, Time.fixedDeltaTime * 15f));
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * 15f));
        }
    }

    /// <summary>
    /// 本端是否是该物体位置的权威方（供外部脚本判断是否在本端触发逻辑）。
    /// </summary>
    public bool IsLocalAuthority() => isPositionAuthority;

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        isHeldLocally = true;
        isPositionAuthority = true; // 抓取即声明权威
        hasRemoteTarget = false;
        sendTimer = 0f;

        // 从盘子分离：防止 PlateController 持续覆盖位置与 XR 抓取产生冲突（弹飞）
        if (isSnappedToPlate)
        {
            isSnappedToPlate = false;
            if (rb != null) rb.isKinematic = false;
            // 通知所有 PlateController 解除对本物体的追踪
            foreach (var plate in FindObjectsByType<PlateController>(FindObjectsSortMode.None))
                plate.Detach(transform);
        }
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        isHeldLocally = false;
        SendTransform(isHeld: false);
    }

    private void SendTransform(bool isHeld)
    {
        if (context.Scene == null) return;
        var pos = transform.position;
        var rot = transform.rotation;
        context.SendJson(new TransformMessage
        {
            px = pos.x, py = pos.y, pz = pos.z,
            rx = rot.x, ry = rot.y, rz = rot.z, rw = rot.w,
            isHeld = isHeld
        });
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<TransformMessage>();

        if (msg.destroy)
        {
            Destroy(gameObject);
            return;
        }

        if (isHeldLocally) return;
        if (isSnappedToPlate) return; // PlateController 控制位置，忽略远端更新
        targetPosition = new Vector3(msg.px, msg.py, msg.pz);
        targetRotation = new Quaternion(msg.rx, msg.ry, msg.rz, msg.rw);

        if (msg.isHeld)
        {
            // 远端抓起：放弃位置权威，开始跟随
            isPositionAuthority = false;
            hasRemoteTarget = true;
            if (rb != null) rb.isKinematic = true;
        }
        else
        {
            // 远端放下：如果本端持有位置权威则忽略（防止对端初始广播覆盖正确位置）
            if (isPositionAuthority) return;

            transform.SetPositionAndRotation(targetPosition, targetRotation);
            hasRemoteTarget = false;
            if (rb != null && !noAutoUnfreeze)
            {
                rb.isKinematic = false;
                rb.Sleep();
            }
        }
    }

    private IEnumerator BroadcastInitialPosition()
    {
        yield return null;
        yield return null;
        if (this == null || gameObject == null) yield break;
        SendTransform(isHeld: false);

        // 重传：补偿对端对象尚未创建时首播丢失的情况
        // （SpawnWithRoomScope 先在本端实例化，再由服务器广播到对端，存在延迟差）
        yield return new WaitForSeconds(0.2f);
        if (this == null || gameObject == null) yield break;
        if (!isHeldLocally) SendTransform(isHeld: false);

        yield return new WaitForSeconds(0.5f);
        if (this == null || gameObject == null) yield break;
        if (!isHeldLocally) SendTransform(isHeld: false);
    }
}
