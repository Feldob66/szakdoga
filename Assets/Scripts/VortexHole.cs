using UnityEngine;

public class VortexHole : MonoBehaviour
{
    [SerializeField] private float vortexForce = 10f;  // Force magnitude pulling players toward the center
    public bool HasSomeoneWon { get; set; }  // Tracks if game winner state reached, to disable vortex effect

    private void Start()
    {
        // Initialize winner flag to false at start
        HasSomeoneWon = false;
    }

    private void OnTriggerStay(Collider other)
    {
        // Check if object inside trigger is a Player and no winner declared yet
        if (other.CompareTag("Player") && !HasSomeoneWon)
        {
            // Calculate direction vector from player to vortex center
            Vector3 directionToCenter = (transform.position - other.transform.position).normalized;

            // Apply force pulling player toward the center of the vortex
            other.attachedRigidbody.AddForce(directionToCenter * vortexForce);
        }
    }
}