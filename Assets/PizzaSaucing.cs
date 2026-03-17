using UnityEngine;
using UnityEngine.UI;

public class PizzaSaucing : MonoBehaviour
{
    [Header("完成后替换的模型")]
    public GameObject saucedPizzaPrefab;

    [Header("进度条")]
    public Slider progressSlider;

    public void UpdateProgress(float progress)
    {
        progressSlider.transform.parent.gameObject.SetActive(true);
        progressSlider.value = progress;

        if (progress >= 1f)
        {
            CompleteSaucing();
        }
    }

    void CompleteSaucing()
    {
        Instantiate(saucedPizzaPrefab, transform.position, transform.rotation);
        progressSlider.transform.parent.gameObject.SetActive(false);
        Destroy(gameObject);
    }
}