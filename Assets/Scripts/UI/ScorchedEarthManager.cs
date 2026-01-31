using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class ScorchedEarthManager : MonoBehaviour
{
    public static ScorchedEarthManager Instance { get; private set; }

    [Header("Settings")]
    [Tooltip("Radius around the player to check for NPCs when Scorched Earth starts.")]
    [SerializeField] private float detectionRadius = 50f; 
    [Tooltip("Minimum distance player must move to create a new breadcrumb.")]
    [SerializeField] private float breadcrumbDistance = 2.0f;
    [Tooltip("Distance at which NPC considers a breadcrumb 'reached' and moves to next.")]
    [SerializeField] private float arrivalDistance = 2.0f;
    [Tooltip("Time in seconds to wait at each breadcrumb before moving to the next.")]
    [SerializeField] private float waypointWaitTime = 0.0f;
    [SerializeField] private LayerMask npcLayer = ~0; // Default to all, filter later

    [Header("Debug")]
    [SerializeField] private bool isScorchedEarthActive = false;
    [SerializeField] private List<Vector3> breadcrumbs = new List<Vector3>();

    // Track which node each swarming NPC is currently targeting
    private Dictionary<NavMeshAgent, int> swarmAgents = new Dictionary<NavMeshAgent, int>();
    // Track when an agent ARRIVED at their current waypoint (for waiting)
    private Dictionary<NavMeshAgent, float> swarmWaitTimers = new Dictionary<NavMeshAgent, float>();
    
    private Transform _playerTransform;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _playerTransform = p.transform;
    }

    private void Update()
    {
        if (!isScorchedEarthActive || _playerTransform == null) return;

        // 1. Record Breadcrumbs
        UpdateBreadcrumbs();

        // 2. Command Swarm
        UpdateSwarmMovement();
    }

    public void TriggerScorchedEarth()
    {
        if (isScorchedEarthActive) return; // Already active

        Debug.Log("[Scorched Earth] SYSTEM ACTIVATED! SWARM INITIATED.");
        isScorchedEarthActive = true;
        
        // Clear previous if any (though usually one-time event per stealth fill?)
        breadcrumbs.Clear();
        swarmAgents.Clear();
        swarmWaitTimers.Clear();

        // Add initial point (Point A)
        if (_playerTransform != null) breadcrumbs.Add(_playerTransform.position);

        // Find NPCs
        WakeUpNPCs();

        // Trigger Visuals
        if (UI.VignetteEffect.Instance != null)
        {
            UI.VignetteEffect.Instance.SetForcedDanger(true);
        }
    }

    private void WakeUpNPCs()
    {
        if (_playerTransform == null) return;

        Collider[] hits = Physics.OverlapSphere(_playerTransform.position, detectionRadius, npcLayer);
        foreach (var hit in hits)
        {
            // Filter: Must represent an NPC
            // Exclude Government
            if (hit.GetComponent<GovernmentOfficial>() != null) continue;
            if (hit.GetComponentInParent<GovernmentOfficial>() != null) continue;

            // Must have NavMeshAgent
            NavMeshAgent agent = hit.GetComponent<NavMeshAgent>();
            if (agent == null) agent = hit.GetComponentInParent<NavMeshAgent>();

            if (agent != null && !swarmAgents.ContainsKey(agent))
            {
                // Add to swarm, targeting index 0 (Start Point)
                swarmAgents.Add(agent, 0);
                
                // Aggressive setting
                agent.speed *= 1.5f; // Enrage speed
                agent.stoppingDistance = 0.5f;
                agent.autoBraking = false; // continuous movement
            }
        }
        
        Debug.Log($"[Scorched Earth] Swarm Size: {swarmAgents.Count}");
    }

    private void UpdateBreadcrumbs()
    {
        if (breadcrumbs.Count == 0) return;

        Vector3 lastPoint = breadcrumbs[breadcrumbs.Count - 1];
        float dist = Vector3.Distance(_playerTransform.position, lastPoint);

        if (dist >= breadcrumbDistance)
        {
            breadcrumbs.Add(_playerTransform.position);
        }
    }

    private void UpdateSwarmMovement()
    {
        List<NavMeshAgent> toRemove = new List<NavMeshAgent>();
        // Agents that are ready to advance index
        List<NavMeshAgent> toIncrement = new List<NavMeshAgent>();

        foreach (var kvp in swarmAgents)
        {
            NavMeshAgent agent = kvp.Key;
            int targetIndex = kvp.Value;

            if (agent == null || !agent.gameObject.activeInHierarchy)
            {
                toRemove.Add(agent);
                continue;
            }

            // --- MOVEMENT LOGIC ---
            if (targetIndex < breadcrumbs.Count)
            {
                // Check if currently Waiting
                if (swarmWaitTimers.ContainsKey(agent))
                {
                    // Check timer
                    if (Time.time > swarmWaitTimers[agent] + waypointWaitTime)
                    {
                        // Wait Finished -> Remove Timer, Add to Increment List
                        swarmWaitTimers.Remove(agent);
                        toIncrement.Add(agent);
                    }
                    // Else: Still waiting, do nothing (agent should stay or arguably we loop anim)
                    continue; 
                }

                // If NOT Waiting -> Move
                Vector3 targetPos = breadcrumbs[targetIndex];
                
                // Only set destination if significantly different to reduce API calls
                if (Vector3.Distance(agent.destination, targetPos) > 1.0f)
                {
                    agent.SetDestination(targetPos);
                }

                // Check Arrival
                if (!agent.pathPending && agent.remainingDistance < arrivalDistance)
                {
                    // Arrived at current breadcrumb.
                    // Decide: Wait or Move?
                    if (waypointWaitTime > 0.05f) 
                    {
                         // Start Wait
                         swarmWaitTimers[agent] = Time.time;
                         // (Optional) Stop moving while waiting?
                         agent.SetDestination(agent.transform.position); 
                    }
                    else
                    {
                        // No logic to wait, move immediately
                        toIncrement.Add(agent);
                    }
                }
            }
            else
            {
                // Caught up to latest breadcrumb -> Chase Player directly
                agent.SetDestination(_playerTransform.position);
            }
        }

        // Apply removals (dead agents)
        foreach (var r in toRemove) 
        {
             swarmAgents.Remove(r);
             if (swarmWaitTimers.ContainsKey(r)) swarmWaitTimers.Remove(r);
        }

        // Apply index updates
        foreach (var agent in toIncrement)
        {
            if (swarmWaitTimers.ContainsKey(agent)) swarmWaitTimers.Remove(agent); // Safety remove
            swarmAgents[agent]++; // Move to next breadcrumb
        }
    }


    private void OnDrawGizmos()
    {
        // Auto-find player for Gizmo visualization if missing
        if (_playerTransform == null)
        {
             GameObject p = GameObject.FindGameObjectWithTag("Player");
             if (p != null) _playerTransform = p.transform;
        }
        
        if (_playerTransform != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 1f); // Solid Orange Wire
            Gizmos.DrawWireSphere(_playerTransform.position, detectionRadius);
             
             // Draw Breadcrumbs
            if (isScorchedEarthActive)
            {
                Gizmos.color = Color.red;
                for (int i = 0; i < breadcrumbs.Count - 1; i++)
                {
                    Gizmos.DrawLine(breadcrumbs[i], breadcrumbs[i+1]);
                    Gizmos.DrawSphere(breadcrumbs[i], 0.3f);
                }
                if (breadcrumbs.Count > 0) Gizmos.DrawSphere(breadcrumbs[breadcrumbs.Count-1], 0.3f);
            }
        }
    }
}
