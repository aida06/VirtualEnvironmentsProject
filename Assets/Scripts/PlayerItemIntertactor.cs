using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerItemInteractor : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public Transform holdPoint;

    [Header("Interaction Settings")]
    public float interactDistance = 3f;

    private GameObject heldObject;
    private Rigidbody heldRigidbody;
    private Collider heldCollider;

    void Update()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (heldObject == null)
        {
                TryTakeItem();
        }
        else
        {
            DropObject();
        }
}

        if (heldObject != null && holdPoint != null)
        {
            heldObject.transform.position = holdPoint.position;
            heldObject.transform.rotation = holdPoint.rotation;
        }
    }

    void TryTakeItem()
    {
        if (playerCamera == null)
        {
            Debug.LogWarning("Player Camera is not assigned.");
            return;
        }

        if (holdPoint == null)
        {
            Debug.LogWarning("Hold Point is not assigned.");
            return;
        }

        Ray ray = playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            ItemBox box = hit.collider.GetComponentInParent<ItemBox>();

            if (box != null)
            {
                heldObject = box.SpawnItem();

                if (heldObject != null)
                {
                    heldRigidbody = heldObject.GetComponent<Rigidbody>();
                    heldCollider = heldObject.GetComponent<Collider>();

                    if (heldRigidbody != null)
                    {
                        heldRigidbody.isKinematic = true;
                        heldRigidbody.useGravity = false;
                        heldRigidbody.linearVelocity = Vector3.zero;
                        heldRigidbody.angularVelocity = Vector3.zero;
                    }

                    if (heldCollider != null)
                    {
                        heldCollider.enabled = false;
                    }

                    heldObject.transform.position = holdPoint.position;
                    heldObject.transform.rotation = holdPoint.rotation;
                }
            }
        }
    }

    void DropObject()
    {
        if (heldObject == null)
            return;

        if (heldRigidbody != null)
        {
            heldRigidbody.isKinematic = false;
            heldRigidbody.useGravity = true;
        }

        if (heldCollider != null)
        {
            heldCollider.enabled = true;
        }

        heldObject = null;
        heldRigidbody = null;
        heldCollider = null;
    }
}