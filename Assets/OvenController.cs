using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

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

        if (fogEffect != null)
        {
            fogEffect.Stop();
        }
    }

    void Update()
    {
        if (!isBaking) return;

        bakeTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(bakeTimer / bakeTime);

        if (progressSlider != null)
        {
            progressSlider.value = progress;
        }

        if (progress >= 1f)
        {
            CompleteBaking();
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
        if (isBaking)
        {
            Debug.Log("烤箱正在工作中...");
            return;
        }

        if (!plateReady || currentPlate == null)
        {
            Debug.Log("挡板上没有盘子！");
            return;
        }

        currentBakedPrefab = null;
        PlateController plateController = currentPlate.GetComponent<PlateController>();
        if (plateController != null)
        {
            foreach (Transform pizza in plateController.GetSnappedPizzas())
            {
                Debug.Log("找到吸附的披萨: " + pizza.gameObject.name);
                foreach (var recipe in bakeRecipes)
                {
                    if (pizza.gameObject.name == recipe.rawPizzaName)
                    {
                        currentBakedPrefab = recipe.bakedPrefab;
                        break;
                    }
                }
                if (currentBakedPrefab != null) break;
            }
        }

        if (currentBakedPrefab == null)
        {
            Debug.Log("盘子上没有可以烘烤的披萨！");
            return;
        }

        StartBaking();
    }

    void StartBaking()
    {
        isBaking = true;
        bakeTimer = 0f;

        if (bakingSound != null)
        {
            audioSource.clip = bakingSound;
            audioSource.Play();
        }

        var grab = currentPlate.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grab != null) grab.enabled = false;

        if (fogEffect != null) fogEffect.Play();

        if (progressSlider != null)
        {
            progressSlider.gameObject.SetActive(true);
            progressSlider.value = 0f;
        }

        Debug.Log("烤箱启动！开始烘烤...");
    }

    void CompleteBaking()
    {
        isBaking = false;

        audioSource.Stop();

        if (progressSlider != null) progressSlider.gameObject.SetActive(false);

        if (fogEffect != null)
        {
            fogEffect.Stop();
            fogEffect.Clear();
        }

        if (currentPlate != null)
        {
            PlateController plateController = currentPlate.GetComponent<PlateController>();
            if (plateController != null)
            {
                Transform pizzaChild = null;
                foreach (Transform pizza in plateController.GetSnappedPizzas())
                {
                    foreach (var recipe in bakeRecipes)
                    {
                        if (pizza.gameObject.name == recipe.rawPizzaName)
                        {
                            pizzaChild = pizza;
                            break;
                        }
                    }
                    if (pizzaChild != null) break;
                }

                if (pizzaChild != null)
                {
                    Vector3 spawnPos = pizzaChild.position;
                    Quaternion spawnRot = pizzaChild.rotation;

                    plateController.Detach(pizzaChild);
                    Destroy(pizzaChild.gameObject);

                    GameObject baked = Instantiate(currentBakedPrefab, spawnPos, spawnRot);

                    Rigidbody bakedRb = baked.GetComponent<Rigidbody>();
                    if (bakedRb != null) bakedRb.isKinematic = true;

                    Collider[] bakedCols = baked.GetComponents<Collider>();
                    foreach (var col in bakedCols) col.enabled = false;

                    var bakedGrab = baked.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
                    if (bakedGrab != null) bakedGrab.enabled = false;

                    plateController.SnapPizzaPublic(baked.transform);

                    Debug.Log("烘烤完成！Pizza已变为: " + currentBakedPrefab.name);
                }
            }

            var plateGrab = currentPlate.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if (plateGrab != null) plateGrab.enabled = true;
        }

        plateReady = false;
        currentPlate = null;
        currentBakedPrefab = null;
    }
}