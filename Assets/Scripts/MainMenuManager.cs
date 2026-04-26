using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("=== PANELS ===")]
    public GameObject settingsPanel;

    [Header("=== MUSIC BUTTON ===")]
    public GameObject musicOnImage;
    public GameObject musicOffImage;

    void Start()
    {
        settingsPanel.SetActive(false);
        UpdateMusicIcon();
    }

    public void OnPlayPressed()
    {
        SceneManager.LoadScene("Level-1");
    }

    public void OnQuitPressed()
    {
        Application.Quit();
    }

    public void OnSettingsPressed()
    {
        settingsPanel.SetActive(true);
    }

    public void OnCloseSettings()
    {
        settingsPanel.SetActive(false);
    }

    public void OnMusicPressed()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.ToggleMusic();
        UpdateMusicIcon();
    }

    void UpdateMusicIcon()
{
    if (AudioManager.Instance == null) return;
    bool isOn = AudioManager.Instance.IsMusicEnabled();
    musicOffImage.SetActive(!isOn); 
}
}