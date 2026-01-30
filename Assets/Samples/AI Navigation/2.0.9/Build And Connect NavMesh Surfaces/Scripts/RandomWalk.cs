using UnityEngine;
using UnityEngine.AI;

namespace Unity.AI.Navigation.Samples
{
    /// <summary>
    /// Walk to a random position and repeat
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class RandomWalk : MonoBehaviour
    {
        public float m_Range = 25.0f;
        NavMeshAgent m_Agent;

        void Start()
        {
            m_Agent = GetComponent<NavMeshAgent>();
        }

        void Update()
        {
            if (m_Agent.pathPending || !m_Agent.isOnNavMesh || m_Agent.remainingDistance > 0.1f)
                return;

            Vector2 randomCircle = Random.insideUnitCircle * m_Range;
            Vector3 randomPoint = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, m_Range, NavMesh.AllAreas))
            {
                m_Agent.destination = hit.position;
            }
        }


        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, m_Range);
        }

    }
}