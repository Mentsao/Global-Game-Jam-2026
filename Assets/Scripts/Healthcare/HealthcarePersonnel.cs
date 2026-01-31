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
    
    // Animation
    private Animator _animator;
    [Header("Animation Settings")]
    [SerializeField] private float animTransitionTime = 0.2f;
    private string _currentAnimState;
    private const string ANIM_IDLE = "HealthCareIdle";
    private const string ANIM_WALK = "HealthCareWalking";
    private const string ANIM_ATTACK = "HealthcareAttack";

    void Start()
    {
        _animator = GetComponentInChildren<Animator>();

        // Physics Fix: Prevent tipping over
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

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

        // 2. Follow or Patrol
        if (_isFollowing && _playerTransform != null)
        {
            FollowBehavior();
        }
        else
        {
            PatrolBehavior();
        }

        // 3. Update Animations
        UpdateAnimationState();
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
            // Jealousy Logic: Check if player already has a nurse
            var hp = _playerTransform.GetComponent<Player.PlayerHealth>();
            if (hp != null)
            {
                if (hp.HasNurse)
                {
                    // Check Cooldown
                    if (Time.time < _lastAttackTime + attackCooldown) return;

                    Debug.Log("[Healthcare] JEALOUSY! Player has another nurse. EXECUTING PROTOCOL.");
                    
                     _lastAttackTime = Time.time;

                    // Attack Animation
                    if (!_isAttacking)
                    {
                        StartCoroutine(PlayAttackAnimation());
                    }
                    
                    // Deal 1 damage
                    hp.TakeDamage(1); 
                    return; // Do not start following
                }

                // Normal Behavior: Grant Bonus Life
                hp.GrantBonusHealth(1);
                
                _isFollowing = true;
                Debug.Log("[Healthcare] Player entered trigger. Following started.");
            }
        }
    }

    private float _lastAttackTime = 0f;
    [SerializeField] private float attackCooldown = 2.0f;
    private bool _isAttacking = false;

    private System.Collections.IEnumerator PlayAttackAnimation()
    {
        _isAttacking = true;
        PlayAnimation(ANIM_ATTACK);
        
        // Wait for animation duration (approx 1s or configurable)
        yield return new WaitForSeconds(1.0f);
        
        _isAttacking = false;
    }

    private void UpdateAnimationState()
    {
        if (_animator == null) return;
        
        // Priority: Attack
        if (_isAttacking) return; 

        // Determine Move State
        bool isMoving = false;
        
        // Check actual movement distance per frame or intent
        // Simple way: check velocity if using NavMesh, or manual check
        // Since we translate manually:
        // For Follow:
        if (_isFollowing && _playerTransform != null)
        {
             float dist = Vector3.Distance(transform.position, _playerTransform.position);
             if (dist > stopDistance) isMoving = true;
        }
        // For Patrol:
        else if (_isPatrolling)
        {
            float dist = Vector3.Distance(transform.position, _currentPatrolPoint);
            if (dist > 0.5f) isMoving = true;
        }

        string desiredState = isMoving ? ANIM_WALK : ANIM_IDLE;
        PlayAnimation(desiredState);
    }

    // Patrol Variables
    private Vector3 _currentPatrolPoint;
    private bool _isPatrolling = false;
    private float _patrolWaitTimer = 0f;
    [Header("Patrol Settings")]
    [SerializeField] private float patrolWaitTime = 3f;

    private void PatrolBehavior()
    {
        // If not patrolling or reached destination, wait then pick new point
        if (!_isPatrolling)
        {
            _patrolWaitTimer += Time.deltaTime;
            if (_patrolWaitTimer >= patrolWaitTime)
            {
                SetRandomPatrolPoint();
            }
        }
        else
        {
            // Move to patrol point
            float dist = Vector3.Distance(transform.position, _currentPatrolPoint);
            if (dist < 0.5f)
            {
                // Reached point
                _isPatrolling = false;
                _patrolWaitTimer = 0f;
            }
            else
            {
                Vector3 direction = (_currentPatrolPoint - transform.position).normalized;
                transform.position += direction * (moveSpeed * 0.5f) * Time.deltaTime; // Half speed for patrol
                
                // Look at point
                transform.LookAt(new Vector3(_currentPatrolPoint.x, transform.position.y, _currentPatrolPoint.z));
            }
        }
    }

    private void SetRandomPatrolPoint()
    {
        // Pick random point in Zone Radius around Home Position
        Vector2 randomCircle = Random.insideUnitCircle * zoneRadius;
        Vector3 potentialPoint = _homePosition + new Vector3(randomCircle.x, 0, randomCircle.y);
        
        _currentPatrolPoint = potentialPoint;
        _isPatrolling = true;
        Debug.Log($"[Healthcare] Patrol to: {_currentPatrolPoint}");
    }

    private void PlayAnimation(string newState)
    {
        if (_currentAnimState == newState) return;

        if (_animator != null)
        {
            _animator.CrossFade(newState, animTransitionTime);
            _currentAnimState = newState;
        }
    }

    private void FollowBehavior()
    {
        if (_playerTransform == null) return;

        // check if Player is within the Allowed Zone
        float distToZoneCenter = Vector3.Distance(_playerTransform.position, _homePosition);

        if (distToZoneCenter > zoneRadius)
        {
            // Player left the area -> Stop following
            StopFollowing();
            return;
        }

        // Move towards Player
        float distToPlayer = Vector3.Distance(transform.position, _playerTransform.position);
        
        if (distToPlayer > stopDistance)
        {
            Vector3 direction = (_playerTransform.position - transform.position).normalized;
            // Simple Translate 
            transform.position += direction * moveSpeed * Time.deltaTime;
            
            // Look at player
            transform.LookAt(new Vector3(_playerTransform.position.x, transform.position.y, _playerTransform.position.z));
        }
    }

    private void StopFollowing()
    {
        if (!_isFollowing) return;

        Debug.Log("[Healthcare] Player left the zone. Stopping.");
        
        // Revoke Bonus Life
        if (_playerTransform != null)
        {
            var hp = _playerTransform.GetComponent<Player.PlayerHealth>();
            if (hp != null) hp.RevokeBonusHealth(1);
        }

        _isFollowing = false;
        _playerTransform = null; 
    }

    public void Die()
    {
        Debug.Log("Healthcare Personnel Killed.");
        
        // Revoke Bonus Life if currently providing it
        if (_isFollowing && _playerTransform != null)
        {
             var hp = _playerTransform.GetComponent<Player.PlayerHealth>();
             if (hp != null) hp.RevokeBonusHealth(1);
        }

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
