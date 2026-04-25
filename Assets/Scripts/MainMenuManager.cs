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

    // Play button
    public void OnPlayPressed()
    {
        SceneManager.LoadScene("Level-1");
    }

    // Quit button
    public void OnQuitPressed()
    {
        Application.Quit();
        Debug.Log("Quit");
    }

    // Settings button
    public void OnSettingsPressed()
    {
        settingsPanel.SetActive(true);
    }

    // Close settings button
    public void OnCloseSettings()
    {
        settingsPanel.SetActive(false);
    }

    // Music toggle button
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
    musicOffImage.SetActive(!isOn); // գիծը երևում է միայն երբ անջատված է
}
}