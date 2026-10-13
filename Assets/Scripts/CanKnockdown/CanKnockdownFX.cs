using UnityEngine;

/// <summary>
/// Visual juice only: confetti bursts on knockdowns and round wins.
/// Pure runtime: soft-dot sprite generated in code, three carnival colors,
/// one-shot ParticleSystems that clean themselves up. No assets required.
/// Never touches scoring, physics, or game state.
/// </summary>
public static class CanKnockdownFX
{
    static Material dotCacheRed;
    static Material dotCacheGold;
    static Material dotCacheTeal;

    /// <summary>Burst at a knocked-down can. Bigger count = bigger burst.</summary>
    public static void BurstAt(Vector3 pos, int totalFresh)
    {
        int perColor = totalFresh >= 3 ? 45 : 30;
        SpawnConfetti(pos, new Color(0.9f, 0.15f, 0.22f), perColor, 5f);
        SpawnConfetti(pos, new Color(1f, 0.78f, 0.15f), perColor, 4.2f);
        SpawnConfetti(pos, new Color(0.1f, 0.7f, 0.75f), perColor, 4.6f);
    }

    /// <summary>Celebration burst for full clears and perfect rounds.</summary>
    public static void WinBurst(Vector3 pos)
    {
        SpawnConfetti(pos, new Color(1f, 0.78f, 0.15f), 90, 7f);
        SpawnConfetti(pos, new Color(0.95f, 0.95f, 1f), 60, 6f);
        SpawnConfetti(pos, new Color(0.9f, 0.15f, 0.22f), 60, 6.5f);
    }

    static void SpawnConfetti(Vector3 pos, Color color, int count, float speed)
    {
        var go = new GameObject("Confetti");
        go.transform.position = pos;
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.startLifetime = 1.1f;
        main.startSpeed = speed;
        main.startSize = 0.09f;
        main.gravityModifier = 1.4f;
        main.maxParticles = count + 20;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 32f;
        shape.radius = 0.12f;

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.material = DotMaterial(color);

        ps.Play();
        Object.Destroy(go, 2.2f);
    }

    static Material DotMaterial(Color color)
    {
        // Cache one material per color so bursts share assets.
        if (color.r > 0.8f && color.g < 0.3f && dotCacheRed == null)
            dotCacheRed = new Material(ParticleShader()) { color = color };
        else if (color.g > 0.6f && color.b < 0.4f && dotCacheGold == null)
            dotCacheGold = new Material(ParticleShader()) { color = color };
        else if (color.b > 0.6f && dotCacheTeal == null)
            dotCacheTeal = new Material(ParticleShader()) { color = color };

        if (color.r > 0.8f && color.g < 0.3f) return dotCacheRed;
        if (color.g > 0.6f && color.b < 0.4f) return dotCacheGold;
        if (color.b > 0.6f) return dotCacheTeal;
        var fallback = new Material(ParticleShader()) { color = color };
        return fallback;
    }

    static Shader ParticleShader()
    {
        return Shader.Find("Universal Render Pipeline/Particles/Unlit")
            ?? Shader.Find("Particles/Standard Unlit")
            ?? Shader.Find("Unlit/Color")
            ?? Shader.Find("Standard");
    }

    /// <summary>Attaches a short pale-gold trail to a ball. Visual only.</summary>
    public static void AddBallTrail(GameObject ball)
    {
        if (ball == null || ball.GetComponent<TrailRenderer>() != null) return;
        var trail = ball.AddComponent<TrailRenderer>();
        trail.time = 0.35f;
        trail.startWidth = 0.09f;
        trail.endWidth = 0.015f;
        trail.minVertexDistance = 0.05f;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard"));
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", new Color(1f, 0.88f, 0.55f));
        else if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(1f, 0.88f, 0.55f));
        trail.material = mat;
    }
}
