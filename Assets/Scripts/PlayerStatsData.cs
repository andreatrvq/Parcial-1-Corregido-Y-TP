using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStatsData", menuName = "Parcial1/Player Stats")]
public class PlayerStatsData : ScriptableObject
{
    [Header("Vida")]
    public int maxHealth = 100;

    [Header("Estamina")]
    public float maxStamina = 10f;
    public float staminaRegenPerSec = 1f;
}

