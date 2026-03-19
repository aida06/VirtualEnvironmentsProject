using UnityEngine;
using UnityEngine.UI;

public class PizzaSaucing : MonoBehaviour
{
    [Header("完成后生成的撒酱Pizza Prefab")]
    public GameObject saucedPizzaPrefab;

    [Header("进度条")]
    public Slider progressSlider;
    public GameObject progressCanvas;

    [Header("Audio")]

    private bool isCompleted = false;
    void Awake()
    {
   

        if (progressSlider != null)
        {
            progressSlider.transform.parent.gameObject.SetActive(false);
            progressSlider.value = 0f;

            RectTransform rt = progressSlider.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(150f, rt.sizeDelta.y);
            }
        }
        else
        {
            Debug.LogError("PizzaSaucing: progressSlider 未赋值！", gameObject);
        }
    }

    public void ShowProgress()
    {
        if (progressSlider != null && !isCompleted)
        {
            progressSlider.transform.parent.gameObject.SetActive(true);
        }

  
    }

    public void HideProgress()
    {
        if (progressSlider != null)
        {
            progressSlider.transform.parent.gameObject.SetActive(false);
        }

    }

    public void UpdateProgress(float progress)
    {
        if (isCompleted) return;

        if (progressSlider != null)
        {
            progressSlider.value = progress;
        }

        if (progress >= 1f)
        {
            CompleteSaucing();
        }
    }

    void CompleteSaucing()
    {
        if (isCompleted) return;
        isCompleted = true;



        if (progressSlider != null)
        {
            progressSlider.transform.parent.gameObject.SetActive(false);
        }

        if (saucedPizzaPrefab == null)
        {
            Debug.LogError("saucedPizzaPrefab 未赋值！", gameObject);
            return;
        }

        Vector3 spawnPos = transform.position + Vector3.up * 0.05f;
        Quaternion spawnRot = Quaternion.identity;

        GameObject newPizza = Instantiate(saucedPizzaPrefab, spawnPos, spawnRot);
        newPizza.name = saucedPizzaPrefab.name;

        Debug.Log("生成了新披萨: " + newPizza.name + " 位置: " + newPizza.transform.position);

        Destroy(gameObject);
    }
}