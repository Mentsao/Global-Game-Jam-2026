using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Winning : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject winText;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            winText.SetActive(true);
            StartCoroutine(TriggerBacktoTitle());
        }
    }

    private IEnumerator TriggerBacktoTitle()
    {
        yield return new WaitForSeconds(2f);
        SceneManager.LoadScene("TitleScene");
    }
}
