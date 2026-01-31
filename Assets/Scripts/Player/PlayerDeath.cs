using UnityEngine;

public class PlayerDeath : MonoBehaviour
{
    public static int deathCount;
    public static int documentCount;

    private void OnDestroy()
    {
        deathCount++;
    }

    public void DocumentFound()
    {
        documentCount++;
    }
}
