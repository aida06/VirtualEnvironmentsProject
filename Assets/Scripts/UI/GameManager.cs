using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

/// <summary>
/// Central game manager. Handles game state, countdown timer, and orchestrates all UI components.
/// Drop this on any GameObject in your game scene. All UI is created automatically at runtime.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Playing, GameOver }

    [Header("Game Settings")]
    [SerializeField] private float gameDuration = 240f;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Order Settings")]
    [SerializeField] private float orderSpawnInterval = 8f;
    [SerializeField] private float orderTimeLimit = 60f;
    [SerializeField] private int maxActiveOrders = 5;

    public GameState CurrentState { get; private set; }
    public float RemainingTime { get; private set; }
    public Canvas UICanvas { get; private set; }

    public event System.Action<GameState> OnStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        RemainingTime = gameDuration;
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

        RemainingTime -= Time.deltaTime;
        if (RemainingTime <= 0f)
        {
            RemainingTime = 0f;
            CurrentState = GameState.GameOver;
            OnStateChanged?.Invoke(CurrentState);
        }
    }

    private void CreateCanvas()
    {
        // NOTE: ScreenSpaceOverlay works in the Editor and flat-screen builds.
        // For VR headset rendering, change to RenderMode.WorldSpace and parent
        // the Canvas to your XR Camera, or use RenderMode.ScreenSpaceCamera.
        var canvasObj = new GameObject("GameUI_Canvas");
        UICanvas = canvasObj.AddComponent<Canvas>();
        UICanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        UICanvas.sortingOrder = 10;

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
        var orderGO = new GameObject("OrderManager");
        orderGO.transform.SetParent(transform);
        var orderMgr = orderGO.AddComponent<OrderManager>();
        orderMgr.Initialize(UICanvas.transform, orderSpawnInterval, orderTimeLimit, maxActiveOrders);

        var timerGO = new GameObject("CountdownTimerUI");
        timerGO.transform.SetParent(transform);
        var timer = timerGO.AddComponent<CountdownTimerUI>();
        timer.Initialize(UICanvas.transform);

        var gameOverGO = new GameObject("GameOverUI");
        gameOverGO.transform.SetParent(transform);
        var gameOver = gameOverGO.AddComponent<GameOverUI>();
        gameOver.Initialize(UICanvas.transform, mainMenuSceneName);
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
    /// Completes the oldest active order.
    /// </summary>
    public void CompleteOldestOrder()
    {
        var orderMgr = GetComponentInChildren<OrderManager>();
        if (orderMgr != null) orderMgr.CompleteOldestOrder();
    }
}
