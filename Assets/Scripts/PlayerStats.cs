using UnityEngine;
using System;

public class PlayerStats : MonoBehaviour
{
    [Header("Data (ScriptableObject opcional)")]
    [SerializeField] private PlayerStatsData data;  // Arrastra aqui tu asset PlayerStatsData

    [Header("Vida")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    [Header("Estamina")]
    [SerializeField] private float maxStamina = 10f;
    [SerializeField] private float currentStamina;
    [SerializeField] private float staminaRegenPerSec = 1f;

    // Eventos (sin Header porque no aplica a eventos)
    public event Action OnDied;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public float MaxStamina => maxStamina;
    public float CurrentStamina => currentStamina;
    public bool RegenPaused => regenPaused;
    public bool IsDead => isDead;

    private bool regenPaused = false;
    private bool isDead = false;

    void Reset()
    {
        maxHealth = 100;
        maxStamina = 10f;
        staminaRegenPerSec = 1f;
    }

    void Awake()
    {
        if (data != null)
        {
            maxHealth = data.maxHealth;
            maxStamina = data.maxStamina;
            staminaRegenPerSec = data.staminaRegenPerSec;
        }

        currentHealth = maxHealth;
        currentStamina = maxStamina;
        isDead = false;
    }

    void Update()
    {
        if (!regenPaused)
        {
            currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenPerSec * Time.deltaTime);
        }
    }

    // Aplica dano y dispara OnDied una sola vez
    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth = Mathf.Max(0, currentHealth - Mathf.Abs(amount));
        if (currentHealth <= 0 && !isDead)
        {
            isDead = true;
            OnDied?.Invoke();
        }
    }

    public void DrainStaminaPerSecond(float perSecond)
    {
        regenPaused = true;
        currentStamina = Mathf.Max(0f, currentStamina - perSecond * Time.deltaTime);
    }

    public void PauseRegen(bool pause) => regenPaused = pause;

    public void SetMaxHealth(int value)
    {
        maxHealth = Mathf.Max(1, value);
        currentHealth = Mathf.Min(currentHealth, maxHealth);
    }

    public void SetMaxStamina(float value)
    {
        maxStamina = Mathf.Max(0.1f, value);
        currentStamina = Mathf.Min(currentStamina, maxStamina);
    }

    // Utilidad para pruebas
    public void Kill()
    {
        if (isDead) return;
        currentHealth = 0;
        isDead = true;
        OnDied?.Invoke();
    }
}



