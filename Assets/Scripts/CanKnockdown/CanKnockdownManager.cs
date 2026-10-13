using System.Collections;
using UnityEngine;

/// <summary>
/// Game states, score, remaining throws, and round progression.
/// Flow: MainMenu -> Playing -> Resolving (after each throw) -> RoundComplete.
/// Never ends mid-flight: scoring waits settleTime + until cans settle.
/// </summary>
public class CanKnockdownManager : MonoBehaviour
{
    public enum GameState { MainMenu, Playing, Resolving, RoundComplete }

    [Header("Refs (auto-found if empty)")]
    [SerializeField] CanStackManager stackManager;
    [SerializeField] ThrowController throwController;
    [SerializeField] CanKnockdownUI ui;
    [Tooltip("Balloon Rush round. Stopped whenever knockdown owns the screen.")]
    [SerializeField] BalloonRushManager rush;

    [Header("Round (3 balls / 6 cans)")]
    [SerializeField] int totalThrows = 3;

    [Header("Scoring")]
    [SerializeField] int pointsPerCan = 100;
    [SerializeField] int fullClearBonus = 500;

    [Header("Settling")]
    [Tooltip("Base wait after each throw before scoring, so cans can tumble.")]
    [SerializeField] float settleTime = 2.5f;
    [Tooltip("Extra max wait for velocities to die down before scoring.")]
    [SerializeField] float maxExtraWait = 2f;
    [SerializeField] float sleepThreshold = 0.2f;

    public GameState State { get; private set; } = GameState.MainMenu;
    public int Score { get; private set; }
    public int ThrowsRemaining { get; private set; }
    public int ThrowsUsed { get; private set; }
    public int SuccessfulThrows { get; private set; }

    public int TotalCans => stackManager != null ? stackManager.TotalCans : 0;
    public int TotalKnocked => stackManager != null ? stackManager.CountKnockedDown() : 0;

    void Awake()
    {
        // Self-heal: build the booth if this manager was dropped into an empty scene.
        CanKnockdownBuilder.EnsureLevelInPlace();
        if (stackManager == null) stackManager = Object.FindAnyObjectByType<CanStackManager>();
        if (throwController == null) throwController = Object.FindAnyObjectByType<ThrowController>();
        if (ui == null) ui = Object.FindAnyObjectByType<CanKnockdownUI>();
        if (rush == null) rush = Object.FindAnyObjectByType<BalloonRushManager>();
        if (CanKnockdownAudio.Instance == null)
        {
            var audioGo = new GameObject("KnockdownAudio");
            audioGo.AddComponent<CanKnockdownAudio>();
        }
    }

    void Start()
    {
        ThrowsRemaining = totalThrows;
        ShowMainMenu();
    }

    void Update()
    {
        // R restarts from the end screen (same shortcut as LaserCorridor).
        if (State == GameState.RoundComplete)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.rKey.wasPressedThisFrame) StartGame();
#else
            if (Input.GetKeyDown(KeyCode.R)) StartGame();
#endif
        }
    }

    public void ShowMainMenu()
    {
        State = GameState.MainMenu;
        if (ui != null) ui.ShowMainMenu(Score, ThrowsRemaining);
    }

    /// <summary>Starts a fresh round: score/throws reset, cans restored, balls cleared.</summary>
    public void StartGame()
    {
        if (rush != null) rush.StopAndHide();
        Score = 0;
        ThrowsUsed = 0;
        SuccessfulThrows = 0;
        ThrowsRemaining = totalThrows;
        if (stackManager != null) stackManager.ResetStack();
        if (throwController != null)
        {
            throwController.ClearBalls();
            throwController.ResetCooldown();
        }
        State = GameState.Playing;
        if (ui != null)
        {
            ui.ShowHUD();
            ui.UpdateScore(Score);
            ui.UpdateThrows(ThrowsRemaining, totalThrows);
        }
    }

    /// <summary>Called by ThrowController after a successful launch.</summary>
    public void OnThrowUsed()
    {
        if (State != GameState.Playing) return;
        ThrowsRemaining = Mathf.Max(0, ThrowsRemaining - 1);
        ThrowsUsed++;
        if (ui != null) ui.UpdateThrows(ThrowsRemaining, totalThrows);
        StartCoroutine(ResolveThrow());
    }

    IEnumerator ResolveThrow()
    {
        State = GameState.Resolving;

        yield return new WaitForSeconds(settleTime);

        // Wait for motion to die down (capped so a rolling can can't stall the game).
        float extra = 0f;
        while (extra < maxExtraWait)
        {
            if (stackManager == null || stackManager.AllSettled(sleepThreshold)) break;
            extra += 0.15f;
            yield return new WaitForSeconds(0.15f);
        }

        // Score each newly knocked can exactly once.
        var fresh = stackManager != null
            ? stackManager.CollectNewlyKnockedDown()
            : new System.Collections.Generic.List<CanController>();
        int gained = fresh.Count * pointsPerCan;
        bool clearedAllInOneThrow = fresh.Count == TotalCans && TotalCans > 0;
        if (clearedAllInOneThrow)
            gained += fullClearBonus;

        Score += gained;
        if (fresh.Count > 0)
        {
            SuccessfulThrows++;
            if (CanKnockdownAudio.Instance != null) CanKnockdownAudio.Instance.PlayKnockdown();
            // Juice only: confetti at each fresh can (centroid when many).
            if (fresh.Count == 1 && fresh[0] != null)
                CanKnockdownFX.BurstAt(fresh[0].transform.position, 1);
            else
            {
                Vector3 center = Vector3.zero;
                int n = 0;
                foreach (var c in fresh)
                    if (c != null) { center += c.transform.position; n++; }
                if (n > 0) CanKnockdownFX.BurstAt(center / n, fresh.Count);
            }
            if (clearedAllInOneThrow)
                CanKnockdownFX.WinBurst(new Vector3(0f, 1.6f, 4f));
        }

        if (ui != null)
        {
            ui.UpdateScore(Score);
            if (fresh.Count > 0)
            {
                string msg = $"+{gained}  ({fresh.Count} can{(fresh.Count > 1 ? "s" : "")})";
                if (clearedAllInOneThrow) msg += $"  FULL CLEAR +{fullClearBonus}!";
                ui.Notify(msg);
                ui.ScorePopup(gained);
            }
            else if (ThrowsRemaining > 0)
            {
                ui.Notify("Miss — aim a little higher!");
            }
        }

        // Round ends on no balls left — or early when every can is already down.
        if (ThrowsRemaining <= 0 || (TotalCans > 0 && TotalKnocked >= TotalCans))
            FinishRound();
        else
            State = GameState.Playing;
    }

    void FinishRound()
    {
        State = GameState.RoundComplete;
        int knocked = TotalKnocked;
        int total = TotalCans;
        bool perfect = total > 0 && knocked >= total;
        if (perfect && CanKnockdownAudio.Instance != null)
            CanKnockdownAudio.Instance.PlayWin();
        if (perfect)
            CanKnockdownFX.WinBurst(new Vector3(0f, 1.6f, 4f));

        float accuracy = ThrowsUsed > 0 ? (float)SuccessfulThrows / ThrowsUsed : 0f;
        if (ui != null) ui.ShowRoundOver(Score, knocked, total, accuracy, perfect);
    }

    public void QuitToMenu()
    {
        if (rush != null) rush.StopAndHide();
        if (throwController != null) throwController.ClearBalls();
        if (stackManager != null) stackManager.ResetStack();
        ThrowsRemaining = totalThrows;
        ShowMainMenu();
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
