using UnityEngine;

/// <summary>Balloon Pop Rush target kinds. Tuning lives on BalloonSpawner.</summary>
public enum BalloonType { Standard, Golden, Bonus }

/// <summary>Per-type tuning knobs, edited in the BalloonSpawner Inspector.</summary>
[System.Serializable]
public struct BalloonTypeConfig
{
    public BalloonType type;
    [Tooltip("Points awarded for popping one.")]
    public int points;
    [Tooltip("Bonus type only: extra balls granted on pop.")]
    public int bonusAmmo;
    public Color color;
    [Tooltip("Base cruise speed in m/s (multiplied by round difficulty).")]
    public float speed;
    [Tooltip("Visual scale multiplier.")]
    public float scale;
    [Tooltip("Relative spawn chance.")]
    [Range(0f, 100f)] public float spawnWeight;
}
