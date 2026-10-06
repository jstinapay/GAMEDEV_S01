using UnityEngine;
using System.Collections;

/// <summary>
/// Floor bonus panel. Steps on it once for a temporary or permanent bonus.
/// One-shot per touch (consumed guard) so it never fires every frame.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class BonusPanel : MonoBehaviour
{
    public enum BonusType { SpeedBoost, Invulnerability, Heal, DamageReduction }

    [Header("Bonus")]
    public BonusType bonusType = BonusType.Heal;
    [Tooltip("Seconds for temporary effects. Ignored by Heal.")]
    public float duration = 6f;
    [Tooltip("Speed multiplier / damage multiplier / heal amount by type.")]
    public float magnitude = 1.5f;
    public int healAmount = 30;

    [Header("Reuse")]
    [Tooltip("Seconds until panel reactivates. 0 = stays consumed until Reset.")]
    public float respawnDelay;
    public bool consumed;

    [Header("Feedback (auto-wired)")]
    public LaserCorridorUI ui;
    public Renderer panelRenderer;
    [Tooltip("Floating type icon above the panel. Auto-created, hidden while consumed.")]
    public GameObject icon;
    Color baseColor = Color.green;

    Coroutine effectRoutine;
    Coroutine respawnRoutine;
    Coroutine popRoutine;
    Vector3 initialScale = Vector3.one;

    void Awake()
    {
        var col = GetComponent<BoxCollider>();
        col.isTrigger = true;
        // Tall trigger (1.6m above the plate) so walking AND jumping over
        // the panel both collect it. Computed from world scale, not assumptions.
        float worldY = Mathf.Max(0.01f, transform.lossyScale.y * col.size.y);
        float height = 1.6f;
        col.size = new Vector3(col.size.x, height / transform.lossyScale.y, col.size.z);
        col.center = new Vector3(col.center.x, (height / 2f - worldY / 2f) / transform.lossyScale.y, col.center.z);
        if (panelRenderer == null)
            panelRenderer = GetComponentInChildren<Renderer>();
        if (panelRenderer == null)
            panelRenderer = GetComponent<Renderer>();
        if (ui == null)
            ui = Object.FindAnyObjectByType<LaserCorridorUI>();
        initialScale = transform.localScale;
        EnsureIcon();
        ApplyTypeVisual();
    }

    /// <summary>Ensures a floating icon exists and matches the current bonus type.</summary>
    public void EnsureIcon()
    {
        if (icon == null)
        {
            icon = BonusIconLibrary.AttachIcon(transform, bonusType);
            return;
        }
        // If type changed after Awake (builder moves/retypes panels), rebuild icon.
        var rend = icon.GetComponent<SpriteRenderer>();
        var want = BonusIconLibrary.GetIcon(bonusType);
        if (rend == null || rend.sprite != want)
        {
            Object.Destroy(icon);
            icon = BonusIconLibrary.AttachIcon(transform, bonusType);
            // Keep it visible if panel is active.
            if (!consumed && icon != null) icon.SetActive(true);
        }
        else if (!consumed && !icon.activeSelf)
        {
            icon.SetActive(true);
        }
    }

    /// <summary>Called by builder when it retypes/moves an existing panel at runtime.</summary>
    public void RefreshAfterMove()
    {
        EnsureIcon();
        ApplyTypeVisual();
        if (!consumed && icon != null && !icon.activeSelf)
            icon.SetActive(true);
    }

    void OnValidate()
    {
        var col = GetComponent<BoxCollider>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (consumed)
        {
            Debug.Log($"Bonus {bonusType} already used - press Retry (R) to reset.");
            return;
        }
        // Parent fallback: if the touching collider lives on a child object,
        // the health/controller still resolve from the player root.
        var health = other.GetComponent<PlayerHealth>();
        if (health == null) health = other.GetComponentInParent<PlayerHealth>();
        var controller = other.GetComponent<CustomPlayerController>();
        if (controller == null) controller = other.GetComponentInParent<CustomPlayerController>();
        if (health == null || controller == null)
        {
            Debug.LogWarning($"BonusPanel: Player entered but health/controller not found on '{other.name}'.");
            return;
        }

        consumed = true;
        ApplyBonus(health, controller);
        DimVisual();
        PlayPickupVfx();

        if (respawnDelay > 0f)
        {
            if (respawnRoutine != null) StopCoroutine(respawnRoutine);
            respawnRoutine = StartCoroutine(RespawnRoutine());
        }
    }

    void ApplyBonus(PlayerHealth health, CustomPlayerController controller)
    {
        switch (bonusType)
        {
            case BonusType.SpeedBoost:
                controller.SetSpeedMultiplier(magnitude <= 0f ? 1.5f : magnitude);
                Announce($"{Label()}! +Speed {duration:0}s");
                RestartEffect(ExpireSpeed(controller));
                break;
            case BonusType.Invulnerability:
                health.SetInvulnerableTimed(duration);
                Announce($"{Label()}! {duration:0}s");
                break;
            case BonusType.Heal:
                health.Heal(healAmount > 0 ? healAmount : 30);
                Announce($"{Label()}! +{healAmount} HP");
                break;
            case BonusType.DamageReduction:
                health.SetDamageReduction(magnitude > 0f && magnitude < 1f ? magnitude : 0.5f, duration);
                Announce($"{Label()}! {duration:0}s");
                break;
        }
        Debug.Log($"Bonus picked up: {bonusType}");
    }

    void RestartEffect(IEnumerator routine)
    {
        if (effectRoutine != null) StopCoroutine(effectRoutine);
        effectRoutine = StartCoroutine(routine);
    }

    IEnumerator ExpireSpeed(CustomPlayerController controller)
    {
        yield return new WaitForSeconds(duration);
        if (controller != null) controller.ResetEffects();
        effectRoutine = null;
    }

    IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);
        ResetPanel();
    }

    void Announce(string text)
    {
        if (ui != null) ui.ShowBonus(text, duration);
        else Debug.Log(text);
    }

    string Label()
    {
        switch (bonusType)
        {
            case BonusType.SpeedBoost: return "SPEED BOOST";
            case BonusType.Invulnerability: return "INVULNERABLE";
            case BonusType.Heal: return "HEAL";
            case BonusType.DamageReduction: return "SHIELD";
            default: return "BONUS";
        }
    }

    void ApplyTypeVisual()
    {
        switch (bonusType)
        {
            case BonusType.SpeedBoost: baseColor = Color.cyan; break;
            case BonusType.Invulnerability: baseColor = Color.yellow; break;
            case BonusType.Heal: baseColor = Color.green; break;
            case BonusType.DamageReduction: baseColor = new Color(0.4f, 0.6f, 1f); break;
        }
        if (panelRenderer != null)
        {
            var mat = panelRenderer.material; // instance per panel
            mat.color = baseColor;
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", baseColor * 1.5f);
            }
        }
    }

    void DimVisual()
    {
        if (panelRenderer != null)
        {
            var mat = panelRenderer.material;
            mat.color = Color.gray;
            if (mat.HasProperty("_EmissionColor"))
                mat.SetColor("_EmissionColor", Color.black);
        }
        if (icon != null) icon.SetActive(false);
    }

    /// <summary>Restores panel for a new run. Stops pending expiry safely.</summary>
    public void ResetPanel()
    {
        consumed = false;
        if (respawnRoutine != null) { StopCoroutine(respawnRoutine); respawnRoutine = null; }
        EnsureIcon();
        if (icon != null) icon.SetActive(true);
        ApplyTypeVisual();
    }

    public void CancelEffect(CustomPlayerController controller)
    {
        if (effectRoutine != null) { StopCoroutine(effectRoutine); effectRoutine = null; }
        if (controller != null && bonusType == BonusType.SpeedBoost)
            controller.ResetEffects();
    }

    /// <summary>All visual feedback for a pickup: screen flash, particle burst, panel pop.</summary>
    void PlayPickupVfx()
    {
        if (ui != null) ui.BonusFlash(baseColor);
        SpawnBurst(baseColor);
        if (popRoutine != null) StopCoroutine(popRoutine);
        popRoutine = StartCoroutine(PopRoutine());
    }

    void SpawnBurst(Color color)
    {
        var go = new GameObject("PickupBurst");
        go.transform.position = transform.position + Vector3.up * 0.6f;
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.startColor = color;
        main.startSpeed = 4f;
        main.startLifetime = 0.6f;
        main.startSize = 0.15f;
        main.maxParticles = 40;
        main.gravityModifier = -0.5f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission;
        em.enabled = true;
        em.rateOverTime = 0f;
        em.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30) });
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.6f;
        ps.Play();
        Object.Destroy(go, 1.2f);
    }

    IEnumerator PopRoutine()
    {
        Vector3 peak = new Vector3(initialScale.x * 1.25f, initialScale.y * 1.6f, initialScale.z * 1.25f);
        float t = 0f;
        while (t < 0.12f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(initialScale, peak, t / 0.12f);
            yield return null;
        }
        t = 0f;
        while (t < 0.25f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(peak, initialScale, t / 0.25f);
            yield return null;
        }
        transform.localScale = initialScale;
        popRoutine = null;
    }

    /// <summary>Right-click menu in Play mode: applies this bonus without walking over it.</summary>
    [ContextMenu("Test: Apply Bonus Now")]
    public void TestApplyBonus()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) { Debug.LogWarning("BonusPanel test: no Player in scene."); return; }
        var health = p.GetComponent<PlayerHealth>();
        var controller = p.GetComponent<CustomPlayerController>();
        if (health == null || controller == null) { Debug.LogWarning("BonusPanel test: Player missing health/controller."); return; }
        bool wasConsumed = consumed;
        consumed = false;
        ApplyBonus(health, controller);
        consumed = true;
        DimVisual();
        PlayPickupVfx();
        if (wasConsumed) Debug.Log("BonusPanel test: was already consumed, applied anyway for testing.");
    }

    public static void ResetAllPanels()
    {
        foreach (var p in Object.FindObjectsByType<BonusPanel>())
            p.ResetPanel();
    }
}
