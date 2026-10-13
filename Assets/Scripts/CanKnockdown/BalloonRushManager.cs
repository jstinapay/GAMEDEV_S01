using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Balloon Pop Rush: 60-second timed mode on the shared slingshot. Owns the
/// timer, ammo, score, streaks, and spawner; ThrowController consults
/// AcceptsShots()/OnShotFired() so one throwing system serves both modes.
/// Can Knockdown is untouched: this manager never resolves cans.
/// </summary>
public class BalloonRushManager : MonoBehaviour
{
    public static BalloonRushManager Instance { get; private set; }

    public enum RushState { Idle, Countdown, Playing, RoundComplete }

    [Header("Refs (auto-found if empty)")]
    [SerializeField] BalloonSpawner spawner;
    [SerializeField] BalloonRushUI ui;
    [SerializeField] ThrowController throwController;
    [SerializeField] CanKnockdownManager knockdown;
    [SerializeField] CanKnockdownUI knockdownUI;
    [SerializeField] CanStackManager stack;

    [Header("Round")]
    [SerializeField] float roundDuration = 60f;
    [SerializeField] int maxAmmo = 12;
    [SerializeField] float countdownStep = 0.6f;

    [Header("Streaks")]
    [SerializeField] int streakBonusEvery = 3;
    [SerializeField] int streakBonusPoints = 50;

    public RushState State { get; private set; } = RushState.Idle;
    public int Score { get; private set; }
    public int AmmoLeft { get; private set; }
    public int Popped { get; private set; }
    public int GoldenPopped { get; private set; }
    public int ShotsFired { get; private set; }
    public int BestStreak { get; private set; }

    float timeLeft;
    int currentStreak;
    int lastPopSeq = -2;
    int nextSeq;
    readonly HashSet<int> hitSeqs = new HashSet<int>();

    public float TimeLeft => Mathf.Max(0f, timeLeft);

    /// <summary>0 at GO, 1 at time-up. Drives spawner difficulty.</summary>
    public float RoundProgress()
    {
        if (roundDuration <= 0f) return 1f;
        return Mathf.Clamp01(1f - timeLeft / roundDuration);
    }

