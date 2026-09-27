using UnityEngine;

/// <summary>Returns player to start when falling below killY.</summary>
public class FallRespawn : MonoBehaviour
{
    [Header("Refs")]
    public Transform player;
    public CustomPlayerController playerController;

    [Header("Settings")]
    public Vector3 spawnPoint = new Vector3(0f, 2f, 0f);
    public float killY = -8f;

    [Header("HUD (optional)")]
    public UnityEngine.UI.Text fallsText;

    public int Falls { get; private set; }

    public void ResetFalls() { Falls = 0; UpdateFallsText(); }

    void Start()
    {
        if (playerController == null && player != null)
            playerController = player.GetComponent<CustomPlayerController>();
        if (playerController != null)
            playerController.SetSpawn(spawnPoint);
        UpdateFallsText();
    }

    void Update()
    {
        if (player == null) return;
        if (player.position.y < killY)
        {
            Falls++;
            Debug.Log($"Player fell (#{Falls}). Respawning at start.");
            UpdateFallsText();
            if (playerController != null)
                playerController.Respawn();
            else
                player.position = spawnPoint;
        }
    }

    void UpdateFallsText()
    {
        if (fallsText != null)
            fallsText.text = $"Falls: {Falls}";
    }
}
