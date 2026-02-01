using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Player;
using Unity.VisualScripting;
using TMPro;

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
    public List<GameObject> npcLine = new List<GameObject>();
    [SerializeField] private Transform lineStartPoint;
    [SerializeField] private float spacing = 2f;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float lineTime = 5f; // Wait time before clearing line
    [SerializeField] private float timer;
    public bool hasEntered = false;
    private PlayerPickup playerPickup;
    private int count = 0;


    [Header("Patrol Settings")]
    [SerializeField] private float patrolRadius = 15f;
    [SerializeField] private float patrolWaitTime = 3f;
    private float _patrolTimer;

    [Header("Interaction")]
    [SerializeField] private float interactionDistance = 2f;
    [SerializeField] private GameObject dialogue;
    private TextMeshProUGUI dialogueText;

    private AudioSource audioSource;

    // Internal
    private string _currentAnimState = "";

    private void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        dialogue = GameObject.Find("DialogueText");
        dialogueText = dialogue.GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        FindPlayer();

        // Setup 3D Audio via AudioManager
        audioSource = gameObject.AddComponent<AudioSource>();
        
        // Start Waiting Loop if in that state
        if (currentState == PoliceState.WaitingInLine && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySpatialPoliceLoop(audioSource);
        }
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
        if (playerPickup.isWeapon)
        {
            count++;
        }

        if (count <= 0) return;

        if (!hasEntered) return;

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
            hasEntered = false;
            // Debug.Log("[PoliceNPC] Line Empty! Switching to CHASE.");
            // Debug.Log("[PoliceNPC] Line Empty! Switching to CHASE.");
            currentState = PoliceState.Chasing;
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            if (AudioManager.Instance != null) 
            {
                AudioManager.Instance.PlayPoliceDetect();
            }
        }
    }

    private void HandleChaseLogic()
    {
        if (player == null) return;

        // Check for Police Mask OR Government Mask (Immunity)
        PlayerPickup pickup = player.GetComponent<PlayerPickup>();
        if (pickup != null && (pickup.CurrentMaskType == Items.Masks.MaskType.Police || pickup.CurrentMaskType == Items.Masks.MaskType.Government))
        {
            // Player is disguised! Stop chasing.
            Debug.Log("[Police] Player is disguised as Police. Switching to Patrol.");
            currentState = PoliceState.Patrolling;
            agent.ResetPath();
            return;
        }

        agent.isStopped = false;
        agent.stoppingDistance = 5f; 
        agent.SetDestination(player.position);

        // Optional: Manual Distance Check for Interaction
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= agent.stoppingDistance)
        {
            animator.Play("Idle");
            // We are close enough to "Touch"
            CheckForDocument(player.gameObject);
        }
        else
        {
            animator.Play("Running");
            agent.SetDestination(player.position);
        }
    }

    private void HandlePatrolLogic()
    {
        // 1. Dynamic Mask Check: If player is unmasked and nearby, resume chase
        if (player != null && playerPickup != null)
        {
            // Resume chase only if NOT Police AND NOT Government
            if (playerPickup.CurrentMaskType != Items.Masks.MaskType.Police && playerPickup.CurrentMaskType != Items.Masks.MaskType.Government)
            {
                float dist = Vector3.Distance(transform.position, player.position);
                if (dist < patrolRadius)
                {
                    Debug.Log("[Police] Player uncovered! Resuming chase.");
                    currentState = PoliceState.Chasing;
                    return;
                }
            }
        }

        // 2. Random Roam
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
        if (p != null)
        {
            player = p.transform;
            playerPickup = p.GetComponent<PlayerPickup>();
        }
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
                        dialogueText.text = "Police: Document Verified! Switching to Patrol.";
                        Animator dialogueAnim = dialogue.GetComponent<Animator>();
                        dialogueAnim.SetTrigger("DialogueTrigger");
                        AudioManager.Instance.PlayPoliceDecision(true);
                        currentState = PoliceState.Patrolling; 
                        agent.ResetPath(); // Stop chasing immediately
                        pickup.ConsumeHeldItem();
                    }
                    else
                    {
                        dialogueText.text = "Police: Wrong Item! (Need 'Document' tag)";
                        Animator dialogueAnim = dialogue.GetComponent<Animator>();
                        dialogueAnim.SetTrigger("DialogueTrigger");
                        AudioManager.Instance.PlayPoliceDecision(false);
                    }
                }
                else
                {
                    dialogueText.text = "Police: Show me your papers! (Hands Empty)";
                    Animator dialogueAnim = dialogue.GetComponent<Animator>();
                    dialogueAnim.SetTrigger("DialogueTrigger");
                }
            }
        }
    }
}
