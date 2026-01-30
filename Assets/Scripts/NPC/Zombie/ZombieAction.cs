using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class ZombieActions : MonoBehaviour
{
    [Header("Zombie")]
    private NavMeshAgent agent;
    [SerializeField] private bool destinationReached = false;
    [SerializeField] private float QTETimeLimit = 5f;
    [SerializeField] private float QTETimeLeft = 5f;
    [SerializeField] private float blowBackDist = 7f;

    [Header("Canvas")]
    [SerializeField] private GameObject canvas;
    [SerializeField] private Slider slider;

    [Header("Scripts")]
    private ZombieNPCDetect zombieNPCDetect;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        zombieNPCDetect = GetComponent<ZombieNPCDetect>();
        canvas.SetActive(false);
    }

    private void Update()
    {
        if (zombieNPCDetect.target == null)
        {
            QTETimeLeft = QTETimeLimit;
            return;
        }
            
        if (!destinationReached)
        {
            agent.isStopped = false;
            MoveToTarget();
        }
        else
        {
            agent.isStopped = true;
            QTEAttack();
            QTETimeLeft -= Time.deltaTime; 
        }

        if (agent.hasPath && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            destinationReached = true;
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
        if (zombieNPCDetect.target.CompareTag("Player"))
        {
            canvas.SetActive(true);
        }

        slider.value -= 0.2f * Time.deltaTime;

        if (slider.value <= 1 && QTETimeLeft <= 0)
        {
            Destroy(zombieNPCDetect.target);
            EndQTE();
            return;
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            slider.value += 0.1f;
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
        canvas.SetActive(false);
        slider.value = 0;
        QTETimeLeft = 5f; 
        destinationReached = false;
    }

}
