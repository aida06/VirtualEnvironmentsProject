using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

/// <summary>
/// Central game manager. Handles game state, countdown timer, and orchestrates all UI components.
/// Drop this on any GameObject in your game scene. All UI is created automatically at runtime.
///
/// TUTORIAL MODE: Set isTutorialMode = true in the Inspector for the Tutorial scene.
/// The timer freezes at frozenTimerValue, orders are not spawned, and TutorialGameOverUI
/// replaces GameOverUI with "TUTORIAL COMPLETE" and a "START GAME" button.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Playing, GameOver }

    [Header("Game Settings")]
    [SerializeField] private float gameDuration = 120f;
    [SerializeField] private string mainMenuSceneName = "00MainMenu";

    [Header("Order Settings")]
    [SerializeField] private float orderSpawnInterval = 15f;
    [SerializeField] private float orderTimeLimit = 90f;
    [SerializeField] private int maxActiveOrders = 5;

    [Header("Tutorial Mode")]
    [SerializeField] private bool isTutorialMode = false;
    [SerializeField] private float frozenTimerValue = 300f;
    [SerializeField] private string gameSceneName = "02Demo";

    public GameState CurrentState { get; private set; }
    public float RemainingTime { get; private set; }
    public Canvas UICanvas { get; private set; }

    private bool timerActive = true;

    // Public properties for other scripts
    public bool IsTutorialMode => isTutorialMode;
    public float FrozenTimerValue => frozenTimerValue;
    public string GameSceneName => gameSceneName;

    public event System.Action<GameState> OnStateChanged;

    // Audio
    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Audio source for UI sounds
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        RemainingTime = isTutorialMode ? frozenTimerValue : gameDuration;
        CreateCanvas();
        CreateUIComponents();
    }

    private void Start()
    {
        CurrentState = GameState.Playing;
        OnStateChanged?.Invoke(CurrentState);
    }

    private void Update()
    {
        if (CurrentState != GameState.Playing) return;

        // In tutorial mode, or when frozen by NetworkedGameState (non-host), timer is paused
        if (!isTutorialMode && timerActive)
        {
            RemainingTime -= Time.deltaTime;
            if (RemainingTime <= 0f)
            {
                RemainingTime = 0f;
                CurrentState = GameState.GameOver;
                OnStateChanged?.Invoke(CurrentState);
            }
        }
    }

    private void CreateCanvas()
    {
        var canvasObj = new GameObject("GameUI_Canvas");
        UICanvas = canvasObj.AddComponent<Canvas>();

        // ScreenSpaceCamera renders correctly in VR headsets (Quest 3 etc.)
        UICanvas.renderMode = RenderMode.ScreenSpaceCamera;
        UICanvas.worldCamera = Camera.main;
        UICanvas.planeDistance = 1.0f;
        UICanvas.sortingOrder = 10;

        if (UICanvas.worldCamera == null)
            StartCoroutine(AssignCameraWhenReady(UICanvas));

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            esObj.AddComponent<StandaloneInputModule>();
#endif
        }
    }

    private void CreateUIComponents()
    {
        // Score manager (must be created first)
        var scoreGO = new GameObject("ScoreManager");
        scoreGO.transform.SetParent(transform);
        scoreGO.AddComponent<ScoreManager>();

        // Orders — skip in tutorial mode (no order list needed)
        if (!isTutorialMode)
        {
            var orderGO = new GameObject("OrderManager");
            orderGO.transform.SetParent(transform);
            var orderMgr = orderGO.AddComponent<OrderManager>();
            orderMgr.Initialize(UICanvas.transform, orderSpawnInterval, orderTimeLimit, maxActiveOrders);
        }

        // Countdown timer (always shown; frozen in tutorial mode)
        var timerGO = new GameObject("CountdownTimerUI");
        timerGO.transform.SetParent(transform);
        var timer = timerGO.AddComponent<CountdownTimerUI>();
        timer.Initialize(UICanvas.transform);

        // Currency bar
        var currencyGO = new GameObject("CurrencyBarUI");
        currencyGO.transform.SetParent(transform);
        var currencyBar = currencyGO.AddComponent<CurrencyBarUI>();
        currencyBar.Initialize(UICanvas.transform);

        // Game over / tutorial complete screen
        if (isTutorialMode)
        {
            var tutOverGO = new GameObject("TutorialGameOverUI");
            tutOverGO.transform.SetParent(transform);
            var tutOver = tutOverGO.AddComponent<TutorialGameOverUI>();
            tutOver.Initialize(UICanvas.transform, gameSceneName);
        }
        else
        {
            var gameOverGO = new GameObject("GameOverUI");
            gameOverGO.transform.SetParent(transform);
            var gameOver = gameOverGO.AddComponent<GameOverUI>();
            gameOver.Initialize(UICanvas.transform, mainMenuSceneName);
        }
    }

    /// <summary>由 NetworkedGameState 调用：暂停/恢复本地计时器（非房主接收房主广播前冻结）</summary>
    public void SetTimerActive(bool active) => timerActive = active;

    /// <summary>由 NetworkedGameState 调用：将剩余时间同步为房主广播值，并解冻计时器</summary>
    public void SyncTime(float remainingTime)
    {
        RemainingTime = remainingTime;
        timerActive = true;
    }

    /// <summary>由 NetworkedGameState 调用：强制进入 GameOver 状态（非房主收到游戏结束通知时）</summary>
    public void ForceGameOver()
    {
        if (CurrentState == GameState.GameOver) return;
        RemainingTime = 0f;
        CurrentState = GameState.GameOver;
        OnStateChanged?.Invoke(CurrentState);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    /// <summary>
    /// Call this from your game logic when a player delivers the correct dish.
    /// Awards the specified score and removes the oldest matching order.
    /// </summary>
    public void CompleteOldestOrder(int score = 50, string recipeName = null)
    {
        ScoreManager.Instance?.AddScore(score);
        PlaySound("Audio/coin_collect");

        var orderMgr = GetComponentInChildren<OrderManager>();
        if (orderMgr != null)
        {
            if (!string.IsNullOrEmpty(recipeName))
                orderMgr.CompleteOldestOrderByType(recipeName);
            else
                orderMgr.CompleteOldestOrder();
        }
    }

    /// <summary>
    /// Called by TutorialManager when all tutorial steps are completed.
    /// Transitions to the GameOver state (showing TutorialGameOverUI).
    /// </summary>
    public void TutorialComplete()
    {
        if (!isTutorialMode) return;
        CurrentState = GameState.GameOver;
        OnStateChanged?.Invoke(CurrentState);
    }

    // ---- Audio helpers ----

    public void PlaySound(string resourcePath)
    {
        var clip = Resources.Load<AudioClip>(resourcePath);
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    // ---- Camera helper ----

    private IEnumerator AssignCameraWhenReady(Canvas canvas)
    {
        while (canvas != null && canvas.worldCamera == null)
        {
            canvas.worldCamera = Camera.main;
            yield return null;
        }
    }
}
