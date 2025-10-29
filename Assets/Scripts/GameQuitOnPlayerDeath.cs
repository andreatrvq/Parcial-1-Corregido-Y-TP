using UnityEngine;

public class GameQuitOnPlayerDeath : MonoBehaviour
{
    [SerializeField] private PlayerStats player;

    void Awake()
    {
        // Si no se asigna por Inspector, busca una instancia en escena (API nueva).
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerStats>(); // o FindAnyObjectByType<PlayerStats>()
        }
    }

    void OnEnable()
    {
        if (player != null) player.OnDied += HandlePlayerDied;
    }

    void OnDisable()
    {
        if (player != null) player.OnDied -= HandlePlayerDied;
    }

    private void HandlePlayerDied()
    {
        Debug.Log("Player died -> quit requested");
        GlobalGameControls.Quit();
    }
}


