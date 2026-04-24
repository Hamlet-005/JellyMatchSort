using UnityEngine;
using System.Collections.Generic;

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
    private Vector3 originalPosition;
    private bool isDragging = false;
    private Camera mainCamera;
    private List<GameObject> activeShadows = new List<GameObject>();
    private bool isPlacedOnGrid = false;

    void Start()
    {
        mainCamera = Camera.main;
        originalPosition = transform.position;
        SetupParts();
        SetupCollider();
    }

    // LevelManager-ը կանչում է սա նոր մոդել բերելիս
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
        foreach (MeshRenderer rend in renderers) { partsList.Add(rend.transform); rendererList.Add(rend); }
        jellyParts = partsList.ToArray();
        partsRenderers = rendererList.ToArray();
        foreach (Transform part in jellyParts) { if (part.GetComponent<Collider>() == null) part.gameObject.AddComponent<BoxCollider>(); }
    }

    void SetupCollider() {
        BoxCollider col = GetComponent<BoxCollider>();
        if (jellyParts.Length == 0) return;
        Bounds bounds = new Bounds(jellyParts[0].localPosition, Vector3.one);
        foreach (Transform part in jellyParts) bounds.Encapsulate(new Bounds(part.localPosition, Vector3.one));
        col.center = bounds.center; col.size = bounds.size;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) HandleSelection();
        if (Input.GetMouseButton(0) && isDragging) HandleDragging();
        if (Input.GetMouseButtonUp(0) && isDragging) HandleRelease();
    }

    void HandleSelection() {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            if (hit.transform == transform || hit.transform.IsChildOf(transform)) StartDrag();
    }

    void StartDrag() {
        isDragging = true;
        if (isPlacedOnGrid) { FreePreviousSlots(); isPlacedOnGrid = false; }
        CreateShadows(); SetShapeTransparency(dragTransparency);
    }

    void HandleDragging() {
        Plane movePlane = new Plane(Vector3.up, new Vector3(0, 0.5f, 0));
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (movePlane.Raycast(ray, out float dist)) transform.position = ray.GetPoint(dist) + Vector3.up * 0.5f;
        // UpdateShadows() կանչը այստեղ
    }

    void HandleRelease()
{
    isDragging = false;
    DestroyShadows();
    SetShapeTransparency(1.0f);

    if (!HandleDrop())
    {
        transform.position = originalPosition;

        // Հայտնում ենք LevelManager-ին, որ այս մոդելը վերադարձավ
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnModelReturned(this.gameObject);
        }
    }
}
    bool HandleDrop() { // Փոխել ենք վերադարձվող տիպը bool-ի
    GridSlot[] foundSlots = new GridSlot[jellyParts.Length];
    bool allValid = true;

    for (int i = 0; i < jellyParts.Length; i++) {
        GridSlot slot = FindClosestSlot(jellyParts[i].position);
        
        // Խիստ ստուգում. եթե թեկուզ մեկ կտոր սլոթի վրա չէ կամ զբաղված է
        if (slot == null || slot.isOccupied) {
            allValid = false;
            break;
        }
        
        // Ստուգում ենք, որ նույն սլոթը երկու անգամ չվերցնենք
        for (int j = 0; j < i; j++) {
            if (foundSlots[j] == slot) {
                allValid = false;
                break;
            }
        }

        if (!allValid) break;
        foundSlots[i] = slot;
    }

    if (allValid && foundSlots.Length == jellyParts.Length) {
        // SNAP LOGIC: Հավասարեցնում ենք ճիշտ սլոթի կոորդինատներով
        Vector3 offset = transform.position - jellyParts[0].position;
        transform.position = new Vector3(
            foundSlots[0].transform.position.x + offset.x,
            0.5f, // Հստակ բարձրություն
            foundSlots[0].transform.position.z + offset.z
        );

        // Գրանցում ենք սլոթերը որպես զբաղված
        for (int i = 0; i < foundSlots.Length; i++) {
            foundSlots[i].isOccupied = true;
            foundSlots[i].occupant = gameObject;
        }

        isPlacedOnGrid = true;

        if (LevelManager.Instance != null) {
            LevelManager.Instance.OnModelPlaced(originalPosition);
        }

        if (GridManager.Instance != null) {
            GridManager.Instance.CheckWinCondition();
        }
        return true;
    }
    
    return false;
}
    void SetShapeTransparency(float a) { foreach (var r in partsRenderers) { if (r.material.HasProperty("_Color")) { Color c = r.material.color; c.a = a; r.material.color = c; } } }
    void CreateShadows() { if (shadowPrefab == null) return; foreach (var p in jellyParts) activeShadows.Add(Instantiate(shadowPrefab)); }
    void DestroyShadows() { foreach (var s in activeShadows) if(s != null) Destroy(s); activeShadows.Clear(); }
    
    void FreePreviousSlots() {
        GridSlot[] allSlots = FindObjectsByType<GridSlot>(FindObjectsSortMode.None);
        foreach (GridSlot slot in allSlots)
            if (slot.occupant == gameObject) { slot.isOccupied = false; slot.occupant = null; }
    }

    GridSlot FindClosestSlot(Vector3 pos) {
        GridSlot[] slots = FindObjectsByType<GridSlot>(FindObjectsSortMode.None);
        GridSlot best = null; float min = 0.7f;
        foreach (var s in slots) { float d = Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(s.transform.position.x, s.transform.position.z)); if (d < min) { min = d; best = s; } }
        return best;
    }
}