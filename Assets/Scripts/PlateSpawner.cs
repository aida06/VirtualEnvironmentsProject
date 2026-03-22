using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class PlateSpawner : MonoBehaviour
{
    [Header("设置")]
    public GameObject platePrefab;
    public float spawnDistance = 0.3f; // 盘子离开多远就判定为被拿走
    public float checkDelay = 0.5f;

    private GameObject currentPlate;
    private float timer = 0f;
    private bool waiting = false;

    void Start()
    {
        SpawnPlate();
    }

    void Update()
    {
        // 检测盘子是否被拿走（离开了生成点）
        bool plateGone = currentPlate == null ||
                         Vector3.Distance(currentPlate.transform.position, transform.position) > spawnDistance;

        if (plateGone && !waiting)
        {
            waiting = true;
            timer = 0f;
        }

        if (waiting)
        {
            timer += Time.deltaTime;
            if (timer >= checkDelay)
            {
                waiting = false;
                SpawnPlate();
            }
        }
    }

    void SpawnPlate()
    {
        currentPlate = Instantiate(platePrefab, transform.position, transform.rotation);
    }
}