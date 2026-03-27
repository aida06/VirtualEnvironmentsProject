using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class SauceBottle : MonoBehaviour
{
    [Header("参数")]
    public float detectionRadius = 0.4f;
    public float detectionHeight = 0.5f;
    public float totalAngleNeeded = 3600f;
    public float minDistance = 0.05f;
    public float minAngleDelta = 0.5f;

    [Header("粒子效果")]
    public ParticleSystem sauceParticle;

    [Header("Audio")]
    public AudioClip saucingSound;

    private float accumulatedAngle = 0f;
    private float lastAngle = 0f;
    private bool isTracking = false;
    private bool isHeld = false;
    private PizzaSaucing targetPizza = null;
    private AudioSource audioSource;
    private XRGrabInteractable grabInteractable;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = saucingSound;
        audioSource.loop = true;
        audioSource.playOnAwake = false;

        grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrab);
            grabInteractable.selectExited.AddListener(OnRelease);
        }
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        isHeld = true;
    }

    void OnRelease(SelectExitEventArgs args)
    {
        isHeld = false;
        StopSaucing();
    }

    void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrab);
            grabInteractable.selectExited.RemoveListener(OnRelease);
        }
    }

    void Update()
    {
        if (!isHeld)
        {
            StopSaucing();
            return;
        }

        PizzaSaucing pizza = FindPizzaBelow();

        if (pizza == null)
        {
            StopSaucing();
            return;
        }

        if (!isTracking)
        {
            StartSaucing(pizza);
        }

        Vector3 offset = transform.position - targetPizza.transform.position;
        float currentAngle = Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;

        if (isTracking)
        {
            float delta = Mathf.DeltaAngle(lastAngle, currentAngle);
            float distance = new Vector2(offset.x, offset.z).magnitude;

            if (distance > minDistance && Mathf.Abs(delta) > minAngleDelta)
            {
                accumulatedAngle += Mathf.Abs(delta);
            }
        }

        lastAngle = currentAngle;

        float progress = Mathf.Clamp01(accumulatedAngle / totalAngleNeeded);
        targetPizza.UpdateProgress(progress);
    }

    PizzaSaucing FindPizzaBelow()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("pizza"))
            {
                float heightDiff = transform.position.y - hit.transform.position.y;

                if (heightDiff > 0 && heightDiff < detectionHeight)
                {
                    return hit.GetComponent<PizzaSaucing>();
                }
            }
        }
        return null;
    }

    void StartSaucing(PizzaSaucing pizza)
    {
        isTracking = true;
        targetPizza = pizza;
        accumulatedAngle = 0f;

        targetPizza.ShowProgress();

        Vector3 offset = transform.position - pizza.transform.position;
        lastAngle = Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;

        if (sauceParticle != null) sauceParticle.Play();

        if (saucingSound != null && !audioSource.isPlaying)
            audioSource.Play();
    }

    void StopSaucing()
    {
        if (isTracking && targetPizza != null)
        {
            targetPizza.HideProgress();
        }

        isTracking = false;
        targetPizza = null;

        if (sauceParticle != null) sauceParticle.Stop();

        audioSource.Stop();
    }
}