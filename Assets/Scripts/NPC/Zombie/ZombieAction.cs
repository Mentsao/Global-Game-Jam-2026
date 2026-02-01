using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using UnityEngine.InputSystem; // Added for New Input System

public class ZombieActions : MonoBehaviour
{

    [Header("Zombie")]
    private NavMeshAgent agent;
    [SerializeField] private bool destinationReached = false;
    [SerializeField] private float QTETimeLimit = 5f; // Reference value
    [SerializeField] private float QTETimeLeft = 5f;
    [SerializeField] private float attackCooldown = 6f;
    private float currentCooldown = 0f;
    [SerializeField] private float blowBackDist = 7f;
    
    [Header("Attack Settings")]
    [SerializeField] private float attackRadius = 1.0f;
    [SerializeField] private float attackOffset = 0.5f;
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
    private GameObject _qteTarget; // Cached target for QTE
    private int _tapCount = 0; // New discrete counter for stable QTE

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        zombieNPCDetect = GetComponent<ZombieNPCDetect>();
        _animator = GetComponentInChildren<Animator>();

        // Check if canvas is assigned
        if (canvas != null)
        {
            // If the canvas is a Prefab Asset (not in the scene), instantiate it
            if (!canvas.scene.IsValid())
            {
                GameObject canvasInstance = Instantiate(canvas, transform);
                canvas = canvasInstance;
                // Update slider reference from the new instance
                slider = canvas.GetComponentInChildren<Slider>();
            }
            
            canvas.SetActive(false);
        }
        
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
        if (canvas != null && canvas.activeSelf)
        {
             // QTE is running, stop moving
            if (agent != null) agent.isStopped = true;
            QTEAttack(_qteTarget); 
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
                Debug.Log($"[Zombie] Grabbed: {hit.name}!"); 
                QTEAttack(hit.gameObject); // Passing the player object explicitly
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

    public void QTEAttack(GameObject playerObj = null)
    {
        // 1. Initialize QTE if it hasn't started yet
        if (canvas != null && !canvas.activeSelf)
        {
            if (playerObj != null && playerObj.CompareTag("Player"))
            {
                // Check if player is already grappled by another zombie
                var playerMovement = playerObj.GetComponent<Player.PlayerMovement>();
                if (playerMovement != null)
                {
                    if (playerMovement.IsGrappled) return; // Already busy
                    
                    playerMovement.IsGrappled = true;
                    playerMovement.SetControlActive(false);
                }

                Debug.Log("[Zombie] Initializing QTE UI");
                _qteTarget = playerObj; // Cache it!
                Debug.Log("[Zombie] Initializing QTE UI");
                
                // Ensure TimeLimit is sane (inspector could be 0)
                if (QTETimeLimit < 1f) QTETimeLimit = 5f;
                QTETimeLeft = QTETimeLimit; // Explicitly reset timer on Start
                
                canvas.SetActive(true);
                if (slider != null) 
                {
                    slider.wholeNumbers = false; 
                    slider.minValue = 0f;
                    slider.maxValue = 1f;
                    // slider.value = 0.25f; // My previous tuning
                    // Reference doesn't explicitly set start value in QTEAttack, but usually sliders start at 0 or inspector value?
                    // User's reference script: "if (zombieNPCDetect.target.CompareTag("Player")) canvas.SetActive(true);" 
                    // It doesn't set slider.value! It just starts where it is.
                    // But standard QTE usually starts low. I'll keep 0.25 or set to 0? 
                    // Reference says "slider.value -= ...".
                    // I will stick to a sane default or what I had, but maybe 0 is better if they want pure mash?
                    // Let's keep 0.25 to give a chance? Or match reference exactly? 
                    // Reference script line 208: "slider.value = 0.35f;" WAIT, I see it in their text request trace!
                    // Line 208 in their REQUEST snippet says: "slider.value = 0.35f;"
                    // Wait, looking at the snippet in Step 65: "slider.value = 0.35f; // Start with some progress"
                    // Looking at the snippet in Step 193: "if (zombieNPCDetect.target.CompareTag("Player")) { canvas.SetActive(true); }" -> NO slider set.
                    // I will check Step 193 snippet closely.
                    // Step 193 snippet DOES NOT set slider.value on init.
                    // However, relying on previous inspector value is risky. I'll set it to 0.35f (from Step 65 reference) or 0.
                    // Let's set to 0.
                    // Actually, if I look at Step 193 snippet, it says: "slider.value -= 0.2f..."
                    // If it starts at 0, it goes negative immediately.
                    // I will set it to 0.35f as a safe middle ground based on previous context, or 0.
                    // Let's stick to 0.35f as it was in the ORIGINAL original script.
                    slider.value = 0f; // Start at 0%
                    _tapCount = 0; // Reset taps
                    Debug.Log($"[Zombie] QTE Started. TimeLimit: {QTETimeLimit}");
                } 
                
                // Stop Zombie
                if (agent != null) agent.isStopped = true;
            }
            else
            {
                return;
            }
            
            // Return here to prevent checking win/loss condition on the very first frame of initialization
            return;
        }
        else if (canvas == null)
        {
            Debug.LogError("[Zombie] QTE Canvas is MISSING on " + gameObject.name);
            return;
        }

        // QTE Logic - NO DECAY, just display taps
        // float decayAmount = 0.05f * Time.deltaTime; // REMOVED DECAY
        // slider.value -= decayAmount;
        if (slider.value <= 1 && QTETimeLeft <= 0)
        {
            Debug.Log($"[Zombie] QTE Failed! Slider: {slider.value}, Time: {QTETimeLeft}");
            
            // Kill Player
             // Use PlayerHealth instead to trigger death sequence
            var health = _qteTarget.GetComponent<Player.PlayerHealth>(); // Fix NRE: Use _qteTarget
            if (health != null)
            {
                health.TakeDamage(1000); // Massive damage to ensure kill
            }
            else
            {
                // Fallback if no health script
                Destroy(_qteTarget);
            }
            
            EndQTE();
            return;
        }

        playerObj = _qteTarget; // Ensure we use the cached target for logic below
        
        // DEBUG: Check if target is null (which would block input)
        if (playerObj == null) 
        {
             if (Time.frameCount % 30 == 0) Debug.LogError($"[ZombieQTE] PlayerObj is NULL! Input blocked. _qteTarget: {_qteTarget}");
             return;
        }

        // Tap Mechanics - Support both Legacy and New Input System
        bool fPressed = false;
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame) fPressed = true;
        else if (Input.GetKeyDown(KeyCode.F)) fPressed = true;
        
        // DEBUG: Print input status
        if (fPressed || Time.frameCount % 10 == 0) 
            Debug.Log($"[ZombieQTE] Slider: {slider.value:F4} | Pressed: {fPressed} | Target: {playerObj.name}");
        
        if (fPressed)
        {
            _tapCount++;
            slider.value = _tapCount / 3f; // 1 tap = 0.33, 2 taps = 0.66, 3 taps = 1.0
            
            // Win Condition
            if (_tapCount >= 3)
            {
                Debug.Log("Player Escaped!");
                agent.Move(-transform.forward * blowBackDist);
                // Also can apply small stun here if desired
                EndQTE();
            }
        }
    }

    private void EndQTE()
    {
        // Re-enable Player Controls
        // Need to check null in case player was destroyed
        if (_qteTarget != null) // Use cached target
        {
            var playerMovement = _qteTarget.GetComponent<Player.PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.SetControlActive(true);
                playerMovement.IsGrappled = false; // Release grapple lock
            }
        }

        canvas.SetActive(false);
        slider.value = 0;
        QTETimeLeft = QTETimeLimit; // Reset to the configured limit
        destinationReached = false;
        _qteTarget = null; // Clear cache
        
        // Start Cooldown
        currentCooldown = attackCooldown;
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector3 center = transform.position + transform.forward * attackOffset;
        Gizmos.DrawWireSphere(center, attackRadius);
    }
    
    private void OnDestroy()
    {
        // Safety check: ensure player is released if Zombie is destroyed during QTE
        if (_qteTarget != null)
        {
            EndQTE();
        }
    }

}
