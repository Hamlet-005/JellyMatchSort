using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    // Play button
    public void OnPlayPressed()
    {
        SceneManager.LoadScene("Level-1"); // փոխիր քո scene-ի անունով
    }

    // Quit button
    public void OnQuitPressed()
    {
        Application.Quit();
        Debug.Log("Quit"); // Editor-ում ստուգելու համար
    }
}