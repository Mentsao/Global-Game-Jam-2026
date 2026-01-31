using UnityEngine;

namespace Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class BackgroundMusic : MonoBehaviour
    {
        private static BackgroundMusic _instance;

        public static BackgroundMusic Instance
        {
            get { return _instance; }
        }

        private AudioSource _audioSource;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(this.gameObject);

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            _audioSource.loop = true;
            _audioSource.playOnAwake = true;
        }

        private void Start()
        {
            if (!_audioSource.isPlaying)
            {
                _audioSource.Play();
            }
        }

        /// <summary>
        /// Changes the background music and plays it.
        /// </summary>
        /// <param name="newClip">The new audio clip to play.</param>
        public void PlayMusic(AudioClip newClip)
        {
            if (_audioSource.clip == newClip) return;

            _audioSource.clip = newClip;
            _audioSource.Play();
        }

        /// <summary>
        /// Stops the background music.
        /// </summary>
        public void StopMusic()
        {
            _audioSource.Stop();
        }
    }
}
