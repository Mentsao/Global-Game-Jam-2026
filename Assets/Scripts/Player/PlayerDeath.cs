using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerDeath : MonoBehaviour
{
    public static int deathCount;
    public static int documentCount;

    private void OnDestroy()
    {
        deathCount++;
        SceneManager.LoadScene("GameScene");
    }

    public void DocumentFound()
    {
        documentCount++;
    }
}
