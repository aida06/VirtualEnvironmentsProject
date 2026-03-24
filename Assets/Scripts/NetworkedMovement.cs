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
    private Rigidbody rb;

    private Vector3 lastPosition;
    private Quaternion lastRotation;

    private bool isGrabbed = false;

    [Header("Sync Settings")]
    [SerializeField] private float sendInterval = 0.05f;
    [SerializeField] private float positionThreshold = 0.001f;
    [SerializeField] private float rotationThreshold = 0.5f;

    private float nextSendTime = 0f;

    [Header("Remote Smoothing")]
    [SerializeField] private float positionLerpSpeed = 15f;
    [SerializeField] private float rotationLerpSpeed = 15f;

    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool hasRemoteState = false;

    // 关键：最近收到远端消息的时间
    private float lastRemoteMessageTime = -999f;

    // 关键：收到远端消息后，多长时间内认为“当前由远端控制”
    [SerializeField] private float remoteControlTimeout = 0.15f;

    private void Start()
    {
        context = NetworkScene.Register(this);
        grabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        lastPosition = transform.position;
        lastRotation = transform.rotation;

        targetPosition = transform.position;
        targetRotation = transform.rotation;

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

        // 松手时发一次最终状态
        SendCurrentTransform(true);
    }

    private void Update()
    {
        bool isRemoteControlled = (Time.time - lastRemoteMessageTime) < remoteControlTimeout;

        // 如果最近收到远端消息，并且本地没抓着，就平滑跟随远端
        if (!isGrabbed && hasRemoteState && isRemoteControlled)
        {
            transform.position = Vector3.Lerp(
                transform.position,
                targetPosition,
                Time.deltaTime * positionLerpSpeed
            );

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * rotationLerpSpeed
            );

            return;
        }

        // 发送频率限制
        if (Time.time < nextSendTime)
            return;

        SendCurrentTransform(false);
    }

    private void SendCurrentTransform(bool forceSend)
    {
        Vector3 currentPosition = transform.position;
        Quaternion currentRotation = transform.rotation;

        bool positionChanged = Vector3.Distance(currentPosition, lastPosition) > positionThreshold;
        bool rotationChanged = Quaternion.Angle(currentRotation, lastRotation) > rotationThreshold;

        if (!forceSend && !positionChanged && !rotationChanged)
            return;

        lastPosition = currentPosition;
        lastRotation = currentRotation;
        nextSendTime = Time.time + sendInterval;

        context.SendJson(new TransformMessage
        {
            position = currentPosition,
            rotation = currentRotation,
            isGrabbed = isGrabbed
        });
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<TransformMessage>();

        // 如果本地正在抓，就忽略远端消息
        if (isGrabbed)
            return;

        hasRemoteState = true;
        targetPosition = msg.position;
        targetRotation = msg.rotation;
        lastRemoteMessageTime = Time.time;
    }
}
