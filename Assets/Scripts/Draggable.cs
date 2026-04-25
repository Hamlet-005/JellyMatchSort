using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

[RequireComponent(typeof(BoxCollider))]
public class Draggable : MonoBehaviour
{
    [Header("=== DESIGNER SETTINGS ===")]
    public GameObject shadowPrefab;
    public Color validColor = new Color(0f, 1f, 0f, 0.6f);
    public Color invalidColor = new Color(1f, 0f, 0f, 0.6f);

    [Range(0f, 1f)]
    public float dragTransparency = 0.5f;

    private Transform[] jellyParts;
    private Renderer[] partsRenderers;
    private Vector3 originalPosition;
    private bool isDragging = false;
    private Camera mainCamera;
    private List<GameObject> activeShadows = new List<GameObject>();
    private int activeTouchId = -1;

    void Start()
    {
        mainCamera = Camera.main;
        originalPosition = transform.position;
        SetupParts();
        SetupCollider();
    }

    void SetupParts()
    {
        List<Transform> partsList = new List<Transform>();
        List<Renderer> rendererList = new List<Renderer>();

        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer rend in renderers)
        {
            partsList.Add(rend.transform);
            rendererList.Add(rend);
        }

        jellyParts = partsList.ToArray();
        partsRenderers = rendererList.ToArray();

        foreach (Transform part in jellyParts)
        {
            if (part.GetComponent<Collider>() == null)
                part.gameObject.AddComponent<BoxCollider>();
        }
    }

    void SetupCollider()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        if (jellyParts.Length == 0) return;

        Bounds bounds = new Bounds(jellyParts[0].localPosition, Vector3.one);
        foreach (Transform part in jellyParts)
            bounds.Encapsulate(new Bounds(part.localPosition, Vector3.one));

        col.center = bounds.center;
        col.size = bounds.size;
    }

    void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseInput();
#else
        HandleTouchInput();
