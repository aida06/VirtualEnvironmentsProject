using System;
using UnityEngine;
using Ubiq.Messaging;

public class NetworkedStateObject : MonoBehaviour
{
    [Serializable]
    public struct StateMessage
    {
        public int state;
    }

    public GameObject[] stateObjects;
    public int currentState = 0;

    private NetworkContext context;

    private void Awake()
    {
        if (stateObjects == null || stateObjects.Length == 0)
        {
            stateObjects = new GameObject[2];

            Transform doughBall = transform.Find("DoughBall");
            Transform dough = transform.Find("Dough");

            if (doughBall != null) stateObjects[0] = doughBall.gameObject;
            if (dough != null) stateObjects[1] = dough.gameObject;
        }

        Debug.Log($"[NetworkedStateObject] root={gameObject.name}, child0={(stateObjects.Length > 0 && stateObjects[0] != null ? stateObjects[0].name : "null")}, child1={(stateObjects.Length > 1 && stateObjects[1] != null ? stateObjects[1].name : "null")}, currentState={currentState}");
    }

    private void Start()
    {
        context = NetworkScene.Register(this);
        ApplyState(currentState);

        var doughBall = transform.Find("DoughBall");
        if (doughBall != null)
        {
            var mr = doughBall.GetComponent<MeshRenderer>();
            var mf = doughBall.GetComponent<MeshFilter>();

            Debug.Log($"[Check] DoughBall worldPos={doughBall.position}");
            Debug.Log($"[Check] DoughBall localPos={doughBall.localPosition}");
            Debug.Log($"[Check] DoughBall localScale={doughBall.localScale}");
            Debug.Log($"[Check] MeshRenderer enabled={(mr != null ? mr.enabled.ToString() : "no renderer")}");
            Debug.Log($"[Check] Mesh exists={(mf != null && mf.sharedMesh != null)}");
        }
    }

    public void SetState(int newState)
    {
        if (stateObjects == null || newState < 0 || newState >= stateObjects.Length)
            return;

        currentState = newState;
        ApplyState(currentState);

        context.SendJson(new StateMessage
        {
            state = currentState
        });
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<StateMessage>();
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
                bool shouldBeActive = (i == state);
                stateObjects[i].SetActive(shouldBeActive);
                Debug.Log($"[NetworkedStateObject] {stateObjects[i].name} active = {shouldBeActive}");
            }
            else
            {
                Debug.LogWarning($"[NetworkedStateObject] stateObjects[{i}] is null");
            }
        }
    }
}

