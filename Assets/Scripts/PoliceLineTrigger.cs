using UnityEngine;

public class PoliceLineTrigger : MonoBehaviour
{
    [SerializeField] private GameObject policePoint;
    private PoliceNPC policeNPC;
    private void Start()
    {
        policeNPC = policePoint.GetComponent<PoliceNPC>();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            policeNPC.hasEntered = true;
        }
    }
}
