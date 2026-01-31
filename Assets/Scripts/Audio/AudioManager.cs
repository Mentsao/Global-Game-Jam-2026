using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [System.Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float pitch = 1f;
    }

    [Header("Sound Registry")]
    [SerializeField] private List<Sound> musicTracks = new List<Sound>();
    [SerializeField] private List<Sound> sfxClips = new List<Sound>();

    [Header("Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep across scenes
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Auto-add sources if missing
        if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();

        musicSource.loop = true;
    }

    // --- MUSIC ---
    public void PlayMusic(string name)
    {
        Sound s = musicTracks.Find(x => x.name == name);
        if (s == null)
        {
            Debug.LogWarning($"[AudioManager] Music '{name}' not found!");
            return;
        }

        // Don't restart if already playing
        if (musicSource.clip == s.clip && musicSource.isPlaying) return;

        musicSource.clip = s.clip;
        musicSource.volume = s.volume;
        musicSource.pitch = s.pitch;
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    // --- SFX ---
    public void PlaySFX(string name)
    {
        Sound s = sfxClips.Find(x => x.name == name);
        if (s == null)
        {
            Debug.LogWarning($"[AudioManager] SFX '{name}' not found!");
            return;
        }

        sfxSource.PlayOneShot(s.clip, s.volume);
    }

    // Optional: Play Sound at location (3D)
    public void PlaySFXAtPosition(string name, Vector3 position)
    {
        Sound s = sfxClips.Find(x => x.name == name);
        if (s == null) return;

        AudioSource.PlayClipAtPoint(s.clip, position, s.volume);
    }
}
