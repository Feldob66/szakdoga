using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float speed;                 // Movement speed towards waypoints
    public List<Transform> waypoints;  // List of waypoints to patrol

    private int waypointIndex;          // Current target waypoint index
    private float range;                // Distance threshold to switch to next waypoint
    private Rigidbody rb;               // Rigidbody component for physics (currently unused)

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        waypointIndex = 0;
        range = 1.0f; // How close it must be to waypoint to move to next
    }

    void Update()
    {
        Move();
    }

    // Moves enemy towards current waypoint, looping through waypoints
    void Move()
    {
        // Rotate to face current waypoint
        transform.LookAt(waypoints[waypointIndex]);

        // Move forward towards waypoint at defined speed, frame-rate independent
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        // Check distance to waypoint, advance if close enough
        if (Vector3.Distance(transform.position, waypoints[waypointIndex].position) < range)
        {
            waypointIndex++;

            // Loop back to first waypoint after last
            if (waypointIndex >= waypoints.Count)
            {
                waypointIndex = 0;
            }
        }
    }
}