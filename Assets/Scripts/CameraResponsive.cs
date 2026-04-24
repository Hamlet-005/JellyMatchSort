using UnityEngine;

public class CameraResponsive : MonoBehaviour
{
    public GridManager grid;

    [Header("Zoom Settings")]
    public float padding = -1f;   // որքան մոտ լինի camera-ն

    void Start()
    {
        AdjustCamera();
    }

    void AdjustCamera()
    {
        if (grid == null)
        {
            Debug.LogError("Grid reference not set!");
            return;
        }

        Camera cam = GetComponent<Camera>();

        float gridWidth = grid.cols * grid.spacing;
        float gridHeight = grid.rows * grid.spacing;

        float aspect = (float)Screen.width / Screen.height;

        float size;

        // ընտրում ենք ճիշտ fit (width vs height)
        if (aspect >= 1f)
        {
            // wide screen
            size = gridHeight / 2f;
        }
        else
        {
            // tall screen
            size = (gridWidth / aspect) / 2f;
        }

        // 🎯 camera zoom (մոտիկացում)
        cam.orthographicSize = Mathf.Max(1f, size + padding);
    }

    void Update()
    {
        // եթե ուզում ես runtime resize support
        if (Input.GetKeyDown(KeyCode.R))
        {
            AdjustCamera();
        }
    }
}