using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    public List<GameObject> allModels;
    public Transform[] spawnPoints;
    public float waitingZoneScale = 0.6f;

    private List<GameObject> activeModels = new List<GameObject>();
    private List<GameObject> waitingList = new List<GameObject>(); 
    private bool isGameOver = false;

    void Awake() { Instance = this; }

    void Start()
    {
        isGameOver = false;
        activeModels.Clear();
        waitingList.Clear();

        foreach (var model in allModels)
        {
            model.SetActive(false);
            waitingList.Add(model);
        }

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (waitingList.Count > 0)
            {
                GameObject model = waitingList[0];
                waitingList.RemoveAt(0);
                activeModels.Add(model);
                ShowAtPoint(model, spawnPoints[i]);
            }
        }
    }

    void ShowAtPoint(GameObject model, Transform point)
    {
        model.SetActive(true);
        model.transform.localScale = Vector3.one;

        Bounds combinedBounds = GetMaxBounds(model);
        Vector3 offset = model.transform.position - combinedBounds.center;
        model.transform.position = point.position + new Vector3(offset.x, 0, offset.z);

        model.transform.localScale = Vector3.zero;
        model.transform.DOKill();
        model.transform.DOScale(Vector3.one * waitingZoneScale, 0.35f).SetEase(Ease.OutBack);
        
        Drag d = model.GetComponent<Drag>();
        if (d != null) d.UpdateOriginalPosition(model.transform.position);
    }

    public void OnModelPlaced(GameObject placedModel)
    {
        if (isGameOver) return;

        if (activeModels.Contains(placedModel))
        {
            activeModels.Remove(placedModel);
            ShiftModels();

            if (waitingList.Count > 0 && activeModels.Count < spawnPoints.Length)
            {
                GameObject nextModel = waitingList[0];
                waitingList.RemoveAt(0);
                activeModels.Add(nextModel);
                ShowAtPoint(nextModel, spawnPoints[activeModels.Count - 1]);
            }
            CheckWin();
        }
    }

    public void OnModelTakenFromGrid(GameObject model)
    {
        if (isGameOver) return;

        if (activeModels.Count >= spawnPoints.Length)
        {
            GameObject modelToBack = activeModels[activeModels.Count - 1];
            activeModels.RemoveAt(activeModels.Count - 1);
            waitingList.Insert(0, modelToBack);
            modelToBack.transform.DOScale(Vector3.zero, 0.2f).OnComplete(() => modelToBack.SetActive(false));
        }

        if (!activeModels.Contains(model)) activeModels.Add(model);
        ShiftModels();
    }

    void ShiftModels()
    {
        for (int i = 0; i < activeModels.Count; i++)
        {
            if (i < spawnPoints.Length)
            {
                Transform targetPoint = spawnPoints[i];
                Drag d = activeModels[i].GetComponent<Drag>();
                if (d != null) d.UpdateOriginalPosition(targetPoint.position);

                if (activeModels[i].activeSelf)
                {
                    activeModels[i].transform.DOKill();
                    activeModels[i].transform.DOMove(targetPoint.position, 0.3f).SetEase(Ease.OutCubic);
                }
            }
        }
    }

    void CheckWin()
    {
        GridSlot[] allSlots = FindObjectsByType<GridSlot>(FindObjectsSortMode.None);
        int occupiedCount = 0;
        foreach (var slot in allSlots) if (slot.isOccupied) occupiedCount++;

        if (occupiedCount >= allSlots.Length && allSlots.Length > 0)
        {
            WinGame();
        }
    }

    public void WinGame()
    {
        if (isGameOver) return;
        isGameOver = true;

        // 1. Ձայն
        if (AudioManager.Instance != null) AudioManager.Instance.PlayWin();

        // 2. Պանել և Թայմեր
        GridManager gm = FindFirstObjectByType<GridManager>();
        if (gm != null)
        {
            if (gm.winPanel != null) gm.winPanel.SetActive(true);
            if (gm.timerText != null) gm.timerText.gameObject.SetActive(false); // Թայմերը վերանում է
        }

        DisableAllInteractions();
    }

    public void LoseGame()
    {
        if (isGameOver) return;
        isGameOver = true;

        // 1. Ձայն
        if (AudioManager.Instance != null) AudioManager.Instance.PlayLose();

        // 2. Պանել և Թայմեր
        GridManager gm = FindFirstObjectByType<GridManager>();
        if (gm != null)
        {
            if (gm.losePanel != null) gm.losePanel.SetActive(true);
            if (gm.timerText != null) gm.timerText.gameObject.SetActive(false); // Թայմերը վերանում է
        }

        DisableAllInteractions();
    }

    void DisableAllInteractions()
    {
        // Բոլոր մոդելները դարձնում ենք unclickable
        Drag[] allDraggables = FindObjectsByType<Drag>(FindObjectsSortMode.None);
        foreach (Drag d in allDraggables)
        {
            d.enabled = false;
        }
    }

    Bounds GetMaxBounds(GameObject g)
    {
        var renderers = g.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(g.transform.position, Vector3.zero);
        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
        return b;
    }
}