using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Balloon Pop Rush presentation: HUD (score/ammo/timer/popped/streak),
/// countdown overlay, and results screen. Builds its own canvas on first Awake
/// so the mode needs no manual scene setup; hidden until a round starts.
/// </summary>
public class BalloonRushUI : MonoBehaviour
{
    [Header("Refs (auto-built when empty)")]
    [SerializeField] BalloonRushManager manager;

    TMP_Text scoreText;
    TMP_Text ammoText;
    TMP_Text timerText;
    TMP_Text poppedText;
    TMP_Text streakText;
    TMP_Text hintText;
    TMP_Text popupText;
    TMP_Text notifyText;
    TMP_Text countdownText;
    GameObject hudRoot;
    GameObject resultsPanel;
    TMP_Text resultsTitle;
    TMP_Text resultsStats;
    Button playAgainButton;
    Button modeButton;

    Coroutine notifyRoutine;
    Coroutine popupRoutine;

    void Awake()
    {
        if (manager == null) manager = Object.FindAnyObjectByType<BalloonRushManager>();
        if (scoreText == null) BuildCanvas();
        HideAll();
    }

    void Start()
    {
        if (playAgainButton != null)
        {
            playAgainButton.onClick.RemoveAllListeners();
            playAgainButton.onClick.AddListener(() => { if (manager != null) manager.OnPlayAgain(); });
        }
        if (modeButton != null)
        {
            modeButton.onClick.RemoveAllListeners();
            modeButton.onClick.AddListener(() => { if (manager != null) manager.OnModeSelect(); });
        }
    }

    public void ShowHUD()
    {
        if (hudRoot != null) hudRoot.SetActive(true);
        if (resultsPanel != null) resultsPanel.SetActive(false);
        if (hintText != null)
        {
            hintText.gameObject.SetActive(true);
            hintText.text = "Grab the ball, pull back, pop balloons!";
        }
        ClearTransient();
        Cursor.visible = true;
    }

    public void Refresh(int score, int ammo, int ammoMax, int popped, int streak, float timeLeft, float duration)
    {
        if (scoreText != null) scoreText.text = $"SCORE: {score}";
        if (ammoText != null) ammoText.text = $"BALLS: {ammo}/{ammoMax}";
        if (poppedText != null) poppedText.text = $"POPPED: {popped}";
        if (streakText != null)
        {
            streakText.gameObject.SetActive(streak >= 2);
            streakText.text = $"STREAK x{streak}";
        }
        UpdateTimer(timeLeft, duration);
    }

    public void UpdateTimer(float timeLeft, float duration)
    {
        if (timerText == null) return;
        timerText.text = $"TIME: {Mathf.CeilToInt(Mathf.Max(0f, timeLeft))}";
        timerText.color = timeLeft <= 10f ? new Color(1f, 0.35f, 0.3f) : Color.white;
    }

    public void ShowCountdown(string text)
    {
        if (countdownText == null) return;
        countdownText.gameObject.SetActive(true);
        countdownText.text = text;
    }

    public void HideCountdown()
    {
        if (countdownText != null) countdownText.gameObject.SetActive(false);
    }

    public void ShowResults(int score, int popped, int golden, int shots, float accuracy, int bestStreak)
    {
        if (hudRoot != null) hudRoot.SetActive(false);
        HideCountdown();
        if (resultsPanel != null) resultsPanel.SetActive(true);
        if (resultsTitle != null) resultsTitle.text = "TIME UP!";
        if (resultsStats != null)
        {
            resultsStats.text = $"FINAL SCORE: {score}\n"
                + $"Balloons popped: {popped}  (golden: {golden})\n"
                + $"Shots fired: {shots}  •  Accuracy: {Mathf.RoundToInt(accuracy * 100f)}%\n"
                + $"Best streak: x{Mathf.Max(1, bestStreak)}";
        }
        Cursor.visible = true;
    }

    public void HideAll()
    {
        if (hudRoot != null) hudRoot.SetActive(false);
        if (resultsPanel != null) resultsPanel.SetActive(false);
        HideCountdown();
        ClearTransient();
    }

    public void Notify(string message, float duration = 2f)
    {
        if (notifyText == null) return;
        if (notifyRoutine != null) StopCoroutine(notifyRoutine);
        notifyRoutine = StartCoroutine(NotifyRoutine(message, duration));
    }

    IEnumerator NotifyRoutine(string message, float duration)
    {
        notifyText.text = message;
        notifyText.gameObject.SetActive(true);
        SetChip(notifyText, true);
        yield return new WaitForSeconds(duration);
        notifyText.gameObject.SetActive(false);
        SetChip(notifyText, false);
    }

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

    void ClearTransient()
    {
        if (notifyRoutine != null) StopCoroutine(notifyRoutine);
        if (popupRoutine != null) StopCoroutine(popupRoutine);
        if (notifyText != null) notifyText.gameObject.SetActive(false);
        if (popupText != null) popupText.gameObject.SetActive(false);
        SetChip(notifyText, false);
        SetChip(popupText, false);
    }

    // ---- self-built canvas (same TMP look as the knockdown HUD) ----

