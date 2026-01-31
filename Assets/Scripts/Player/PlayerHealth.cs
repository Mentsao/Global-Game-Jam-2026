using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace Player
{
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Realism Settings")]
        [Tooltip("Default is 1. Bonus health can increase this.")]
        [SerializeField] private int maxHealth = 1;
        [SerializeField] private int currentHealth;

        private bool _isDead = false;

        private void Start()
        {
            currentHealth = maxHealth;
        }

        public void TakeDamage(int damage)
        {
            if (_isDead) return;

            currentHealth -= damage;
            Debug.Log($"[PlayerHealth] Damage: {damage}. Current HP: {currentHealth}/{maxHealth}");

            // Audio Feedback
            // if (AudioManager.Instance != null && currentHealth <= 0) AudioManager.Instance.PlayPlayerDeath(); 

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        public bool HasNurse { get; private set; } // Track if a nurse is already attached

        public void GrantBonusHealth(int amount)
        {
            if (HasNurse) return; // Prevent double stacking logic if called safely

            HasNurse = true;
            maxHealth += amount;
            currentHealth += amount; 
            Debug.Log($"[PlayerHealth] Nurse Attached! Bonus Health Granted. HP: {currentHealth}/{maxHealth}");
        }

        public void RevokeBonusHealth(int amount)
        {
            if (!HasNurse) return;

            HasNurse = false;
            maxHealth -= amount;
            if (currentHealth > maxHealth)
            {
                currentHealth = maxHealth;
            }
            if (currentHealth < 1 && !_isDead) currentHealth = 1; 
            
            Debug.Log($"[PlayerHealth] Nurse Left. Bonus Health Revoked. HP: {currentHealth}/{maxHealth}");
        }

        private void Die()
        {
            if (_isDead) return;
            _isDead = true;
            
            Debug.Log("YOU DIED");

            // Disable Movement
            var movement = GetComponent<PlayerMovement>();
            if (movement != null) movement.enabled = false;

            var attack = GetComponent<PlayerAttack>();
            if (attack != null) attack.enabled = false;

            // Reload Scene after delay
            Invoke(nameof(ReloadScene), 3f);
        }

        private void ReloadScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void Heal(int amount)
        {
            if (_isDead) return;
            currentHealth += amount;
            if (currentHealth > maxHealth) currentHealth = maxHealth;
            Debug.Log($"[PlayerHealth] Healed {amount}. Health: {currentHealth}/{maxHealth}");
        }
    }
}
