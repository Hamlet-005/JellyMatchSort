using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class GridManager : MonoBehaviour
{
    [Header("=== GRID SETTINGS ===")]
    public GameObject slotPrefab;
    public int rows = 6;
    public int cols = 6;
    public float spacing = 1.2f;

    [Header("=== TIMER SETTINGS ===")]
    public float timeLimit = 7f;
    public TMP_Text timerText;

    [Header("=== UI PANELS ===")]
    public GameObject winPanel;
    public GameObject losePanel;

    public static GridManager Instance;

    private float currentTime;
    private bool gameActive = true;

    private Vector3 originalScale;
    public float pulseSpeed = 4f;
    public float pulseAmount = 0.15f;

    void Awake()
    {
        Instance = this;
        GenerateGrid();
    }

    void Start()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);

        Time.timeScale = 1f;
        currentTime = timeLimit;

        if (timerText != null)
            originalScale = timerText.transform.localScale;
    }

    void Update()
    {
        if (!gameActive) return;

        currentTime -= Time.deltaTime;

        if (timerText != null)
        {
            float displayTime = Mathf.Max(0, currentTime);

            int minutes = Mathf.FloorToInt(displayTime / 60);
            int seconds = Mathf.FloorToInt(displayTime % 60);

            timerText.text = string.Format("{0}:{1:00}", minutes, seconds);

            timerText.color = currentTime <= 10f ? Color.red : Color.white;

            if (currentTime <= 10f)
            {
                float pulse = 1 + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
                timerText.transform.localScale = originalScale * pulse;
            }
            else
            {
                timerText.transform.localScale = originalScale;
            }
        }

        if (currentTime <= 0f)
        {
            currentTime = 0f;
            gameActive = false;
            StartCoroutine(ShowLose());
        }
    }

    void GenerateGrid()
    {
        float offsetX = (cols - 1) * spacing / 2f;
        float offsetZ = (rows - 1) * spacing / 2f;

        for (int x = 0; x < cols; x++)
        {
            for (int z = 0; z < rows; z++)
            {
                Vector3 pos = new Vector3(
                    x * spacing - offsetX,
                    0,
                    z * spacing - offsetZ
                );
                Instantiate(slotPrefab, pos, Quaternion.identity, transform);
            }
        }
    }

    public void CheckWinCondition()
    {
        if (!gameActive) return;

        GridSlot[] allSlots = FindObjectsByType<GridSlot>(FindObjectsSortMode.None);

        foreach (GridSlot slot in allSlots)
        {
            if (!slot.isOccupied) return;
        }

        gameActive = false;
        StartCoroutine(ShowWin());
    }

    IEnumerator ShowWin()
    {
        yield return new WaitForSeconds(0.3f);

        if (AudioManager.Instance != null) AudioManager.Instance.PlayWin();

        if (timerText != null) timerText.gameObject.SetActive(false);
        if (winPanel != null) winPanel.SetActive(true);

        DisableAllDraggables();

        Time.timeScale = 0f;
    }

    IEnumerator ShowLose()
    {
        yield return new WaitForSeconds(0.3f);

        if (AudioManager.Instance != null) AudioManager.Instance.PlayLose();

        if (timerText != null) timerText.gameObject.SetActive(false);
        if (losePanel != null) losePanel.SetActive(true);

        DisableAllDraggables();

        Time.timeScale = 0f;
    }

    void DisableAllDraggables()
    {
        Draggable[] allDraggables = FindObjectsByType<Draggable>(FindObjectsSortMode.None);
        foreach (Draggable d in allDraggables)
        {
            d.enabled = false;
        }
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