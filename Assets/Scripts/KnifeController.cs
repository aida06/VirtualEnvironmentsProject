using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class KnifeController : MonoBehaviour
{
    public float minimumCutSpeed = 0.15f;
    public float cutDirectionDotThreshold = 0.5f;
    public float cutCooldown = 0.25f;

    public Transform cutDirectionReference;

    private Vector3 lastPosition;
    private CuttableIngredient currentIngredient;

    private XRGrabInteractable grabInteractable;
    private bool isGrabbed = false;
    private float lastCutTime = -999f;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable()
    {
        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);
    }

    private void OnDisable()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        grabInteractable.selectExited.RemoveListener(OnReleased);
    }

    private void Start()
    {
        lastPosition = transform.position;
    }

    private void Update()
    {
        Vector3 movement = transform.position - lastPosition;
        float speed = movement.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);

        if (isGrabbed && currentIngredient != null && Time.time - lastCutTime >= cutCooldown)
        {
            Vector3 cutAxis = cutDirectionReference != null ? cutDirectionReference.forward : transform.forward;
            Vector3 movementDir = movement.normalized;

            float dot = Vector3.Dot(movementDir, cutAxis);

            if (speed > minimumCutSpeed && dot > cutDirectionDotThreshold)
            {
                currentIngredient.AddCut();
                lastCutTime = Time.time;
            }
        }

        lastPosition = transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        CuttableIngredient ingredient = other.GetComponentInParent<CuttableIngredient>();

        if (ingredient != null && !ingredient.IsCutComplete())
        {
            currentIngredient = ingredient;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        CuttableIngredient ingredient = other.GetComponentInParent<CuttableIngredient>();

        if (ingredient != null && ingredient == currentIngredient)
        {
            currentIngredient = null;
        }
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        isGrabbed = true;
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        isGrabbed = false;
        currentIngredient = null;
    }
}
