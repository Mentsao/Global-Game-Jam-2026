using System.Collections;
using UnityEngine;

public class CheckpointInstaDeath : MonoBehaviour
{
    public GameObject blackPanel;
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject police;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            player = other.gameObject;
            PoliceNPC policeNPC = police.GetComponent<PoliceNPC>();
            if (policeNPC.npcLine.Count > 0)
            {
                StartCoroutine(TriggerBlackPanel());
            }
        }
    }

    private IEnumerator TriggerBlackPanel()
    {
        blackPanel.SetActive(true);
        yield return new WaitForSeconds(2f);
        GameObject cam = GameObject.Find("Main Camera");
        cam.transform.SetParent(blackPanel.transform, false);
        Destroy(player);
    }
}
