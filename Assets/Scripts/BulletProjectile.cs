using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BulletProjectile : MonoBehaviour
{
    [Header("Runtime")]
    [SerializeField] private float speed = 60f;
    [SerializeField] private float damage = 25f;
    [SerializeField] private float lifeTime = 3f;   // safety timeout
    [SerializeField] private float maxRange = 30f;  // hard distance limit

    private Rigidbody rb;
    private string ownerTagToIgnore = "Player";
    private Vector3 spawnPos;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        // fallback spawn position in case Init is not called
        spawnPos = transform.position;

        // safety destroy by time
        Destroy(gameObject, Mathf.Max(0.1f, lifeTime));
    }

    void Update()
    {
        // manual move if no rigidbody
        if (rb == null)
        {
            transform.position += transform.forward * speed * Time.deltaTime;
        }

        // kill by distance
        if (Vector3.Distance(spawnPos, transform.position) >= maxRange)
        {
            Destroy(gameObject);
        }
    }

    // Original-style Init (kept for compatibility)
    public void Init(Vector3 direction, float speedOverride, float damageOverride, string ownerTag = "Player")
    {
        ownerTagToIgnore = ownerTag;
        if (speedOverride > 0f) speed = speedOverride;
        if (damageOverride >= 0f) damage = damageOverride;

        Launch(direction);
    }

    // Extended Init with range and spawn position
    public void Init(Vector3 direction, float speedOverride, float damageOverride, string ownerTag, float maxRangeOverride, Vector3 spawnPosition)
    {
        ownerTagToIgnore = ownerTag;
        if (speedOverride > 0f) speed = speedOverride;
        if (damageOverride >= 0f) damage = damageOverride;
        if (maxRangeOverride > 0f) maxRange = maxRangeOverride;

        spawnPos = spawnPosition;
        Launch(direction);
    }

    private void Launch(Vector3 direction)
    {
        Vector3 v = direction.normalized * speed;

        if (rb != null)
        {
            // Unity 6 uses linearVelocity; in older versions use velocity
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = v;
#else
            rb.velocity = v;
#endif
        }
        else
        {
            transform.forward = direction.normalized;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (ShouldIgnore(other)) return;
        TryDamage(other);
        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Collider other = collision.collider;
        if (ShouldIgnore(other)) return;
        TryDamage(other);
        Destroy(gameObject);
    }

    private bool ShouldIgnore(Collider other)
    {
        // ignore owner by tag
        if (!string.IsNullOrEmpty(ownerTagToIgnore) && other.CompareTag(ownerTagToIgnore))
            return true;

        return false;
    }

    private void TryDamage(Collider col)
    {
        // look for EnemyAI on the hit object or its parents
        EnemyAI enemy = col.GetComponentInParent<EnemyAI>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
        }
    }
}



