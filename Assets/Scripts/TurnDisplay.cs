using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TurnDisplay : MonoBehaviour
{
    public Image innerImage;          // Assign TurnDisplayInner UI Image here
    public TMP_Text turnText;         // Assign TurnDisplayText UI Text here
    public Image innerImageShadow;    // Assign InnerImageShadow UI Image here (optional)

    public Color orangeColor = new Color(1f, 0.5f, 0f);  // Orange color (hex #ff8000)
    public Color greenColor = new Color(0f, 0.8f, 0.2f); // Green color (hex #00cc33)

    // Update the turn display UI indicating current player's turn and waiting status
    public void UpdateTurnDisplay(int currentPlayerIndex, bool waitingForBalls)
    {
        if (innerImage == null || turnText == null)
        {
            Debug.LogError("TurnDisplay references not set! Assign InnerImage and TurnText in inspector.");
            return;
        }

        // Toggle shadow visibility based on whether waiting for balls
        if (innerImageShadow != null)
            innerImageShadow.gameObject.SetActive(waitingForBalls);

        string suffix = waitingForBalls ? " II" : "";

        if (currentPlayerIndex == 0)
        {
            innerImage.color = orangeColor;
            turnText.text = "Orange's turn" + suffix;
        }
        else
        {
            innerImage.color = greenColor;
            turnText.text = "Green's turn" + suffix;
        }
    }

    // Set the chooser indication UI for the player who is currently making a decision
    public void SetChooser(int chooserIndex)
    {
        if (innerImage == null || turnText == null)
        {
            Debug.LogError("TurnDisplay references not set! Assign InnerImage and TurnText in inspector.");
            return;
        }

        if (chooserIndex == 0)
        {
            innerImage.color = orangeColor;
            turnText.text = "Orange is deciding...";
        }
        else
        {
            innerImage.color = greenColor;
            turnText.text = "Green is deciding...";
        }

        // Hide shadow during chooser state
        if (innerImageShadow != null)
            innerImageShadow.gameObject.SetActive(false);
    }
}