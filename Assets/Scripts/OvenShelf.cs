using UnityEngine;

public class OvenShelf : MonoBehaviour
{
    public OvenController ovenController;

    void OnTriggerEnter(Collider other)
    {
        PlateController plate = other.GetComponentInParent<PlateController>();
        if (plate != null)
        {
            ovenController.OnPlateDetected(plate.gameObject);
        }
    }

    void OnTriggerExit(Collider other)
    {
        PlateController plate = other.GetComponentInParent<PlateController>();
        if (plate != null)
        {
            ovenController.OnPlateRemoved();
        }
    }
}