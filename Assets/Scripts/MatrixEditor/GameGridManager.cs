using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameGridManager : MonoBehaviour
{
    [SerializeField]
    private VoxelGridData _voxelData;
    [SerializeField]
    private GridView _view;
    [SerializeField]
    private GridVisualCell _gridViewCellPrefab;

    private GridVisualCell[,] _cells;
    private PlaceableObject[,] _placeables;

    public VoxelGridData GetGridData => _voxelData;
    public GridView View => _view;

    // El resto del proyecto (GameManager, GridController) sigue hablando en
    // términos de Surface, así que se deriva de la vista en vez de duplicar estado.
    public PlaceableSurface Surface => _view == GridView.Floor ? PlaceableSurface.Floor : PlaceableSurface.Wall;

    const int MIN_DISTANCE = 4;

    private Vector2Int ViewSize => GridViewProjection.ViewSize(_view, _voxelData);
    public int Width => ViewSize.x;
    public int Height => ViewSize.y;

    private Vector3Int ToVoxel(int u, int v) => GridViewProjection.ToVoxel(_view, u, v, _voxelData);

    public bool IsWithinBounds(int u, int v)
    {
        Vector2Int size = ViewSize;
        return u >= 0 && v >= 0 && u < size.x && v < size.y;
    }

    public bool IsCellEmpty(int u, int v)
    {
        if (!IsWithinBounds(u, v)) return false;
        Vector3Int vox = ToVoxel(u, v);
        return _voxelData.GetType(vox.x, vox.y, vox.z) == CellType.Empty;
    }

    public void ClearCell(int u, int v)
    {
        if (!IsWithinBounds(u, v)) return;
        Vector3Int vox = ToVoxel(u, v);
        _voxelData.SetType(vox.x, vox.y, vox.z, CellType.Empty);
        _voxelData.SetItem(vox.x, vox.y, vox.z, null);
    }

    public Vector3 GetWorldPosition(int u, int v, Vector3 offset)
    {
        Vector3 localPos = new Vector3(u, 0f, v) + offset;
        return transform.TransformPoint(localPos);
    }

    public void ClearPlacedItems()
    {
        if (_voxelData == null || _voxelData._cells == null) return;

        for (int i = 0; i < _voxelData._cells.Length; i++)
        {
            _voxelData._cells[i].type = CellType.Empty;
            _voxelData._cells[i].item = null;
            _voxelData._cells[i].rotation = Quaternion.identity;
        }
    }

    public void Init()
    {
        if (_voxelData == null) return;

        Vector2Int size = ViewSize;
        _placeables = new PlaceableObject[size.x, size.y];

        if (SceneManager.GetActiveScene().name != "PreparationScene") return;

        _cells = new GridVisualCell[size.x, size.y];

        for (int v = 0; v < size.y; v++)
        {
            for (int u = 0; u < size.x; u++)
            {
                Vector3 localPos = new Vector3(u + 0.5f, 0f, v + 0.5f);
                Vector3 worldPos = transform.TransformPoint(localPos);

                GridVisualCell cell = Instantiate(_gridViewCellPrefab, worldPos, transform.rotation, transform);

                cell.Init(u, v);
                _cells[u, v] = cell;
            }
        }
    }

    public void SetGridVisible(bool visible)
    {
        if (_cells == null) return;

        Vector2Int size = ViewSize;
        for (int v = 0; v < size.y; v++)
            for (int u = 0; u < size.x; u++)
                if (_cells[u, v] != null)
                    _cells[u, v].gameObject.SetActive(visible);
    }

    public bool UpdateVisualCell(int newU, int newV, int startU, int startV, PlaceableItemData item)
    {
        Vector2Int size = ViewSize;
        if (newU < 0 || newV < 0 || newU >= size.x || newV >= size.y) return false;

        bool valid = CanPlaceItem(newU, newV, startU, startV, item);

        if (valid)
            _cells[newU, newV].SetState(CellVisualState.Empty);
        else
            _cells[newU, newV].SetState(CellVisualState.Blocked);

        return valid;
    }

    public void ResetVisualGrid()
    {
        Vector2Int size = ViewSize;
        for (int v = 0; v < size.y; v++)
        {
            for (int u = 0; u < size.x; u++)
            {
                _cells[u, v].SetState(CellVisualState.Default);
                Vector3Int vox = ToVoxel(u, v);
                if (_voxelData.GetIsEntrance(vox.x, vox.y, vox.z))
                {
                    _cells[u, v].SetState(CellVisualState.Blocked);
                }
            }
        }
    }

    public void ClearLastCell(int lastU, int lastV)
    {
        Vector2Int size = ViewSize;
        if (lastU < 0 || lastV < 0 || lastU >= size.x || lastV >= size.y) return;

        _cells[lastU, lastV].SetState(CellVisualState.Default);
    }

    public void SaveGrid(int newU, int newV, int startU, int startV, PlaceableItemData itemData, Quaternion rotation = default)
    {
        if (_voxelData == null) return;

        Vector2Int size = ViewSize;
        if (newU < 0 || newV < 0 || newU >= size.x || newV >= size.y) return;

        Vector3Int newVox = ToVoxel(newU, newV);
        _voxelData.SetType(newVox.x, newVox.y, newVox.z, CellType.Occupied);
        _voxelData.SetItem(newVox.x, newVox.y, newVox.z, itemData);
        _voxelData.SetRotation(newVox.x, newVox.y, newVox.z, rotation == default ? Quaternion.identity : rotation);

        bool hasValidStart = startU != -1 && startV != -1;
        bool movedToNewCell = startU != newU || startV != newV;

        if (hasValidStart && movedToNewCell)
        {
            Vector3Int startVox = ToVoxel(startU, startV);
            _voxelData.SetType(startVox.x, startVox.y, startVox.z, CellType.Empty);
            _voxelData.SetItem(startVox.x, startVox.y, startVox.z, null);
            _voxelData.SetRotation(startVox.x, startVox.y, startVox.z, Quaternion.identity);
            _placeables[newU, newV] = _placeables[startU, startV];
            _placeables[startU, startV] = null;
        }
    }

    public void PlaceableGenerator()
    {
        Transform placeableFolder = GameObject.Find("PlaceableItems")?.transform;
        if (placeableFolder == null)
            placeableFolder = new GameObject("PlaceableItems").transform;

        Vector2Int size = ViewSize;

        for (int v = 0; v < size.y; v++)
        {
            for (int u = 0; u < size.x; u++)
            {
                Vector3Int vox = ToVoxel(u, v);
                GridCell cell = _voxelData.GetCell(vox.x, vox.y, vox.z);
                if (_voxelData.GetType(vox.x, vox.y, vox.z) != CellType.Occupied || cell.item == null) continue;

                Vector3 localPos = new Vector3(u, 0f, v) + cell.item.placementOffset;
                Vector3 worldPos = transform.TransformPoint(localPos);

                Quaternion spawnRot = cell.rotation != Quaternion.identity ? cell.rotation : transform.rotation;
                GameObject instance = Instantiate(cell.item.prefab, worldPos, spawnRot, placeableFolder);

                PlaceableObject placeable = instance.GetComponent<PlaceableObject>();

                if (placeable != null)
                {
                    placeable.SetGridManager(this);
                    placeable.InstancePlaceableObjectCreated(u, v);
                    placeable.Init(cell.item);
                    _placeables[u, v] = placeable;
                }
            }
        }

        if (_view == GridView.Floor)
        {
            for (int v = 0; v < size.y; v++)
            {
                for (int u = 0; u < size.x; u++)
                {
                    PlaceableObject placeable = _placeables[u, v];
                    if (placeable == null) continue;
                    if (placeable.GetItemData().category == PlaceableCategory.Chair)
                        RotateTowardsTable(placeable, u, v);
                }
            }

            if (SceneController.Instance.IsSceneLoaded("PreparationScene"))
                ValidateAllChairs();
        }
    }

    public void ValidateAllChairs()
    {
        if (_view != GridView.Floor) return;

        Vector2Int size = ViewSize;
        for (int v = 0; v < size.y; v++)
        {
            for (int u = 0; u < size.x; u++)
            {
                Vector3Int vox = ToVoxel(u, v);
                GridCell cell = _voxelData.GetCell(vox.x, vox.y, vox.z);

                if (cell.item != null && cell.item.category == PlaceableCategory.Chair)
                {
                    PlaceableObject chair = _placeables[u, v];

                    if (chair == null) continue;

                    if (CountAdjacentTables(u, v) == 1 && IsCellReachableFromEntrance(u, v)) chair.SetValid(true);
                    else chair.SetValid(false);
                }
            }
        }
    }

    public bool CheckPlaceables()
    {
        if (CountChairs() > 0 && CountTables() > 0)
            return true;
        return false;
    }

    public int CountChairs()
    {
        int numChairs = 0;
        Vector2Int size = ViewSize;
        for (int v = 0; v < size.y; v++)
        {
            for (int u = 0; u < size.x; u++)
            {
                Vector3Int vox = ToVoxel(u, v);
                GridCell cell = _voxelData.GetCell(vox.x, vox.y, vox.z);

                if (cell.item != null && cell.item.category == PlaceableCategory.Chair)
                    numChairs++;
            }
        }
        return numChairs;
    }

    public int CountTables()
    {
        int numTables = 0;
        Vector2Int size = ViewSize;
        for (int v = 0; v < size.y; v++)
        {
            for (int u = 0; u < size.x; u++)
            {
                Vector3Int vox = ToVoxel(u, v);
                GridCell cell = _voxelData.GetCell(vox.x, vox.y, vox.z);

                if (cell.item != null && cell.item.category == PlaceableCategory.Table)
                    numTables++;
            }
        }
        return numTables;
    }

    public bool IsValidTablePlacement(int posU, int posV, int startU, int startV)
    {
        if (CountAdjacentTables(posU, posV, startU, startV) > 0)
            return true;

        Vector2Int size = ViewSize;
        for (int v = 0; v < size.y; v++)
        {
            for (int u = 0; u < size.x; u++)
            {
                if (u == startU && v == startV) continue;

                Vector3Int vox = ToVoxel(u, v);
                GridCell cell = _voxelData.GetCell(vox.x, vox.y, vox.z);

                if (cell.item == null || cell.item.category != PlaceableCategory.Table) continue;

                int du = Mathf.Abs(u - posU);
                int dv = Mathf.Abs(v - posV);

                int dist = du + dv;

                if (dist == 1) continue;
                if (dist < MIN_DISTANCE) return false;
            }
        }
        return true;
    }

    public void RotateTowardsTable(PlaceableObject obj, int u, int v)
    {
        if (GetAdjacentTableDirection(u, v) == Vector2Int.zero) return;
        obj.transform.rotation = GetChairRotation(u, v);
    }

    // rotación que toma una silla mirando a su mesa adyacente; rotación del grid si no hay ninguna
    public Quaternion GetChairRotation(int u, int v)
    {
        Vector2Int dir = GetAdjacentTableDirection(u, v);
        if (dir == Vector2Int.zero) return transform.rotation;

        float angle = 0f;
        if (dir == Vector2Int.up) angle = 0f;
        else if (dir == Vector2Int.down) angle = 180f;
        else if (dir == Vector2Int.right) angle = 90f;
        else if (dir == Vector2Int.left) angle = -90f;

        return Quaternion.Euler(0f, angle, 0f);
    }

    public bool CanStartDay()
    {
        Vector2Int size = ViewSize;
        for (int v = 0; v < size.y; v++)
        {
            for (int u = 0; u < size.x; u++)
            {
                Vector3Int vox = ToVoxel(u, v);
                var cell = _voxelData.GetCell(vox.x, vox.y, vox.z);

                if (cell.item != null && cell.item.category == PlaceableCategory.Chair)
                {
                    if (!CanPlaceItem(u, v, u, v, cell.item))
                        return false;
                }
            }
        }
        if (!CheckPlaceables())
            return false;
        return true;
    }

    public bool CanPlaceItem(int u, int v, int startU, int startV, PlaceableItemData item, bool ignoreChairRules = false)
    {
        Vector2Int size = ViewSize;
        if (u < 0 || v < 0 || u >= size.x || v >= size.y)
            return false;

        Vector3Int vox = ToVoxel(u, v);
        if (_voxelData.GetType(vox.x, vox.y, vox.z) != CellType.Empty && !(u == startU && v == startV))
            return false;

        if (item.category == PlaceableCategory.Chair && !ignoreChairRules)
        {
            if (CountAdjacentTables(u, v, startU, startV) != 1)
                return false;
            if (!IsCellReachableFromEntrance(u, v))
                return false;
        }
        if (item.category == PlaceableCategory.Table)
        {
            if (!IsValidTablePlacement(u, v, startU, startV))
                return false;
        }

        return true;
    }

    public int CountAdjacentTables(int u, int v, int ignoreU = -1, int ignoreV = -1)
    {
        int count = 0;

        if (IsTable(u, v + 1, ignoreU, ignoreV)) count++;
        if (IsTable(u, v - 1, ignoreU, ignoreV)) count++;
        if (IsTable(u + 1, v, ignoreU, ignoreV)) count++;
        if (IsTable(u - 1, v, ignoreU, ignoreV)) count++;

        return count;
    }

    public Vector2Int GetAdjacentTableDirection(int u, int v)
    {
        if (IsTable(u, v + 1)) return Vector2Int.up;
        if (IsTable(u, v - 1)) return Vector2Int.down;
        if (IsTable(u + 1, v)) return Vector2Int.right;
        if (IsTable(u - 1, v)) return Vector2Int.left;

        return Vector2Int.zero;
    }

    // una mesa no se puede levantar mientras tenga sillas arrimadas
    public bool HasAdjacentChairs(int u, int v)
    {
        return IsChair(u, v + 1) || IsChair(u, v - 1) || IsChair(u + 1, v) || IsChair(u - 1, v);
    }

    private bool IsChair(int u, int v)
    {
        Vector2Int size = ViewSize;
        if (u < 0 || v < 0 || u >= size.x || v >= size.y)
            return false;

        Vector3Int vox = ToVoxel(u, v);
        GridCell cell = _voxelData.GetCell(vox.x, vox.y, vox.z);
        return cell.item != null && cell.item.category == PlaceableCategory.Chair;
    }

    private bool IsTable(int u, int v, int ignoreU = -1, int ignoreV = -1)
    {
        Vector2Int size = ViewSize;
        if (u < 0 || v < 0 || u >= size.x || v >= size.y)
            return false;

        if (u == ignoreU && v == ignoreV)
            return false;

        Vector3Int vox = ToVoxel(u, v);
        GridCell cell = _voxelData.GetCell(vox.x, vox.y, vox.z);

        if (cell.item != null)
        {
            if (cell.item.category == PlaceableCategory.Table)
                return true;
        }
        return false;
    }

    public bool IsCellReachableFromEntrance(int targetU, int targetV)
    {
        Vector2Int size = ViewSize;
        bool[,] visited = new bool[size.x, size.y];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        for (int v = 0; v < size.y; v++)
        {
            for (int u = 0; u < size.x; u++)
            {
                Vector3Int vox = ToVoxel(u, v);
                if (_voxelData.GetIsEntrance(vox.x, vox.y, vox.z))
                {
                    queue.Enqueue(new Vector2Int(u, v));
                    visited[u, v] = true;
                }
            }
        }

        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            if (current.x == targetU && current.y == targetV)
                return true;

            foreach (var dir in directions)
            {
                int nu = current.x + dir.x;
                int nv = current.y + dir.y;

                if (nu < 0 || nv < 0 || nu >= size.x || nv >= size.y) continue;
                if (visited[nu, nv]) continue;

                bool isTarget = (nu == targetU && nv == targetV);

                Vector3Int nvox = ToVoxel(nu, nv);
                bool isWalkable = _voxelData.GetType(nvox.x, nvox.y, nvox.z) == CellType.Empty
                            || _voxelData.GetIsEntrance(nvox.x, nvox.y, nvox.z);

                if (isTarget || isWalkable)
                {
                    visited[nu, nv] = true;
                    queue.Enqueue(new Vector2Int(nu, nv));
                }
            }
        }
        return false;
    }
    public PlaceableObject GetPlaceableAt(int u, int v) { return _placeables[u, v]; }
    public void SetPlaceableAt(int u, int v, PlaceableObject obj) { _placeables[u, v] = obj; }
}