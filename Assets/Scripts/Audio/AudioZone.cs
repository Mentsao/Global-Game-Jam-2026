using UnityEngine;

public class AudioZone : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string soundName;
    [SerializeField] private bool isMusic = true; // True = BGM (Loop), False = SFX (OneShot)
    [SerializeField] private bool stopMusicOnEnter = false; // Only if you want silence

    [Header("Debug")]
    [SerializeField] private bool showGizmo = true;
    [SerializeField] private Color gizmoColor = new Color(0, 1, 0, 0.4f);

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (AudioManager.Instance == null) return;

            if (stopMusicOnEnter)
            {
                AudioManager.Instance.StopMusic();
            }
            else if (isMusic)
            {
                AudioManager.Instance.PlayMusic(soundName);
            }
            else
            {
                AudioManager.Instance.PlaySFX(soundName);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (showGizmo)
        {
            Gizmos.color = gizmoColor;
            // Draw cube matching collider if box
            BoxCollider box = GetComponent<BoxCollider>();
            if (box != null)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
            }
            else
            {
                Gizmos.DrawSphere(transform.position, 1f);
            }
        }
    }
}
