using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Ubiq.Messaging;
using Ubiq.Rooms;
using Ubiq.Spawning;

/// <summary>
/// 同步分数和计时器。
///
/// 计时同步策略（选举制）：
///   - 加入房间后启动房主选举：每个客户端广播一个随机优先级，
///     优先级最高的客户端成为房主。
///   - 房主每隔 broadcastInterval 秒向所有人广播剩余时间。
///   - 非房主：冻结本地计时器，收到房主的广播后解冻并同步时间。
///   - Restart 由网络同步，确保双端同时重载场景。
/// </summary>
public class NetworkedGameState : MonoBehaviour
{
    private struct GameStateMessage
    {
        /// <summary>"score" | "timerBroadcast" | "gameOver" | "requestSync" | "hostClaim" | "restart"</summary>
        public string type;
        public int score;
        public float remainingTime;
        public float priority;   // 用于 hostClaim 选举
    }

    [Tooltip("房主广播计时器的间隔（秒）")]
    [SerializeField] private float broadcastInterval = 2f;

    private NetworkContext context;
    private RoomClient roomClient;
    private bool applyingRemoteScore = false;

    private bool isHost = false;
    private bool hostDecided = false;

    /// <summary>本客户端是否已完成房主选举（供外部脚本等待）</summary>
    public static bool HostDecided { get; private set; }
    /// <summary>本客户端是否是房主</summary>
    public static bool LocalIsHost { get; private set; }

    // 选举相关
    private float myElectionPriority;
    private bool receivedHigherPriority = false;

    private void Start()
    {
        myElectionPriority = Random.value;

        context = NetworkScene.Register(this);

        roomClient = RoomClient.Find(this);
        if (roomClient != null)
        {
            roomClient.OnJoinedRoom.AddListener(OnJoinedRoom);
            roomClient.OnPeerAdded.AddListener(OnPeerAdded);
        }

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreChanged += OnLocalScoreChanged;
        else
            StartCoroutine(WaitForScoreManager());

        StartCoroutine(WaitForGameManager());
    }

    private void OnDestroy()
    {
        HostDecided = false;
        LocalIsHost = false;
        if (roomClient != null)
        {
            roomClient.OnJoinedRoom.RemoveListener(OnJoinedRoom);
            roomClient.OnPeerAdded.RemoveListener(OnPeerAdded);
        }
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreChanged -= OnLocalScoreChanged;
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnGameStateChanged;
    }

    private IEnumerator WaitForScoreManager()
    {
        while (ScoreManager.Instance == null) yield return null;
        ScoreManager.Instance.OnScoreChanged += OnLocalScoreChanged;
    }

    private IEnumerator WaitForGameManager()
    {
        while (GameManager.Instance == null) yield return null;
        GameManager.Instance.OnStateChanged += OnGameStateChanged;
    }

    // ── 房主选举 ──────────────────────────────────────────────────────────

    private void OnJoinedRoom(IRoom room)
    {
        // 每次加入房间（包括重启后重连）都重新选举
        hostDecided = false;
        isHost = false;
        receivedHigherPriority = false;
        myElectionPriority = Random.value;
        HostDecided = false;
        LocalIsHost = false;
        StartCoroutine(ElectHost());
    }

    /// <summary>
    /// 广播自己的优先级，等待其他客户端响应；
    /// 优先级最高者成为房主。
    /// </summary>
    private IEnumerator ElectHost()
    {
        // 等待连接稳定，让 OnPeerAdded 先触发
        yield return new WaitForSeconds(0.5f);

        if (hostDecided) yield break;

        // 广播自己的优先级
        if (context.Scene != null)
            context.SendJson(new GameStateMessage { type = "hostClaim", priority = myElectionPriority });

        // 等待其他客户端响应
        yield return new WaitForSeconds(0.8f);

        if (hostDecided) yield break;

        // 如果没有收到更高优先级 → 我是房主
        BecomeHost(!receivedHigherPriority);
    }

    private void OnPeerAdded(IPeer peer)
    {
        if (isHost)
        {
            // 已是房主：新玩家加入，立刻发一次广播让他同步
            SendTimerBroadcast();
        }
    }

