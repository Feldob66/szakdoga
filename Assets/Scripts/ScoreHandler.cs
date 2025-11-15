using UnityEngine;
using TMPro;

public class ScoreHandler : MonoBehaviour
{
    // Score arrays hold scores for 9 holes each player
    public int[] orangeScores = new int[9];
    public int[] greenScores = new int[9];

    // UI Text fields to display scores per hole and total scores
    public TMP_Text[] orangeFields;
    public TMP_Text[] greenFields;
    public TMP_Text orangeTotalField;
    public TMP_Text greenTotalField;

    public int currentHole = 0;

    // Sets score data arrays and sets which hole is active
    public void SetScoreArrays(int[] orange, int[] green, int holeActive)
    {
        for (int i = 0; i < 9; i++)
        {
            orangeScores[i] = orange[i];
            greenScores[i] = green[i];
        }
        currentHole = holeActive;
    }

    // Updates score for given player and hole, then refreshes scoreboard UI
    public void UpdateScore(int playerIndex, int hole, int shots)
    {
        // Debug.Log($"UpdateScore called: player {playerIndex}, hole {hole}, shots {shots}");
        if (playerIndex == 0)
            orangeScores[hole] = shots;
        else
            greenScores[hole] = shots;

        UpdateScoreboard();
    }

    // Updates the UI text elements for each hole score and total sums
    public void UpdateScoreboard()
    {
        // Debug.Log("Scoreboard update called");
        int orangeSum = 0, greenSum = 0;
        for (int i = 0; i < 9; i++)
        {
            // Orange player's hole scores
            if (orangeFields != null && orangeFields.Length > i)
            {
                if (orangeScores[i] == 0 && i != currentHole)
                {
                    orangeFields[i].text = ""; // Clear if no score and not current hole
                    // Debug.Log($"Orange {i + 1}: Empty");
                }
                else
                {
                    orangeFields[i].text = orangeScores[i].ToString();
                    // Debug.Log($"Orange {i + 1}: {orangeScores[i]}");
                }
            }
            // Green player's hole scores
            if (greenFields != null && greenFields.Length > i)
            {
                if (greenScores[i] == 0 && i != currentHole)
                {
                    greenFields[i].text = ""; // Clear if no score and not current hole
                    // Debug.Log($"Green {i + 1}: Empty");
                }
                else
                {
                    greenFields[i].text = greenScores[i].ToString();
                    // Debug.Log($"Green {i + 1}: {greenScores[i]}");
                }
            }
            orangeSum += orangeScores[i];
            greenSum += greenScores[i];
        }
        // Update total score UI fields if set
        if (orangeTotalField != null)
        {
            orangeTotalField.text = orangeSum.ToString();
            // Debug.Log($"Orange total: {orangeSum}");
        }
        if (greenTotalField != null)
        {
            greenTotalField.text = greenSum.ToString();
            // Debug.Log($"Green total: {greenSum}");
        }
    }
}