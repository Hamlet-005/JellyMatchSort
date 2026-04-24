using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public GameObject winCanvas;
    public GameObject loseCanvas;

    // Հաղթանակի ֆունկցիա
    public void WinGame()
    {
        winCanvas.SetActive(true);
        Time.timeScale = 0f; 
    }

    // Պարտության ֆունկցիա
    public void LoseGame()
    {
        loseCanvas.SetActive(true);
        Time.timeScale = 0f; 
    }

    // Next Level կոճակի համար
    public void NextLevel()
    {
        Time.timeScale = 1f;
        // Սա ավտոմատ բացում է հաջորդ Scene-ը ըստ Build Settings-ի հերթականության
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    // Retry կոճակի համար
    public void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Quit կոճակի համար
    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Game Exited");
    }
}