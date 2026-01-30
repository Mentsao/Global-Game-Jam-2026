using UnityEngine;

[RequireComponent(typeof(NPCDetect))]
public class GovernmentOfficial : MonoBehaviour
{
    private NPCDetect detection;
    
    [Header("Movement")]
    public float moveSpeed = 2f; 
    public float stopDistance = 5f; 

    [Header("Stats")]
    public float health = 100f;
    public bool isVulnerable = false;

    void Start()
    {
        detection = GetComponent<NPCDetect>();
        detection.fieldOfView = 175f; 
        detection.rangeOfView = 25f; 
    }

    void Update()
    {
        if (detection.player == null) return;

        bool canSeePlayer = detection.inFOV && detection.detectsPlayer;

        if (canSeePlayer)
        {
            Debug.Log("Government Official is tracking you...");
            FollowPlayer();
        }
    }

    void FollowPlayer()
    {
        float distance = Vector3.Distance(transform.position, detection.player.position);
        
        if (distance > stopDistance)
        {
            Vector3 direction = (detection.player.position - transform.position).normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;
        }
        
        transform.LookAt(detection.player.position);
    }

    public void TakeDamage(float damage)
    {
        if (isVulnerable)
        {
            health -= damage;
        }
        else
        {
            health -= damage * 0.1f;
        }

        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Official Killed. Player can now take the Official Mask.");
        Destroy(gameObject);
    }
}
