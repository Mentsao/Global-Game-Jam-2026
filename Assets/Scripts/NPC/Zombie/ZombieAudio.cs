using System.Collections;
using UnityEngine;

namespace NPC.Zombie
{
    [RequireComponent(typeof(AudioSource))]
    public class ZombieAudio : MonoBehaviour
    {
        [Header("Audio Clips")]
        [Tooltip("Assign 4 variations of zombie sounds here")]
        [SerializeField] private AudioClip[] audioClips;

        [Header("Settings")]
        [SerializeField] private float minInterval = 3f;
        [SerializeField] private float maxInterval = 8f;
        [SerializeField] [Range(0f, 1f)] private float volume = 1f;
        [SerializeField] private float minDistance = 1f;
        [SerializeField] private float maxDistance = 20f;

        private AudioSource _audioSource;

        private void Start()
        {
            _audioSource = GetComponent<AudioSource>();

            // Configure 3D Sound
            _audioSource.spatialBlend = 1.0f; // Fully 3D
            _audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            _audioSource.minDistance = minDistance;
            _audioSource.maxDistance = maxDistance;
            _audioSource.playOnAwake = false;
            
            StartCoroutine(PlayRandomSFX());
        }

        private IEnumerator PlayRandomSFX()
        {
            while (true)
            {
                // Wait for random interval
                float waitTime = Random.Range(minInterval, maxInterval);
                yield return new WaitForSeconds(waitTime);

                if (audioClips != null && audioClips.Length > 0)
                {
                    // Pick random clip
                    int randomIndex = Random.Range(0, audioClips.Length);
                    AudioClip clip = audioClips[randomIndex];

                    if (clip != null)
                    {
                        // Play
                        _audioSource.PlayOneShot(clip, volume);
                    }
                }
            }
        }

        private void OnValidate()
        {
            if (_audioSource == null) _audioSource = GetComponent<AudioSource>();
            if (_audioSource != null)
            {
                _audioSource.minDistance = minDistance;
                _audioSource.maxDistance = maxDistance;
            }
        }
    }
}