    void BuildCanvas()
    {
        var hud = new GameObject("BalloonRushHUD");
        var canvas = hud.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 15;
        var scaler = hud.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        hud.AddComponent<GraphicRaycaster>();

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        hudRoot = new GameObject("HudRoot");
        hudRoot.transform.SetParent(hud.transform, false);
        var hudRt = hudRoot.AddComponent<RectTransform>();
        hudRt.anchorMin = Vector2.zero; hudRt.anchorMax = Vector2.one;
        hudRt.offsetMin = Vector2.zero; hudRt.offsetMax = Vector2.zero;

        scoreText = MakeTMP(hudRoot.transform, "RushScore", font, 42, TextAlignmentOptions.TopLeft,
            new Vector2(0f, 1f), new Vector2(220f, -50f), "SCORE: 0");
        ammoText = MakeTMP(hudRoot.transform, "RushAmmo", font, 42, TextAlignmentOptions.TopRight,
            new Vector2(1f, 1f), new Vector2(-220f, -50f), "BALLS: 12/12");
        timerText = MakeTMP(hudRoot.transform, "RushTimer", font, 54, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0f, -60f), "TIME: 60");
        poppedText = MakeTMP(hudRoot.transform, "RushPopped", font, 32, TextAlignmentOptions.TopLeft,
            new Vector2(0f, 1f), new Vector2(220f, -110f), "POPPED: 0");
        streakText = MakeTMP(hudRoot.transform, "RushStreak", font, 36, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0f, -130f), "");
        streakText.color = new Color(1f, 0.82f, 0.25f);
        streakText.gameObject.SetActive(false);
        notifyText = MakeTMP(hudRoot.transform, "RushNotify", font, 36, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0f, -190f), "");
        notifyText.gameObject.SetActive(false);
        AddOutline(notifyText, 0.18f, new Color32(60, 5, 12, 255));
        AddChip(hudRoot.transform, notifyText, 560f, 86f, 0.62f).SetActive(false);
        hintText = MakeTMP(hudRoot.transform, "RushHint", font, 26, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0f), new Vector2(0f, 60f), "");
        popupText = MakeTMP(hudRoot.transform, "RushPopup", font, 72, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 80f), "");
        popupText.color = Color.yellow;
        popupText.gameObject.SetActive(false);
        AddOutline(popupText, 0.14f, new Color32(60, 30, 0, 255));
        AddChip(hudRoot.transform, popupText, 440f, 130f, 0.55f).SetActive(false);
        foreach (var t in new[] { scoreText, ammoText, poppedText })
            AddChip(hudRoot.transform, t);

        countdownText = MakeTMP(hud.transform, "RushCountdown", font, 160, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), Vector2.zero, "");
        countdownText.color = new Color(1f, 0.85f, 0.3f);
        countdownText.gameObject.SetActive(false);

        resultsPanel = MakePanel(hud.transform, "RushResults", new Color(0.08f, 0.03f, 0.12f, 0.92f));
        resultsTitle = MakeTMP(resultsPanel.transform, "RushResultsTitle", font, 84, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.72f), Vector2.zero, "TIME UP!");
        resultsTitle.color = new Color(1f, 0.82f, 0.25f);
        resultsStats = MakeTMP(resultsPanel.transform, "RushResultsStats", font, 36, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), Vector2.zero, "");
        resultsStats.rectTransform.sizeDelta = new Vector2(1000f, 260f);
        playAgainButton = MakeButton(resultsPanel.transform, "RushAgainButton", font, "PLAY AGAIN (R)",
            new Vector2(0.5f, 0.26f), new Vector2(340f, 80f));
        modeButton = MakeButton(resultsPanel.transform, "RushMenuButton", font, "MODE SELECT",
            new Vector2(0.5f, 0.16f), new Vector2(340f, 70f));
        var againLabel = playAgainButton.transform.Find("Label")?.GetComponent<TMP_Text>();
        if (againLabel != null)
        {
            playAgainButton.GetComponent<Image>().color = new Color(0.95f, 0.70f, 0.14f);
            againLabel.color = new Color(0.28f, 0.12f, 0.05f);
        }
        resultsPanel.SetActive(false);
    }

    static TMP_Text MakeTMP(Transform parent, string name, TMP_FontAsset font, int size,
        TextAlignmentOptions align, Vector2 anchor, Vector2 offset, string text)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.color = Color.white;
        tmp.text = text;
        tmp.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor;
        rt.anchoredPosition = offset;
        rt.sizeDelta = new Vector2(700f, 80f);
        return tmp;
    }

    static GameObject AddChip(Transform hudRoot, TMP_Text text, float w = 400f, float h = 78f, float alpha = 0.45f)
    {
        var chip = new GameObject(text.name + "_Chip");
        chip.transform.SetParent(hudRoot, false);
        var img = chip.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, alpha);
        img.raycastTarget = false;
        var rt = chip.GetComponent<RectTransform>();
        var src = text.rectTransform;
        rt.anchorMin = src.anchorMin; rt.anchorMax = src.anchorMax; rt.pivot = src.pivot;
        rt.anchoredPosition = src.anchoredPosition;
        rt.sizeDelta = new Vector2(w, h);
        chip.transform.SetAsFirstSibling();
        return chip;
    }

    /// <summary>Dark SDF outline so text reads over bright booth colors. Extra padding prevents edge clipping.</summary>
    static void AddOutline(TMP_Text tmp, float width, Color color)
    {
        tmp.outlineWidth = width;
        tmp.outlineColor = color;
        tmp.extraPadding = true;
    }

    static GameObject MakePanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return go;
    }

    static Button MakeButton(Transform parent, string name, TMP_FontAsset font,
        string label, Vector2 anchor, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.85f, 0.25f, 0.15f);
        var btn = go.AddComponent<Button>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
        var lgo = new GameObject("Label");
        lgo.transform.SetParent(go.transform, false);
        var tmp = lgo.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.fontSize = 30;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.text = label;
        tmp.raycastTarget = false;
        var lrt = lgo.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
        return btn;
    }
}
