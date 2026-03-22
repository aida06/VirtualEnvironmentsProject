using System;
using UnityEngine;
using Ubiq.Messaging;

public class NetworkedStateObject : MonoBehaviour
{
    [Serializable]
    public struct StateMessage
    {
        public int state;
        public Vector3 position;
        public Quaternion rotation;
    }

    [Header("State Objects")]
    public GameObject[] stateObjects;

    [Header("Current State")]
    public int currentState = 0;

    [Header("Optional")]
    public bool syncPose = true;

    private NetworkContext context;

    private void Start()
    {
        context = NetworkScene.Register(this);
        ApplyState(currentState);
    }

    public void SetState(int newState)
    {
        if (!IsValidState(newState)) return;

        currentState = newState;
        ApplyState(currentState);
        SendState();
    }

    public void SetStateAndPose(int newState, Vector3 worldPos, Quaternion worldRot)
    {
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
            rotation = transform.rotation
        });
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<StateMessage>();

        if (syncPose)
        {
            transform.position = msg.position;
            transform.rotation = msg.rotation;
        }

        currentState = msg.state;
        ApplyState(currentState);
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

        Debug.Log($"[NetworkedStateObject] ApplyState -> {state}, pos={transform.position}");
    }
}
