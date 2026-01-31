using UnityEngine;

public class PlayerDeath : MonoBehaviour
{
    public static int deathCount;
    public static int documentCount;

    private SceneLoader sceneLoader;

    private void Awake()
    {
        sceneLoader = GameObject.Find("SceneManager").GetComponent<SceneLoader>();
    }
    private void OnDestroy()
    {
        deathCount++;
        sceneLoader.LoadGameScene();
    }

    public void DocumentFound()
    {
        documentCount++;
    }
}
