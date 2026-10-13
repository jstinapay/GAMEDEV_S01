using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Carnival HUD + menus using TextMeshProUGUI. Panels:
/// MainMenu (title/mode buttons/quit), HUD (score/throws/crosshair/hint),
/// RoundOver (final score/summary/play-again/menu).
/// </summary>
public class CanKnockdownUI : MonoBehaviour
{
    [Header("Refs (auto-wired by builder)")]
    [SerializeField] CanKnockdownManager manager;
    [SerializeField] BalloonRushManager rushManager;

    [Header("HUD Texts")]
    [SerializeField] TMP_Text scoreText;
    [SerializeField] TMP_Text throwsText;
    [SerializeField] TMP_Text notificationText;
    [SerializeField] TMP_Text hintText;
    [SerializeField] TMP_Text crosshairText;
    [SerializeField] TMP_Text popupText;

    [Header("Main Menu")]
    [SerializeField] GameObject mainMenuPanel;
    [SerializeField] TMP_Text titleText;
    [SerializeField] Button playButton;
    [SerializeField] Button rushButton;
    [SerializeField] Button quitButton;

    [Header("Round Over")]
    [SerializeField] GameObject roundOverPanel;
    [SerializeField] TMP_Text finalScoreText;
    [SerializeField] TMP_Text summaryText;
    [SerializeField] Button playAgainButton;
    [SerializeField] Button menuButton;

    [Header("HUD Root")]
    [SerializeField] GameObject hudRoot;

    Coroutine notifyRoutine;
    Coroutine popupRoutine;

    void Awake()
    {
        if (manager == null) manager = Object.FindAnyObjectByType<CanKnockdownManager>();
        if (rushManager == null) rushManager = Object.FindAnyObjectByType<BalloonRushManager>();
    }

    void Start()
    {
        Wire(playButton, () => { if (manager != null) manager.StartGame(); });
        Wire(rushButton, () => { if (rushManager != null) rushManager.StartRound(); });
        Wire(playAgainButton, () => { if (manager != null) manager.StartGame(); });
        Wire(menuButton, () => { if (manager != null) manager.QuitToMenu(); });
        Wire(quitButton, () => { if (manager != null) manager.QuitGame(); });
        if (crosshairText != null) crosshairText.raycastTarget = false;
        if (popupText != null) popupText.gameObject.SetActive(false);
        Cursor.visible = true;
    }

    static void Wire(Button b, UnityEngine.Events.UnityAction action)
    {
        if (b == null || action == null) return;
        b.onClick.RemoveListener(action);
        b.onClick.AddListener(action);
    }

    void Update()
    {
        // Crosshair follows the mouse so aim reads 1:1 with the throw ray.
        if (crosshairText != null && crosshairText.gameObject.activeInHierarchy)
        {
            Vector2 mp;
#if ENABLE_INPUT_SYSTEM
            var mouse = UnityEngine.InputSystem.Mouse.current;
            mp = mouse != null ? mouse.position.ReadValue() : (Vector2)Input.mousePosition;
#else
            mp = Input.mousePosition;
#endif
            crosshairText.rectTransform.position = mp;
        }
    }

    public void ShowMainMenu(int score, int throws)
    {
        SetActive(mainMenuPanel, true);
        SetActive(roundOverPanel, false);
        SetActive(hudRoot, false);
        SetCrosshairVisible(false);
        Cursor.visible = true;
    }

    /// <summary>Hides every knockdown panel (used when Balloon Rush takes the screen).</summary>
    public void HideAll()
    {
        SetActive(mainMenuPanel, false);
        SetActive(roundOverPanel, false);
        SetActive(hudRoot, false);
        SetCrosshairVisible(false);
        ClearNotification();
    }

    public void ShowHUD()
    {
        SetActive(mainMenuPanel, false);
        SetActive(roundOverPanel, false);
        SetActive(hudRoot, true);
        SetCrosshairVisible(false); // slingshot aims via the pouch, not a cursor
        if (hintText != null)
            hintText.text = "Drag the ball back to aim  •  Release to fire";
        ClearNotification();
        Cursor.visible = true;
    }

    public void ShowRoundOver(int score, int knocked, int total, float accuracy, bool perfect)
    {
        SetActive(roundOverPanel, true);
        SetActive(mainMenuPanel, false);
        SetCrosshairVisible(false);
        if (finalScoreText != null) finalScoreText.text = $"FINAL SCORE: {score}";
        if (summaryText != null)
        {
            string line = $"Cans knocked down: {knocked}/{total}\nThrows that hit: {Mathf.RoundToInt(accuracy * 100)}%";
            if (perfect) line += "\nPERFECT ROUND! All cans down!";
            else if (knocked == 0) line += "\nNo cans down — try aiming higher!";
            summaryText.text = line;
        }
        Cursor.visible = true;
    }

    public void UpdateScore(int score)
    {
        if (scoreText != null) scoreText.text = $"SCORE: {score}";
    }

    public void UpdateThrows(int remaining, int total)
    {
        if (throwsText != null) throwsText.text = $"BALLS: {remaining}/{total}";
    }

    public void Notify(string message, float duration = 2f)
    {
        if (notificationText == null) return;
        if (notifyRoutine != null) StopCoroutine(notifyRoutine);
        notifyRoutine = StartCoroutine(NotifyRoutine(message, duration));
    }

    IEnumerator NotifyRoutine(string message, float duration)
    {
        notificationText.text = message;
        notificationText.gameObject.SetActive(true);
        SetChip(notificationText, true);
        yield return new WaitForSeconds(duration);
        notificationText.gameObject.SetActive(false);
        SetChip(notificationText, false);
    }

    void ClearNotification()
    {
        if (notifyRoutine != null) StopCoroutine(notifyRoutine);
        if (notificationText != null) notificationText.gameObject.SetActive(false);
        SetChip(notificationText, false);
    }

    /// <summary>Big center pop on scoring (+100 etc). Scale-punch, no tween package.</summary>
    public void ScorePopup(int points)
    {
        if (popupText == null) return;
        if (popupRoutine != null) StopCoroutine(popupRoutine);
        popupRoutine = StartCoroutine(PopupRoutine($"+{points}"));
    }

    IEnumerator PopupRoutine(string text)
    {
        popupText.text = text;
        popupText.gameObject.SetActive(true);
        SetChip(popupText, true);
        Vector3 baseScale = Vector3.one;
        float t = 0f;
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            float k = 1f + Mathf.Sin(Mathf.Min(1f, t / 0.6f) * Mathf.PI) * 0.35f;
            popupText.rectTransform.localScale = baseScale * k;
            yield return null;
        }
        popupText.rectTransform.localScale = baseScale;
        popupText.gameObject.SetActive(false);
        SetChip(popupText, false);
    }

    /// <summary>Chips are siblings named TextName_Chip; mirror the text's visibility.</summary>
    void SetChip(TMP_Text text, bool visible)
    {
        if (text == null || text.transform.parent == null) return;
        var chip = text.transform.parent.Find(text.name + "_Chip");
        if (chip != null) chip.gameObject.SetActive(visible);
    }

    public void SetCrosshairVisible(bool visible)
    {
        if (crosshairText != null)
            crosshairText.gameObject.SetActive(visible);
    }

    static void SetActive(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }
}
