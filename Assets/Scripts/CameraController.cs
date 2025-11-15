using UnityEngine;

public class CameraController : MonoBehaviour
{
    public GameObject player;           // Player to follow and orbit around
    private Vector3 offset;             // Offset from player to camera position
    private float rotationY = 0f;       // Yaw rotation angle (horizontal)
    private float rotationX = 0f;       // Pitch rotation angle (vertical)
    public float sensitivity = 5f;      // Mouse sensitivity for rotation
    private Camera cam;                 // Reference to this camera component
    private bool isDragging = false;    // Is right mouse button currently dragging?
    private bool dragStartedInViewport = false; // Did drag start inside this camera's viewport?

    void Start()
    {
        // Initialize offset between camera and player position
        offset = transform.position - player.transform.position;

        // Get the Camera component attached to this object
        cam = GetComponent<Camera>();

        // Initialize rotation angles from current transform
        Vector3 angles = transform.eulerAngles;
        rotationX = angles.y; // Horizontal rotation (yaw)
        rotationY = angles.x; // Vertical rotation (pitch)
    }

    void LateUpdate()
    {
        // Right mouse button down: start dragging if inside viewport
        if (Input.GetMouseButtonDown(1))
        {
            isDragging = true;
            dragStartedInViewport = IsMouseInViewport();
        }

        // Right mouse button up: stop dragging
        if (Input.GetMouseButtonUp(1))
        {
            isDragging = false;
            dragStartedInViewport = false;
        }

        if (isDragging && dragStartedInViewport)
        {
            // Update rotation angles based on mouse movement and sensitivity
            rotationX += Input.GetAxis("Mouse X") * sensitivity;
            rotationY -= Input.GetAxis("Mouse Y") * sensitivity;

            // Clamp vertical rotation to prevent flipping over
            rotationY = Mathf.Clamp(rotationY, -80, 80);

            // Calculate new rotation quaternion
            Quaternion rot = Quaternion.Euler(rotationY, rotationX, 0);

            // Apply rotation to offset and set camera position accordingly
            transform.position = player.transform.position + rot * offset;

            // Look at the player
            transform.LookAt(player.transform.position);
        }
        else
        {
            // No dragging: keep current rotation angles but update position and rotation
            transform.position = player.transform.position + Quaternion.Euler(rotationY, rotationX, 0) * offset;
            transform.LookAt(player.transform.position);
        }
    }

    // Checks if mouse position is inside the camera's split-screen viewport
    bool IsMouseInViewport()
    {
        Vector3 mousePos = Input.mousePosition;
        Rect camRect = cam.rect;
        float screenW = Screen.width;
        float screenH = Screen.height;

        float leftEdge = camRect.x * screenW;
        float rightEdge = (camRect.x + camRect.width) * screenW;
        float topEdge = (1f - camRect.y) * screenH;
        float bottomEdge = (1f - (camRect.y + camRect.height)) * screenH;

        // Returns true if mouse in both horizontal and vertical bounds
        return mousePos.x >= leftEdge && mousePos.x <= rightEdge &&
               mousePos.y >= bottomEdge && mousePos.y <= topEdge;
    }

    // Allows updating the camera offset externally
    public void UpdateOffset(Vector3 newOffset)
    {
        offset = newOffset;
    }
}