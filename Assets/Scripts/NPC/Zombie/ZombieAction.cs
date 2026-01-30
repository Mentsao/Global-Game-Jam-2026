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

    [Header("Scripts")]
    private ZombieNPCDetect zombieNPCDetect;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        zombieNPCDetect = GetComponent<ZombieNPCDetect>();
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
        // Handle Cooldown
        if (currentCooldown > 0)
        {
            currentCooldown -= Time.deltaTime;
        }

        if (zombieNPCDetect.target == null)
        {
            QTETimeLeft = QTETimeLimit;
            return;
        }
            
        // Always try to move to target unless QTE is active (canvas active)
        if (!canvas.activeSelf)
        {
            agent.isStopped = false;
            MoveToTarget();
            
            // Check for Physical Attack Trigger if not in cooldown
            if (currentCooldown <= 0)
            {
                TryAttackPlayer();
            }
        }
        else
        {
            // QTE is running, stop moving
            agent.isStopped = true;
            QTEAttack();
            QTETimeLeft -= Time.deltaTime;
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
