using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Ubiq.Messaging;

public class SauceBottle : MonoBehaviour
{
    private NetworkContext context;
    private bool owner;

    private Transform nozzle;
    private GameObject currentSauce;
    private Material sauceMaterial;

    private bool isPouring;

    private struct Message
    {
        public Vector3 position;
        public Quaternion rotation;
        public bool isPouring;

        public Message(Transform t, bool pouring)
        {
            position = t.position;
            rotation = t.rotation;
            isPouring = pouring;
        }
    }

    private void Start()
    {
        nozzle = transform.Find("Nozzle");

        var shader = Shader.Find("Sprites/Default");
        sauceMaterial = new Material(shader);

        var grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);

        context = NetworkScene.Register(this);
    }

    private void FixedUpdate()
    {
        if (owner)
        {
            CheckTilt();

            context.SendJson(new Message(transform, isPouring));
        }
    }

    private void CheckTilt()
    {
        float dot = Vector3.Dot(transform.up, Vector3.down);

        bool shouldPour = dot > 0.7f; // 倒置角度阈值

        if (shouldPour && !isPouring)
        {
            BeginSauce();
            isPouring = true;
        }
        else if (!shouldPour && isPouring)
        {
            EndSauce();
            isPouring = false;
        }
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage msg)
    {
        var data = msg.FromJson<Message>();

        transform.position = data.position;
        transform.rotation = data.rotation;

        if (data.isPouring && !isPouring)
        {
            BeginSauce();
            isPouring = true;
        }

        if (!data.isPouring && isPouring)
        {
            EndSauce();
            isPouring = false;
        }
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        owner = true;
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        owner = false;
        EndSauce();
        isPouring = false;
    }

    private void BeginSauce()
    {
        currentSauce = new GameObject("Sauce");
        var trail = currentSauce.AddComponent<TrailRenderer>();

        trail.time = Mathf.Infinity;
        trail.material = sauceMaterial;
        trail.startWidth = 0.05f;
        trail.endWidth = 0.05f;
        trail.minVertexDistance = 0.02f;

        currentSauce.transform.parent = nozzle;
        currentSauce.transform.localPosition = Vector3.zero;
        currentSauce.transform.localRotation = Quaternion.identity;
    }

    private void EndSauce()
    {
        if (currentSauce == null) return;

        currentSauce.transform.parent = null;
        currentSauce.GetComponent<TrailRenderer>().emitting = false;
        currentSauce = null;
    }
}