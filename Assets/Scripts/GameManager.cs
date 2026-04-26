using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public GameObject winCanvas;
    public GameObject loseCanvas;

    public void WinGame()
    {
        winCanvas.SetActive(true);
        Time.timeScale = 0f; 
    }

    public void LoseGame()
    {
        loseCanvas.SetActive(true);
        Time.timeScale = 0f; 
    }

    public void NextLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}