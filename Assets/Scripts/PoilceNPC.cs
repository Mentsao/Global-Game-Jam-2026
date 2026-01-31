using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Player; 

// Make sure your Filename is "PoilceNPC.cs" (typo preserved to match file)
public class PoliceNPC : MonoBehaviour
{
    public enum PoliceState
    {
        WaitingInLine,
        Chasing,
        Patrolling
    }

    [Header("State")]
    [SerializeField] private PoliceState currentState = PoliceState.WaitingInLine;
    [SerializeField] private bool debugForceChase = false;

    [Header("References")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform player;

    [Header("Line Settings")]
    [SerializeField] private List<GameObject> npcLine = new List<GameObject>();
    [SerializeField] private Transform lineStartPoint;
    [SerializeField] private float spacing = 2f;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float lineTime = 5f; // Wait time before clearing line
    [SerializeField] private float timer;

    [Header("Patrol Settings")]
    [SerializeField] private float patrolRadius = 15f;
    [SerializeField] private float patrolWaitTime = 3f;
    private float _patrolTimer;

    [Header("Interaction")]
    [SerializeField] private float interactionDistance = 2.0f;

    // Internal
    private string _currentAnimState = "";

    private void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        FindPlayer();
    }

    private void Update()
    {
        // 1. Ensure Player Reference
        if (player == null) FindPlayer();

        // 2. Logic based on State
        switch (currentState)
        {
            case PoliceState.WaitingInLine:
                HandleLineLogic();
                break;

            case PoliceState.Chasing:
                HandleChaseLogic();
                break;

            case PoliceState.Patrolling:
                HandlePatrolLogic();
                break;
        }

        // 3. Update Animations
        UpdateAnimations();
    }

    // --- LOGIC HANDLERS ---

    private void HandleLineLogic()
    {
        // Waiting for line to clear
        timer += Time.deltaTime;

        if (timer >= lineTime)
        {
            RemoveOneNPC();
            timer = 0f;
        }

        UpdateLinePositions();

        // Check Exit Condition
        if (npcLine.Count == 0 || debugForceChase)
        {
            // Debug.Log("[PoliceNPC] Line Empty! Switching to CHASE.");
            currentState = PoliceState.Chasing;
        }
    }

    private void HandleChaseLogic()
    {
        if (player == null) return;

        agent.isStopped = false;
        agent.stoppingDistance = 0.8f; 
        agent.SetDestination(player.position);

        // Optional: Manual Distance Check for Interaction
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= interactionDistance)
        {
             // We are close enough to "Touch"
             CheckForDocument(player.gameObject);
        }
    }

    private void HandlePatrolLogic()
    {
        // Random Roam
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

    // --- HELPER FUNCTIONS ---

    private void FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    private void RemoveOneNPC()
    {
        if (npcLine.Count > 0)
        {
            GameObject npc = npcLine[0];
            npcLine.RemoveAt(0);
            if(npc != null) Destroy(npc);
        }
    }

    private void UpdateLinePositions()
    {
        // Visual Only - Move props in line
        if (lineStartPoint == null) return;

        for (int i = 0; i < npcLine.Count; i++)
        {
            if (npcLine[i] == null) continue;
            Vector3 target = lineStartPoint.position + lineStartPoint.forward * (-spacing * i);
            npcLine[i].transform.position = Vector3.Lerp(npcLine[i].transform.position, target, moveSpeed * Time.deltaTime);
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

    // --- ANIMATIONS ---

    private void UpdateAnimations()
    {
        if (animator == null) return;
        if (agent == null) return;

        string desiredState = "Idle";
        float speed = agent.velocity.magnitude;

        // Threshold for moving
        if (speed > 0.1f)
        {
            // Decide specific run/walk based on Logic State
            if (currentState == PoliceState.Chasing)
            {
                desiredState = "Running";
            }
            else if (currentState == PoliceState.Patrolling)
            {
                desiredState = "Walking";
            }
            else 
            {
                // Waiting in line or other movement
                 desiredState = "Walking"; // Default move
            }
        }
        else
        {
            desiredState = "Idle";
        }

        // Apply
        if (_currentAnimState != desiredState)
        {
            // Debug.Log($"[PoliceNPC] Playing: {desiredState}");
            animator.Play(desiredState);
            _currentAnimState = desiredState;
        }
    }

    // --- INTERACTION ---

    private void OnCollisionEnter(Collision collision)
    {
        CheckForDocument(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        CheckForDocument(other.gameObject);
    }

    public void CheckForDocument(GameObject obj)
    {
        // Only care if we are Chasing (inspecting). 
        // If already Patrolling, we ignore calls.
        if (currentState == PoliceState.Patrolling) return;

        if (obj.CompareTag("Player"))
        {
            PlayerPickup pickup = obj.GetComponent<PlayerPickup>();
            if (pickup == null) pickup = obj.GetComponentInParent<PlayerPickup>();

            if (pickup != null)
            {
                if (pickup.HeldItem != null)
                {
                    bool isDoc = pickup.HeldItem.CompareTag("Document");
                    if (isDoc)
                    {
                        // Success!
                        Debug.Log("Police: Document Verified! Switching to Patrol.");
                        pickup.ConsumeHeldItem();
                        AudioManager.Instance.PlayPoliceDecision(true);
                        currentState = PoliceState.Patrolling; 
                        agent.ResetPath(); // Stop chasing immediately
                    }
                    else
                    {
                        Debug.Log("Police: Wrong Item! (Need 'Document' tag)");
                        AudioManager.Instance.PlayPoliceDecision(false);
                    }
                }
                else
                {
                    Debug.Log("Police: Show me your papers! (Hands Empty)");
                }
            }
        }
    }
}
