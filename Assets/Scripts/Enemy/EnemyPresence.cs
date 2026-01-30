using System.Collections.Generic;
using UnityEngine;

namespace Enemy
{
    public class EnemyPresence : MonoBehaviour
    {
        [Header("Detection Settings")]
        [Tooltip("Distance at which the player starts seeing the vignette effect.")]
        public float detectionRange = 10f;

        // Static list to keep track of all enemies in the scene
        // We use a static list for efficient access from the UI script
        public static List<EnemyPresence> AllEnemies = new List<EnemyPresence>();

        private void OnEnable()
        {
            AllEnemies.Add(this);
        }

        private void OnDisable()
        {
            AllEnemies.Remove(this);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
        }
    }
}
