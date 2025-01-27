using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenu;  // Drag your PauseMenu here in the Inspector

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))  // Press Escape to pause/unpause
        {
            if (pauseMenu.activeSelf)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    void PauseGame()
    {
        pauseMenu.SetActive(true);  // Show pause menu
        Time.timeScale = 0f;  // Pause the game
    }

    void ResumeGame()
    {
        pauseMenu.SetActive(false);  // Hide pause menu
        Time.timeScale = 1f;  // Resume the game
    }
}