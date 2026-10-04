using UnityEngine;

/// <summary>
/// Makes a bonus icon hover above its panel: billboards to the camera,
/// bobs up/down and gently pulses. Follows the panel if it moves.
/// </summary>
public class BonusIconFloat : MonoBehaviour
{
    public Transform follow;
    public float height = 1.35f;
    public float size = 1.15f;

    float phase;
    Vector3 baseScale;
    static int spawned;

    void Start()
    {
        phase = (spawned++ * 0.77f) % (Mathf.PI * 2f);
        baseScale = Vector3.one * Mathf.Max(0.01f, size);
        transform.localScale = baseScale;
    }

    void Update()
    {
        if (follow != null)
            transform.position = follow.position + Vector3.up * (height + Mathf.Sin(Time.time * 2.5f + phase) * 0.12f);
        var cam = Camera.main;
        if (cam != null)
            transform.rotation = cam.transform.rotation;
        float p = 1f + Mathf.Sin(Time.time * 3f + phase) * 0.06f;
        transform.localScale = baseScale * p;
    }
}
