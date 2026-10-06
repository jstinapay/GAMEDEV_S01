using UnityEngine;

/// <summary>
/// Minimal HUD for the laser corridor. Uses legacy UnityEngine.UI.Text
/// like the existing Traversal HUD. Shows health, active bonus, and
/// game-over / completion panels with retry wiring.
/// </summary>
public class LaserCorridorUI : MonoBehaviour
{
    [Header("HUD")]
    public UnityEngine.UI.Text healthText;
    public UnityEngine.UI.Text bonusText;
    public UnityEngine.UI.Text hintText;

    [Header("Overlays")]
    public UnityEngine.UI.Image damageFlash;
    public GameObject gameOverPanel;
    public GameObject completePanel;
    public UnityEngine.UI.Text gameOverText;
    public UnityEngine.UI.Text completeText;

    [Header("Buttons")]
    public UnityEngine.UI.Button retryButton;
    public UnityEngine.UI.Button playAgainButton;

    public LaserCorridorManager manager;
    public PlayerHealth health;
    [Tooltip("FP camera to free/relock the cursor for end-panel buttons.")]
    public CameraFollow follow;

    [Header("HP Bar (top right)")]
    public GameObject hpBar;
    public UnityEngine.UI.Image hpBarFill;
    public UnityEngine.UI.Image hpBarGhost;
    public UnityEngine.UI.Text hpBarText;

    [Header("Bonus FX")]
    [Tooltip("Full-screen flash tinted per bonus type on pickup.")]
    public UnityEngine.UI.Image bonusFlash;

    [Header("Debug (bottom-left, helps verify HP/laser chain)")]
    public UnityEngine.UI.Text debugText;

    float bonusTimeLeft;
    float bonusTotal;
    float ghostDelay;
    float targetFill = 1f;

    void Awake()
    {
        if (manager == null)
            manager = Object.FindAnyObjectByType<LaserCorridorManager>();
        if (follow == null)
            follow = Object.FindAnyObjectByType<CameraFollow>();
    }

