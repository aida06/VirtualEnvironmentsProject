using System;
using UnityEngine;
using Ubiq.Messaging;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class NetworkedStateObject : MonoBehaviour
{
    [Serializable]
    public struct StateMessage
    {
        public int state;
        public Vector3 position;
        public Quaternion rotation;

        public bool hasOwner;
        public string ownerId;
    }

    [Header("State Objects")]
    public GameObject[] stateObjects;

    [Header("Current State")]
    public int currentState = 0;

    [Header("Optional")]
    public bool syncPose = true;

    [Header("Ownership")]
    public bool hasOwner = false;
    public string ownerId = "";

    private string localClientId;
    private NetworkContext context;
    private XRGrabInteractable grabInteractable;

    private float sendInterval = 0.05f;
    private float lastSendTime = 0f;

    private void Awake()
    {
        localClientId = System.Guid.NewGuid().ToString();
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
        context = NetworkScene.Register(this);
        ApplyState(currentState);
    }

    private void Update()
    {
        if (IsOwner() && syncPose && Time.time - lastSendTime > sendInterval)
        {
            SendState();
            lastSendTime = Time.time;
        }
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        ClaimOwnership();
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        // 先不自动释放，避免刚放桌时 ownership 丢掉
        // 真要放手就释放，可以取消下面这行注释
        // ReleaseOwnership();
    }

    public bool IsOwner()
    {
        return hasOwner && ownerId == localClientId;
    }

    public void ClaimOwnership()
    {
        hasOwner = true;
        ownerId = localClientId;
        SendState();

        Debug.Log($"[Ownership] Claimed by {ownerId}");
    }

    public void ReleaseOwnership()
    {
        if (!IsOwner()) return;

        hasOwner = false;
        ownerId = "";
        SendState();

        Debug.Log("[Ownership] Released");
    }

    public void SetState(int newState)
    {
        if (!IsOwner()) return;
        if (!IsValidState(newState)) return;

        currentState = newState;
        ApplyState(currentState);
        SendState();
    }

    public void SetStateAndPose(int newState, Vector3 worldPos, Quaternion worldRot)
    {
        if (!IsOwner()) return;
        if (!IsValidState(newState)) return;

        if (syncPose)
        {
            transform.position = worldPos;
            transform.rotation = worldRot;
        }

        currentState = newState;
        ApplyState(currentState);
        SendState();
    }

    private bool IsValidState(int state)
    {
        return stateObjects != null && state >= 0 && state < stateObjects.Length;
    }

    private void SendState()
    {
        context.SendJson(new StateMessage
        {
            state = currentState,
            position = transform.position,
            rotation = transform.rotation,
            hasOwner = hasOwner,
            ownerId = ownerId
        });
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<StateMessage>();

        hasOwner = msg.hasOwner;
        ownerId = msg.ownerId;

        // 只有非 owner 才应用远端同步
        if (!IsOwner())
        {
            if (syncPose)
            {
                transform.position = msg.position;
                transform.rotation = msg.rotation;
            }

            currentState = msg.state;
            ApplyState(currentState);
        }
    }

    private void ApplyState(int state)
    {
        if (stateObjects == null) return;

        for (int i = 0; i < stateObjects.Length; i++)
        {
            if (stateObjects[i] != null)
            {
                stateObjects[i].SetActive(i == state);
            }
        }

        Debug.Log($"[NetworkedStateObject] ApplyState -> {state}, owner={ownerId}, pos={transform.position}");
    }
}
