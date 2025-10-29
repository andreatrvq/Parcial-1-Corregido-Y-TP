using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class EnemyAI : MonoBehaviour
{
    public enum State { normal, chase, damage, dead }

    [Header("Refs")]
    [SerializeField] private Transform targetPlayer;

    [Header("Vida")]
    [SerializeField] private float maxHealth = 50f;
    [SerializeField] private float currentHealth;

    [Header("Movimiento (horizontal)")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Gravedad (sin salto)")]
    [SerializeField] private bool useGravity = true;
    [SerializeField] private float gravity = 20f;

    [Header("Ground Check (opcional)")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.25f;
    [SerializeField] private LayerMask groundLayers = ~0;

    [Header("Cono de vision (unica percepcion)")]
    [SerializeField] private float viewAngle = 60f;
    [SerializeField] private float viewRange = 5f; // pon 5 por defecto en el Inspector
    [SerializeField] private float eyeHeight = 1.6f;
    [SerializeField] private LayerMask visionObstacles = ~0; // incluye paredes/suelo

    [Header("Drenaje de estamina del jugador")]
    [SerializeField] private float staminaDrainPerSec = 1f;

    [Header("Ataque al jugador")]
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackDamage = 25f;
    [SerializeField] private float attackRate = 1.0f;
    private float nextAttackTime = 0f;

    [Header("Aiming / Giro")]
    [SerializeField] private bool faceWhenNotChasing = true;
    [SerializeField] private float faceDistance = 12f;

    [Header("Death / Visibility")]
    [SerializeField] private bool hideOnDeath = true;
    [SerializeField] private float hideDelay = 0f;

    private Vector3 spawnPos;
    private Quaternion spawnRot;
    private State currentState = State.normal;
    private static EnemyAI singleton;

    private PlayerStats playerStats;
    private Renderer[] renderers;
    private Collider[] colliders;
    private CharacterController controller;

    private float yVelocity = 0f;
    private bool isGrounded = false;

    void Awake()
    {
        if (singleton != null && singleton != this)
        {
            Debug.LogWarning("Ya existe un Enemy activo. Este se destruira para cumplir 'solo 1 enemigo'.");
            Destroy(gameObject);
            return;
        }
        singleton = this;

        controller = GetComponent<CharacterController>();

        spawnPos = transform.position;
        spawnRot = transform.rotation;

        currentHealth = maxHealth;

        if (targetPlayer == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) targetPlayer = p.transform;
        }

        if (targetPlayer != null)
            playerStats = targetPlayer.GetComponent<PlayerStats>();

        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);
    }

    void Update()
    {
        // Respawn solo si esta dead (ajuste pedido por el profesor)
        if (Input.GetKeyDown(KeyCode.F3) && currentState == State.dead)
            RespawnAtSpawn();

        if (currentState == State.dead || targetPlayer == null) return;

        bool canChase = IsPlayerInsideVisionCone();

        if (canChase)
        {
            SetState(State.chase);
            FacePlayer();
        }
        else
        {
            SetState(State.normal);
            if (playerStats != null) playerStats.PauseRegen(false);

            if (faceWhenNotChasing && IsPlayerWithinFaceDistance())
                FacePlayer();
        }

        Vector3 horizontal = canChase ? transform.forward * moveSpeed : Vector3.zero;

        if (useGravity)
        {
            isGrounded = CheckGrounded();
            if (isGrounded && yVelocity < 0f) yVelocity = -2f;
            yVelocity -= gravity * Time.deltaTime;
        }

        Vector3 motion = horizontal + Vector3.up * yVelocity;
        controller.Move(motion * Time.deltaTime);

        if (canChase)
        {
            if (playerStats != null)
                playerStats.DrainStaminaPerSecond(staminaDrainPerSec);

            TryAttackIfInRange();
        }
    }

    private void FacePlayer()
    {
        Vector3 dir = (targetPlayer.position - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion look = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, rotationSpeed * Time.deltaTime);
    }

    private bool CheckGrounded()
    {
        if (groundCheck != null)
        {
            return Physics.CheckSphere(
                groundCheck.position,
                groundCheckRadius,
                groundLayers,
                QueryTriggerInteraction.Ignore
            );
        }
        Vector3 origin = transform.position + Vector3.up * 0.05f;
        float rayLen = (controller != null ? controller.skinWidth : 0.05f) + 0.25f;
        return Physics.SphereCast(
            origin,
            groundCheckRadius,
            Vector3.down,
            out _,
            rayLen,
            groundLayers,
            QueryTriggerInteraction.Ignore
        );
    }

    private bool IsPlayerWithinFaceDistance()
    {
        float dist = Vector3.Distance(transform.position, targetPlayer.position);
        return dist <= faceDistance;
    }

    // Cono + LOS (raycast) para que paredes bloqueen la vision
    private bool IsPlayerInsideVisionCone()
    {
        Vector3 eye = transform.position + Vector3.up * eyeHeight;
        Vector3 head = targetPlayer.position + Vector3.up * 1.0f;

        Vector3 toPlayer = head - eye;
        float dist = toPlayer.magnitude;
        if (dist > viewRange) return false;

        Vector3 fwd = transform.forward; fwd.y = 0f;
        Vector3 flat = toPlayer; flat.y = 0f;
        if (flat.sqrMagnitude < 0.0001f) return true; // casi encima

        float angle = Vector3.Angle(fwd, flat);
        if (angle > (viewAngle * 0.5f)) return false;

        // Linea de vision: si golpea algo antes que el player, no ve
        if (Physics.Raycast(eye, toPlayer.normalized, out RaycastHit hit, dist, visionObstacles, QueryTriggerInteraction.Ignore))
        {
            // Si lo primero no es el Player (o su jerarquia), esta bloqueado
            if (!hit.transform.IsChildOf(targetPlayer))
                return false;
        }
        return true;
    }

    private void TryAttackIfInRange()
    {
        if (playerStats == null || currentState == State.dead) return;

        float dist = Vector3.Distance(transform.position, targetPlayer.position);
        if (dist <= attackRange && Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + (1f / Mathf.Max(0.01f, attackRate));
            int dmg = Mathf.RoundToInt(attackDamage);
            playerStats.TakeDamage(dmg);
            Debug.Log("Enemy hits player for " + dmg + ". Player HP: " + playerStats.CurrentHealth);
        }
    }

    public void TakeDamage(float amount)
    {
        if (currentState == State.dead) return;

        currentHealth -= Mathf.Abs(amount);
        SetState(State.damage);

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            SetState(State.dead);
            HandleDeath();
        }
    }

    private void HandleDeath()
    {
        if (playerStats != null) playerStats.PauseRegen(false);

        if (!hideOnDeath) return;

        if (hideDelay > 0f) StartCoroutine(HideAfterDelay(hideDelay));
        else SetVisible(false);
    }

    private IEnumerator HideAfterDelay(float t)
    {
        yield return new WaitForSeconds(t);
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (renderers != null)
            foreach (var r in renderers) if (r) r.enabled = visible;

        if (colliders != null)
            foreach (var c in colliders) if (c) c.enabled = visible;
    }

    private void RespawnAtSpawn()
    {
        if (controller != null) controller.enabled = false;
        transform.SetPositionAndRotation(spawnPos, spawnRot);
        if (controller != null) controller.enabled = true;

        currentHealth = maxHealth;
        yVelocity = 0f;
        nextAttackTime = 0f;

        SetState(State.normal);
        SetVisible(true);
        if (playerStats != null) playerStats.PauseRegen(false);
    }

    private void SetState(State newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        Debug.Log("Enemy state: " + currentState);
    }

    void OnDrawGizmosSelected()
    {
        // Cono
        Gizmos.color = Color.red;
        Vector3 origin = transform.position;
        Vector3 fwd = transform.forward;

        Quaternion left = Quaternion.AngleAxis(-viewAngle * 0.5f, Vector3.up);
        Quaternion right = Quaternion.AngleAxis(viewAngle * 0.5f, Vector3.up);
        Vector3 leftDir = left * fwd;
        Vector3 rightDir = right * fwd;

        Gizmos.DrawLine(origin, origin + leftDir.normalized * viewRange);
        Gizmos.DrawLine(origin, origin + rightDir.normalized * viewRange);

        int steps = 16;
        Vector3 prev = origin + leftDir.normalized * viewRange;
        for (int i = 1; i <= steps; i++)
        {
            float t = (float)i / steps;
            Quaternion q = Quaternion.Slerp(left, right, t);
            Vector3 dir = q * fwd;
            Vector3 next = origin + dir.normalized * viewRange;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }

        // Attack range
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // GroundCheck
        Gizmos.color = Color.green;
        if (groundCheck != null)
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}





