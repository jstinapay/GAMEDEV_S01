using UnityEngine;

/// <summary>
/// Central SFX with per-type throttling so multi-can pileups don't spam audio.
/// All clips optional: when unassigned, short synthetic tones are generated
/// at runtime so the game still has feedback with zero assets.
/// </summary>
public class CanKnockdownAudio : MonoBehaviour
{
    public static CanKnockdownAudio Instance { get; private set; }

    [Header("Optional clips (uses synthetic fallback when empty)")]
    [SerializeField] AudioClip throwClip;
    [SerializeField] AudioClip ballHitClip;
    [SerializeField] AudioClip canHitClip;
    [SerializeField] AudioClip knockdownClip;
    [SerializeField] AudioClip winClip;
    [SerializeField] AudioClip popClip;
    [SerializeField] AudioClip goldPopClip;

    [Header("Mix")]
    [SerializeField] float volume = 0.8f;
    [Tooltip("Min seconds between hit sounds. Stops machine-gun clanking.")]
    [SerializeField] float hitThrottle = 0.06f;
    [SerializeField] bool useSyntheticFallback = true;

    AudioSource source;
    float lastBallHit;
    float lastCanHit;
    float lastPop;

    // Cached synthetic tones (generated once).
    AudioClip synthThrow;
    AudioClip synthBall;
    AudioClip synthCan;
    AudioClip synthScore;
    AudioClip synthWin;
    AudioClip synthPop;
    AudioClip synthGold;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        source = GetComponent<AudioSource>();
        if (source == null)
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f; // 2D UI-style mix, audible regardless of camera
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void PlayThrow()
    {
        Play(throwClip, ref synthThrow, 520f, 0.18f, 0.5f);
    }

    public void PlayBallHit(float impactSpeed)
    {
        if (Time.time - lastBallHit < hitThrottle) return;
        lastBallHit = Time.time;
        Play(ballHitClip, ref synthBall, 220f, 0.12f, ImpactVolume(impactSpeed));
    }

    public void PlayCanHit(float impactSpeed)
    {
        if (Time.time - lastCanHit < hitThrottle) return;
        lastCanHit = Time.time;
        Play(canHitClip, ref synthCan, 880f, 0.1f, ImpactVolume(impactSpeed));
    }

    public void PlayKnockdown()
    {
        Play(knockdownClip, ref synthScore, 660f, 0.25f, 0.7f);
    }

    public void PlayWin()
    {
        Play(winClip, ref synthWin, 990f, 0.5f, 0.8f);
    }

    public void PlayPop()
    {
        if (Time.time - lastPop < hitThrottle) return;
        lastPop = Time.time;
        Play(popClip, ref synthPop, 1250f, 0.08f, 0.7f);
    }

    public void PlayGoldPop()
    {
        if (Time.time - lastPop < hitThrottle) return;
        lastPop = Time.time;
        Play(goldPopClip, ref synthGold, 1700f, 0.14f, 0.8f);
    }

    static float ImpactVolume(float speed)
    {
        // Louder for harder hits, clamped so soft taps are still audible.
        return Mathf.Clamp(0.3f + speed * 0.06f, 0.3f, 0.9f);
    }

    void Play(AudioClip clip, ref AudioClip synthSlot, float synthFreq, float duration, float vol)
    {
        if (source == null) return;
        AudioClip toPlay = clip;
        if (toPlay == null && useSyntheticFallback)
        {
            if (synthSlot == null)
                synthSlot = MakeTone(synthFreq, duration);
            toPlay = synthSlot;
        }
        if (toPlay == null) return;
        source.PlayOneShot(toPlay, vol * volume);
    }

    /// <summary>Generates a short decaying sine blip. No assets required.</summary>
    static AudioClip MakeTone(float freq, float duration)
    {
        int rate = 22050;
        int samples = Mathf.Max(1, Mathf.RoundToInt(rate * duration));
        var data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / rate;
            float envelope = 1f - (float)i / samples; // linear decay
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.6f;
        }
        var clip = AudioClip.Create($"Synth_{freq}Hz", samples, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
