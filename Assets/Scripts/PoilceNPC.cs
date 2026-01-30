using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Enemy;
using Player; 

public class PoliceNPC : EnemyPresence
{
    [SerializeField] private Transform player;
    [SerializeField] private float stopDistance = 1.5f;
    [SerializeField] private List<GameObject> npcLine = new List<GameObject>();

    [SerializeField] private Transform lineStartPoint;
    [SerializeField] private float spacing = 2f;
    [SerializeField] private float moveSpeed = 5f;

    [SerializeField] private float lineTime = 60f;

    [SerializeField] private float timer;
    [SerializeField] private NavMeshAgent agent;

    private bool _isPatrolling = false;
    private float _patrolTimer = 0f;
    private float _patrolWaitTime = 3f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }
    private void Update()
    {
        if (_isPatrolling)
        {
            PatrolBehavior();
            return;
        }

        HandleTimer();
        UpdateNPCPositions();
        if (npcLine.Count == 0)
        {
            // Debug.Log("Next in Line"); // Spammy
            FollowPlayer();
        }
    }

    [SerializeField] private float interactionDistance = 2.0f;

    private void FollowPlayer()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

            if (playerObj == null)
                return;

            player = playerObj.transform;
        }

        agent.isStopped = false;
        agent.stoppingDistance = 0.5f; // Get closer
        agent.SetDestination(player.position);

        // Proximity Check
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= interactionDistance)
        {
            CheckForDocument(player.gameObject);
        }
    }

    private void HandleTimer()
    {
        if (npcLine.Count == 0)
                return;

        timer += Time.deltaTime;

        if (timer >= lineTime)
        {
            RemoveOneNPC();
            timer = 0f;
        }
    }

    private void RemoveOneNPC()
    {
        GameObject npc = npcLine[0];
        npcLine.RemoveAt(0);
        Destroy(npc);
    }

    private void UpdateNPCPositions()
    {
        for (int i = 0; i < npcLine.Count; i++)
        {
            Vector3 targetPos = lineStartPoint.position + lineStartPoint.forward * (-spacing * i);

            npcLine[i].transform.position = Vector3.Lerp(npcLine[i].transform.position,targetPos, moveSpeed * Time.deltaTime);
        }
    }

    // --- Interaction Logic ---

    private void OnCollisionEnter(Collision collision)
    {
        CheckForDocument(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Also check triggers in case the collider setup is different
        CheckForDocument(other.gameObject);
    }

    public void CheckForDocument(GameObject obj)
    {
        // Debug Log to see what we hit
        Debug.Log($"Police collided with: {obj.name} (Tag: {obj.tag})");

        if (obj.CompareTag("Player"))
        {
            PlayerPickup pickup = obj.GetComponent<PlayerPickup>();
            if (pickup == null)
            {
                // Try searching in parent in case collision was with a child collider
                pickup = obj.GetComponentInParent<PlayerPickup>();
            }

            if (pickup != null)
            {
                if (pickup.HeldItem != null)
                {
                    Debug.Log($"Player is holding: {pickup.HeldItem.name} | Tag: {pickup.HeldItem.tag} | Layer: {LayerMask.LayerToName(pickup.HeldItem.gameObject.layer)}");

                    // Check Tag AND Layer
                    bool tagMatch = pickup.HeldItem.CompareTag("Document");
                    bool layerMatch = pickup.HeldItem.gameObject.layer == LayerMask.NameToLayer("Document");

                    if (tagMatch && layerMatch)
                    {
                        Debug.Log("Police: Document verified. Moving along.");
                        pickup.ConsumeHeldItem(); // Consume the document
                        StartPatrol();
                    }
                    else
                    {
                        Debug.Log($"Police: Invalid Item. Tag Match: {tagMatch}, Layer Match: {layerMatch}. YOU DIED.");
                    }
                }
                else
                {
                    Debug.Log("Police: Hands empty! YOU DIED (No Document).");
                }
            }
            else
            {
                Debug.Log("Police: Could not find PlayerPickup script on player!");
            }
        }
    }

    private void StartPatrol()
    {
        _isPatrolling = true;
        agent.stoppingDistance = 0f;
        SetRandomDestination();
    }

    private void PatrolBehavior()
    {
        // Basic random patrol
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            _patrolTimer += Time.deltaTime;
            if (_patrolTimer >= _patrolWaitTime)
            {
                SetRandomDestination();
                _patrolTimer = 0f;
            }
        }
    }

    private void SetRandomDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * 20f;
        randomDirection += transform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, 20f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }
}
