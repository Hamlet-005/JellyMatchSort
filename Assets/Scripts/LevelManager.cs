using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    public List<GameObject> allModels;
    public Transform[] spawnPoints;

    private Queue<GameObject> waitingQueue = new Queue<GameObject>();
    private Dictionary<Transform, GameObject> activeAtPoint = new Dictionary<Transform, GameObject>();

    private Transform pendingReturnPoint = null;

    void Awake() { Instance = this; }

    void Start()
    {
        foreach (var model in allModels)
            model.SetActive(false);

        for (int i = 0; i < allModels.Count; i++)
        {
            if (i < spawnPoints.Length)
                ShowAtPoint(allModels[i], spawnPoints[i]);
            else
                waitingQueue.Enqueue(allModels[i]);
        }
    }

    void ShowAtPoint(GameObject model, Transform point)
    {
        model.transform.position = point.position;
        model.SetActive(true);
        
        model.transform.DOKill();
        model.transform.localScale = Vector3.zero;
        model.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);

        Drag d = model.GetComponent<Drag>();
        if (d != null) d.UpdateOriginalPosition(point.position);
        activeAtPoint[point] = model;
    }

    public void OnModelPlaced(Vector3 fromPos, bool wasOnGrid = false)
    {
        Transform point = FindPointAt(fromPos);
        if (point == null) return;

        activeAtPoint.Remove(point);
        pendingReturnPoint = null;

        if (waitingQueue.Count > 0)
        {
            GameObject next = waitingQueue.Dequeue();
            ShowAtPoint(next, point);
        }
    }

    public void OnModelTakenFromGrid(Vector3 fromPos)
    {
        Transform point = FindPointAt(fromPos);
        if (point == null) return;

        if (activeAtPoint.ContainsKey(point))
        {
            GameObject current = activeAtPoint[point];
            current.SetActive(false);

            List<GameObject> list = new List<GameObject>(waitingQueue);
            list.Insert(0, current);
            waitingQueue = new Queue<GameObject>(list);

            activeAtPoint.Remove(point);
            pendingReturnPoint = point;
        }
    }

    public void OnModelReturned(GameObject returnedModel)
    {
        Drag d = returnedModel.GetComponent<Drag>();
        if (d == null) return;

        Transform point = FindPointAt(d.originalPosition);
        if (point == null) return;

        if (activeAtPoint.ContainsKey(point))
        {
            GameObject current = activeAtPoint[point];
            if (current != returnedModel)
            {
                current.SetActive(false);
                List<GameObject> list = new List<GameObject>(waitingQueue);
                list.Insert(0, current);
                waitingQueue = new Queue<GameObject>(list);
            }
        }

        activeAtPoint[point] = returnedModel;
        pendingReturnPoint = null;
    }

    Transform FindPointAt(Vector3 pos)
    {
        foreach (Transform point in spawnPoints)
            if (Vector3.Distance(point.position, pos) < 0.5f)
                return point;
        return null;
    }
}