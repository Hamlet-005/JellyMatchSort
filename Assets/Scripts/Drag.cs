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
    private Vector3[] localPartPositions;
    [HideInInspector] public Vector3 originalPosition;
    private bool isDragging = false;
    private Camera mainCamera;
    private List<GameObject> activeShadows = new List<GameObject>();
    private bool isPlacedOnGrid = false;
    private Vector3 dragOffset;
    private int activeTouchId = -1;

    [Header("=== MOBILE SETTINGS ===")]
    public float liftOffsetY = 1.5f;
    public float liftOffsetZ = -1.5f;

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
    }

    void SetupParts()
    {
        List<Transform> partsList = new List<Transform>();
        List<Renderer> rendererList = new List<Renderer>();
        List<Vector3> localPosList = new List<Vector3>();

        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer rend in renderers)
        {
            partsList.Add(rend.transform);
            rendererList.Add(rend);
            localPosList.Add(rend.transform.localPosition);
        }

        jellyParts = partsList.ToArray();
        partsRenderers = rendererList.ToArray();
        localPartPositions = localPosList.ToArray();

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
        // Փոքրացնում ենք քոլայդերը 10%-ով, որ հարևանների հետ կոնֆլիկտ չլինի
        col.size = bounds.size * 0.9f; 
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
                if (movePlane.Raycast(ray, out float dist))
                {
                    Vector3 clickPoint = ray.GetPoint(dist);
                    dragOffset = transform.position - clickPoint;
                }
                StartDrag();
            }
        }
    }

    void StartDrag()
    {
        isDragging = true;
        
        if (isPlacedOnGrid)
        {
            FreePreviousSlots();
            isPlacedOnGrid = false;
            if (LevelManager.Instance != null)
                LevelManager.Instance.OnModelTakenFromGrid(gameObject);
        }

        CreateShadows();
        SetShapeTransparency(dragTransparency);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayPickUp();

        transform.DOKill();
        transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
    }

    void DragTo(Vector3 screenPos)
    {
        Plane movePlane = new Plane(Vector3.up, new Vector3(0, 0.5f, 0));
        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        if (movePlane.Raycast(ray, out float dist))
        {
            Vector3 targetPos = ray.GetPoint(dist) + dragOffset;
#if !UNITY_EDITOR && !UNITY_STANDALONE
            targetPos.y += liftOffsetY;
            targetPos.z += liftOffsetZ;
#else
            targetPos.y = 0.5f;
#endif
            transform.position = targetPos;
        }
        UpdateShadows();
    }

    void HandleRelease()
    {
        isDragging = false;
        DestroyShadows();
        SetShapeTransparency(1.0f);
        transform.DOKill();

        bool successfullyDropped = HandleDrop();

        if (successfullyDropped)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayPlace();

            transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutQuad);
            if (LevelManager.Instance != null)
                LevelManager.Instance.OnModelPlaced(gameObject);
        }
        else
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayPickUp();

            float targetScale = (LevelManager.Instance != null) ? LevelManager.Instance.waitingZoneScale : 0.7f;
            
            Sequence returnSeq = DOTween.Sequence();
            returnSeq.Join(transform.DOMove(originalPosition, 0.25f).SetEase(Ease.OutCubic));
            returnSeq.Join(transform.DOScale(Vector3.one * targetScale, 0.25f).SetEase(Ease.OutCubic));
        }
    }

    bool HandleDrop()
    {
        GridSlot[] foundSlots = new GridSlot[jellyParts.Length];
        for (int i = 0; i < jellyParts.Length; i++)
        {
            Vector3 partWorldPos = jellyParts[i].position;
            GridSlot slot = FindClosestSlot(partWorldPos);
            
            if (slot == null || slot.isOccupied) return false;
            for (int j = 0; j < i; j++) if (foundSlots[j] == slot) return false;
            foundSlots[i] = slot;
        }

        Vector3 offset = transform.position - jellyParts[0].position;
        transform.position = new Vector3(
            foundSlots[0].transform.position.x + offset.x,
            0.5f,
            foundSlots[0].transform.position.z + offset.z
        );

        foreach (var slot in foundSlots)
        {
            slot.isOccupied = true;
            slot.occupant = gameObject;
        }

        isPlacedOnGrid = true;
        return true;
    }

    void UpdateShadows()
    {
        bool allValid = true;
        GridSlot[] matchedSlots = new GridSlot[jellyParts.Length];

        for (int i = 0; i < jellyParts.Length; i++)
        {
            Vector3 partWorldPos = jellyParts[i].position;
            GridSlot slot = FindClosestSlot(partWorldPos);
            
            if (slot == null || slot.isOccupied)
                allValid = false;

            for (int j = 0; j < i; j++)
                if (matchedSlots[j] == slot) { allValid = false; break; }

            matchedSlots[i] = slot;
        }

        Color shadowColor = allValid ? validColor : invalidColor;

        for (int i = 0; i < activeShadows.Count; i++)
        {
            activeShadows[i].SetActive(true);
            if (matchedSlots[i] != null)
            {
                activeShadows[i].transform.position = new Vector3(matchedSlots[i].transform.position.x, 0.05f, matchedSlots[i].transform.position.z);
                SetShadowColor(activeShadows[i], shadowColor);
            }
            else
            {
                activeShadows[i].transform.position = new Vector3(jellyParts[i].position.x, 0.05f, jellyParts[i].position.z);
                SetShadowColor(activeShadows[i], invalidColor);
            }
        }
    }

    void CreateShadows()
    {
        DestroyShadows();
        if (shadowPrefab == null) return;
        foreach (var p in jellyParts)
        {
            GameObject s = Instantiate(shadowPrefab);
            s.SetActive(false);
            activeShadows.Add(s);
        }
    }

    void SetShadowColor(GameObject shadow, Color color)
    {
        Renderer rend = shadow.GetComponent<Renderer>();
        if (rend != null) rend.material.color = color;
    }

    void DestroyShadows()
    {
        foreach (var s in activeShadows) if (s != null) Destroy(s);
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
        foreach (var slot in allSlots)
        {
            if (slot.occupant == gameObject)
            {
                slot.isOccupied = false;
                slot.occupant = null;
            }
        }
    }

    GridSlot FindClosestSlot(Vector3 pos)
    {
        GridSlot[] allSlots = FindObjectsByType<GridSlot>(FindObjectsSortMode.None);
        GridSlot best = null;
        float close = float.MaxValue;
        foreach (var s in allSlots)
        {
            float d = Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(s.transform.position.x, s.transform.position.z));
            if (d < 0.75f && d < close) { close = d; best = s; }
        }
        return best;
    }
}