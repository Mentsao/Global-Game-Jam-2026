using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using UnityEngine.InputSystem; // Added for New Input System

public class ZombieActions : MonoBehaviour
{
    [Header("Zombie")]
    private NavMeshAgent agent;
    [SerializeField] private bool destinationReached = false;
    [SerializeField] private float QTETimeLimit = 10f; // Increased to 10s
    [SerializeField] private float QTETimeLeft = 10f;
    [SerializeField] private float attackCooldown = 6f;
    private float currentCooldown = 0f;
    [SerializeField] private float blowBackDist = 7f;
    
    [Header("Attack Settings")]
    [SerializeField] private float attackRadius = 1.0f;
    [SerializeField] private float attackOffset = 1.0f;
    [SerializeField] private LayerMask attackLayer;

    [Header("Canvas")]
    [SerializeField] private GameObject canvas;
    [SerializeField] private Slider slider;

    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float runSpeed = 6f;
    [SerializeField] private float patrolRadius = 10f;
    [SerializeField] private float patrolWaitTime = 3f;
    private float _patrolTimer;

    private ZombieNPCDetect zombieNPCDetect;
    private Animator _animator;
    private string _currentAnimState = "";

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        zombieNPCDetect = GetComponent<ZombieNPCDetect>();
        _animator = GetComponentInChildren<Animator>();

        // Check if canvas is assigned to avoid null ref if user forgot, but it's SerializedField so should be fine if set in inspector
        if (canvas != null) canvas.SetActive(false);
        
        // Default attack layer to 'Default' or 'Player' if not set, to avoid failing silently
        if (attackLayer == 0) attackLayer = LayerMask.GetMask("Default", "Player", "Ignore Raycast"); 

        // Ensure the zombie gets close enough to attack
        if (agent.stoppingDistance > attackRadius)
        {
            agent.stoppingDistance = attackRadius * 0.8f; 
        }
    }

    private void Update()
    {
        UpdateAnimations();

        // Handle Cooldown
        if (currentCooldown > 0)
        {
            currentCooldown -= Time.deltaTime;
        }

        // QTE Logic (High Priority)
        if (canvas.activeSelf)
        {
             // QTE is running, stop moving
            agent.isStopped = true;
            QTEAttack();
            QTETimeLeft -= Time.deltaTime;
            return; // Skip other movement logic
        }

        // Behavior Logic
        if (zombieNPCDetect.target != null)
        {
             // CHASE
             QTETimeLeft = QTETimeLimit;
             
             // Run Speed
             agent.speed = runSpeed;
             agent.isStopped = false;

             MoveToTarget();

             // Check for Attack
             if (currentCooldown <= 0)
             {
                 TryAttackPlayer();
             }
        }
        else
        {
             // PATROL
             agent.speed = walkSpeed; // Walk Speed
             PatrolBehavior();
        }

        // Legacy destinationReached logic checks
        if (agent.hasPath && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            destinationReached = true;
        }
        else
        {
            destinationReached = false;
        }
    }

    private void PatrolBehavior()
    {
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            _patrolTimer += Time.deltaTime;
            if (_patrolTimer >= patrolWaitTime)
            {
                SetRandomDestination();
                _patrolTimer = 0f;
            }
        }
    }

    private void SetRandomDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += transform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    [SerializeField] private float animTransitionTime = 0.25f;

    private void UpdateAnimations()
    {
        if (_animator == null) return;
        
        string newState = "ZombieIdle";

        // Priority 1: Attacking (QTE)
        if (canvas.activeSelf)
        {
            newState = "ZombieAttack";
        }
        else
        {
            // Priority 2: Movement
            float speed = agent.velocity.magnitude;
            if (speed > 0.1f)
            {
                if (speed > 3.5f) // Tuning threshold
                {
                    newState = "ZombieRunning";
                }
                else
                {
                    newState = "ZombieWalking";
                }
            }
            else
            {
                newState = "ZombieIdle";
            }
        }

        if (_currentAnimState != newState)
        {
            // CrossFade blends the new animation over 'animTransitionTime' seconds
            _animator.CrossFade(newState, animTransitionTime);
            _currentAnimState = newState;
        }
    }

    private void TryAttackPlayer()
    {
        Vector3 center = transform.position + transform.forward * attackOffset;
        Collider[] hitColliders = Physics.OverlapSphere(center, attackRadius, attackLayer);

        foreach (var hit in hitColliders)
        {
            if (hit.CompareTag("Player"))
            {
                Debug.Log("Zombie Grabbed Player!"); 
                QTEAttack();
                return; // Start attack immediately
            }
        }
    }

    public void MoveToTarget()
    {
        if (zombieNPCDetect.target != null && zombieNPCDetect.inFront && zombieNPCDetect.inFOV && zombieNPCDetect.inRange)
        {
            agent.SetDestination(zombieNPCDetect.target.transform.position);
        }
    }

    public void QTEAttack()
    {
        // Initialize QTE if it hasn't started yet (Canvas check is a simple way to know)
        if (!canvas.activeSelf)
        {
            if (zombieNPCDetect.target.CompareTag("Player"))
            {
                canvas.SetActive(true);
                slider.value = 0.35f; // Start with some progress
                
                // Disable Player Controls
                var playerMovement = zombieNPCDetect.target.GetComponent<Player.PlayerMovement>();
                if (playerMovement != null)
                {
                    playerMovement.SetControlActive(false);
                }
            }
            else
            {
                return;
            }
        }

        // QTE Logic
        slider.value -= 0.15f * Time.deltaTime; // Reduced drain rate (easier)

        // Lose Condition: Timer runs out OR slider hits 0
        if ((slider.value <= 1 && QTETimeLeft <= 0) || slider.value <= 0)
        {
            // Kill Player
            Destroy(zombieNPCDetect.target);
            EndQTE();
            return;
        }

        // Tap Mechanics - Support both Legacy and New Input System
        bool fPressed = false;
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame) fPressed = true;
        else if (Input.GetKeyDown(KeyCode.F)) fPressed = true;

        if (fPressed)
        {
            slider.value += 0.1f; // Increased progress per tap (easier)
            
            // Win Condition
            if (slider.value >= 1)
            {
                Debug.Log("Player Escaped!");
                agent.Move(-transform.forward * blowBackDist);
                agent.ResetPath(); 
                EndQTE();
            }
        }
    }

    private void EndQTE()
    {
        // Re-enable Player Controls
        // Need to check null in case player was destroyed
        if (zombieNPCDetect.target != null)
        {
            var playerMovement = zombieNPCDetect.target.GetComponent<Player.PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.SetControlActive(true);
            }
        }

        canvas.SetActive(false);
        slider.value = 0;
        QTETimeLeft = QTETimeLimit; // Reset to the configured limit
        destinationReached = false;
        
        // Start Cooldown
        currentCooldown = attackCooldown;
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector3 center = transform.position + transform.forward * attackOffset;
        Gizmos.DrawWireSphere(center, attackRadius);
    }

}
