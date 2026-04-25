using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("=== AUDIO CLIPS ===")]
    public AudioClip bgMusic;
    public AudioClip winSound;
    public AudioClip loseSound;
    public AudioClip pickUpSound;
    public AudioClip placeSound;

    private AudioSource musicSource;
    private AudioSource sfxSource;

    private const string MUSIC_KEY = "MusicEnabled";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        musicSource = gameObject.AddComponent<AudioSource>();
        sfxSource = gameObject.AddComponent<AudioSource>();

        musicSource.loop = true;
        musicSource.clip = bgMusic;
    }

    void Start()
    {
        bool musicEnabled = PlayerPrefs.GetInt(MUSIC_KEY, 1) == 1;
        if (musicEnabled) musicSource.Play();
    }

    public bool IsMusicEnabled()
    {
        return PlayerPrefs.GetInt(MUSIC_KEY, 1) == 1;
    }

    public void ToggleMusic()
    {
        bool current = IsMusicEnabled();
        bool newVal = !current;
        PlayerPrefs.SetInt(MUSIC_KEY, newVal ? 1 : 0);
        PlayerPrefs.Save();

        if (newVal) musicSource.Play();
        else musicSource.Stop();
    }

    public void PlayWin()
    {
        sfxSource.PlayOneShot(winSound);
    }

    public void PlayLose()
    {
        sfxSource.PlayOneShot(loseSound);
    }

    // Play when picking up, returning, or dropping in wrong place
    public void PlayPickUp()
    {
        sfxSource.PlayOneShot(pickUpSound);
    }

    // Play when successfully placing on grid
    public void PlayPlace()
    {
        sfxSource.PlayOneShot(placeSound);
    }
}