    void Start()
    {
        if (health == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) health = p.GetComponent<PlayerHealth>();
        }
        if (health != null)
        {
            health.OnHealthChanged -= OnHealthChanged;
            health.OnHealthChanged += OnHealthChanged;
            OnHealthChanged(health.currentHealth, health.maxHealth);
        }
        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(OnRetryPressed);
            retryButton.onClick.AddListener(OnRetryPressed);
        }
        if (playAgainButton != null)
        {
            playAgainButton.onClick.RemoveListener(OnRetryPressed);
            playAgainButton.onClick.AddListener(OnRetryPressed);
        }
        HideEndPanels();
        ClearBonus();
        SetFlash(0f);
        EnsureFirstPersonHud();
        EnsureRuntimeHud();
        if (health != null)
            UpdateHpBar(health.currentHealth, health.maxHealth);
    }

    /// <summary>
    /// Builds HUD pieces added after the scene was first built (HP bar,
    /// bonus flash) so old scenes pick them up at runtime. Also called by
    /// the builder so fresh builds bake them in.
    /// </summary>
    public void EnsureRuntimeHud()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        EnsureHpBar(font);
        EnsureBonusFlashImage();
        EnsureDebugText(font);
    }

    void EnsureDebugText(Font font)
    {
        var t = transform.Find("DebugText");
        UnityEngine.UI.Text text;
        if (t != null) text = t.GetComponent<UnityEngine.UI.Text>();
        else
        {
            var go = new GameObject("DebugText");
            go.transform.SetParent(transform, false);
            text = go.AddComponent<UnityEngine.UI.Text>();
            text.font = font;
            text.fontSize = 13;
            text.alignment = TextAnchor.LowerLeft;
            text.color = new Color(1f, 1f, 1f, 0.75f);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(12f, 12f);
            rt.sizeDelta = new Vector2(600f, 40f);
        }
        debugText = text;
    }

    void EnsureHpBar(Font font)
    {
        // Rebuild from scratch: a scene-baked bar can carry a Simple-type
        // image or wrong order that ignores fillAmount and looks permanently
        // full (label updates, bar doesn't). Fresh build guarantees Filled.
        var legacy = transform.Find("HpBar");
        if (legacy != null)
        {
            if (Application.isPlaying) Object.Destroy(legacy.gameObject);
            else Object.DestroyImmediate(legacy.gameObject);
        }
        GameObject root = new GameObject("HpBar");
        root.transform.SetParent(transform, false);
        var rt = root.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-140f, -30f);
        rt.sizeDelta = new Vector2(260f, 30f);
        var back = root.AddComponent<UnityEngine.UI.Image>();
        back.color = new Color(0f, 0f, 0f, 0.6f);
        hpBar = root;

        var fillT = root.transform.Find("HpBarFill");
        UnityEngine.UI.Image fill;
        if (fillT != null) fill = fillT.GetComponent<UnityEngine.UI.Image>();
        else
        {
            var fillGo = new GameObject("HpBarFill");
            fillGo.transform.SetParent(root.transform, false);
            fill = fillGo.AddComponent<UnityEngine.UI.Image>();
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 1f;
            var frt = fillGo.GetComponent<RectTransform>();
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.offsetMin = new Vector2(3f, 3f);
            frt.offsetMax = new Vector2(-3f, -3f);
        }
        hpBarFill = fill;
        // Enforce filled-type even on scene-baked bars (a Simple-type image ignores fillAmount and looks permanently full).
        hpBarFill.type = UnityEngine.UI.Image.Type.Filled;
        hpBarFill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        hpBarFill.fillOrigin = 0;

        var labelT = root.transform.Find("HpBarText");
        UnityEngine.UI.Text label;
        if (labelT != null) label = labelT.GetComponent<UnityEngine.UI.Text>();
        else
        {
            var labelGo = new GameObject("HpBarText");
            labelGo.transform.SetParent(root.transform, false);
            label = labelGo.AddComponent<UnityEngine.UI.Text>();
            label.font = font;
            label.fontSize = 16;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            var lrt = labelGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
        }
        hpBarText = label;

        // Trailing ghost layer: drains slowly after damage so hits read clearly.
        var ghostT = root.transform.Find("HpBarGhost");
        UnityEngine.UI.Image ghost;
        if (ghostT != null) ghost = ghostT.GetComponent<UnityEngine.UI.Image>();
        else
        {
            var ghostGo = new GameObject("HpBarGhost");
            ghostGo.transform.SetParent(root.transform, false);
            ghost = ghostGo.AddComponent<UnityEngine.UI.Image>();
            ghost.color = new Color(1f, 0.85f, 0.25f, 0.95f);
            ghost.type = UnityEngine.UI.Image.Type.Filled;
            ghost.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            ghost.fillOrigin = 0;
            ghost.fillAmount = 1f;
            var grt = ghostGo.GetComponent<RectTransform>();
            grt.anchorMin = Vector2.zero;
            grt.anchorMax = Vector2.one;
            grt.offsetMin = new Vector2(3f, 3f);
            grt.offsetMax = new Vector2(-3f, -3f);
        }
        hpBarGhost = ghost;
        hpBarGhost.type = UnityEngine.UI.Image.Type.Filled;
        hpBarGhost.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        hpBarGhost.fillOrigin = 0;
        // Order: dark back (root) -> ghost -> instant fill -> label on top.
        if (hpBarFill != null)
            ghost.transform.SetSiblingIndex(hpBarFill.transform.GetSiblingIndex());
        if (hpBarText != null)
            hpBarText.transform.SetAsLastSibling();
    }

    void EnsureBonusFlashImage()
    {
        var t = transform.Find("BonusFlash");
        UnityEngine.UI.Image img;
        if (t != null) img = t.GetComponent<UnityEngine.UI.Image>();
        else
        {
            var go = new GameObject("BonusFlash");
            go.transform.SetParent(transform, false);
            img = go.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(1f, 1f, 1f, 0f);
            img.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        bonusFlash = img;
    }

    /// <summary>Brings older built scenes up to FP spec: crosshair (hint removed).</summary>
    void EnsureFirstPersonHud()
    {
        if (hintText != null)
        {
            if (Application.isPlaying) Destroy(hintText.gameObject);
            else DestroyImmediate(hintText.gameObject);
            hintText = null;
        }
        var staleHint = transform.Find("HintText");
        if (staleHint != null)
        {
            if (Application.isPlaying) Destroy(staleHint.gameObject);
            else DestroyImmediate(staleHint.gameObject);
        }
        if (transform.Find("Crosshair") != null) return;
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var go = new GameObject("Crosshair");
        go.transform.SetParent(transform, false);
        var text = go.AddComponent<UnityEngine.UI.Text>();
        text.font = font;
        text.fontSize = 28;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = "+";
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(60f, 60f);
    }

    void OnDestroy()
    {
        if (health != null) health.OnHealthChanged -= OnHealthChanged;
        if (retryButton != null) retryButton.onClick.RemoveListener(OnRetryPressed);
        if (playAgainButton != null) playAgainButton.onClick.RemoveListener(OnRetryPressed);
    }

    void Update()
    {
        // Direct tracker: bar follows live HP every frame, so it can never
        // freeze even if a health event was missed. Event still drives
        // text, color, ghost pause and damage flash below/elsewhere.
        if (health != null && health.maxHealth > 0)
        {
            targetFill = Mathf.Clamp01((float)health.currentHealth / health.maxHealth);
            if (hpBarFill != null)
                hpBarFill.color = targetFill > 0.5f ? Color.green : (targetFill > 0.25f ? Color.yellow : Color.red);
        }
        if (bonusTimeLeft > 0f)
        {
            bonusTimeLeft -= Time.deltaTime;
            if (bonusText != null && bonusTotal > 0f)
            {
                string base_msg = bonusText.text.Split('(')[0].TrimEnd();
                if (bonusTimeLeft > 0f)
                    bonusText.text = $"{base_msg} ({Mathf.CeilToInt(bonusTimeLeft)}s)";
                else
                    ClearBonus();
            }
        }
        // Fade damage + bonus flashes.
        if (damageFlash != null && damageFlash.color.a > 0f)
        {
            var c = damageFlash.color;
            c.a = Mathf.Max(0f, c.a - Time.deltaTime * 1.5f);
            damageFlash.color = c;
        }
        if (bonusFlash != null && bonusFlash.color.a > 0f)
        {
            var bc = bonusFlash.color;
            bc.a = Mathf.Max(0f, bc.a - Time.deltaTime * 1.2f);
            bonusFlash.color = bc;
        }
        // Main bar shortens smoothly toward the latest HP...
        if (hpBarFill != null && !Mathf.Approximately(hpBarFill.fillAmount, targetFill))
            hpBarFill.fillAmount = Mathf.MoveTowards(hpBarFill.fillAmount, targetFill, Time.deltaTime * 2.5f);
        // ...then the ghost layer drains down to meet it after a short pause.
        if (hpBarGhost != null && hpBarFill != null && hpBarGhost.fillAmount > hpBarFill.fillAmount)
        {
            if (ghostDelay > 0f)
                ghostDelay -= Time.deltaTime;
            else
                hpBarGhost.fillAmount = Mathf.Max(hpBarFill.fillAmount, hpBarGhost.fillAmount - Time.deltaTime * 0.4f);
        }
        UpdateDebugText();
        LowHpPulse();
    }

    void LowHpPulse()
    {
        if (damageFlash == null || health == null || health.maxHealth <= 0) return;
        if (manager != null && manager.State != LaserCorridorManager.GameState.Playing) return;
        if ((float)health.currentHealth / health.maxHealth > 0.3f) return;
        float floor = 0.16f + 0.1f * Mathf.Sin(Time.time * 5f);
        if (damageFlash.color.a < floor)
        {
            var c = damageFlash.color;
            c.a = floor;
            damageFlash.color = c;
        }
    }

    float debugTimer;
    void UpdateDebugText()
    {
        debugTimer -= Time.deltaTime;
        if (debugTimer > 0f || debugText == null) return;
        debugTimer = 0.25f;
        int lasers = -1;
        string state = "?";
        if (manager != null)
        {
            state = manager.State.ToString();
            if (manager.spawner != null) lasers = manager.spawner.ActiveCount;
        }
        int hp = health != null ? health.currentHealth : -1;
        int max = health != null ? health.maxHealth : -1;
        debugText.text = $"HP {hp}/{max} | Lasers {lasers} | {state}";
    }

    /// <summary>Screen-edge flash in the bonus color. Called by BonusPanel.</summary>
    public void BonusFlash(Color color)
    {
        if (bonusFlash == null) return;
        bonusFlash.color = new Color(color.r, color.g, color.b, 0.35f);
    }

    void OnHealthChanged(int current, int max)
    {
        if (healthText != null)
            healthText.text = $"HP: {current}/{max}";
        FlashDamage();
        UpdateHpBar(current, max);
    }

    void UpdateHpBar(int current, int max)
    {
        float f = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
        targetFill = f; // main bar animates toward this in Update so every hit visibly shortens it
        if (hpBarFill != null)
            hpBarFill.color = f > 0.5f ? Color.green : (f > 0.25f ? Color.yellow : Color.red);
        if (hpBarText != null)
            hpBarText.text = $"{current}/{max}";
        if (hpBarGhost != null)
        {
            if (f >= hpBarGhost.fillAmount)
                hpBarGhost.fillAmount = f; // heal/reset: snap up instantly
            else
                ghostDelay = 0.5f; // damage: pause, then drain down in Update
        }
    }

    public void ShowBonus(string message, float duration)
    {
        if (bonusText != null)
        {
            bonusText.text = message;
            bonusText.gameObject.SetActive(true);
        }
        bonusTimeLeft = duration;
        bonusTotal = duration;
        // Instant heals show briefly.
        if (duration <= 0f) bonusTimeLeft = 2f;
    }

    public void ClearBonus()
    {
        bonusTimeLeft = 0f;
        bonusTotal = 0f;
        if (bonusText != null)
        {
            bonusText.text = "";
            bonusText.gameObject.SetActive(false);
        }
    }

    public void FlashDamage() { SetFlash(0.45f); }

    void SetFlash(float a)
    {
        if (damageFlash != null)
        {
            var c = damageFlash.color;
            c.a = a;
            damageFlash.color = c;
        }
    }

    public void ShowGameOver()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (gameOverText != null) gameOverText.text = "GAME OVER - Press Retry (R)";
        SetCursorForUI(true);
    }

    public void ShowComplete()
    {
        if (completePanel != null) completePanel.SetActive(true);
        if (completeText != null) completeText.text = "CORRIDOR CLEARED!";
        SetCursorForUI(true);
    }

    public void HideEndPanels()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (completePanel != null) completePanel.SetActive(false);
        SetCursorForUI(false);
    }

    void SetCursorForUI(bool uiOpen)
    {
        if (follow == null)
            follow = Object.FindAnyObjectByType<CameraFollow>();
        if (follow != null) follow.SetUIOpen(uiOpen);
        if (!Application.isPlaying) return;
        if (uiOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (follow != null && follow.firstPersonOnly)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void OnRetryPressed()
    {
        if (manager != null) manager.ResetRun();
    }
}
