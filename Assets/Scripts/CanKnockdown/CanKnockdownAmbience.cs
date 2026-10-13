using UnityEngine;

/// <summary>
/// Ambient life for the carnival booth: bobbing balloons, swaying bunting,
/// twinkling bulbs, breathing spotlight. Visual transforms only — every animated
/// object is decor with no collider, so this can never touch physics, scoring,
/// or aiming. Cheap: cached arrays, sine motion, zero per-frame allocation.
/// </summary>
public class CanKnockdownAmbience : MonoBehaviour
{
    [Header("Motion (subtle by design)")]
    [SerializeField] float balloonBob = 0.12f;
    [SerializeField] float balloonSpeed = 1.1f;
    [SerializeField] float buntingSway = 0.10f;
    [SerializeField] float buntingSpeed = 1.6f;
    [SerializeField] float bulbTwinkle = 0.08f;
    [SerializeField] float bulbSpeed = 2.2f;
    [SerializeField] float spotBreathe = 0.10f;

    Transform[] balloons;
    Vector3[] balloonBase;
    float[] balloonPhase;
    Transform[] flags;
    Quaternion[] flagBase;
    float[] flagPhase;
    Transform[] bulbs;
    Vector3[] bulbBase;
    float[] bulbPhase;
    Light stackSpot;
    float spotBase;

    void Start()
    {
        var all = Object.FindObjectsByType<Transform>();
        var bal = new System.Collections.Generic.List<Transform>();
        var fla = new System.Collections.Generic.List<Transform>();
        var bul = new System.Collections.Generic.List<Transform>();
        foreach (var t in all)
        {
            if (t == null) continue;
            string n = t.name;
            if (n.StartsWith("Balloon_")) bal.Add(t);
            else if (n.StartsWith("Bunting_")) fla.Add(t);
            else if (n.StartsWith("Bulb_") || n.StartsWith("BulbFront_")) bul.Add(t);
        }
        balloons = bal.ToArray();
        flags = fla.ToArray();
        bulbs = bul.ToArray();

        balloonBase = new Vector3[balloons.Length];
        balloonPhase = new float[balloons.Length];
        for (int i = 0; i < balloons.Length; i++)
        {
            balloonBase[i] = balloons[i].position;
            balloonPhase[i] = i * 1.37f;
        }
        flagBase = new Quaternion[flags.Length];
        flagPhase = new float[flags.Length];
        for (int i = 0; i < flags.Length; i++)
        {
            flagBase[i] = flags[i].rotation;
            flagPhase[i] = i * 0.93f;
        }
        bulbBase = new Vector3[bulbs.Length];
        bulbPhase = new float[bulbs.Length];
        for (int i = 0; i < bulbs.Length; i++)
        {
            bulbBase[i] = bulbs[i].localScale;
            bulbPhase[i] = i * 1.71f;
        }

        var spotGo = GameObject.Find("StackSpot");
        if (spotGo != null) stackSpot = spotGo.GetComponent<Light>();
        if (stackSpot != null) spotBase = stackSpot.intensity;
    }

    void Update()
    {
        float t = Time.time;
        for (int i = 0; i < balloons.Length; i++)
        {
            if (balloons[i] == null) continue;
            Vector3 p = balloonBase[i];
            p.y += Mathf.Sin(t * balloonSpeed + balloonPhase[i]) * balloonBob;
            balloons[i].position = p;
        }
        for (int i = 0; i < flags.Length; i++)
        {
            if (flags[i] == null) continue;
            flags[i].rotation = flagBase[i] * Quaternion.Euler(
                Mathf.Sin(t * buntingSpeed + flagPhase[i]) * buntingSway * Mathf.Rad2Deg, 0f, 0f);
        }
        for (int i = 0; i < bulbs.Length; i++)
        {
            if (bulbs[i] == null) continue;
            float k = 1f + Mathf.Sin(t * bulbSpeed + bulbPhase[i]) * bulbTwinkle;
            bulbs[i].localScale = bulbBase[i] * k;
        }
        if (stackSpot != null)
            stackSpot.intensity = spotBase * (1f + Mathf.Sin(t * 0.9f) * spotBreathe);
    }
}
