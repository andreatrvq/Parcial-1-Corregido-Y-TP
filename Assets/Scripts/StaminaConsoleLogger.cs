using UnityEngine;
using System.Collections;

public class StaminaConsoleLogger : MonoBehaviour
{
    [SerializeField] private PlayerStats player;
    [SerializeField] private float interval = 0.5f;         
    [SerializeField] private bool onlyOnChange = false;     
    [SerializeField] private float changeThreshold = 0.05f; 

    private float prev = float.NaN;
    private bool prevPaused = false;
    private bool prevInit = false;

    void Awake()
    {
        if (player == null)
            player = GetComponent<PlayerStats>() ?? GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerStats>();
    }

    void OnEnable() { StartCoroutine(LogLoop()); }

    IEnumerator LogLoop()
    {
        var wait = new WaitForSeconds(interval);
        while (enabled && player != null)
        {
            float cur = player.CurrentStamina;
            bool paused = player.RegenPaused;

            bool shouldLog;
            if (!onlyOnChange || !prevInit)
            {
                shouldLog = true;
            }
            else
            {
                bool staminaChanged = Mathf.Abs(cur - prev) >= changeThreshold;
                bool pauseChanged = paused != prevPaused;
                shouldLog = staminaChanged || pauseChanged;
            }

            if (shouldLog)
            {
                float deltaPerSec = prevInit ? (cur - prev) / Mathf.Max(0.0001f, interval) : 0f;
                string trend = !prevInit ? "•"
                              : Mathf.Abs(deltaPerSec) < 0.001f ? "•"
                              : (deltaPerSec > 0 ? "Regenerando" : "Drenando");

                Debug.Log($"[STAMINA] {cur:0.00}/{player.MaxStamina:0.##} {trend} | regen {(paused ? "PAUSADA" : "ON")}");

                prev = cur;
                prevPaused = paused;
                prevInit = true;
            }

            yield return wait;
        }
    }
}