    private void BecomeHost(bool host)
    {
        if (hostDecided) return;   // 幂等保护，防止重复调用
        hostDecided = true;
        isHost = host;
        HostDecided = true;
        LocalIsHost = host;
        Debug.Log($"[NetworkedGameState] isHost = {isHost}");

        if (!isHost)
        {
            // 非房主：冻结计时器，等候房主广播
            GameManager.Instance?.SetTimerActive(false);
            // 主动请求同步
            if (context.Scene != null)
                context.SendJson(new GameStateMessage { type = "requestSync" });
        }
        else
        {
            // 房主：启动定期广播
            StartCoroutine(TimerBroadcastLoop());
        }
    }

    /// <summary>
    /// 向所有客户端广播重启指令，并在本地同步重载场景。
    /// 由 GameManager.RestartGame() 调用。
    /// 重启前先清除所有动态生成（room-scope）的物件，避免残留到下一局。
    /// </summary>
    public void BroadcastRestart()
    {
        DespawnAllSpawnedObjects();
        if (context.Scene != null)
            context.SendJson(new GameStateMessage { type = "restart" });
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// 销毁场景内所有通过 NetworkSpawnManager.SpawnWithRoomScope 动态生成的物件。
    /// 判断依据：NetworkedGrabbable.NetworkId 由 SpawnManager 在生成时赋值，
    /// 场景预置物的 NetworkId 保持 default，因此 != default 即为动态生成物。
    /// </summary>
    private void DespawnAllSpawnedObjects()
    {
        var spawnManager = NetworkSpawnManager.Find(this);
        if (spawnManager == null) return;

        var grabbables = FindObjectsByType<NetworkedGrabbable>(FindObjectsSortMode.None);
        foreach (var g in grabbables)
        {
            if (g.NetworkId != default)
                spawnManager.Despawn(g.gameObject);
        }
    }

    // ── 房主广播循环 ──────────────────────────────────────────────────────

    private IEnumerator TimerBroadcastLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(broadcastInterval);
            if (context.Scene == null || GameManager.Instance == null) yield break;
            SendTimerBroadcast();
            if (GameManager.Instance.CurrentState == GameManager.GameState.GameOver) yield break;
        }
    }

    private void SendTimerBroadcast()
    {
        if (context.Scene == null || GameManager.Instance == null) return;
        context.SendJson(new GameStateMessage
        {
            type = "timerBroadcast",
            remainingTime = GameManager.Instance.RemainingTime
        });
    }

    // ── 本地事件 ──────────────────────────────────────────────────────────

    private void OnLocalScoreChanged(int newScore)
    {
        if (applyingRemoteScore) return;
        if (context.Scene == null) return;
        context.SendJson(new GameStateMessage { type = "score", score = newScore });
    }

    private void OnGameStateChanged(GameManager.GameState state)
    {
        // 房主游戏结束时，立刻通知所有其他玩家
        if (isHost && state == GameManager.GameState.GameOver && context.Scene != null)
        {
            context.SendJson(new GameStateMessage { type = "gameOver" });
            Debug.Log("[NetworkedGameState] Host sent gameOver");
        }
    }

    // ── 网络接收 ─────────────────────────────────────────────────────────

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<GameStateMessage>();

        switch (msg.type)
        {
            case "score":
                if (ScoreManager.Instance == null) break;
                applyingRemoteScore = true;
                ScoreManager.Instance.SetScoreSilent(msg.score);
                applyingRemoteScore = false;
                break;

            case "timerBroadcast":
                // 非房主：收到广播后同步时间（SyncTime 内部会解冻计时器）
                if (!isHost)
                    GameManager.Instance?.SyncTime(msg.remainingTime);
                break;

            case "gameOver":
                // 非房主：收到游戏结束通知
                if (!isHost)
                {
                    GameManager.Instance?.ForceGameOver();
                    Debug.Log("[NetworkedGameState] Non-host received gameOver");
                }
                break;

            case "requestSync":
                // 新玩家请求同步 → 房主立刻广播一次
                if (isHost) SendTimerBroadcast();
                break;

            case "hostClaim":
                // 收到其他客户端的选举广播
                if (msg.priority > myElectionPriority)
                {
                    // 对方优先级更高 → 本端让位，立即成为非房主
                    receivedHigherPriority = true;
                    BecomeHost(false);
                }
                else if (msg.priority < myElectionPriority && !hostDecided)
                {
                    // 本端优先级更高 → 回应自己的优先级，让对方也知道
                    if (context.Scene != null)
                        context.SendJson(new GameStateMessage { type = "hostClaim", priority = myElectionPriority });
                }
                break;

            case "restart":
                // 收到重启指令 → 重载场景
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                break;
        }
    }
}
