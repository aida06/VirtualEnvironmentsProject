using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ItemBox : MonoBehaviour
{
    [Header("Item Settings")]
    public GameObject itemPrefab;
    public Transform spawnPoint;

    [Header("XR Interaction")]
    public XRSimpleInteractable interactable;

    private void Reset()
    {
        interactable = GetComponent<XRSimpleInteractable>();
    }

    private void OnEnable()
    {
        if (interactable != null)
        {
            interactable.activated.AddListener(OnActivated);
        }
    }

    private void OnDisable()
    {
        if (interactable != null)
        {
            interactable.activated.RemoveListener(OnActivated);
        }
    }

    private void OnActivated(ActivateEventArgs args)
    {
        SpawnItem();
    }

    public GameObject SpawnItem()
    {
        if (itemPrefab == null)
        {
            Debug.LogWarning("Item Prefab is not assigned.");
            return null;
        }

        Vector3 spawnPosition = spawnPoint != null
            ? spawnPoint.position
            : transform.position + Vector3.up * 0.5f;

        Quaternion spawnRotation = spawnPoint != null
            ? spawnPoint.rotation
            : Quaternion.identity;

        return Instantiate(itemPrefab, spawnPosition, spawnRotation);
    }
}