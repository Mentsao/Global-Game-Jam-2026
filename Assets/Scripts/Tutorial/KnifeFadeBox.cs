using UnityEngine;

public class KnifeFadeBox : MonoBehaviour
{
    public Tutorial tutorial;
    private void OnTriggerExit(Collider other)
    {
        tutorial.startFade = true;
    }
}
