using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    // === Trajectory Settings ===
    [Header("Trajectory Settings")]
    [SerializeField] private float maxPullDistance = 3f;
    private const float SHOT_POWER = 3.6f;
    private const float BOUNCE_MULTIPLIER = 1f;
    private const float MAX_BOUNCE_FORCE = 6f;
    private const float MIN_BOUNCE_SPEED = 0.5f;
    private const float GROUND_FRICTION = 0.95f;
    private const float MIN_LAUNCH_ANGLE = 20f;
    private const float MAX_LAUNCH_ANGLE = 45f;
    private const float TRAJECTORY_TIME_STEP = 0.1f;
    private const int MAX_BOUNCES = 3;
    private static readonly Color PLAYER_1_COLOR = new Color(1f, 0.647f, 0f, 1f); // Orange (FFA500)
    private static readonly Color PLAYER_2_COLOR = Color.green;
    private IBounceStrategy bounceStrategy;

    // === Game State Tracking ===
    public bool HasCompletedHole = false;
    private Vector3 originalStartPosition;
    public int currentHole;
    public static int firstKickCount = 0;
    public static bool hasFirstKickRepositioned = false;

    // === Prediction Visuals ===
    [Header("Prediction Visuals")]
    [SerializeField] private int dotCount = 20;
    [SerializeField] private GameObject trajectoryPointPrefab;
    [SerializeField] private LineRenderer powerLineRenderer;
    [SerializeField] private LineRenderer lineRenderer;

    // === Movement and Game State ===
    [Header("Movement Settings")]
    [SerializeField] private float stopVelocity = 0.02f;
    private Rigidbody rb;
    public Rigidbody Rb { get { return rb; } }
    private Camera playerCamera;
    private MenuController menuController;
    private ScoreHandler scoreHandler;
    private AudioSource pop;
    private Transform respawnPoint;
    public float speedMultiplier = 1f;

    public int playerIndex;
    private bool isIdle = true;
    private bool isAiming;
    public bool isMyTurn;
    private float screenHalf;

    private List<GameObject> trajectoryPoints = new List<GameObject>();
    private List<Vector3> lastCalculatedTrajectory;
    private Vector3 currentPullPoint;
    private Vector3 pullStartPoint;
    private Vector3 lastValidPosition;
    public int shotCount;
    public int ShotCount { get { return shotCount; } }

    void Start()
    {
        // Component references
        pop = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody>();
        Mesh ballMesh = GetComponent<MeshFilter>().mesh;
        bounceStrategy = new DefaultBounceStrategy();

        // Collider and physics center
        GetComponent<SphereCollider>().center = ballMesh.bounds.center;
        rb.centerOfMass = ballMesh.bounds.center;

        // References for respawn, menu and score
        respawnPoint = GameObject.Find("RespawnPoint").transform;
        menuController = GameObject.Find("Canvas").GetComponent<MenuController>();
        scoreHandler = GameObject.Find("Canvas/CountPanel").GetComponent<ScoreHandler>();
        playerCamera = transform.parent.GetChild(1).GetComponent<Camera>();

        // Init logic
        shotCount = 0;
        lastValidPosition = transform.position;
        screenHalf = playerIndex == 0 ? 0.5f : 1.0f;
        isMyTurn = playerIndex == 0;
        menuController.AddCountText(playerIndex, shotCount);

        // Ball color setup
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        Material newMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        Color playerColor = playerIndex == 0 ? PLAYER_1_COLOR : PLAYER_2_COLOR;
        newMaterial.SetColor("_BaseColor", playerColor);
        newMaterial.SetColor("_EmissionColor", playerColor);
        meshRenderer.material = newMaterial;

        // LineRenderer setup
        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
        if (powerLineRenderer == null) powerLineRenderer = transform.Find("PowerLine")?.GetComponent<LineRenderer>();
        lineRenderer.enabled = false;
        powerLineRenderer.enabled = false;
        powerLineRenderer.numCapVertices = 10;
        powerLineRenderer.numCornerVertices = 10;
        powerLineRenderer.startWidth = transform.localScale.x;
        powerLineRenderer.endWidth = transform.localScale.x / 2f;
        powerLineRenderer.startColor = Color.white;
        powerLineRenderer.endColor = Color.white;

        // Trajectory dots
        for (int i = 0; i < dotCount; i++)
        {
            GameObject point = Instantiate(trajectoryPointPrefab, transform);
            point.transform.localScale = Vector3.one * 0.8f;
            MeshRenderer pointRenderer = point.GetComponent<MeshRenderer>();
            Material pointMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            // Transparency setup
            pointMaterial.SetFloat("_WorkflowMode", 0);
            pointMaterial.SetFloat("_Surface", 1);
            pointMaterial.SetFloat("_Blend", 0);
            pointMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            pointMaterial.SetShaderPassEnabled("ShadowCaster", false);
            pointMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            pointMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            pointMaterial.SetInt("_ZWrite", 0);
            pointMaterial.SetColor("_BaseColor", new Color(1, 1, 1, 0.36f));
            pointMaterial.renderQueue = 3000;
            pointRenderer.material = pointMaterial;
            Collider collider = point.GetComponent<Collider>();
            if (collider != null) DestroyImmediate(collider);
            point.SetActive(false);
            trajectoryPoints.Add(point);
        }
    }

    // Bounce physics for wall/plinko, wall boost
    /*
    private Vector3 CalculateBounceVelocity(Vector3 incomingVelocity, Vector3 normal)
    {
        Vector3 reflection = Vector3.Reflect(incomingVelocity, normal);
        float angleWithUp = Vector3.Angle(normal, Vector3.up);
        float multiplier = BOUNCE_MULTIPLIER;
        if (angleWithUp > 85f) multiplier *= 1.2f;
        float currentSpeed = incomingVelocity.magnitude;
        return reflection.normalized * (currentSpeed * multiplier);
    }*/
    private Vector3 CalculateBounceVelocity(Vector3 incomingVelocity, Vector3 normal)
    {
        return bounceStrategy.CalculateBounce(incomingVelocity, normal);
    }

    // Trajectory prediction and line draw
    private void DrawLine(Vector3 targetPoint)
    {
        Vector3 direction = (targetPoint - transform.position);
        float totalDistance = direction.magnitude;
        if (totalDistance < 0.001f) return;
        direction /= totalDistance;
        List<Vector3> positions = CalculateTrajectory(direction, totalDistance);

        for (int i = 0; i < trajectoryPoints.Count; i++)
        {
            if (i < positions.Count)
            {
                trajectoryPoints[i].transform.position = positions[i];
                trajectoryPoints[i].SetActive(true);
            }
            else
            {
                trajectoryPoints[i].SetActive(false);
            }
        }

        float pullDistance = Vector3.Distance(transform.position, currentPullPoint);
        Color baseColor = playerIndex == 0 ? PLAYER_1_COLOR : PLAYER_2_COLOR;
        Color lineColor = pullDistance > maxPullDistance ? Color.red : baseColor;
        lineColor.a = 0.8f;

        powerLineRenderer.startColor = lineColor;
        powerLineRenderer.endColor = lineColor;
        powerLineRenderer.positionCount = 2;
        powerLineRenderer.SetPositions(new Vector3[] { transform.position, currentPullPoint });
        powerLineRenderer.enabled = true;
    }

    private void OnMouseEnter()
    {
        if (!isAiming && isIdle && isMyTurn && IsMouseInPlayerScreen())
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }

    private void OnMouseExit()
    {
        if (!isAiming) Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    private void OnMouseDown()
    {
        float normalizedMouseX = Input.mousePosition.x / Screen.width;
        if ((playerIndex == 0 && normalizedMouseX > 0.5f) ||
            (playerIndex == 1 && normalizedMouseX < 0.5f)) return;

        if (isMyTurn && !HasCompletedHole && shotCount < 13 && AreBothBallsStopped())
        {
            isAiming = true;
            pullStartPoint = transform.position;
            currentPullPoint = transform.position;
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }

    private bool AreBothBallsStopped()
    {
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        if (players == null || players.Length == 0) return false;

        var validPlayers = players.Where(player => player != null && player.rb != null && !player.HasCompletedHole && player.shotCount < 13);
        if (!validPlayers.Any()) return true;
        return validPlayers.All(player => player.rb.linearVelocity.magnitude < stopVelocity);
    }

    private bool IsMouseInPlayerScreen()
    {
        if (!AreBothBallsStopped()) return false;
        float normalizedMouseX = Input.mousePosition.x / Screen.width;
        return playerIndex == 0 ? normalizedMouseX < 0.5f : normalizedMouseX >= 0.5f;
    }

    private void ProcessAim()
    {
        if (!isAiming || !isIdle) return;

        Plane groundPlane = new Plane(Vector3.up, transform.position);
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

        if (groundPlane.Raycast(ray, out float enter))
        {
            currentPullPoint = ray.GetPoint(enter);
            currentPullPoint.y = transform.position.y;
            Vector3 pullVector = currentPullPoint - transform.position;
            float pullDistance = pullVector.magnitude;

            if (pullDistance < 0.5f)
            {
                powerLineRenderer.enabled = false;
                foreach (var point in trajectoryPoints) point.SetActive(false);
            }
            else
            {
                float power = Mathf.Min(pullDistance, maxPullDistance);
                Vector3 shotDirection = -pullVector.normalized;
                Vector3 shotTarget = transform.position + (shotDirection * power * 1.8f);
                DrawLine(shotTarget);
            }

            if (Input.GetMouseButtonUp(0))
            {
                if (pullDistance < 0.5f)
                {
                    isAiming = false;
                    powerLineRenderer.enabled = false;
                    foreach (var point in trajectoryPoints) point.SetActive(false);
                }
                else
                {
                    Vector3 shotDirection = -pullVector.normalized;
                    Shoot(shotDirection * Mathf.Min(pullDistance, maxPullDistance));
                    var turnDisplay = FindFirstObjectByType<TurnDisplay>();
                    if (turnDisplay != null) turnDisplay.UpdateTurnDisplay(playerIndex, true);
                }
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
        }
    }

    private void Awake()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        Material uniqueMaterial = new Material(meshRenderer.sharedMaterial);
        meshRenderer.material = uniqueMaterial;
    }

    private List<Vector3> CalculateTrajectory(Vector3 direction, float power)
    {
        List<Vector3> bouncePositions = new List<Vector3>();
        Vector3 currentPos = transform.position;
        Vector3 currentVelocity = CalculateInitialVelocity(direction, power);
        bouncePositions.Add(currentPos);

        for (int i = 0; i < dotCount * 2 && bouncePositions.Count < dotCount; i++)
        {
            Vector3 nextPos = CalculateArcPoint(currentPos, currentVelocity, TRAJECTORY_TIME_STEP);
            RaycastHit hit;
            Vector3 direction2Next = (nextPos - currentPos).normalized;
            float distance2Next = Vector3.Distance(currentPos, nextPos);

            if (Physics.Raycast(currentPos, direction2Next, out hit, distance2Next))
            {
                bouncePositions.Add(hit.point);
                currentVelocity = CalculateBounceVelocity(currentVelocity, hit.normal);
                currentPos = hit.point + hit.normal * 0.02f;
            }
            else
            {
                bouncePositions.Add(nextPos);
                currentPos = nextPos;
                currentVelocity += Physics.gravity * TRAJECTORY_TIME_STEP;
            }
        }
        return bouncePositions;
    }

    public void SaveStartPosition()
    {
        originalStartPosition = transform.position;
    }

    // Handles shooting mechanic and post-shot logic
    private void Shoot(Vector3 shotVector)
    {
        isAiming = false;
        foreach (var point in trajectoryPoints)
            point.SetActive(false);
        powerLineRenderer.enabled = false;

        shotCount++;
        menuController.AddCountText(playerIndex, shotCount);

        // Update scores in MenuController (by playerIndex)
        if (menuController != null)
        {
            if (playerIndex == 0)
                menuController.orangeScores[menuController.currentHole] = shotCount;
            else
                menuController.greenScores[menuController.currentHole] = shotCount;
        }

        // Ball force application with speed multiplier
        if (lastCalculatedTrajectory != null && lastCalculatedTrajectory.Count > 1)
        {
            float power = shotVector.magnitude;
            Vector3 direction = (lastCalculatedTrajectory[1] - lastCalculatedTrajectory[0]).normalized;
            Vector3 launchVelocity = CalculateInitialVelocity(direction, power);
            rb.AddForce(launchVelocity * speedMultiplier, ForceMode.Impulse);
        }
        else
        {
            float power = shotVector.magnitude;
            Vector3 direction = shotVector.normalized;
            Vector3 launchVelocity = CalculateInitialVelocity(direction, power);
            rb.AddForce(launchVelocity * speedMultiplier, ForceMode.Impulse);
        }

        speedMultiplier = 1f; // Reset for next shot, so effect only applies once

        isIdle = false;
        SwapTurns();
    }

    // Turn swapping logic for multiplayer turn-based play
    void SwapTurns()
    {
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        int currentPlayerIdx = -1;
        for (int i = 0; i < players.Length; ++i)
        {
            if (players[i].isMyTurn)
            {
                currentPlayerIdx = i; break;
            }
        }

        foreach (var player in players) player.isMyTurn = false;
        int nextIdx = -1;
        for (int i = 1; i <= players.Length; ++i)
        {
            int testIdx = (currentPlayerIdx + i) % players.Length;
            var candidate = players[testIdx];
            if (!candidate.HasCompletedHole && candidate.ShotCount < 13)
            {
                nextIdx = testIdx; break;
            }
        }
        if (nextIdx != -1)
        {
            players[nextIdx].isMyTurn = true;
            var turnDisplay = FindFirstObjectByType<TurnDisplay>();
            if (turnDisplay != null && isIdle) turnDisplay.UpdateTurnDisplay(players[nextIdx].playerIndex, !AreBothBallsStopped());
        }
    }

    private void CheckLoseOrNextTurn()
    {
        VortexHole hole = FindFirstObjectByType<VortexHole>();
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        if (hole != null && !hole.HasSomeoneWon)
        {
            bool allPlayersExceededShots = players.All(p => p.ShotCount >= 13);
            bool noBallInHole = true;
            if (hole.gameObject.GetComponent<Collider>() is Collider holeCollider)
            {
                foreach (var player in players)
                {
                    if (Physics.ComputePenetration(
                        player.GetComponent<Collider>(),
                        player.transform.position,
                        player.transform.rotation,
                        holeCollider,
                        holeCollider.transform.position,
                        holeCollider.transform.rotation,
                        out Vector3 direction,
                        out float distance))
                    {
                        noBallInHole = false;
                        break;
                    }
                }
            }
            if (allPlayersExceededShots && noBallInHole)
            {
                if (menuController == null)
                    menuController = GameObject.Find("Canvas").GetComponent<MenuController>();
                menuController.FinishGame();
                foreach (var player in players)
                    player.gameObject.SetActive(false);
            }
            else
            {
                bool oneInHole = players.Any(p => p.HasCompletedHole);
                bool oneOutOfShots = players.Any(p => !p.HasCompletedHole && p.ShotCount >= 13);
                if (oneInHole && oneOutOfShots)
                {
                    if (menuController == null)
                        menuController = GameObject.Find("Canvas").GetComponent<MenuController>();
                    menuController.FinishGame();
                    foreach (var player in players)
                        player.gameObject.SetActive(false);
                }
                else
                {
                    SwapTurns();
                }
            }
        }
    }

    void FixedUpdate()
    {
        if (HasCompletedHole) return;

        RaycastHit hit;
        float rayDistance = 1.0f;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.2f;
        bool didHit = Physics.Raycast(rayOrigin, Vector3.down, out hit, rayDistance);

        float slopeAngle = didHit ? Vector3.Angle(hit.normal, Vector3.up) : 0f;
        bool isFlat = slopeAngle <= 10f;
        if (rb.linearVelocity.magnitude < stopVelocity && (!didHit || isFlat))
        {
            Stop();
            return;
        }

        if (didHit)
        {
            float surfaceY = hit.point.y;
            float ballRadius = GetComponent<SphereCollider>().radius * transform.localScale.y;
            float targetY = surfaceY + ballRadius;
            if (transform.position.y > targetY + 0.02f)
            {
                Vector3 pos = transform.position;
                pos.y = Mathf.Lerp(pos.y, targetY, 0.5f);
                transform.position = pos;
                Vector3 originalVel = rb.linearVelocity;
                Vector3 projectedVel = Vector3.ProjectOnPlane(originalVel, hit.normal);
                rb.linearVelocity = projectedVel;
                // Debug log available for slope projection
            }
        }
    }

    public static void UpdateBallToBallCollision()
    {
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        if (players.Length != 2) return;
        var ballA = players[0];
        var ballB = players[1];
        bool allowCollision = (!ballA.HasCompletedHole && !ballB.HasCompletedHole)
            || (ballA.HasCompletedHole && ballB.HasCompletedHole);

        Collider colliderA = ballA.GetComponent<Collider>();
        Collider colliderB = ballB.GetComponent<Collider>();
        if (colliderA != null && colliderB != null)
        {
            Physics.IgnoreCollision(colliderA, colliderB, !allowCollision);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Hole") && !HasCompletedHole)
        {
            HasCompletedHole = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            var renderer = GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                var mat = renderer.material;
                Color c = mat.color;
                c.a = 128f / 255f;
                mat.color = c;
                mat.SetFloat("_Surface", 1);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = 3000;
            }
            PlayerController.UpdateBallToBallCollision();
            CheckShowScoreboard();
        }
    }

    private void Stop()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        isIdle = true;
        if (IsGrounded()) lastValidPosition = transform.position;
        if (AreBothBallsStopped()) CheckLoseOrNextTurn();
    }

    void CheckShowScoreboard()
    {
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        bool allDone = players.All(p => p.HasCompletedHole || p.shotCount >= 13);
        if (allDone)
        {
            if (menuController == null)
                menuController = GameObject.Find("Canvas").GetComponent<MenuController>();
            menuController.FinishGame();
        }
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, 0.6f);
    }

    void Update()
    {
        if (transform.position.y < -10) Respawn();

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        Color currentColor = meshRenderer.material.GetColor("_BaseColor");
        Color correctColor = playerIndex == 0 ? PLAYER_1_COLOR : PLAYER_2_COLOR;
        if (currentColor != correctColor)
        {
            meshRenderer.material.SetColor("_BaseColor", correctColor);
            meshRenderer.material.SetColor("_EmissionColor", correctColor);
        }
        meshRenderer.material.SetFloat("_EmissionIntensity", isMyTurn ? 1f : 0f);

        if (isAiming) ProcessAim();
    }

    private void OnCollisionEnter(Collision collision)
    {
        var otherPlayer = collision.gameObject.GetComponent<PlayerController>();
        if (otherPlayer != null && !HasCompletedHole && !otherPlayer.HasCompletedHole)
        {
            Vector3 dir = (otherPlayer.transform.position - transform.position).normalized;
            float force = rb.linearVelocity.magnitude * 0.08f;
            otherPlayer.Rb.AddForce(dir * force, ForceMode.Impulse);
            rb.linearVelocity = rb.linearVelocity * 0.06f;
            firstKickCount++;
            if (firstKickCount >= 2 && !hasFirstKickRepositioned)
            {
                var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
                if (players.Length >= 2)
                {
                    players[0].transform.position = new Vector3(-2f, 0.5f, 0f);
                    players[1].transform.position = new Vector3(2f, 0.5f, 0f);
                    players[0].Rb.linearVelocity = Vector3.zero;
                    players[0].Rb.angularVelocity = Vector3.zero;
                    players[1].Rb.linearVelocity = Vector3.zero;
                    players[1].Rb.angularVelocity = Vector3.zero;
                }
                hasFirstKickRepositioned = true;
            }
            return;
        }

        if (rb.linearVelocity.magnitude > MIN_BOUNCE_SPEED)
        {
            foreach (var contact in collision.contacts)
            {
                Vector3 normal = contact.normal;
                Vector3 flatNormal = new Vector3(normal.x, 0f, normal.z);
                float flatNormalMag = flatNormal.magnitude;
                Vector3 flatNormalNorm = flatNormalMag > 0.001f ? flatNormal.normalized : Vector3.zero;
                Vector3 flatVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).normalized;
                float dot = flatNormalMag > 0.001f ? Vector3.Dot(flatVelocity, flatNormalNorm) : 0f;

                float wallBounceMultiplier = 1.4f;
                if (flatNormalMag > 0.4f && dot > 0f && Mathf.Abs(normal.y) < 0.5f)
                {
                    Vector3 bounceVelocity = Vector3.Reflect(
                        new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z), flatNormalNorm) * wallBounceMultiplier;
                    rb.linearVelocity = new Vector3(bounceVelocity.x, 0f, bounceVelocity.z);
                    rb.position += flatNormalNorm * 0.02f;
                    return;
                }
            }
        }
    }

    private Vector3 CalculateInitialVelocity(Vector3 direction, float power)
    {
        float ballMass = rb.mass;
        Vector3 flatDirection = new Vector3(direction.x, 0f, direction.z).normalized;
        float powerMultiplier = SHOT_POWER / ballMass / 2f;
        return flatDirection * power * powerMultiplier;
    }

    private Vector3 CalculateArcPoint(Vector3 start, Vector3 velocity, float time)
    {
        return start + velocity * time + 0.5f * Physics.gravity * time * time;
    }

    void Respawn()
    {
        pop.Play();
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.Sleep();
        transform.position = lastValidPosition;
    }

    void EndGame()
    {
        menuController.FinishGame();
        foreach (var player in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            player.gameObject.SetActive(false);
        }
    }
}