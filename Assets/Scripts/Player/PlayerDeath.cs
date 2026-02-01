using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerDeath : MonoBehaviour
{
    public static int deathCount;
    public static int documentCount;

    private void OnDestroy()
    {
        deathCount++;
        // Load the currently active scene instead of a hardcoded one
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void DocumentFound()
    {
        documentCount++;
    }
}
