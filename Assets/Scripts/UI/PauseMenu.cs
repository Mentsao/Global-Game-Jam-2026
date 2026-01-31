using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class PauseMenu : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject pausePanel;
        
        private bool isPaused = false;

        private void Start()
        {
            // Ensure panels are in correct state at start
            if (pausePanel != null)
                pausePanel.SetActive(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // If Settings is open, we let it handle escape to close itself (which should bring us back here)
                if (SettingsPanel.Instance != null && SettingsPanel.Instance.gameObject.activeInHierarchy)
                {
                    // Check if the content is actually visible
                    // (SettingsPanel.cs has a panelContent reference)
                    return; 
                }

                if (isPaused)
                {
                    Resume();
                }
                else
                {
                    Pause();
                }
            }
        }

        public void Pause()
        {
            isPaused = true;
            if (pausePanel != null)
                pausePanel.SetActive(true);
            
            Time.timeScale = 0f;
            
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        public void Resume()
        {
            isPaused = false;
            if (pausePanel != null)
                pausePanel.SetActive(false);
            
            Time.timeScale = 1f;
            
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        public void OpenSettings()
        {
            if (SettingsPanel.Instance != null)
            {
                if (pausePanel != null)
                    pausePanel.SetActive(false);
                
                SettingsPanel.Instance.OpenFrom(pausePanel);
            }
            else
            {
                Debug.LogWarning("SettingsPanel Instance not found!");
            }
        }

        public void QuitGame()
        {
            Debug.Log("Quitting game...");
            Application.Quit();
        }
        
        public void RestartLevel()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
