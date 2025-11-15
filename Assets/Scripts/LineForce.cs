using UnityEngine;

public class LineForce : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer; // Visual aiming line
    private bool isIdle;          // True if object is stationary and ready to be aimed
    private bool isAiming;        // True if player is currently aiming a shot
    private new Rigidbody rigidbody;  // Rigidbody component for physics forces

    [SerializeField] private float stopVelocity = 0.05f;  // Velocity threshold to consider object stopped
    [SerializeField] private float shotPower = 150f;      // Multiplier force applied when shooting

    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
        isAiming = false;
        lineRenderer.enabled = false; // Initially hide aiming line
    }

    private void Start()
    {
        // Ensure lineRenderer reference is assigned, fallback to component on same object
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }
        lineRenderer.enabled = false; // Start disabled
    }

    private void OnMouseDown()
    {
        // Allow aiming only if stationary
        if (isIdle)
        {
            isAiming = true;
        }
    }

    private void ProcessAim()
    {
        if (!isAiming || !isIdle)
        {
            return; // Skip if not aiming or not idle
        }

        Vector3? worldPoint = CastMouseClickRay();

        if (worldPoint.HasValue)
        {
            DrawLine(worldPoint.Value);

            // On left mouse button release, execute shot
            if (Input.GetMouseButtonUp(0))
            {
                Shoot(worldPoint.Value);
            }
        }
        else
        {
            lineRenderer.enabled = false; // Hide line if no valid point hit
        }
    }

    private void Shoot(Vector3 worldPoint)
    {
        isAiming = false;
        lineRenderer.enabled = false;

        // Only consider horizontal component for direction
        Vector3 horizontalWorldPoint = new Vector3(worldPoint.x, transform.position.y, worldPoint.z);
        Vector3 direction = (horizontalWorldPoint - transform.position).normalized;

        // Strength based on distance from object to target point
        float strength = Vector3.Distance(horizontalWorldPoint, transform.position);

        // Apply force scaled by strength and shot power multipler
        rigidbody.AddForce(direction * strength * shotPower);

        isIdle = false; // Mark as no longer idle since it's moving now
    }

    // Physics update, runs fixed timestep
    private void FixedUpdate()
    {
        // If velocity below threshold, stop motion and enable aiming again
        if (rigidbody.linearVelocity.magnitude < stopVelocity)
        {
            Stop();
        }

        ProcessAim();
    }

    private void Stop()
    {
        rigidbody.linearVelocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
        isIdle = true; // Ready for next aim/shoot
    }

    private void DrawLine(Vector3 worldPoint)
    {
        Vector3[] positions =
        {
            transform.position,
            worldPoint
        };

        lineRenderer.SetPositions(positions);
        lineRenderer.enabled = true;
    }

    private Vector3? CastMouseClickRay()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            return hit.point;
        }
        return null;
    }
}