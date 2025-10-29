using UnityEngine;

public class PistolProjectile : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera cam;
    [SerializeField] private Transform playerRoot;
    [SerializeField] private CharacterController playerCC;
    [SerializeField] private GameObject bulletPrefab;

    [Header("Fire (semi-auto)")]
    [SerializeField] private float fireRate = 4f;      // clicks per second
    [SerializeField] private float bulletSpeed = 40f;
    [SerializeField] private float bulletDamage = 10f;
    [SerializeField] private float maxRange = 30f;     // hard limit of the shot
    [SerializeField] private float spawnOffset = 0.25f;

    [Header("Aiming")]
    [SerializeField] private LayerMask aimMask = ~0;   // layers to aim against

    private float nextShotTime = 0f;
    private Collider[] ownerColliders;

    void Awake()
    {
        if (cam == null) cam = Camera.main;

        if (playerRoot == null)
        {
            var p = GetComponentInParent<PlayerStats>()?.transform;
            playerRoot = p != null ? p : transform.root;
        }

        if (playerCC == null && playerRoot != null)
            playerCC = playerRoot.GetComponent<CharacterController>();

        ownerColliders = playerRoot != null
            ? playerRoot.GetComponentsInChildren<Collider>()
            : GetComponentsInChildren<Collider>();
    }

    void Update()
    {
        if (Time.time >= nextShotTime && Input.GetMouseButtonDown(0))
        {
            FireFromPlayerCenter();
            nextShotTime = Time.time + 1f / Mathf.Max(0.01f, fireRate);
        }
    }

    private Vector3 GetPlayerCenterWorld()
    {
        if (playerRoot == null) return transform.position;
        if (playerCC != null)
            return playerRoot.position + playerCC.center;
        else
            return playerRoot.position + Vector3.up * 1.0f;
    }

    private void FireFromPlayerCenter()
    {
        if (bulletPrefab == null || playerRoot == null)
        {
            Debug.LogWarning("PistolProjectile: missing bulletPrefab or playerRoot");
            return;
        }

        Vector3 origin = GetPlayerCenterWorld();

        // Aim direction with ray limited to maxRange
        Vector3 dir;
        float rayDistance = Mathf.Max(0.01f, maxRange);

        if (cam != null)
        {
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, aimMask, QueryTriggerInteraction.Ignore))
                dir = (hit.point - origin).normalized;
            else
                dir = ray.direction.normalized;
        }
        else
        {
            dir = playerRoot.forward;
        }

        // Avoid spawning inside walls
        float offset = spawnOffset;
        if (Physics.SphereCast(origin, 0.05f, dir, out RaycastHit block, spawnOffset, aimMask, QueryTriggerInteraction.Ignore))
            offset = Mathf.Max(0f, block.distance * 0.5f);

        Vector3 spawnPos = origin + dir * offset;
        Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);

        GameObject go = Instantiate(bulletPrefab, spawnPos, rot);

        // Ignore collision with owner
        if (ownerColliders != null)
        {
            var bulletCols = go.GetComponentsInChildren<Collider>();
            foreach (var oc in ownerColliders)
                foreach (var bc in bulletCols)
                    if (oc && bc) Physics.IgnoreCollision(oc, bc, true);
        }

        // Initialize bullet logic
        var bp = go.GetComponent<BulletProjectile>();
        if (bp != null)
        {
            // Overload that also passes range and spawn position
            bp.Init(dir, bulletSpeed, bulletDamage, "Player", maxRange, spawnPos);
        }

        // Fallback: set velocity directly if Rigidbody present
        var rb = go.GetComponent<Rigidbody>();
        if (rb != null)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = dir * bulletSpeed;
#else
            rb.velocity = dir * bulletSpeed;
#endif
        }
    }
}






