using UnityEngine;

/// <summary>
/// Game state for the laser corridor: Playing / GameOver / Completed.
/// Handles reset without reloading the scene: player position, health,
/// lasers, bonuses and UI are all reinitialized for another attempt.
/// </summary>
public class LaserCorridorManager : MonoBehaviour
{
    public enum GameState { Playing, GameOver, Completed }

    [Header("Refs (auto-found if empty)")]
    public CustomPlayerController player;
    public PlayerHealth playerHealth;
    public LaserSpawner spawner;
    public LaserCorridorUI ui;

    [Header("Run")]
    public Vector3 startPosition = new Vector3(0f, 1.1f, 0f);

    public GameState State { get; private set; } = GameState.Playing;

    void Awake()
    {
        LaserCorridorBuilder.UpgradeLevelInPlace();
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.GetComponent<CustomPlayerController>();
        }
        if (playerHealth == null && player != null)
            playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth == null)
            playerHealth = Object.FindAnyObjectByType<PlayerHealth>();
        if (spawner == null)
            spawner = Object.FindAnyObjectByType<LaserSpawner>();
        if (ui == null)
            ui = Object.FindAnyObjectByType<LaserCorridorUI>();
        if (player != null)
        {
            player.SetSpawn(startPosition);
            startPosition = player.spawnPoint;
        }
        EnforceFirstPerson();
    }

    /// <summary>
    /// Laser corridor is first-person only. Forces the shared CameraFollow
    /// into head-locked FPS mode at runtime so already-built scenes pick it
    /// up without requiring a rebuild. Traversal scene is unaffected (it has
    /// no LaserCorridorManager to call this).
    /// </summary>
    void EnforceFirstPerson()
    {
        var follow = Object.FindAnyObjectByType<CameraFollow>();
        if (follow == null) return;
        follow.firstPersonOnly = true;
        follow.freeLook = true;
        follow.hidePlayerInFirstPerson = true;
        follow.lockCursorInFirstPerson = true;
        follow.minDistance = 0.5f;
        follow.maxDistance = 0.5f;
        if (ui != null && ui.follow == null)
            ui.follow = follow;
    }

    void Start()
    {
        BeginRun();
    }

    void Update()
    {
        // Keyboard shortcut for demos: R retries after game over / win.
        if ((State == GameState.GameOver || State == GameState.Completed))
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame)
                ResetRun();
#else
            if (Input.GetKeyDown(KeyCode.R))
                ResetRun();
#endif
        }
    }

    void BeginRun()
    {
        State = GameState.Playing;
        if (spawner != null)
        {
            spawner.ResetSpawner();
            spawner.StartSpawning();
        }
        if (ui != null)
        {
            ui.HideEndPanels();
            ui.ClearBonus();
        }
        LaserGoal.ResetAllGoals();
    }

    public void OnPlayerDied()
    {
        if (State != GameState.Playing) return;
        State = GameState.GameOver;
        Debug.Log("Game Over - lasers stopped.");
        if (spawner != null)
        {
            spawner.StopSpawning();
            spawner.ClearAll();
        }
        if (player != null) player.Lock();
        if (ui != null) ui.ShowGameOver();
    }

    public void OnGoalReached()
    {
        if (State != GameState.Playing) return;
        State = GameState.Completed;
        Debug.Log("Corridor cleared - challenge disabled.");
        if (spawner != null)
        {
            spawner.StopSpawning();
            spawner.ClearAll();
        }
        if (player != null) player.Lock();
        if (ui != null) ui.ShowComplete();
    }

    /// <summary>Retry without reloading the application.</summary>
    public void ResetRun()
    {
        State = GameState.Playing;
        if (playerHealth != null)
        {
            playerHealth.ClearTemporaryEffects();
            playerHealth.ResetHealth();
        }
        if (player != null)
        {
            player.ResetEffects();
            player.SetSpawn(startPosition);
            player.Respawn();
            player.Unlock();
        }
        // Clear speed-bonus coroutines sitting on panels.
        foreach (var panel in Object.FindObjectsByType<BonusPanel>())
        {
            panel.CancelEffect(player);
            panel.ResetPanel();
        }
        LaserGoal.ResetAllGoals();
        if (spawner != null)
        {
            spawner.ResetSpawner();
            spawner.StartSpawning();
        }
        if (ui != null)
        {
            ui.HideEndPanels();
            ui.ClearBonus();
        }
        Debug.Log("Laser corridor reset - try again!");
    }
}
