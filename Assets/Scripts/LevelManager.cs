using UnityEngine;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    [Header("Առաջին 6-ը կերևան անմիջապես, մնացածը՝ հերթով")]
    public List<GameObject> allModels;
    public Transform[] spawnPoints;

    private Queue<GameObject> waitingQueue = new Queue<GameObject>();

    // Յուրաքանչյուր spawnPoint-ի համար հիշում ենք ո՞ր մոդելն է ակտիվ
    private Dictionary<Transform, GameObject> activeAtPoint = new Dictionary<Transform, GameObject>();
    // Յուրաքանչյուր spawnPoint-ի համար հիշում ենք ո՞ր մոդելն է հերթում (queue-ից դուրս եկած, բայց դեռ չտեղադրված)
    private Dictionary<Transform, GameObject> queuedForPoint = new Dictionary<Transform, GameObject>();

    void Awake() { Instance = this; }

    void Start()
    {
        // Բոլոր մոդելները թաքցնում ենք
        foreach (var model in allModels)
        {
            model.SetActive(false);
        }

        // Առաջին 6-ը (spawnPoints.Length) դնում ենք queue-ի մեջ որպես ակտիվ
        int initialCount = Mathf.Min(spawnPoints.Length, allModels.Count);

        for (int i = 0; i < initialCount; i++)
        {
            GameObject model = allModels[i];
            Transform point = spawnPoints[i];

            model.transform.position = point.position;
            model.SetActive(true);

            Drag d = model.GetComponent<Drag>();
            if (d != null) d.UpdateOriginalPosition(point.position);

            activeAtPoint[point] = model;
        }

        // Մնացածները դնում ենք queue-ի մեջ
        for (int i = initialCount; i < allModels.Count; i++)
        {
            waitingQueue.Enqueue(allModels[i]);
        }
    }

    /// <summary>
    /// Մոդելը grid-ի վրա տեղադրվեց — բերում ենք հաջորդը
    /// </summary>
    public void OnModelPlaced(Vector3 placedFromPos)
{
    Transform usedPoint = FindPointAt(placedFromPos);
    if (usedPoint == null) return;

    // Հին մոդելն արդեն grid-ի վրա է
    activeAtPoint.Remove(usedPoint);
    queuedForPoint.Remove(usedPoint); // մաքրում ենք նախորդ queue entry-ն

    // Ստուգում ենք ընդհանուր ակտիվ մոդելների քանակը
    if (activeAtPoint.Count >= 6) return; // արդեն 6 կա, չենք ավելացնում

    if (waitingQueue.Count > 0)
    {
        GameObject next = waitingQueue.Dequeue();
        next.transform.position = usedPoint.position;
        next.SetActive(true);

        Drag d = next.GetComponent<Drag>();
        if (d != null) d.UpdateOriginalPosition(usedPoint.position);

        activeAtPoint[usedPoint] = next;
        queuedForPoint[usedPoint] = next;
    }
}
    /// <summary>
    /// Մոդելը grid-ից հետ վերցվեց — թաքցնում ենք նորը, ցույց ենք տալիս հինը
    /// </summary>
    public void OnModelReturned(GameObject returnedModel)
    {
        // Գտնում ենք թե որ point-ի մոդելն է վերադարձել
        Transform point = FindPointForModel(returnedModel);
        if (point == null) return;

        // Եթե queue-ից բերված նոր մոդел կա, հետ ենք դնում queue-ի սկիզբը
        if (queuedForPoint.ContainsKey(point))
        {
            GameObject newModel = queuedForPoint[point];

            // Հետ ենք դնում queue-ի սկիզբը
            List<GameObject> list = new List<GameObject>(waitingQueue);
            list.Insert(0, newModel);
            waitingQueue = new Queue<GameObject>(list);

            newModel.SetActive(false);
            queuedForPoint.Remove(point);
            activeAtPoint.Remove(point);
        }

        // Հին մոդելը (returnedModel) արդեն վերադարձել է իր տեղը (Drag.cs-ը կանգնեցրեց)
        activeAtPoint[point] = returnedModel;
    }

    // Ըստ position-ի գտնում ենք spawnPoint-ը
    private Transform FindPointAt(Vector3 pos)
    {
        foreach (Transform point in spawnPoints)
        {
            if (Vector3.Distance(point.position, pos) < 0.5f)
                return point;
        }
        return null;
    }

    // Ըստ մոդելի գտնում ենք նրա spawnPoint-ը
    private Transform FindPointForModel(GameObject model)
    {
        foreach (var kvp in activeAtPoint)
        {
            if (kvp.Value == model)
                return kvp.Key;
        }
        return null;
    }
}