    /// <summary>ThrowController asks this before loading or firing.</summary>
    public bool AcceptsShots()
    {
        return State == RushState.Playing && timeLeft > 0f && AmmoLeft > 0;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (spawner == null) spawner = Object.FindAnyObjectByType<BalloonSpawner>();
        if (ui == null) ui = Object.FindAnyObjectByType<BalloonRushUI>();
        if (throwController == null) throwController = Object.FindAnyObjectByType<ThrowController>();
        if (knockdown == null) knockdown = Object.FindAnyObjectByType<CanKnockdownManager>();
        if (knockdownUI == null) knockdownUI = Object.FindAnyObjectByType<CanKnockdownUI>();
        if (stack == null) stack = Object.FindAnyObjectByType<CanStackManager>();
        EnsureSpawner();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void EnsureSpawner()
    {
        if (spawner != null) return;
        var go = new GameObject("BalloonSpawner");
        spawner = go.AddComponent<BalloonSpawner>();
    }

    void Update()
    {
        // R replays from the results screen only (never mid-round).
        if (State == RushState.RoundComplete)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.rKey.wasPressedThisFrame) StartRound();
#else
            if (Input.GetKeyDown(KeyCode.R)) StartRound();
#endif
            return;
        }
        if (State != RushState.Playing) return;
        timeLeft -= Time.deltaTime;
        if (ui != null) ui.UpdateTimer(TimeLeft, roundDuration);
        if (timeLeft <= 0f)
        {
            FinishRound();
            return;
        }
        // Out of ammo: let live balls finish (capped by their lifetime), then end.
        if (AmmoLeft <= 0 && Object.FindAnyObjectByType<BallController>() == null)
            FinishRound();
    }

    /// <summary>Starts a fresh rush round from menu or results (R replays).</summary>
    public void StartRound()
    {
        StopAllCoroutines();
        Score = 0;
        Popped = 0;
        GoldenPopped = 0;
        ShotsFired = 0;
        BestStreak = 0;
        currentStreak = 0;
        lastPopSeq = -2;
        nextSeq = 0;
        hitSeqs.Clear();
        timeLeft = roundDuration;
        AmmoLeft = maxAmmo;
        if (knockdownUI != null) knockdownUI.HideAll();
        if (throwController != null)
        {
            throwController.ClearBalls();
            throwController.ResetCooldown();
        }
        if (spawner != null) spawner.ClearAll();
        if (stack != null) stack.SetVisible(false);
        if (ui != null)
        {
            ui.ShowHUD();
            ui.Refresh(Score, AmmoLeft, maxAmmo, Popped, currentStreak, TimeLeft, roundDuration);
        }
        State = RushState.Countdown;
        StartCoroutine(CountdownRoutine());
    }

    IEnumerator CountdownRoutine()
    {
        string[] steps = { "3", "2", "1", "GO!" };
        foreach (var s in steps)
        {
            if (State != RushState.Countdown) yield break; // superseded / stopped
            if (ui != null) ui.ShowCountdown(s);
            yield return new WaitForSeconds(countdownStep);
        }
        if (State != RushState.Countdown) yield break;
        if (ui != null) ui.HideCountdown();
        State = RushState.Playing;
        if (spawner != null) spawner.StartSpawning();
    }

    /// <summary>Called by ThrowController exactly once per release. Returns the shot id.</summary>
    public int OnShotFired()
    {
        if (State != RushState.Playing) return -1;
        AmmoLeft = Mathf.Max(0, AmmoLeft - 1);
        ShotsFired++;
        int seq = nextSeq++;
        if (ui != null) ui.Refresh(Score, AmmoLeft, maxAmmo, Popped, currentStreak, TimeLeft, roundDuration);
        return seq;
    }

    /// <summary>Called once per popped balloon (controller guards double-pops).</summary>
    public void OnBalloonPopped(BalloonType type, int points, int bonusAmmo, int seq, Vector3 pos)
    {
        if (State != RushState.Playing) return;
        Popped++;
        if (type == BalloonType.Golden) GoldenPopped++;
        Score += points;
        // Streak: a new hitting ball extends the run only if the previous shot hit too.
        if (seq >= 0 && seq != lastPopSeq)
        {
            currentStreak = (seq == lastPopSeq + 1) ? currentStreak + 1 : 1;
            lastPopSeq = seq;
            hitSeqs.Add(seq);
            if (currentStreak > BestStreak) BestStreak = currentStreak;
            if (streakBonusEvery > 0 && currentStreak > 0 && currentStreak % streakBonusEvery == 0)
            {
                Score += streakBonusPoints;
                if (ui != null) ui.Notify($"STREAK x{currentStreak}!  +{streakBonusPoints}");
            }
        }
        if (bonusAmmo > 0)
        {
            AmmoLeft = Mathf.Min(99, AmmoLeft + bonusAmmo);
            if (ui != null) ui.Notify($"+{bonusAmmo} BALL!");
        }
        if (CanKnockdownAudio.Instance != null)
        {
            if (type == BalloonType.Golden) CanKnockdownAudio.Instance.PlayGoldPop();
            else CanKnockdownAudio.Instance.PlayPop();
        }
        CanKnockdownFX.BurstAt(pos, 1);
        if (ui != null)
        {
            ui.Refresh(Score, AmmoLeft, maxAmmo, Popped, currentStreak, TimeLeft, roundDuration);
            ui.ScorePopup(points);
        }
    }

    /// <summary>A ball despawned — a seq that never popped breaks the streak.</summary>
    public void NoteBallGone(int seq)
    {
        if (State != RushState.Playing || seq < 0) return;
        if (!hitSeqs.Contains(seq))
            currentStreak = 0;
        if (ui != null) ui.Refresh(Score, AmmoLeft, maxAmmo, Popped, currentStreak, TimeLeft, roundDuration);
    }

    void FinishRound()
    {
        if (State != RushState.Playing) return;
        State = RushState.RoundComplete;
        timeLeft = 0f;
        if (spawner != null) spawner.ClearAll();
        float accuracy = ShotsFired > 0 ? (float)hitSeqs.Count / ShotsFired : 0f;
        if (GoldenPopped > 0)
            CanKnockdownFX.WinBurst(new Vector3(0f, 2f, 4f));
        if (ui != null) ui.ShowResults(Score, Popped, GoldenPopped, ShotsFired, accuracy, BestStreak);
    }

    /// <summary>Back to mode select / idle. Safe to call when never started.</summary>
    public void StopAndHide()
    {
        StopAllCoroutines();
        State = RushState.Idle;
        if (spawner != null) spawner.ClearAll();
        if (stack != null) { stack.SetVisible(true); stack.ResetStack(); }
        if (ui != null) ui.HideAll();
    }

    /// <summary>Results-screen button: replay immediately.</summary>
    public void OnPlayAgain() => StartRound();

    /// <summary>Results-screen button: back to the shared main menu.</summary>
    public void OnModeSelect()
    {
        StopAndHide();
        if (throwController != null) throwController.ClearBalls();
        if (knockdown != null) knockdown.ShowMainMenu();
        else if (knockdownUI != null) knockdownUI.HideAll();
    }
}