#endif
    }

    void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0)) TrySelect(Input.mousePosition);
        if (Input.GetMouseButton(0) && isDragging) DragTo(Input.mousePosition);
        if (Input.GetMouseButtonUp(0) && isDragging) HandleRelease();
    }

    void HandleTouchInput()
    {
        foreach (Touch touch in Input.touches)
        {
            if (touch.phase == TouchPhase.Began && activeTouchId == -1)
            {
                TrySelect(touch.position);
                if (isDragging) activeTouchId = touch.fingerId;
            }
            else if (touch.fingerId == activeTouchId)
            {
                if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                    DragTo(touch.position);
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    HandleRelease();
                    activeTouchId = -1;
                }
            }
        }
    }

    void TrySelect(Vector3 screenPos)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
                StartDrag();
        }
    }

    void StartDrag()
    {
        isDragging = true;
        FreePreviousSlots();
        CreateShadows();
        SetShapeTransparency(dragTransparency);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayPickUp();

        transform.DOKill();
        transform.DOScale(Vector3.one * 1.15f, 0.15f).SetEase(Ease.OutBack);
    }

    void DragTo(Vector3 screenPos)
    {
        Plane movePlane = new Plane(Vector3.up, new Vector3(0, 0.5f, 0));
        Ray ray = mainCamera.ScreenPointToRay(screenPos);

        if (movePlane.Raycast(ray, out float distance))
        {
            Vector3 hitPoint = ray.GetPoint(distance);
            transform.position = new Vector3(hitPoint.x, 0.5f, hitPoint.z);
        }
        UpdateShadows();
    }

    void HandleRelease()
    {
        isDragging = false;
        DestroyShadows();
        SetShapeTransparency(1.0f);

        transform.DOKill();
        transform.DOScale(Vector3.one, 0.15f).SetEase(Ease.OutBack);

        HandleDrop();
    }

    void HandleDrop()
    {
        GridSlot[] foundSlots = new GridSlot[jellyParts.Length];
        bool allValid = true;

        for (int i = 0; i < jellyParts.Length; i++)
        {
            GridSlot closestSlot = FindClosestSlot(jellyParts[i].position);
            if (closestSlot == null || closestSlot.isOccupied)
            { allValid = false; break; }

            for (int j = 0; j < i; j++)
                if (foundSlots[j] == closestSlot) { allValid = false; break; }

            if (!allValid) break;
            foundSlots[i] = closestSlot;
        }

        if (allValid)
        {
            transform.DOKill();
            transform.localScale = Vector3.one;

            Vector3 offset = transform.position - jellyParts[0].position;
            transform.position = new Vector3(
                foundSlots[0].transform.position.x + offset.x,
                0.5f,
                foundSlots[0].transform.position.z + offset.z);

            for (int i = 0; i < foundSlots.Length; i++)
            {
                foundSlots[i].isOccupied = true;
                foundSlots[i].occupant = jellyParts[i].gameObject;
            }

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayPlace();

            transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5, 0.5f);

            if (GridManager.Instance != null)
                GridManager.Instance.CheckWinCondition();
        }
        else
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayPickUp();

            transform.position = originalPosition;
            ReoccupySlots();
        }
    }

    void UpdateShadows()
    {
        bool allValid = true;
        GridSlot[] matchedSlots = new GridSlot[jellyParts.Length];

        for (int i = 0; i < jellyParts.Length; i++)
        {
            GridSlot slot = FindClosestSlot(jellyParts[i].position);
            if (slot == null || slot.isOccupied)
                allValid = false;

            for (int j = 0; j < i; j++)
                if (matchedSlots[j] == slot) { allValid = false; break; }

            matchedSlots[i] = slot;
        }

        Color shadowColor = allValid ? validColor : invalidColor;

        for (int i = 0; i < activeShadows.Count; i++)
        {
            if (matchedSlots[i] != null)
            {
                activeShadows[i].SetActive(true);
                activeShadows[i].transform.position = new Vector3(
                    matchedSlots[i].transform.position.x, 0.02f,
                    matchedSlots[i].transform.position.z);
                SetShadowColor(activeShadows[i], shadowColor);
            }
            else
            {
                activeShadows[i].SetActive(true);
                activeShadows[i].transform.position = new Vector3(
                    jellyParts[i].position.x, 0.02f,
                    jellyParts[i].position.z);
                SetShadowColor(activeShadows[i], invalidColor);
            }
        }
    }

    void CreateShadows()
    {
        DestroyShadows();
        if (shadowPrefab == null) return;
        foreach (Transform part in jellyParts)
        {
            GameObject shadow = Instantiate(shadowPrefab);
            shadow.SetActive(false);
            activeShadows.Add(shadow);
        }
    }

    void SetShadowColor(GameObject shadow, Color color)
    {
        Renderer rend = shadow.GetComponent<Renderer>();
        if (rend != null) rend.material.color = color;
    }

    void DestroyShadows()
    {
        foreach (GameObject s in activeShadows)
            if (s != null) Destroy(s);
        activeShadows.Clear();
    }

    void SetShapeTransparency(float alpha)
    {
        foreach (Renderer rend in partsRenderers)
        {
            if (rend.material.HasProperty("_Color"))
            {
                Color c = rend.material.color;
                c.a = alpha;
                rend.material.color = c;
            }
        }
    }

    void FreePreviousSlots()
    {
        GridSlot[] allSlots = FindObjectsByType<GridSlot>(FindObjectsSortMode.None);
        foreach (GridSlot slot in allSlots)
            if (slot.occupant != null && slot.occupant.transform.IsChildOf(transform))
            { slot.isOccupied = false; slot.occupant = null; }
    }

    void ReoccupySlots()
    {
        foreach (Transform part in jellyParts)
        {
            GridSlot slot = FindClosestSlot(part.position);
            if (slot != null) { slot.isOccupied = true; slot.occupant = part.gameObject; }
        }
    }

    GridSlot FindClosestSlot(Vector3 partPosition)
    {
        GridSlot[] allSlots = FindObjectsByType<GridSlot>(FindObjectsSortMode.None);
        float closestDistance = float.MaxValue;
        GridSlot bestSlot = null;

        float scaleX = transform.localScale.x;
        float scaleZ = transform.localScale.z;

        foreach (GridSlot slot in allSlots)
        {
            float adjustedX = transform.position.x + (partPosition.x - transform.position.x) / scaleX;
            float adjustedZ = transform.position.z + (partPosition.z - transform.position.z) / scaleZ;

            float dist = Vector2.Distance(
                new Vector2(adjustedX, adjustedZ),
                new Vector2(slot.transform.position.x, slot.transform.position.z));

            if (dist < 0.7f && dist < closestDistance)
            { closestDistance = dist; bestSlot = slot; }
        }
        return bestSlot;
    }
}