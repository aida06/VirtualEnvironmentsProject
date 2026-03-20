using System;
using UnityEngine;
using Ubiq.Messaging;

public class NetworkedDestroyable : MonoBehaviour
{
    [Serializable]
    public struct DestroyMessage
    {
        public bool destroy;
    }

    private NetworkContext context;
    private bool isDestroyed = false;

    private void Start()
    {
        context = NetworkScene.Register(this);
    }

    public void DestroyForAll()
    {
        if (isDestroyed) return;

        isDestroyed = true;

        // 先发消息给其他客户端
        context.SendJson(new DestroyMessage
        {
            destroy = true
        });

        Debug.Log($"[NetworkedDestroyable] DestroyForAll called on {gameObject.name}");

        // 再销毁本地对象
        Destroy(gameObject);
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<DestroyMessage>();

        if (msg.destroy && !isDestroyed)
        {
            isDestroyed = true;
            Debug.Log($"[NetworkedDestroyable] Remote destroy received for {gameObject.name}");
            Destroy(gameObject);
        }
    }
}