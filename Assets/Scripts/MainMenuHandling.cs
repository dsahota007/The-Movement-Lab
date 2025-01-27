using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuHandling : MonoBehaviour
{
    public void PlayDuskScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex +1);
    }
    public void PlayNightScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex +2);
    }
    public void PlayDaylightScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 3);
    }
    public void PlaySunsetScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 4);
    }
    public void PlayIntenseSunsetScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 5);
    }
    public void PlayStormScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 6);
    }
    public void PlayRedDesertScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 7);
    }
    public void PlayBloodRootScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 8);
    }
    public void PlayChromaticScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 9);
    }
    public void PlayVRMOODScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 10);
    }
    public void PlayBlackMoonScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 11);
    }
    public void PlayBlackMoonNormalScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 12);
    }



}
