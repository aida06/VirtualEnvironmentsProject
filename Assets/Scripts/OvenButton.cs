using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class OvenButton : MonoBehaviour
{
    public OvenController ovenController;

    [Header("Audio")]
    public AudioClip buttonSound; // 👈 拖入按钮音效

    private XRSimpleInteractable interactable;
    private AudioSource audioSource; // 👈 新增

    void Start()
    {
        // 👇 新增
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        interactable = GetComponent<XRSimpleInteractable>();
        if (interactable != null)
        {
            interactable.selectEntered.AddListener(OnPress);
        }
    }

    void OnPress(SelectEnterEventArgs args)
    {
        Debug.Log("烤箱按钮被按下！");

        // 👇 按下时播放一次
        if (buttonSound != null)
            audioSource.PlayOneShot(buttonSound);

        ovenController.OnButtonPressed();
    }

    void OnDestroy()
    {
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(OnPress);
        }
    }
}