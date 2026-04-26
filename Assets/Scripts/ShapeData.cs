using UnityEngine;

public class ShapeData : MonoBehaviour
{
    [Header("Grid cells")]
    public Vector2Int[] cells;

    void OnDrawGizmos()
{
    if (cells == null) return;
    
    float s = GridManager.Instance != null ? GridManager.Instance.spacing : 1.2f;
    
    foreach (Vector2Int cell in cells)
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.4f);
        Vector3 pos = transform.position + new Vector3(cell.x * s, 0.1f, cell.y * s);
        Gizmos.DrawCube(pos, new Vector3(s * 0.9f, 0.1f, s * 0.9f));
    }
}
}