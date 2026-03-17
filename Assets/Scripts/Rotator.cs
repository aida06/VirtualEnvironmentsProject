using UnityEngine;

public class Rotator : MonoBehaviour
{
    // Rotation speed in degrees per second
    public float rotationSpeed = 50f;

    void Update()
    {
        // Rotate around the Y-axis
        transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f);
    }
}
