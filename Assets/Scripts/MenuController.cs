using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    public GameObject mainPanel;
    public GameObject rulesPanel;
    public GameObject endPanel;
    public Text[] countText;
    public GameObject[] pauseUI;
    private static bool isFirstLoad = true;
    private Dictionary<int, float> cameraRotations = new Dictionary<int, float>();
    public GameObject countPanel;
    public GameObject scoreboardPanel;
    public ScoreHandler scoreHandler;
    public int[] orangeScores = new int[9];
    public int[] greenScores = new int[9];
    public int currentHole = 0;
    public GameObject nextHoleButton;
    public float stopVelocity = 0.08f;
    private bool picker1ChoseMap = false;
    private struct ExpansionChoice { public MapBuilder.ExpansionMode mode; public int index; }
    private List<ExpansionChoice> pendingMapChanges = new List<ExpansionChoice>();
    private int strategicPickCount = 0;
    public GameObject effectPreviewBall;
    private GameObject startPreviewObject;
    private GameObject endPreviewObject;
    public static Vector3 PLAYER1_SPAWN_POS = new Vector3(2f, 0.5f, 0f);
    public static Vector3 PLAYER2_SPAWN_POS = new Vector3(-2f, 0.5f, 0f);
    public Vector3 startPreviewPosition = new Vector3(1200f, 1000f, 1000f);
    public Vector3 endPreviewPosition = new Vector3(1400f, 1000f, 1000f);
    public static readonly Vector3 BALL_DEFAULT_SCALE = new Vector3(0.5f, 0.5f, 0.5f);
    public GameObject[] strategicButtons;
    public List<string> EffectsOrCurses = new List<string> {
    "No Change",
    "Speed 0.5x","Speed 0.75x","Speed 1.5x","Speed 2x"
    };

    private int chooserStep = 0;
    private int currentChooserIndex = 0;
    private bool inStrategicPhase = false;
    private int lastPickerWas = 1;
    private int effectTargetPlayerIndex = 0;
    private string rolledEffect;
    private MapBuilder.ExpansionMode rolledExpansionStart;
    private MapBuilder.ExpansionMode rolledExpansionEnd;
    private Dictionary<int, List<string>> activeEffects = new Dictionary<int, List<string>>() { { 0, new List<string>() }, { 1, new List<string>() } };

    public bool AreBothBallsStopped()
    {
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        if (players == null || players.Length == 0)
            return false;
        var validPlayers = players.Where(player => player != null && player.Rb != null && !player.HasCompletedHole && player.shotCount < 13);
        if (!validPlayers.Any()) return true;
        return validPlayers.All(player => player.Rb.linearVelocity.magnitude < stopVelocity);
    }

    private int lastRenderedPlayer = -1;
    private bool lastRenderedWaiting = false;

    void Update()
    {
        if (inStrategicPhase) return; // Don't update turn display in chooser phase

        var turnDisplay = Object.FindFirstObjectByType<TurnDisplay>();
        if (turnDisplay == null) return;
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        PlayerController current = players.FirstOrDefault(p => p.isMyTurn);
        if (current == null) return;
        bool waiting = !AreBothBallsStopped();
        if (current.playerIndex != lastRenderedPlayer || waiting != lastRenderedWaiting)
        {
            turnDisplay.UpdateTurnDisplay(current.playerIndex, waiting);
            lastRenderedPlayer = current.playerIndex;
            lastRenderedWaiting = waiting;
        }
    }

    private void Awake()
    {
        if (isFirstLoad && SceneManager.GetActiveScene().buildIndex != 0)
        {
            isFirstLoad = false;
            SceneManager.LoadScene(0);
            return;
        }
        isFirstLoad = false;
    }

    void Start()
    {
        if (endPanel != null)
        {
            endPanel.SetActive(false);
            endPanel.transform.GetChild(0).GetComponent<Text>().text = "";
        }
        Time.timeScale = 1f;
        cameraRotations[0] = 0f;
        cameraRotations[1] = 0f;
        var turnDisplay = Object.FindFirstObjectByType<TurnDisplay>(UnityEngine.FindObjectsInactive.Include);
        if (turnDisplay != null)
        {
            turnDisplay.gameObject.SetActive(true);
            var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            foreach (var player in players)
            {
                if (player.isMyTurn)
                {
                    turnDisplay.UpdateTurnDisplay(player.playerIndex, !AreBothBallsStopped());
                    break;
                }
            }
        }
        RerollOptions();
    }

    public void ReturnFromRules()
    {
        //hide rules panel, show main panel
        if (rulesPanel != null)
            {
            rulesPanel.SetActive(false);
        }
        if (mainPanel != null)
        {
            mainPanel.SetActive(true);
        }
    }
    public void ShowRules()
    {
        //hide main panel, show rules panel
        if (mainPanel != null)
        {
            mainPanel.SetActive(false);
        }
        if (rulesPanel != null)
        {
            rulesPanel.SetActive(true);
        }
    }

    public void TransitionScene(int level)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(level);
    }

    public void Pause()
    {
        if (pauseUI == null || pauseUI.Length < 2) return;
        Time.timeScale = 0;
        pauseUI[0].SetActive(false);
        pauseUI[1].SetActive(true);
        ShowScoreboardPanel();
    }

    public void UnPause()
    {
        if (pauseUI == null || pauseUI.Length < 2) return;
        Time.timeScale = 1;
        pauseUI[0].SetActive(true);
        pauseUI[1].SetActive(false);

        // Only hide scoreboard if not at final results (endPanel)
        if (scoreboardPanel != null && (endPanel == null || !endPanel.activeSelf))
            scoreboardPanel.SetActive(false);
    }

    public void FinishGame()
    {
        if (nextHoleButton != null && currentHole == 8)
            nextHoleButton.SetActive(false);
        if (endPanel != null)
        {
            endPanel.SetActive(true);
            ShowScoreboardPanel();
            if (countPanel != null)
                countPanel.SetActive(false);

            string scoreboardText = "";
            int orangeTotal = orangeScores.Sum();
            int greenTotal = greenScores.Sum();

            if (currentHole == 8)
            {
                var turnDisplay = Object.FindFirstObjectByType<TurnDisplay>();
                if (turnDisplay != null) turnDisplay.gameObject.SetActive(false);

                if (orangeTotal > greenTotal)
                    scoreboardText = "Winner Green\n\nFinal Scores:";
                else if (greenTotal > orangeTotal)
                    scoreboardText = "Winner Orange\n\nFinal Scores:";
                else
                    scoreboardText = "No clear Winner - Tie\n\nFinal Scores:";
            }
            else
            {
                scoreboardText = $"End of Hole #{currentHole + 1}\n\nCurrent Scores:";
                StartStrategicPhase();
            }

            var finalTextTransform = endPanel.transform.Find("FinalText");
            if (finalTextTransform != null)
            {
                var textComp = finalTextTransform.GetComponent<Text>();
                if (textComp != null)
                    textComp.text = scoreboardText;
            }
        }
    }

    public void ShowScoreboardPanel()
    {
        if (scoreboardPanel != null)
            scoreboardPanel.SetActive(true);
        if (scoreHandler != null)
        {
            scoreHandler.SetScoreArrays(orangeScores, greenScores, currentHole);
            scoreHandler.UpdateScoreboard();
        }
    }

    public void AddCountText(int playerIndex, int count)
    {
        if (countText != null && playerIndex < countText.Length)
            countText[playerIndex].text = "Shots: " + count.ToString();
    }

    private IEnumerator StartNextHoleRoutine()
    {
        if (scoreboardPanel != null)
            scoreboardPanel.SetActive(false);
        if (countPanel != null)
            countPanel.SetActive(true);

        var vortexHole = UnityEngine.Object.FindFirstObjectByType<VortexHole>();
        Collider vortexCollider = null;
        if (vortexHole != null)
        {
            vortexCollider = vortexHole.GetComponent<Collider>();
            if (vortexCollider != null)
                vortexCollider.enabled = false;
            vortexHole.HasSomeoneWon = false;
        }

        yield return new WaitForFixedUpdate();
        var allPlayers = UnityEngine.Object.FindObjectsByType<PlayerController>(
            UnityEngine.FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (allPlayers.Length >= 2)
        {
            allPlayers[0].transform.position = PLAYER1_SPAWN_POS;
            allPlayers[1].transform.position = PLAYER2_SPAWN_POS;
        }
        int shot0 = allPlayers[0].shotCount;
        int shot1 = allPlayers[1].shotCount;
        int starterIndex = (shot0 > shot1) ? 0 : (shot1 > shot0) ? 1 : (allPlayers[0].isMyTurn ? 0 : 1);

        for (int i = 0; i < allPlayers.Length; ++i)
        {
            var player = allPlayers[i];
            var rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            player.shotCount = 0;
            player.HasCompletedHole = false;
            player.isMyTurn = (i == starterIndex);
            if (!player.gameObject.activeSelf)
                player.gameObject.SetActive(true);

            player.transform.localScale = BALL_DEFAULT_SCALE;
        }
        // Effect is applied and then cleared (one-turn effect)
        ApplyStoredEffects();

        yield return new WaitForSecondsRealtime(0.08f);
        if (vortexCollider != null)
            vortexCollider.enabled = true;
        var turnDisplay = UnityEngine.Object.FindFirstObjectByType<TurnDisplay>();
        if (turnDisplay != null)
            turnDisplay.UpdateTurnDisplay(starterIndex, !AreBothBallsStopped());
        if (endPanel != null)
            endPanel.SetActive(false);
        AddCountText(0, 0);
        AddCountText(1, 0);
        currentHole++;
        if (scoreHandler != null)
            scoreHandler.currentHole = currentHole;
    }

    public void StartNextHole()
    {
        StartCoroutine(StartNextHoleRoutine());
        RerollOptions();
    }

    public void StartStrategicPhase()
    {
        inStrategicPhase = true;
        chooserStep = 0;
        strategicPickCount = 0;
        pendingMapChanges.Clear();
        currentChooserIndex = GetCurrentChooserByScore();
        ShowStrategicPhaseUI(true);
        RerollOptions();
        var turnDisplay = Object.FindFirstObjectByType<TurnDisplay>();
        if (turnDisplay != null)
            turnDisplay.SetChooser(currentChooserIndex);

        var vortexHole = UnityEngine.Object.FindFirstObjectByType<VortexHole>();
        if (vortexHole != null)
        {
            var collider = vortexHole.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            vortexHole.HasSomeoneWon = false;
        }
    }

    private int GetCurrentChooserByScore()
    {
        int orange = orangeScores.Sum();
        int green = greenScores.Sum();
        if (orange > green) return 0;
        if (green > orange) return 1;
        lastPickerWas = (lastPickerWas + 1) % 2;
        return lastPickerWas;
    }

    public void RerollOptions()
    {
        rolledEffect = EffectsOrCurses[Random.Range(0, EffectsOrCurses.Count)];
        effectTargetPlayerIndex = Random.Range(0, 2);
        var mapBuilder = Object.FindFirstObjectByType<MapBuilder>();
        if (mapBuilder != null)
        {
            mapBuilder.PickRandomExpansionModes();
            rolledExpansionStart = mapBuilder.expandStart;
            rolledExpansionEnd = mapBuilder.expandEnd;
        }
        if (strategicButtons != null && strategicButtons.Length >= 3)
        {
            strategicButtons[0].GetComponentInChildren<TMP_Text>().text =
                $"{rolledEffect} for {(effectTargetPlayerIndex == 0 ? "Orange" : "Green")}";
            strategicButtons[1].GetComponentInChildren<TMP_Text>().text =
                $"{rolledExpansionStart} at start";
            strategicButtons[2].GetComponentInChildren<TMP_Text>().text =
                $"{rolledExpansionEnd} at end";
        }
        if (startPreviewObject != null)
            Destroy(startPreviewObject);
        if (endPreviewObject != null)
            Destroy(endPreviewObject);

        if (mapBuilder != null)
        {
            GameObject startPrefab = GetExpansionPrefab(mapBuilder, rolledExpansionStart);
            GameObject endPrefab = GetExpansionPrefab(mapBuilder, rolledExpansionEnd);

            if (effectPreviewBall != null)
            {
                var rend = effectPreviewBall.GetComponent<MeshRenderer>();
                if (rend != null)
                {
                    Color ORANGE_COLOR = new Color(1f, 0.647f, 0f, 1f);
                    Color GREEN_COLOR = Color.green;
                    rend.material.color = (effectTargetPlayerIndex == 0) ? ORANGE_COLOR : GREEN_COLOR;
                }
                float previewScale = 1f;
                foreach (var eff in activeEffects[effectTargetPlayerIndex])
                {
                    if (eff.StartsWith("Growth")) previewScale *= ParseMultiplier(eff);
                    else if (eff.StartsWith("Shrink")) previewScale /= ParseMultiplier(eff);
                }
                effectPreviewBall.transform.localScale = BALL_DEFAULT_SCALE * previewScale;
            }
            if (startPrefab != null) startPreviewObject = Instantiate(startPrefab, startPreviewPosition, Quaternion.identity);
            if (endPrefab != null) endPreviewObject = Instantiate(endPrefab, endPreviewPosition, Quaternion.identity);
        }
    }

    public void Option1Button() { HandleStrategicPick(1); }
    public void Option2Button() { HandleStrategicPick(2); }
    public void Option3Button() { HandleStrategicPick(3); }

    private void HandleStrategicPick(int option)
    {
        strategicPickCount++;
        if (option == 1)
        {
            // No speed effect; clear previous effects for this player
            activeEffects[effectTargetPlayerIndex].Clear();

            // Set ball color as visual cue
            var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            if (players.Length > effectTargetPlayerIndex && players[effectTargetPlayerIndex] != null)
            {
                var meshRenderer = players[effectTargetPlayerIndex].GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    Color ORANGE_COLOR = new Color(1f, 0.647f, 0f, 1f);
                    Color GREEN_COLOR = Color.green;
                    meshRenderer.material.color = (effectTargetPlayerIndex == 0) ? ORANGE_COLOR : GREEN_COLOR;
                }
            }
        }
        else if (option == 2)
        {
            pendingMapChanges.Add(new ExpansionChoice { mode = rolledExpansionStart, index = 0 });
            picker1ChoseMap = (currentChooserIndex == 0);
        }
        else if (option == 3)
        {
            var mapBuilder = Object.FindFirstObjectByType<MapBuilder>();
            int currentEndIdx = (mapBuilder != null) ? mapBuilder.GetEndIndex() : 0;
            int safeIdx = Mathf.Max(0, currentEndIdx - 1);
            pendingMapChanges.Add(new ExpansionChoice { mode = rolledExpansionEnd, index = safeIdx });
            picker1ChoseMap = (currentChooserIndex == 0);
        }

        if (chooserStep == 0)
        {
            chooserStep = 1;
            currentChooserIndex = 1 - currentChooserIndex;
            RerollOptions();
            var turnDisplay = Object.FindFirstObjectByType<TurnDisplay>();
            if (turnDisplay != null) turnDisplay.SetChooser(currentChooserIndex);
        }
        else
        {
            ShowStrategicPhaseUI(false);
            ApplyPendingExpansions();
            ApplyStoredEffects();
            inStrategicPhase = false;
            StartNextHole();
        }
    }

    private void ApplyPendingExpansions()
    {
        var mapBuilder = Object.FindFirstObjectByType<MapBuilder>();
        if (mapBuilder != null && pendingMapChanges.Count > 0)
        {
            foreach (var choice in pendingMapChanges)
            {
                mapBuilder.troubleTestMode = choice.mode;
                mapBuilder.insertIndex = choice.index;
                mapBuilder.InsertTileAndShiftForward();
            }
        }
        pendingMapChanges.Clear();
    }

    private void ApplyStoredEffects()
    {
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var kv in activeEffects)
        {
            int idx = kv.Key;
            float cumulativeSpeed = 1f;
            foreach (var eff in kv.Value)
            {
                if (eff.StartsWith("Speed"))
                    cumulativeSpeed *= ParseMultiplier(eff);
            }
            if (players.Length > idx && players[idx] != null)
            {
                var rb = players[idx].GetComponent<Rigidbody>();
                if (rb != null && cumulativeSpeed != 1f)
                    players[idx].GetComponent<PlayerController>().speedMultiplier = cumulativeSpeed;
            }
            kv.Value.Clear(); // One-turn only
        }
    }
    private float ParseMultiplier(string effect)
    {
        string[] parts = effect.Split(' ');
        if (parts.Length < 2) return 1f;
        string xpart = parts[1].TrimEnd('x', 'X');
        float val;
        return float.TryParse(xpart, out val) ? val : 1f;
    }

    private void ShowStrategicPhaseUI(bool show)
    {
        if (strategicButtons == null) return;
        foreach (var btn in strategicButtons)
            if (btn != null) btn.SetActive(show);
    }

    private GameObject GetExpansionPrefab(MapBuilder builder, MapBuilder.ExpansionMode mode)
    {
        string fieldName = mode.ToString() + "Fab";
        fieldName = char.ToLowerInvariant(fieldName[0]) + fieldName.Substring(1);
        var field = typeof(MapBuilder).GetField(fieldName);
        if (field == null)
        {
            // Debug.LogWarning("MapBuilder has no field: " + fieldName);
            return null;
        }
        return field.GetValue(builder) as GameObject;
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}