using System;
using UnityEngine;
using Ubiq.Messaging;

public class NetworkedDestroyable : MonoBehaviour
{
    [Serializable]
    public struct FlattenMessage
    {
        public bool flatten;
        public Vector3 position;
        public Quaternion rotation;
    }

    private NetworkContext context;
    private DestoryDoughBallRollable rollable;

    private void Start()
    {
        context = NetworkScene.Register(this);
        rollable = GetComponent<DestoryDoughBallRollable>();
    }

    public void SendFlatten(Vector3 position, Quaternion rotation)
    {
        context.SendJson(new FlattenMessage
        {
            flatten = true,
            position = position,
            rotation = rotation
        });
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<FlattenMessage>();

        if (!msg.flatten)
            return;

        if (rollable != null)
        {
            rollable.ApplyFlattenFromNetwork(msg.position, msg.rotation);
        }
    }
}
