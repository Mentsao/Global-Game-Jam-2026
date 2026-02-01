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
                Debug.Log("[PauseMenu] ESC Pressed");
                // If Settings is open, close it
                if (SettingsPanel.Instance != null && SettingsPanel.Instance.gameObject.activeInHierarchy)
                {
                    Debug.Log("[PauseMenu] Closing Settings");
                    SettingsPanel.Instance.CloseSettings();
                    return; 
                }

                if (isPaused)
                {
                    Debug.Log("[PauseMenu] Resuming");
                    Resume();
                }
                else
                {
                    Debug.Log("[PauseMenu] Pausing");
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
            
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName != "TitleScene")
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                // Title Scene: Stay visible
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
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
