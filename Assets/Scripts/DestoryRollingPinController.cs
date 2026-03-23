using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class DestoryRollingPinController : MonoBehaviour
{
    [Header("Rolling Detection")]
    public float minimumRollingSpeed = 0.15f;
    public float rollingDirectionDotThreshold = 0.6f;
    public Transform rollingReference;

    private Vector3 lastPosition;
    private DestoryDoughBallRollable currentDoughBall;

    private XRGrabInteractable grabInteractable;
    private bool isGrabbed = false;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }
    }

    private void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }

    private void Start()
    {
        lastPosition = transform.position;
    }

    private void Update()
    {
        Vector3 currentPosition = transform.position;
        Vector3 movement = currentPosition - lastPosition;
        float speed = movement.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);

        if (isGrabbed && currentDoughBall != null && speed > minimumRollingSpeed)
        {
            Vector3 rollingAxis = rollingReference != null ? rollingReference.forward : transform.forward;
            Vector3 movementDirection = movement.normalized;

            float dot = Mathf.Abs(Vector3.Dot(movementDirection, rollingAxis));

            if (dot > rollingDirectionDotThreshold)
            {
                currentDoughBall.AddRollingProgress(Time.deltaTime);
            }
        }

        lastPosition = currentPosition;
    }

    private void OnTriggerEnter(Collider other)
    {
        DestoryDoughBallRollable doughBall = other.GetComponentInParent<DestoryDoughBallRollable>();

        if (doughBall != null && !doughBall.IsFlattened())
        {
            currentDoughBall = doughBall;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        DestoryDoughBallRollable doughBall = other.GetComponentInParent<DestoryDoughBallRollable>();

        if (doughBall != null && doughBall == currentDoughBall)
        {
            currentDoughBall = null;
        }
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        isGrabbed = true;
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        isGrabbed = false;
        currentDoughBall = null;
    }
}