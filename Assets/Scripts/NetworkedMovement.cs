using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Ubiq.Messaging;

public class NetworkedMovement : MonoBehaviour
{
    [Serializable]
    public struct TransformMessage
    {
        public Vector3 position;
        public Quaternion rotation;
        public bool isGrabbed;
    }

    private NetworkContext context;
    private XRGrabInteractable grabInteractable;

    private Vector3 lastPosition;
    private Quaternion lastRotation;

    private bool suppressSend = false;
    private bool isGrabbed = false;

    [Header("Sync Settings")]
    [SerializeField] private float sendInterval = 0.05f; // 20Hz
    [SerializeField] private float positionThreshold = 0.001f;
    [SerializeField] private float rotationThreshold = 0.5f;

    private float nextSendTime = 0f;

    // Remote smoothing target
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool hasRemoteState = false;

    [Header("Remote Smoothing")]
    [SerializeField] private float positionLerpSpeed = 15f;
    [SerializeField] private float rotationLerpSpeed = 15f;

    private void Start()
    {
        context = NetworkScene.Register(this);
        grabInteractable = GetComponent<XRGrabInteractable>();

        lastPosition = transform.localPosition;
        lastRotation = transform.localRotation;

        targetPosition = transform.localPosition;
        targetRotation = transform.localRotation;

        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrab);
            grabInteractable.selectExited.AddListener(OnRelease);
        }
    }

    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrab);
            grabInteractable.selectExited.RemoveListener(OnRelease);
        }
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        isGrabbed = true;
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        isGrabbed = false;
    }

    private void Update()
    {
        // If transform was just updated from network, do not send immediately
        if (suppressSend)
        {
            suppressSend = false;
            lastPosition = transform.localPosition;
            lastRotation = transform.localRotation;
            return;
        }

        // If this object is being controlled remotely, smooth toward target
        if (!isGrabbed && hasRemoteState)
        {
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                targetPosition,
                Time.deltaTime * positionLerpSpeed
            );

            transform.localRotation = Quaternion.Slerp(
                transform.localRotation,
                targetRotation,
                Time.deltaTime * rotationLerpSpeed
            );
        }

        // Limit send frequency
        if (Time.time < nextSendTime)
            return;

        bool positionChanged = Vector3.Distance(transform.localPosition, lastPosition) > positionThreshold;
        bool rotationChanged = Quaternion.Angle(transform.localRotation, lastRotation) > rotationThreshold;

        if (positionChanged || rotationChanged)
        {
            lastPosition = transform.localPosition;
            lastRotation = transform.localRotation;
            nextSendTime = Time.time + sendInterval;

            context.SendJson(new TransformMessage
            {
                position = transform.localPosition,
                rotation = transform.localRotation,
                isGrabbed = isGrabbed
            });
        }
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<TransformMessage>();

        suppressSend = true;
        hasRemoteState = true;
        targetPosition = msg.position;
        targetRotation = msg.rotation;
    }
}

