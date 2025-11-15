using UnityEngine;
using UnityEngine.InputSystem;

public class InitialSpawn : MonoBehaviour
{
    public GameObject prefab; // Player prefab to instantiate

    void Start()
    {
        // Instantiate PlayerInputs for two players with their keys and devices
        var player1 = PlayerInput.Instantiate(prefab, 0, "WASD", 0, Keyboard.current); // Left, Orange
        var player2 = PlayerInput.Instantiate(prefab, 1, "Arrows", 1, Keyboard.current); // Right, Green

        // Position the players at starting points
        player1.transform.position = new Vector3(2, 0.5f, 0);
        player2.transform.position = new Vector3(-2, 0.5f, 0);

        // Access cameras from player objects' parents (assumed structure)
        Camera camera1 = player1.transform.parent.GetChild(1).GetComponent<Camera>();
        Camera camera2 = player2.transform.parent.GetChild(1).GetComponent<Camera>();

        // Configure split-screen viewports
        camera1.rect = new Rect(0, 0, 0.5f, 1);
        camera2.rect = new Rect(0.5f, 0, 0.5f, 1);

        // Set camera positions and rotation to look downward at the playing field
        camera1.transform.position = new Vector3(-2, 10, -10);
        camera2.transform.position = new Vector3(2, 10, -10);
        Quaternion cameraRotation = Quaternion.Euler(45, 0, 0);
        camera1.transform.rotation = cameraRotation;
        camera2.transform.rotation = cameraRotation;

        // Save players' start positions for potential resets during play
        // Can be removed if MenuController manages start positions for holes
        player1.GetComponent<PlayerController>().SaveStartPosition();
        player2.GetComponent<PlayerController>().SaveStartPosition();

        // Disable audio listener on 2nd camera to avoid audio conflicts
        camera1.GetComponent<AudioListener>().enabled = false;

        // Assign player indices (used to differentiate player logic)
        var pc1 = player1.GetComponent<PlayerController>();
        var pc2 = player2.GetComponent<PlayerController>();
        pc1.playerIndex = 0; // Orange, left player
        pc2.playerIndex = 1; // Green, right player

        // Set initial turn state (optional reset)
        pc1.isMyTurn = true;
        pc2.isMyTurn = false;
    }
}