using UnityEngine;
using UnityEngine.AI;

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
    private bool _isEnraged = false; // For Scorched Earth
    private Transform _playerTransform;
    private Vector3 _homePosition;
    
    // Animation
    private Animator _animator;
    private NavMeshAgent _agent; // Added for Scorched Earth control

    [Header("Animation Settings")]
    [SerializeField] private float animTransitionTime = 0.2f;
    private string _currentAnimState;
    private const string ANIM_IDLE = "HealthCareIdle";
    private const string ANIM_WALK = "HealthCareWalking";
    private const string ANIM_ATTACK = "HealthcareAttack";

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
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
        // 0. Scorched Earth Check
        if (ScorchedEarthManager.Instance != null && ScorchedEarthManager.Instance.IsActive)
        {
            if (!_isEnraged)
            {
                _isEnraged = true;
                if (_agent != null)
                {
                    _agent.speed = moveSpeed * 1.5f; // SPRINT
                    _agent.stoppingDistance = 0.5f; // Tighten
                    _agent.autoBraking = false;
                    _agent.acceleration = 20f;
                }
                Debug.Log($"[Healthcare] {name} ENRAGED! Bypassing all zones.");
            }

            if (_isFollowing) StopFollowing();
            _isPatrolling = false;

            HostileBehavior();
            UpdateAnimationState();
            return; // Skip normal behavior
        }
        else if (_isEnraged)
        {
            // Reset if scorched earth ends
            _isEnraged = false;
            if (_agent != null)
            {
                _agent.speed = moveSpeed;
                _agent.stoppingDistance = stopDistance;
                _agent.autoBraking = true;
            }
        }

        // 1. Detection Logic (Trigger Box)
        // Dynamic Mask Check: If following but player is now masked, stop.
        if (_isFollowing && _playerTransform != null)
        {
            Player.PlayerPickup pickup = _playerTransform.GetComponent<Player.PlayerPickup>();
            if (pickup != null && pickup.CurrentMaskType == Items.Masks.MaskType.Nurse)
            {
                Debug.Log("[Healthcare] Player donned mask while followed. Stopping.");
                StopFollowing();
            }
        }

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
                // Check for Nurse Mask or Government Mask (Immunity)
                Player.PlayerPickup pickup = hit.GetComponent<Player.PlayerPickup>();
                if (pickup != null)
                {
                    if (pickup.CurrentMaskType == Items.Masks.MaskType.Nurse || 
                        pickup.CurrentMaskType == Items.Masks.MaskType.Government)
                    {
                        // Ignore disguised player
                        continue; 
                    }
                }

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
                        if (_animator != null) _animator.Play("HealthcareAttack");
                        StartCoroutine(AttackCooldownRoutine());
                    }
                    
                    // Deal 1 damage
                    hp.TakeDamage(1); 
                    return; // Do not start following
                }

                // Normal Behavior: Grant Bonus Life
                hp.GrantBonusHealth(1);
                
                _isFollowing = true;
                
                // Destroy player's masks
                var pickup = _playerTransform.GetComponent<Player.PlayerPickup>();
                if (pickup != null)
                {
                    pickup.DestroyAllMasks();
                }

                Debug.Log("[Healthcare] Player entered trigger. Following started.");
            }
        }
    }

    private float _lastAttackTime = 0f;
    [SerializeField] private float attackCooldown = 2.0f;
    private bool _isAttacking = false;

    private System.Collections.IEnumerator AttackCooldownRoutine()
    {
        _isAttacking = true;
        // Animation is triggered directly via Play() in caller
        
        // Wait for animation duration (approx 1s)
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
        
        // 1. Check Agent first (Scorched Earth / Pathing)
        if (_agent != null && _agent.enabled && _agent.hasPath && _agent.velocity.magnitude > 0.1f)
        {
            isMoving = true;
        }
        else
        {
            // 2. Fallback for manual translation (Standard Follow/Patrol)
            if (_isFollowing && _playerTransform != null)
            {
                 float dist = Vector3.Distance(transform.position, _playerTransform.position);
                 if (dist > stopDistance) isMoving = true;
            }
            else if (_isPatrolling)
            {
                float dist = Vector3.Distance(transform.position, _currentPatrolPoint);
                if (dist > 0.5f) isMoving = true;
            }
            else if (ScorchedEarthManager.Instance != null && ScorchedEarthManager.Instance.IsActive && _playerTransform != null)
            {
                // Hostile manual fallback
                float dist = Vector3.Distance(transform.position, _playerTransform.position);
                if (dist > stopDistance) isMoving = true;
            }
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

    private void HostileBehavior()
    {
        // Re-acquire player if needed
        if (_playerTransform == null)
        {
             GameObject p = GameObject.FindGameObjectWithTag("Player");
             if (p != null) _playerTransform = p.transform;
        }
        
        if (_playerTransform == null) return; // No player found in level

        // Determine detection center (matches Green Sphere Gizmo)
        Vector3 detectCenter = transform.TransformPoint(detectionOffset);
        float distToZone = Vector3.Distance(detectCenter, _playerTransform.position);
        float distToPivot = Vector3.Distance(transform.position, _playerTransform.position);
        
        // Movement logic
        if (distToPivot > stopDistance)
        {
            // Chase Player manually (relentless, bypasses NavMesh freezing)
            Vector3 direction = (_playerTransform.position - transform.position).normalized;
            float currentSpeed = _isEnraged ? moveSpeed * 1.5f : moveSpeed;
            
            transform.position += direction * currentSpeed * Time.deltaTime;
            transform.LookAt(new Vector3(_playerTransform.position.x, transform.position.y, _playerTransform.position.z));
            
            if (_agent != null && _agent.enabled) _agent.isStopped = true; // Stay stopped while manual move takes over
        }
        else
        {
             if (_agent != null && _agent.enabled) _agent.isStopped = true;
        }

        // ATTACK CONDITION: Inside Detection Radius OR very close to pivot
        bool inDetectionZone = distToZone <= detectionRadius;
        bool tooCloseToPivot = distToPivot <= stopDistance;

        if ((inDetectionZone || tooCloseToPivot) && Time.time >= _lastAttackTime + attackCooldown)
        {
            _lastAttackTime = Time.time;
            
            // User Request: "call this animation... no need for transitions"
            if (_animator != null) 
            {
                _animator.Play("HealthcareAttack", 0, 0f); // Force play state
                _currentAnimState = ""; // Force animation refresh afterwards
            }
            
            StartCoroutine(AttackCooldownRoutine()); 

            Debug.Log($"[Healthcare] SCORCHED EARTH ATTACK! InZone: {inDetectionZone} | PivotDist: {distToPivot:F2}"); 
            
            var hp = _playerTransform.GetComponent<Player.PlayerHealth>();
            if (hp != null) hp.TakeDamage(1);
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
        // Instant Hide
        gameObject.SetActive(false);
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
