using UnityEngine;

/// <summary>
/// Goal trigger at the corridor exit. Stops the challenge and shows
/// the completion state. Guarded so it fires exactly once per run.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class LaserGoal : MonoBehaviour
{
    public LaserCorridorManager manager;
    bool finished;

    void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
        if (manager == null)
            manager = Object.FindAnyObjectByType<LaserCorridorManager>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (finished) return;
        if (!other.CompareTag("Player")) return;
        finished = true;
        Debug.Log("Laser corridor complete!");
        if (manager != null)
            manager.OnGoalReached();
    }

    public void ResetGoal() { finished = false; }

    public static void ResetAllGoals()
    {
        foreach (var g in Object.FindObjectsByType<LaserGoal>())
            g.ResetGoal();
    }
}
