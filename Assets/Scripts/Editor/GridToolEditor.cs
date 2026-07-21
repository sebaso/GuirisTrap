using UnityEditor;
using UnityEngine;

public class GridToolEditor : EditorWindow
{
    #region Variables
    private int _fieldWidth = 12;
    private int _fieldHeightVoxel = 3;
    private int _fieldDepth = 7;

    private GridView _activeView = GridView.Floor;
    private GridCell[,] _editorGrid;
    private Vector2Int _viewSize;

    private VoxelGridData _voxelData;
    private Vector2 _scrollbar;
    #endregion

    #region Editor
    [MenuItem("Tools/Grid Generator")]
    public static void Open()
    {
        GetWindow<GridToolEditor>("Grid Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Voxel Grid Settings", EditorStyles.boldLabel);

        _fieldWidth = EditorGUILayout.IntField("Width (X)", _fieldWidth);
        _fieldHeightVoxel = EditorGUILayout.IntField("Height (Y)", _fieldHeightVoxel);
        _fieldDepth = EditorGUILayout.IntField("Depth (Z)", _fieldDepth);

        GUILayout.Space(10);

        _voxelData = (VoxelGridData)EditorGUILayout.ObjectField(
            "Voxel Grid Data", _voxelData, typeof(VoxelGridData), false);

        if (GUILayout.Button("Create New Voxel Grid"))
        {
            if (_voxelData == null)
                Debug.LogWarning("Asigna un VoxelGridData antes de crear la matriz");
            else if (_fieldWidth > 0 && _fieldHeightVoxel > 0 && _fieldDepth > 0)
            {
                CreateVoxelData();
                LoadViewFromData();
                Repaint();
            }
        }

        GUILayout.Space(10);

        EditorGUI.BeginDisabledGroup(_voxelData == null);

        GridView newView = (GridView)EditorGUILayout.EnumPopup("Vista", _activeView);
        if (newView != _activeView)
        {
            if (_editorGrid != null) SaveView();
            _activeView = newView;
            LoadViewFromData();
        }

        if (GUILayout.Button("Reload Vista from Data"))
        {
            LoadViewFromData();
            Repaint();
        }

        EditorGUI.EndDisabledGroup();

        GUILayout.Space(10);

        _scrollbar = EditorGUILayout.BeginScrollView(_scrollbar);
        GUILayout.Label($"Grid — {_activeView}", EditorStyles.boldLabel);
        GUILayout.Space(10);

        DrawGrid();

        GUILayout.Space(10);

        if (GUILayout.Button("Empty Occupied (vista actual)") && _editorGrid != null)
            EmptyOccupiedGrid();

        if (GUILayout.Button("Save Vista") && _editorGrid != null)
            SaveView();

        EditorGUILayout.EndScrollView();
    }
    #endregion

    #region Grid
    private void CreateVoxelData()
    {
        _voxelData.width = _fieldWidth;
        _voxelData.height = _fieldHeightVoxel;
        _voxelData.depth = _fieldDepth;

        int total = _fieldWidth * _fieldHeightVoxel * _fieldDepth;
        _voxelData._cells = new GridCell[total];
        for (int i = 0; i < total; i++)
            _voxelData._cells[i] = new GridCell();

        EditorUtility.SetDirty(_voxelData);
        AssetDatabase.SaveAssets();
    }

    private void LoadViewFromData()
    {
        if (_voxelData == null || _voxelData._cells == null) return;

        _viewSize = GridViewProjection.ViewSize(_activeView, _voxelData);
        _editorGrid = new GridCell[_viewSize.x, _viewSize.y];

        for (int v = 0; v < _viewSize.y; v++)
        {
            for (int u = 0; u < _viewSize.x; u++)
            {
                Vector3Int voxel = GridViewProjection.ToVoxel(_activeView, u, v, _voxelData);
                GridCell real = _voxelData.GetCell(voxel.x, voxel.y, voxel.z);
                _editorGrid[u, v] = new GridCell { type = real.type, isEntrance = real.isEntrance };
            }
        }
    }

    private void SaveView()
    {
        for (int v = 0; v < _viewSize.y; v++)
        {
            for (int u = 0; u < _viewSize.x; u++)
            {
                Vector3Int voxel = GridViewProjection.ToVoxel(_activeView, u, v, _voxelData);
                _voxelData.SetType(voxel.x, voxel.y, voxel.z, _editorGrid[u, v].type);
                _voxelData.SetIsEntrance(voxel.x, voxel.y, voxel.z, _editorGrid[u, v].isEntrance);
            }
        }
        EditorUtility.SetDirty(_voxelData);
        AssetDatabase.SaveAssets();
    }

    private void DrawCell(int u, int v)
    {
        GridCell cell = _editorGrid[u, v];

        GUIStyle style = new GUIStyle(GUI.skin.button) { fixedHeight = 40, fixedWidth = 40 };

        string label = cell.type switch
        {
            CellType.Blocked  => "B",
            CellType.Occupied => "O",
            _ => "E"
        };
        if (cell.isEntrance) label += "*";

        if (GUILayout.Button(label, style))
            ShowOptionsMenu(cell);
    }

    private void DrawGrid()
    {
        if (_editorGrid == null) return;

        for (int v = _viewSize.y - 1; v >= 0; v--)
        {
            EditorGUILayout.BeginHorizontal();
            for (int u = 0; u < _viewSize.x; u++)
                DrawCell(u, v);
            EditorGUILayout.EndHorizontal();
        }
    }

    private void ShowOptionsMenu(GridCell cell)
    {
        GenericMenu optionsMenu = new GenericMenu();
        optionsMenu.AddItem(new GUIContent("Blocked"), cell.type == CellType.Blocked, () => cell.type = CellType.Blocked);
        optionsMenu.AddItem(new GUIContent("Occupied"), cell.type == CellType.Occupied, () => cell.type = CellType.Occupied);
        optionsMenu.AddItem(new GUIContent("Empty"), cell.type == CellType.Empty, () => cell.type = CellType.Empty);
        optionsMenu.AddSeparator("");
        optionsMenu.AddItem(new GUIContent("Entrance"), cell.isEntrance, () => cell.isEntrance = !cell.isEntrance);
        optionsMenu.ShowAsContext();
    }

    private void EmptyOccupiedGrid()
    {
        for (int v = 0; v < _viewSize.y; v++)
            for (int u = 0; u < _viewSize.x; u++)
                if (_editorGrid[u, v].type == CellType.Occupied)
                    _editorGrid[u, v].type = CellType.Empty;
    }
    #endregion
}