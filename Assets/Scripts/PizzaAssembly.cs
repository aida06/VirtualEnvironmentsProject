using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Spawning;
using Ubiq.Rooms;

[System.Serializable]
public class PizzaRecipe
{
    public string recipeName;

    [Header("需要的配料Tag")]
    public string[] requiredIngredients;

    [Header("生成的Pizza")]
    public GameObject resultPrefab;
}

public class PizzaAssembly : MonoBehaviour
{
    [Header("所有配方")]
    public List<PizzaRecipe> recipes = new List<PizzaRecipe>();

    private HashSet<string> currentIngredients = new HashSet<string>();
    private List<GameObject> ingredientObjects = new List<GameObject>();
    private HashSet<string> validIngredientTags = new HashSet<string> { "sausage", "cheese", "mushroom" };

    private NetworkSpawnManager spawnManager;
    private RoomClient roomClient;
    private bool pendingSpawn = false;
    private bool assemblyTriggered = false;
    private Vector3 pendingPos;
    private GameObject pendingResultPrefab;
    private NetworkedGrabbable myNetworkedGrabbable;

    private void Start()
    {
        spawnManager = NetworkSpawnManager.Find(this);
        roomClient = RoomClient.Find(this);
        myNetworkedGrabbable = GetComponent<NetworkedGrabbable>();
        if (spawnManager != null)
            spawnManager.OnSpawned.AddListener(OnNetworkSpawned);
    }

    private void OnDestroy()
    {
        if (spawnManager != null)
            spawnManager.OnSpawned.RemoveListener(OnNetworkSpawned);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!validIngredientTags.Contains(other.tag)) return;
        // 联机时只有面团权威方触发合成，防止双端独立物理模拟重复执行
        if (spawnManager != null && myNetworkedGrabbable != null && !myNetworkedGrabbable.IsLocalAuthority()) return;

        string tag = other.tag;
        if (!currentIngredients.Contains(tag))
        {
            currentIngredients.Add(tag);
            ingredientObjects.Add(other.gameObject);
            Debug.Log("加入配料: " + tag);
        }

        TryAssemble();
    }

    void OnTriggerExit(Collider other)
    {
        if (spawnManager != null && myNetworkedGrabbable != null && !myNetworkedGrabbable.IsLocalAuthority()) return;
        string tag = other.tag;
        if (currentIngredients.Contains(tag))
        {
            currentIngredients.Remove(tag);
            ingredientObjects.Remove(other.gameObject);
            Debug.Log("移除配料: " + tag);
        }
    }

    void TryAssemble()
    {
        if (assemblyTriggered) return; // 防止同一帧多次触发
        foreach (var recipe in recipes)
        {
            if (MatchRecipe(recipe))
            {
                assemblyTriggered = true;
                AssemblePizza(recipe);
                return;
            }
        }
    }

    bool MatchRecipe(PizzaRecipe recipe)
    {
        foreach (string ingredient in recipe.requiredIngredients)
            if (!currentIngredients.Contains(ingredient)) return false;
        return true;
    }

    void AssemblePizza(PizzaRecipe recipe)
    {
        if (recipe.resultPrefab == null)
        {
            Debug.LogError("配方 " + recipe.recipeName + " 没有 prefab！");
            return;
        }

        Debug.Log("生成Pizza: " + recipe.recipeName);

        pendingPos = transform.position + Vector3.up * 0.05f;
        pendingResultPrefab = recipe.resultPrefab;

        // 销毁食材（网络感知）
        foreach (var obj in ingredientObjects)
        {
            if (spawnManager != null) spawnManager.Despawn(obj);
            else Destroy(obj);
        }
        ingredientObjects.Clear();
        currentIngredients.Clear();

        if (spawnManager != null)
        {
            pendingSpawn = true;
            spawnManager.SpawnWithRoomScope(recipe.resultPrefab);
            StartCoroutine(DespawnSelf());
        }
        else
        {
            Instantiate(recipe.resultPrefab, pendingPos, Quaternion.identity);
            Destroy(gameObject);
        }
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

    private void OnNetworkSpawned(GameObject go, IRoom room, IPeer peer, NetworkSpawnOrigin origin)
    {
        if (pendingResultPrefab != null && go.name != pendingResultPrefab.name + "(Clone)") return;

        if (!pendingSpawn)
        {
            if (origin == NetworkSpawnOrigin.Remote)
            {
                var rbOther = go.GetComponent<Rigidbody>();
                if (rbOther != null) rbOther.isKinematic = true;
            }
            return;
        }

        // 发起方：定位已生成的 pizza 并声明位置权威
        pendingSpawn = false;
        var rb = go.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        go.transform.SetPositionAndRotation(pendingPos, Quaternion.identity);
        if (rb != null) { rb.isKinematic = false; rb.Sleep(); }
        go.GetComponent<NetworkedGrabbable>()?.ClaimPositionAuthority();
    }
}
