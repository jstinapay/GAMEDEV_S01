using UnityEngine;
using UnityEngine.UI;

/// <summary>Level-complete trigger on the end platform.</summary>
[RequireComponent(typeof(Collider))]
public class LevelFinish : MonoBehaviour
{
    [Header("Refs")]
    public CustomPlayerController player;
    public GameObject completeText;
    [Tooltip("Panel shown on finish (contains text + Play Again button).")]
    public GameObject finishPanel;
    [Tooltip("Button that restarts the level.")]
    public Button playAgainButton;
    [Tooltip("Fall tracker to reset on replay.")]
    public FallRespawn fallRespawn;
    [Tooltip("Confetti burst played on finish (optional).")]
    public ParticleSystem finishParticles;

    bool finished;

    void Start()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
        if (completeText != null)
            completeText.SetActive(false);
        if (finishPanel != null)
            finishPanel.SetActive(false);
        if (playAgainButton != null)
        {
            playAgainButton.onClick.RemoveListener(PlayAgain);
            playAgainButton.onClick.AddListener(PlayAgain);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (finished) return;
        if (!other.CompareTag("Player")) return;
        finished = true;
        Debug.Log("Level Complete!");
        if (player == null)
            player = other.GetComponent<CustomPlayerController>();
        if (player != null)
            player.Lock();
        if (completeText != null)
            completeText.SetActive(true);
        if (finishPanel != null)
            finishPanel.SetActive(true);
        if (finishParticles != null)
            finishParticles.Play();
    }

    /// <summary>Resets the run without reloading the scene.</summary>
    public void PlayAgain()
    {
        finished = false;
        if (fallRespawn == null)
            fallRespawn = Object.FindFirstObjectByType<FallRespawn>();
        if (fallRespawn != null)
            fallRespawn.ResetFalls();
        if (player != null)
        {
            player.Respawn();
            player.Unlock();
        }
        if (completeText != null)
            completeText.SetActive(false);
        if (finishPanel != null)
            finishPanel.SetActive(false);
        Debug.Log("Restarted - play again!");
    }
}
