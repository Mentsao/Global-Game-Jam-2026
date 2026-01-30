using UnityEngine;

[RequireComponent(typeof(NPCDetect))]
public class HealthcarePersonnel : MonoBehaviour
{
    private NPCDetect detection;
    
    [Header("Companion Settings")]
    public Transform targetArea;
    public float followDistance = 3f;
    public float stopDistance = 1.5f;
    public float moveSpeed = 4f;

    [Header("State")]
    public bool isFollowingPlayer = false;

    void Start()
    {
        detection = GetComponent<NPCDetect>();
        detection.fieldOfView = 60f;
        detection.rangeOfView = 12f; 
    }

    void Update()
    {
        if (detection.player == null) return;

        bool canSeePlayer = detection.inFOV && detection.detectsPlayer;

        if (canSeePlayer)
        {
            isFollowingPlayer = true;
            FollowPlayer();
            UpdateAreaStatus();
        }
        else if (isFollowingPlayer)
        {
            isFollowingPlayer = false;
            Debug.Log("Healthcare Personnel lost sight of player.");
        }
    }

    void FollowPlayer()
    {
        float distance = Vector3.Distance(transform.position, detection.player.position);
        
        if (distance > stopDistance)
        {
            Vector3 direction = (detection.player.position - transform.position).normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;
            
            transform.LookAt(detection.player.position);
        }
    }

    void UpdateAreaStatus()
    {
        if (targetArea != null)
        {
            float distToTarget = Vector3.Distance(transform.position, targetArea.position);
            if (distToTarget < 2f)
            {
                Debug.Log("Healthcare Personnel reached the target area.");
                isFollowingPlayer = false;
            }
        }
        
        // Logical hooks for other systems:
        // - Police: Should ignore player while isFollowingPlayer is true
        // - Zombies: Should increase attraction range to player while isFollowingPlayer is true
    }

    public void Die()
    {
        Debug.Log("Healthcare Personnel Killed. Player gets +1 HP Mask.");
        Destroy(gameObject);
    }
}
