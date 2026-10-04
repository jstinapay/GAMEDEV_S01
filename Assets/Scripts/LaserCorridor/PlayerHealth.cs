using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Simple reusable health component for the laser corridor.
/// Tracks current/max health, damage cooldown (i-frames), temporary
/// invulnerability and damage reduction. Calls the manager on death.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth = 100;

    [Header("Damage protection")]
    [Tooltip("Minimum seconds between damage ticks. Prevents per-frame melt.")]
    public float damageCooldown = 0.8f;
    [Tooltip("Damage after reduction = amount * damageMultiplier (1 = full).")]
    public float damageMultiplier = 1f;

    [Header("Refs (auto-found if empty)")]
    public LaserCorridorManager manager;
    public CustomPlayerController controller;

    public bool IsInvulnerable { get; private set; }
    public bool IsDead => currentHealth <= 0;

    float lastDamageTime = -999f;
    Coroutine invulnRoutine;
    Coroutine reductionRoutine;
    Coroutine blinkRoutine;
    Renderer[] cachedRenderers;

    public event Action<int, int> OnHealthChanged;

    void Awake()
    {
        if (controller == null)
            controller = GetComponent<CustomPlayerController>();
        if (manager == null)
            manager = UnityEngine.Object.FindAnyObjectByType<LaserCorridorManager>();
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
        if (currentHealth <= 0 || currentHealth > maxHealth)
            currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (manager != null && manager.State != LaserCorridorManager.GameState.Playing)
            return;
        if (IsDead) return;
        if (IsInvulnerable) return;
        if (Time.time - lastDamageTime < damageCooldown) return;

        lastDamageTime = Time.time;
        int final = Mathf.Max(1, Mathf.RoundToInt(amount * damageMultiplier));
        currentHealth = Mathf.Max(0, currentHealth - final);
        Debug.Log($"Player took {final} damage. HP: {currentHealth}/{maxHealth}");
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // Brief hit blink so damage is clearly visible.
        if (blinkRoutine != null) StopCoroutine(blinkRoutine);
        blinkRoutine = StartCoroutine(BlinkRoutine());

        if (currentHealth <= 0)
        {
            Debug.Log("Player died.");
            if (manager != null)
                manager.OnPlayerDied();
        }
    }

    public void Heal(int amount)
    {
        if (IsDead) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + Mathf.Max(0, amount));
        Debug.Log($"Player healed +{amount}. HP: {currentHealth}/{maxHealth}");
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        lastDamageTime = -999f;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>Right-click menu in Play mode: real 20-damage hit through the full chain.</summary>
    [ContextMenu("Test: Take 20 Damage")]
    public void TestDamage() { TakeDamage(20); }

    public void ClearTemporaryEffects()
    {
        if (invulnRoutine != null) { StopCoroutine(invulnRoutine); invulnRoutine = null; }
        if (reductionRoutine != null) { StopCoroutine(reductionRoutine); reductionRoutine = null; }
        if (blinkRoutine != null) { StopCoroutine(blinkRoutine); blinkRoutine = null; }
        IsInvulnerable = false;
        damageMultiplier = 1f;
        SetRenderersVisible(true);
    }

    public void SetInvulnerableTimed(float duration)
    {
        if (invulnRoutine != null) StopCoroutine(invulnRoutine);
        invulnRoutine = StartCoroutine(InvulnRoutine(duration));
    }

    public void SetDamageReduction(float multiplier, float duration)
    {
        if (reductionRoutine != null) StopCoroutine(reductionRoutine);
        reductionRoutine = StartCoroutine(ReductionRoutine(Mathf.Clamp(multiplier, 0f, 1f), duration));
    }

    IEnumerator InvulnRoutine(float duration)
    {
        IsInvulnerable = true;
        Debug.Log($"Invulnerable for {duration}s");
        yield return new WaitForSeconds(duration);
        IsInvulnerable = false;
        invulnRoutine = null;
        Debug.Log("Invulnerability expired.");
    }

    IEnumerator ReductionRoutine(float multiplier, float duration)
    {
        damageMultiplier = multiplier;
        Debug.Log($"Damage reduction x{multiplier} for {duration}s");
        yield return new WaitForSeconds(duration);
        damageMultiplier = 1f;
        reductionRoutine = null;
        Debug.Log("Damage reduction expired.");
    }

    IEnumerator BlinkRoutine()
    {
        // Toggle renderers 4 times over ~0.5s. Respects first-person hide by restoring visible.
        for (int i = 0; i < 4; i++)
        {
            SetRenderersVisible(false);
            yield return new WaitForSeconds(0.06f);
            SetRenderersVisible(true);
            yield return new WaitForSeconds(0.06f);
        }
        SetRenderersVisible(true);
        blinkRoutine = null;
    }

    void SetRenderersVisible(bool visible)
    {
        if (cachedRenderers == null || cachedRenderers.Length == 0)
            cachedRenderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in cachedRenderers)
        {
            if (r != null) r.enabled = visible;
        }
    }
}
