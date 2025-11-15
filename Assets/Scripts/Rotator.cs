using UnityEngine;

public class Rotator : MonoBehaviour
{
    // Rotates the GameObject around its Z-axis every frame
    void Update()
    {
        float rotationSpeed = 45f * 1.36f; // degrees per second
        // Rotate around Z-axis using deltaTime for smooth frame-rate independent rotation
        transform.Rotate(new Vector3(0, 0, rotationSpeed) * Time.deltaTime);
    }
}