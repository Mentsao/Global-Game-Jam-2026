using UnityEngine;
using Player; 

// Requires NPCDetect for vision logic
[RequireComponent(typeof(NPCDetect))]
public class GovernmentOfficial : MonoBehaviour
{
    [Header("Government Logic")]
    [Tooltip("If true, the official is currently tracking the player state via NPCDetect")]
    public bool isTracking = false;

    [Header("Stats")]
    public float health = 50f;

    [Header("Vulnerability Settings")]
    [Tooltip("Angle cone for Front/Back invulnerability (in degrees). Default 60 means +/- 60 degrees from Forward/Backward.")]
    [Range(0, 90)] public float protectionAngle = 60f;

    private NPCDetect _detection;

    void Start()
    {
        _detection = GetComponent<NPCDetect>();
        
        // Physics Safeguard: Prevent tipping over
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY; 
            // Note: We might want gravity? If so remove FreezePositionY. 
            // Usually FreezeRotation is enough. Let's stick to Rotation.
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
    }

    void Update()
    {
        // NPCDetect handles the "LookAt" logic if it detects the player
        if (_detection != null)
        {
            isTracking = _detection.detectsPlayer && _detection.inFOV;
            // The constraint "they will not chase... just look at them" is handled by 
            // NPCDetect.LookAtPlayer() which rotates only. 
            // Ensure no movement logic is here or in NPCDetect (checked: NPCDetect only looks).
        }
    }

    /// <summary>
    /// Call this method to deal damage to the Government Official.
    /// Requires the attacker's Transform to calculate relative angle.
    /// </summary>
    public void TakeDamage(float damage, Transform attacker)
    {
        if (attacker == null) return;

        Vector3 toAttacker = (attacker.position - transform.position).normalized;
        float dot = Vector3.Dot(transform.forward, toAttacker);
        
        // Calculate Threshold for Protection
        // Dot Product of 1 is directly in front. 
        // Dot Product of -1 is directly behind.
        // Dot Product of 0 is side.
        // 60 degrees -> cos(60) = 0.5.
        // So if Dot > 0.5 (Front) OR Dot < -0.5 (Back), we are protected.
        
        float threshold = Mathf.Cos(protectionAngle * Mathf.Deg2Rad);

        bool isFront = dot > threshold;
        bool isBack = dot < -threshold;

        if (isFront || isBack)
        {
            // Protected! Kill Player.
            Debug.Log("[Government] Attacked from Front/Back! INSTANT DEATH EXECUTION.");
            KillPlayer(attacker);
        }
        else
        {
            // Side Attack - Vulnerable
            health -= damage;
            Debug.Log($"[Government] Side Attack Success! Health: {health}");
            
            if (health <= 0)
            {
                Die();
            }
        }
    }

    // fallback: Trigger check for weapon collisions
    private void OnTriggerEnter(Collider other)
    {
        // Simple heuristic: If it's a weapon layer or tag, or child of Player
        // Adjust these tags based on your project structure
        if (other.CompareTag("Weapon") || (other.transform.parent != null && other.transform.parent.CompareTag("Player")))
        {
            // Find Player Root
            Transform playerRoot = other.transform.root; 
            if (playerRoot.CompareTag("Player"))
            {
                // Deal damage (default 10 or similar)
                TakeDamage(10, playerRoot);
            }
        }
    }

    private void KillPlayer(Transform playerTransform)
    {
        PlayerHealth ph = playerTransform.GetComponent<PlayerHealth>();
        if (ph != null)
        {
            ph.TakeDamage(9999); // Instant Kill
        }
        else
        {
            // Try in children/parents
             ph = playerTransform.GetComponentInParent<PlayerHealth>();
             if (ph != null) ph.TakeDamage(9999);
        }
    }

    [Header("Loot")]
    [Tooltip("Assign GovtMask.prefab here")]
    [SerializeField] private GameObject maskPrefab;

    private void Die()
    {
        Debug.Log("[Government] Official Eliminate.");
        
        // Guaranteed Drop
        if (maskPrefab != null)
        {
            Instantiate(maskPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            Debug.Log("[Government] Mask Dropped.");
        }
        else
        {
            Debug.LogError("[Government] Mask Prefab NOT ASSIGNED in Inspector!");
        }

        // Instant Hide
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        // Visualize Vulnerability Zones
        // Red = Danger (Front/Back)
        // Green = Vulnerable (Sides)

        Vector3 pos = transform.position + Vector3.up * 1f; // slight offset up
        float radius = 1.5f;

        // Draw Arcs is hard in Gizmos without specific handle tools, using Lines approx
        
        float angle = protectionAngle;
        
        // Front Cone (Red)
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        DrawCone(pos, transform.forward, angle, radius);

        // Back Cone (Red)
        DrawCone(pos, -transform.forward, angle, radius);

        // Side Cones (Green) - Implicitly the space between
        // To visualize sides, we can draw lines for the boundaries
        Gizmos.color = Color.green;
        // Right Side Center
        // Gizmos.DrawLine(pos, pos + transform.right * radius);
        // Left Side Center
        // Gizmos.DrawLine(pos, pos - transform.right * radius);
    }

    private void DrawCone(Vector3 pos, Vector3 forward, float angleDeg, float radius)
    {
        Quaternion leftRayRotation = Quaternion.AngleAxis(-angleDeg, Vector3.up);
        Quaternion rightRayRotation = Quaternion.AngleAxis(angleDeg, Vector3.up);

        Vector3 leftRayDirection = leftRayRotation * forward;
        Vector3 rightRayDirection = rightRayRotation * forward;

        Gizmos.DrawLine(pos, pos + forward * radius);
        Gizmos.DrawLine(pos, pos + leftRayDirection * radius);
        Gizmos.DrawLine(pos, pos + rightRayDirection * radius);
        
        // Connect the tips to indicate "Zone"
        Gizmos.DrawLine(pos + leftRayDirection * radius, pos + forward * radius);
        Gizmos.DrawLine(pos + rightRayDirection * radius, pos + forward * radius);
    }
}
