using UnityEngine;

/// <summary>
/// Tracks the current game score. Created automatically by GameManager.
/// Call AddScore() when a dish is delivered. Score resets each game.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public int CurrentScore { get; private set; }
    public event System.Action<int> OnScoreChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void AddScore(int amount)
    {
        CurrentScore += amount;
        OnScoreChanged?.Invoke(CurrentScore);
    }

    /// <summary>由 NetworkedGameState 调用：静默设置分数，不触发 OnScoreChanged（避免网络回环）</summary>
    public void SetScoreSilent(int score)
    {
        CurrentScore = score;
    }
}
