using UnityEngine;
using UnityEngine.Video;
using UnityEngine.Events; 

public class CutsceneManager : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public UnityEvent onCutsceneEnd;

    void Start()
    {
        videoPlayer.loopPointReached += OnVideoEnded;
        videoPlayer.Play(); 
    }

    void OnVideoEnded(VideoPlayer vp)
    {
        Debug.Log("Cutscene ended. Triggering event.");
        onCutsceneEnd.Invoke(); 
    }
}
