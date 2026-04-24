using System.Collections.Generic;
using UnityEngine;

public class ItemPoolManager : MonoBehaviour
{
    [Header("=== PREFABS (12 TOTAL) ===")]
    public List<GameObject> allPrefabs;

    [Header("=== SPAWN SETTINGS ===")]
    public Transform spawnParent;
    public int activeCount = 6;

    private List<GameObject> activeItems = new List<GameObject>();
    private List<GameObject> reserveItems = new List<GameObject>();

    void Start()
    {
        InitializePool();
    }

    void InitializePool()
    {
        List<GameObject> shuffled = new List<GameObject>(allPrefabs);

        // shuffle
        for (int i = 0; i < shuffled.Count; i++)
        {
            GameObject obj = Instantiate(shuffled[i], GetRandomPosition(), Quaternion.identity, spawnParent);

            if (i < activeCount)
            {
                activeItems.Add(obj);
            }
            else
            {
                obj.SetActive(false);
                reserveItems.Add(obj);
            }
        }
    }

    // 🎯 CALL THIS WHEN PLAYER TAKES ITEM
    public void OnItemTaken(GameObject item)
    {
        if (reserveItems.Count == 0) return;

        if (!activeItems.Contains(item)) return;

        // remove taken item
        activeItems.Remove(item);

        // bring new item
        GameObject newItem = reserveItems[0];
        reserveItems.RemoveAt(0);

        newItem.transform.position = GetRandomPosition();
        newItem.SetActive(true);

        activeItems.Add(newItem);

        // move taken to reserve
        item.SetActive(false);
        reserveItems.Add(item);
    }

    // 🔁 CALL THIS IF PLAYER RETURNS ITEM BACK TO GRID
    public void OnItemReturned(GameObject item)
    {
        if (activeItems.Contains(item))
            return;

        if (activeItems.Count == 0) return;

        // last spawned active item gets removed
        GameObject lastActive = activeItems[activeItems.Count - 1];

        lastActive.SetActive(false);
        reserveItems.Insert(0, lastActive);
        activeItems.Remove(lastActive);

        // returned item becomes active again
        item.SetActive(true);
        activeItems.Add(item);
    }

    Vector3 GetRandomPosition()
    {
        float x = Random.Range(-3f, 3f);
        float z = Random.Range(-3f, 3f);
        return new Vector3(x, 0, z);
    }
}