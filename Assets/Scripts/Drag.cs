using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

[RequireComponent(typeof(BoxCollider))]
public class Drag : MonoBehaviour
{
    [Header("=== DESIGNER SETTINGS ===")]
    public GameObject shadowPrefab;
    public Color validColor = new Color(0f, 1f, 0f, 0.6f);
    public Color invalidColor = new Color(1f, 0f, 0f, 0.6f);
    [Range(0f, 1f)]
    public float dragTransparency = 0.5f;

    private Transform[] jellyParts;
    private Renderer[] partsRenderers;
    public Vector3 originalPosition;
    private bool isDragging = false;
    private Camera mainCamera;
    private List<GameObject> activeShadows = new List<GameObject>();
    private bool isPlacedOnGrid = false;
    private Vector3 dragOffset;
    private int activeTouchId = -1;

    void Start()
    {
        mainCamera = Camera.main;
        if (originalPosition == Vector3.zero)
            originalPosition = transform.position;
        SetupParts();
        SetupCollider();
    }

    public void UpdateOriginalPosition(Vector3 newPos)
    {
        originalPosition = newPos;
        transform.position = newPos;
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
            {
                Plane movePlane = new Plane(Vector3.up, new Vector3(0, 0.5f, 0));
                Ray offsetRay = mainCamera.ScreenPointToRay(screenPos);
                if (movePlane.Raycast(offsetRay, out float dist))
                {
                    Vector3 clickPoint = offsetRay.GetPoint(dist);
                    dragOffset = transform.position - clickPoint;
                }
                StartDrag();
            }
        }
    }

    void DragTo(Vector3 screenPos)
    {
        Plane movePlane = new Plane(Vector3.up, new Vector3(0, 0.5f, 0));
        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        if (movePlane.Raycast(ray, out float dist))
            transform.position = ray.GetPoint(dist) + dragOffset + Vector3.up * 0.5f;
        UpdateShadows();
    }

    void StartDrag()
    {
        isDragging = true;
        if (isPlacedOnGrid)
        {
            FreePreviousSlots();
            isPlacedOnGrid = false;
            if (LevelManager.Instance != null)
                LevelManager.Instance.OnModelTakenFromGrid(originalPosition);
        }
        CreateShadows();
        SetShapeTransparency(dragTransparency);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayPickUp();

        transform.DOKill();
        transform.DOScale(Vector3.one * 1.15f, 0.15f).SetEase(Ease.OutBack);
    }

    void HandleRelease()
    {
        isDragging = false;
        DestroyShadows();
        SetShapeTransparency(1.0f);

        transform.DOKill();
        transform.DOScale(Vector3.one, 0.15f).SetEase(Ease.OutBack);

        bool placed = HandleDrop();

        if (!placed)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayPickUp();

            transform.position = originalPosition;
            if (LevelManager.Instance != null)
                LevelManager.Instance.OnModelReturned(this.gameObject);
        }
    }

    bool HandleDrop()
    {
        GridSlot[] foundSlots = new GridSlot[jellyParts.Length];
        bool allValid = true;

        for (int i = 0; i < jellyParts.Length; i++)
        {
            GridSlot slot = FindClosestSlot(jellyParts[i].position);
            if (slot == null || slot.isOccupied) { allValid = false; break; }
            for (int j = 0; j < i; j++)
                if (foundSlots[j] == slot) { allValid = false; break; }
            if (!allValid) break;
            foundSlots[i] = slot;
        }

        if (allValid && foundSlots.Length == jellyParts.Length)
        {
            transform.DOKill();
            transform.localScale = Vector3.one;

            Vector3 offset = transform.position - jellyParts[0].position;
            transform.position = new Vector3(
                foundSlots[0].transform.position.x + offset.x,
                0.5f,
                foundSlots[0].transform.position.z + offset.z
            );

            for (int i = 0; i < foundSlots.Length; i++)
            {
                foundSlots[i].isOccupied = true;
                foundSlots[i].occupant = gameObject;
            }

            isPlacedOnGrid = true;

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayPlace();

            transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5, 0.5f);

            if (LevelManager.Instance != null)
                LevelManager.Instance.OnModelPlaced(originalPosition, wasOnGrid: false);

            if (GridManager.Instance != null)
                GridManager.Instance.CheckWinCondition();

            return true;
        }

        return false;
    }

    void CreateShadows()
    {
        if (shadowPrefab == null) return;
        DestroyShadows();
        foreach (var p in jellyParts)
        {
            GameObject shadow = Instantiate(shadowPrefab);
            shadow.transform.localScale = new Vector3(0.1f, 0.01f, 0.1f);
            shadow.SetActive(false);
            activeShadows.Add(shadow);
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
                    matchedSlots[i].transform.position.x, 0.01f,
                    matchedSlots[i].transform.position.z);
            }
            else
            {
                activeShadows[i].SetActive(true);
                activeShadows[i].transform.position = new Vector3(
                    jellyParts[i].position.x, 0.01f,
                    jellyParts[i].position.z);
            }

            Renderer r = activeShadows[i].GetComponent<Renderer>();
            if (r != null)
            {
                Color c = shadowColor;
                c.a = 0.6f;
                r.material.color = c;
            }
        }
    }

    void DestroyShadows()
    {
        foreach (var s in activeShadows)
            if (s != null) Destroy(s);
        activeShadows.Clear();
    }

    void SetShapeTransparency(float a)
    {
        foreach (var r in partsRenderers)
        {
            if (r.material.HasProperty("_Color"))
            {
                Color c = r.material.color;
                c.a = a;
                r.material.color = c;
            }
        }
    }

    void FreePreviousSlots()
    {
        GridSlot[] allSlots = FindObjectsByType<GridSlot>(FindObjectsSortMode.None);
        foreach (GridSlot slot in allSlots)
            if (slot.occupant == gameObject) { slot.isOccupied = false; slot.occupant = null; }
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