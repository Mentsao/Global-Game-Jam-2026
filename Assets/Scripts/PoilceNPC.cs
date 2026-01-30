using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PoliceNPC : MonoBehaviour
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

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }
    private void Update()
    {
        HandleTimer();
        UpdateNPCPositions();
        if (npcLine.Count == 0)
        {
            Debug.Log("Next in Line");
            FollowPlayer();
        }
    }

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
        agent.stoppingDistance = stopDistance;
        agent.SetDestination(player.position);
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
}
