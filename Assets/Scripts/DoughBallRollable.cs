using UnityEngine;

public class DoughBallRollable : MonoBehaviour
{
    [Header("Result")]
    public GameObject flattenedDoughPrefab;

    [Header("Rolling Settings")]
    public float requiredRollingTime = 3f;

    [Header("Audio")]
    public AudioClip rollingSound; // 👈 拖入 dough 音效

    private float rollingProgress = 0f;
    private bool isFlattened = false;
    private AudioSource audioSource; // 👈 新增

    private void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = rollingSound;
        audioSource.loop = true;       // 擀面时持续播放
        audioSource.playOnAwake = false;
    }

    public void AddRollingProgress(float amount)
    {
        if (isFlattened)
            return;

        // 👇 开始擀面时播放音效
        if (!audioSource.isPlaying)
            audioSource.Play();

        rollingProgress += amount;

        if (rollingProgress >= requiredRollingTime)
        {
            Flatten();
        }
    }

    public bool IsFlattened()
    {
        return isFlattened;
    }

    private void Flatten()
    {
        if (isFlattened)
            return;

        isFlattened = true;

        // 👇 擀面完成停止音效
        audioSource.Stop();

        if (flattenedDoughPrefab == null)
        {
            Debug.LogWarning("Flattened Dough Prefab is not assigned.");
            return;
        }

        Vector3 spawnPosition = transform.position;
        Quaternion spawnRotation = transform.rotation;

        Instantiate(flattenedDoughPrefab, spawnPosition, spawnRotation);
        Destroy(gameObject);
    }
}