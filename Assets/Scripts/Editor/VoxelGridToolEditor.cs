using UnityEditor;
using UnityEngine;

public class VoxelGridToolEditor : EditorWindow
{
    private enum Layer { Floor, WallNorth, WallEast, WallWest }

    private VoxelGridData _voxelData;
    private int _width = 12;
    private int _height = 3;
    private int _depth = 7;

    private Layer _activeLayer = Layer.Floor;
    private Vector2 _scroll;

    [MenuItem("Tools/Voxel Grid Tool")]
    public static void Open()
    {
        GetWindow<VoxelGridToolEditor>("Voxel Grid Tool");
    }

    private void OnGUI()
    {
        GUILayout.Label("Voxel Grid Data", EditorStyles.boldLabel);

        _voxelData = (VoxelGridData)EditorGUILayout.ObjectField("Voxel Grid Data", _voxelData, typeof(VoxelGridData), false);

        GUILayout.Space(10);

        _width = EditorGUILayout.IntField("Width (X)", _width);
        _height = EditorGUILayout.IntField("Height (Y)", _height);
        _depth = EditorGUILayout.IntField("Depth (Z)", _depth);

        GUILayout.Space(10);

        EditorGUI.BeginDisabledGroup(_voxelData == null || _width <= 0 || _height <= 0 || _depth <= 0);
        if (GUILayout.Button("Crear / Reiniciar matriz"))
        {
            _voxelData.Init(_width, _height, _depth);
            EditorUtility.SetDirty(_voxelData);
            AssetDatabase.SaveAssets();
            Debug.Log($"[VoxelGridToolEditor] Matriz inicializada: {_width}x{_height}x{_depth} en {_voxelData.name}");
        }
        EditorGUI.EndDisabledGroup();

        if (_voxelData == null)
        {
            EditorGUILayout.HelpBox("Asigna un VoxelGridData para poder inicializarlo.", MessageType.Info);
            return;
        }

        GUILayout.Space(20);

        _activeLayer = (Layer)GUILayout.Toolbar((int)_activeLayer,
            new[] { "Suelo", "Pared Norte", "Pared Este", "Pared Oeste" });

        GUILayout.Space(10);

        if (GUILayout.Button("Guardar cambios en disco"))
        {
            EditorUtility.SetDirty(_voxelData);
            AssetDatabase.SaveAssets();
            Debug.Log("[VoxelGridToolEditor] Guardado.");
        }

        EditorGUILayout.HelpBox(
            "Clic en una celda para cambiar su tipo o marcarla como entrada. " +
            "'Occupied' aquí deja la celda ocupada SIN item (solo para pruebas); " +
            "la colocación real de objetos la gestiona GridManager.PlaceItem en Play.",
            MessageType.Info);

        GUILayout.Space(10);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        DrawLayerGrid();
        EditorGUILayout.EndScrollView();
    }

    // Tamaño (u,v) de la capa activa.
    private Vector2Int GetLayerSize()
    {
        return _activeLayer switch
        {
            Layer.Floor     => new Vector2Int(_voxelData.width, _voxelData.depth),
            Layer.WallNorth => new Vector2Int(_voxelData.width, _voxelData.height),
            Layer.WallEast  => new Vector2Int(_voxelData.depth, _voxelData.height),
            Layer.WallWest  => new Vector2Int(_voxelData.depth, _voxelData.height),
            _ => Vector2Int.zero
        };
    }


    private Vector3Int GetVoxelCoords(int u, int v)
    {
        switch (_activeLayer)
        {
            case Layer.Floor:     return new Vector3Int(u, 0, v);
            case Layer.WallNorth: return new Vector3Int(u, v, _voxelData.depth - 1);
            case Layer.WallEast:  return new Vector3Int(_voxelData.width - 1, v, u);
            default:              return new Vector3Int(0, v, u); // WallWest
        }
    }

    private void DrawLayerGrid()
    {
        Vector2Int size = GetLayerSize();
        if (size.x <= 0 || size.y <= 0) return;

        for (int v = size.y - 1; v >= 0; v--)
        {
            EditorGUILayout.BeginHorizontal();
            for (int u = 0; u < size.x; u++)
                DrawCell(u, v);
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawCell(int u, int v)
    {
        Vector3Int voxel = GetVoxelCoords(u, v);

        GUIStyle style = new GUIStyle(GUI.skin.button) { fixedWidth = 40, fixedHeight = 40 };

        CellType type = _voxelData.GetType(voxel.x, voxel.y, voxel.z);
        bool isEntrance = _voxelData.GetIsEntrance(voxel.x, voxel.y, voxel.z);

        string label = type switch
        {
            CellType.Blocked  => "B",
            CellType.Occupied => "O",
            _ => "E"
        };
        if (isEntrance) label += "*";

        Color prevColor = GUI.backgroundColor;
        GUI.backgroundColor = type switch
        {
            CellType.Blocked  => Color.red,
            CellType.Occupied => Color.yellow,
            _ => Color.white
        };

        if (GUILayout.Button(label, style))
            ShowCellMenu(voxel);

        GUI.backgroundColor = prevColor;
    }

    private void ShowCellMenu(Vector3Int voxel)
    {
        GenericMenu menu = new GenericMenu();
        CellType current = _voxelData.GetType(voxel.x, voxel.y, voxel.z);
        bool isEntrance = _voxelData.GetIsEntrance(voxel.x, voxel.y, voxel.z);

        menu.AddItem(new GUIContent("Empty"), current == CellType.Empty, () => SetCellType(voxel, CellType.Empty));
        menu.AddItem(new GUIContent("Blocked"), current == CellType.Blocked, () => SetCellType(voxel, CellType.Blocked));
        menu.AddItem(new GUIContent("Occupied (sin item, solo pruebas)"), current == CellType.Occupied, () => SetCellType(voxel, CellType.Occupied));
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("Es entrada"), isEntrance, () => ToggleEntrance(voxel));

        menu.ShowAsContext();
    }

    private void SetCellType(Vector3Int voxel, CellType type)
    {
        _voxelData.SetType(voxel.x, voxel.y, voxel.z, type);

        if (type != CellType.Occupied)
        {
            _voxelData.SetItem(voxel.x, voxel.y, voxel.z, null);
            _voxelData.SetAnchor(voxel.x, voxel.y, voxel.z, default);
        }

        EditorUtility.SetDirty(_voxelData);
        Repaint();
    }

    private void ToggleEntrance(Vector3Int voxel)
    {
        bool current = _voxelData.GetIsEntrance(voxel.x, voxel.y, voxel.z);
        _voxelData.SetIsEntrance(voxel.x, voxel.y, voxel.z, !current);
        EditorUtility.SetDirty(_voxelData);
        Repaint();
    }
}