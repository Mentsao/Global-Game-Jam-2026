using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Tutorial : MonoBehaviour
{
    [Header("Papers Tutorial")]
    [SerializeField] private GameObject papersTutorial;

    [Header("Pick Up Knife Tutorial")]
    [SerializeField] private bool hasKnife = false;
    public bool startFade = false;
    [SerializeField] private GameObject pickUpKnifeTutorial;
    [SerializeField] private GameObject bigPickUpKnifeText;
    [SerializeField] private List<GameObject> knifeText = new List<GameObject>();
    [SerializeField] private List<string> knifeString = new List<string>();
    [SerializeField] private float animTime = 1.5f;
    [SerializeField] private float bigKnifeTime = 30f;
    [SerializeField] private GameObject knife;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!hasKnife && startFade)
        {
            pickUpKnifeTutorial.SetActive(true);
            animTime -= Time.deltaTime;
            bigKnifeTime -= Time.deltaTime;

            if(bigPickUpKnifeText.activeSelf) 
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                player.transform.LookAt(knife.transform);
                return;
            }

            if (animTime <= 0)
            {
                PickUpKnifeTutorial();
                animTime = 1.5f;
            }

            if (bigKnifeTime <= 0)
            {
                bigPickUpKnifeText.SetActive(true);
            }
        }
        else if (hasKnife)
        {
            pickUpKnifeTutorial.SetActive(false);
        }
    }

    // On Death Tutorials
    public void FindPapersTutorial(bool hasPapers)
    {
        if (!hasPapers)
        {
            papersTutorial.SetActive(true);
        }
    }

    public void CheckpointLineUpTutorial()
    {

    }

    
    // In-game Tutorials
    public void PickUpKnifeTutorial()
    {
        int textNum = Random.Range(0, knifeText.Count);
        Animator knifeAnim = knifeText[textNum].GetComponent<Animator>();

        int textString = Random.Range(0, knifeString.Count);
        knifeText[textNum].GetComponent<TextMeshProUGUI>().text = knifeString[textNum];

        int textSize = Random.Range(70, 261);
        knifeText[textNum].GetComponent<TextMeshProUGUI>().fontSize = textSize;

        if (knifeAnim.GetCurrentAnimatorStateInfo(0).IsName("KnifeIdle"))
        {
            knifeAnim.SetTrigger("Fade");
        }
    }


    public void StealthPracticeTutorial()
    {

    }

    public void HighlightMasksTutorial()
    {

    }
}
