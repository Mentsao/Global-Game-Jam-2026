using UnityEngine;

public class NPCHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private int currentHealth;

    [Header("Death Settings")]
    [SerializeField] private float destroyDelay = 5.0f;
    [Tooltip("If true, requires ZombieAction or specific scripts to handle animation/logic before destroy.")]
    [SerializeField] private bool handleDeathLogic = true;

    private bool _isDead = false;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (_isDead) return;

        currentHealth -= damage;
        // Debug.Log($"[NPCHealth] {gameObject.name} took {damage} damage. Health: {currentHealth}/{maxHealth}");

        // Play Impact Sound (Optional)
        // if (AudioManager.Instance != null) AudioManager.Instance.PlayImpact();

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Optional: Trigger Hurt Animation
            Animator anim = GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.SetTrigger("Hit"); // Generic hit trigger if exists
            }
        }
    }

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;

        if (handleDeathLogic)
        {
            // 1. Check for Zombie
            var zombie = GetComponent<ZombieActions>();
            if (zombie != null)
            {
                // zombie.Die(); // Assuming ZombieActions has a Die method
            }

            // 2. Check for Healthcare Personnel
            var healthcare = GetComponent<HealthcarePersonnel>();
            if (healthcare != null)
            {
                healthcare.Die(); // This handles Destroy internally in current script
                return; // Healthcare handles itself
            }
        }

        // Generic Fallback Death
        Debug.Log($"[NPCHealth] {gameObject.name} Died.");
        
        // Disable Colliders
        Collider[] cols = GetComponentsInChildren<Collider>();
        foreach (var c in cols) c.enabled = false;

        // Play Death Animation
        Animator anim = GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.SetBool("isDead", true);
            anim.CrossFade("Death", 0.2f); // Generic Death state name
        }

        // Destroy
        Destroy(gameObject, destroyDelay);
    }
}
