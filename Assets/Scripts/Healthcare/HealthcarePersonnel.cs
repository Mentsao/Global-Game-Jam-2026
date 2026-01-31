using UnityEngine;

[RequireComponent(typeof(CharacterController))] // Optional, just ensuring basic movability
public class HealthcarePersonnel : MonoBehaviour
{
    [Header("Detection Settings")]
    [Tooltip("Offset from the NPC (Forward)")]
    public Vector3 detectionOffset = new Vector3(0, 1, 1.5f); 
    public float detectionRadius = 1.0f;
    public LayerMask playerLayer;

    [Header("Zone Settings")]
    public Transform targetArea; // The center of their 'Home' or 'Patrol Zone'
    public float zoneRadius = 10f; // Limit they will not leave

    [Header("Follow Settings")]
    public float stopDistance = 1.5f;
    public float moveSpeed = 4f;

    // State
    private bool _isFollowing = false;
    private Transform _playerTransform;
    private Vector3 _homePosition;

    void Start()
    {
        // If no target area assigned, use initial position as home
        if (targetArea != null)
        {
            _homePosition = targetArea.position;
        }
        else
        {
            _homePosition = transform.position;
        }
    }

    void Update()
    {
        // 1. Detection Logic (Trigger Box)
        DetectPlayer();

        // 2. Follow Logic
        if (_isFollowing && _playerTransform != null)
        {
            FollowBehavior();
        }
    }

    private void DetectPlayer()
    {
        // Determine detection center in world space
        Vector3 detectCenter = transform.TransformPoint(detectionOffset);

        // Check for Player overlap
        Collider[] hits = Physics.OverlapSphere(detectCenter, detectionRadius, playerLayer);
        bool playerInTrigger = false;

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                playerInTrigger = true;
                _playerTransform = hit.transform;
                break;
            }
        }

        // Logic: If Player touches trigger, START following
        if (playerInTrigger && !_isFollowing)
        {
            _isFollowing = true;
            Debug.Log("[Healthcare] Player entered trigger. Following started.");
        }
    }

    private void FollowBehavior()
    {
        // check if Player is within the Allowed Zone
        float distToZoneCenter = Vector3.Distance(_playerTransform.position, _homePosition);

        if (distToZoneCenter > zoneRadius)
        {
            // Player left the area -> Stop following
            _isFollowing = false;
            _playerTransform = null; // Forget player
            Debug.Log("[Healthcare] Player left the zone. Stopping.");
            return;
        }

        // Move towards Player
        float distToPlayer = Vector3.Distance(transform.position, _playerTransform.position);
        
        if (distToPlayer > stopDistance)
        {
            Vector3 direction = (_playerTransform.position - transform.position).normalized;
            // Simple Translate (Use NavMeshAgent if available in future, but keeping simple as requested)
            transform.position += direction * moveSpeed * Time.deltaTime;
            
            // Look at player (Lock Y axis if preferred, but LookAt is simple)
            transform.LookAt(new Vector3(_playerTransform.position.x, transform.position.y, _playerTransform.position.z));
        }
    }

    public void Die()
    {
        Debug.Log("Healthcare Personnel Killed.");
        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        // 1. Detection Trigger (Green) - "The Wake Up Spot"
        Gizmos.color = new Color(0f, 1f, 0f, 0.4f);
        Vector3 detectCenter = transform.TransformPoint(detectionOffset);
        Gizmos.DrawSphere(detectCenter, detectionRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(detectCenter, detectionRadius);

        // 2. Home Zone (Yellow) - " The Territory"
        Gizmos.color = Color.yellow;
        Vector3 zoneCenter = (Application.isPlaying) ? _homePosition : (targetArea != null ? targetArea.position : transform.position);
        Gizmos.DrawWireSphere(zoneCenter, zoneRadius);
    }
}
