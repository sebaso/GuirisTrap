using UnityEditor;
using UnityEngine;


public class GridToolEditor : EditorWindow
{
    #region Variables
    private int _fieldWidth = 5;
    private int _fieldHeight = 5;
    private int _gridWidth;
    private int _gridHeight;
    private GameObject _wallPrefab;
    private GameObject _floorPrefab;
    private GameObject _squarePrefab;
    private GridCell[,] _editorGrid;
    #endregion
    #region Editor
    [MenuItem("Tools/Grid Generator")]
    public static void Open()
    {
        GetWindow<GridToolEditor>("Grid Generator");
    }
    //  Método que se encarga de crear el editor y del flujo del programa
    private void OnGUI()
    {
        GUILayout.Label("Grid Settings", EditorStyles.boldLabel);

        _fieldWidth = EditorGUILayout.IntField("Widht", _fieldWidth);
        _fieldHeight = EditorGUILayout.IntField("Height", _fieldHeight);
        _wallPrefab = (GameObject)EditorGUILayout.ObjectField("Wall Prefab", _wallPrefab, typeof(GameObject), false);
        _floorPrefab = (GameObject)EditorGUILayout.ObjectField("Floor Prefab", _floorPrefab, typeof(GameObject), false);
        _squarePrefab = (GameObject)EditorGUILayout.ObjectField("Square Prefab", _squarePrefab, typeof(GameObject), false);
        
        GUILayout.Space(10);
        if(GUILayout.Button("Genereate Grid"))
        {
            if(_fieldWidth > 0 && _fieldHeight > 0)
            {
                _gridWidth =_fieldWidth;
                _gridHeight = _fieldHeight;
                CreateGrid();
                Repaint();
            }
        }
        if (GUILayout.Button("Clear"))
        {
            Clear();
        }
        GUILayout.Space(10);
        GUILayout.Label("Grid", EditorStyles.boldLabel);
        GUILayout.Space(10);
        DrawGrid();
        GUILayout.Space(10);
        if(GUILayout.Button("Generate chiringuito"))
        {
            if(_editorGrid != null && _editorGrid.Length > 0 && _editorGrid.GetLength(0) > 0)
            {
                GenerateChiringuito();
            }
        }
        if(GUILayout.Button("Erase chiringuito"))
        {
            EraseChiringuito();
        }
    }
    #endregion
    #region Grid
    //  Genera la matriz en el editor con los valores proporcionados por el usuario
    private void CreateGrid()
    {
        _editorGrid = new GridCell[_gridWidth, _gridHeight];

        for(int y = 0; y < _gridHeight; y++)
        {
            for(int x = 0; x < _gridWidth; x++)
            {
                _editorGrid[x,y] = new GridCell();
            }
        }
    }
    // Crea cada botón de la matriz
    private void DrawCell(int x, int y)
    {
        GridCell cell = _editorGrid[x, y];

        GUIStyle style = new GUIStyle(GUI.skin.button);
        style.fixedHeight = 40;
        style.fixedWidth = 40;
        string label = "-";
        if(cell.type == PrefabType.Floor)
        {
            label = "F";
        }else if(cell.type == PrefabType.Wall)
        {
            label = "W";
        }
        else if(cell.type == PrefabType.Square)
        {
            label = "S";
        }
        if(GUILayout.Button(label, style))
        {
            ShowOptionsMenu(cell);
        }
    }
    // Dibuja la matriz
    private void DrawGrid()
    {
        if(_editorGrid == null) return;

        for(int y = _gridHeight - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();
            for(int x = 0; x < _gridWidth; x++)
            {
                DrawCell(x,y);
            }
            EditorGUILayout.EndHorizontal();
        }
    }
    // Opciones disponibles en los botones de la matriz
    private void ShowOptionsMenu(GridCell cell)
    {
        GenericMenu optionsMenu = new GenericMenu();

        optionsMenu.AddItem(new GUIContent("Floor"), cell.type == PrefabType.Floor, () =>
        {
            cell.type = PrefabType.Floor;
        });

        optionsMenu.AddItem(new GUIContent("Wall"), cell.type == PrefabType.Wall, () =>
        {
            cell.type = PrefabType.Wall;
        });

        optionsMenu.AddItem(new GUIContent("Square"), cell.type == PrefabType.Square, () =>
        {
           cell.type = PrefabType.Square; 
        });

        optionsMenu.AddSeparator("");

        optionsMenu.AddItem(new GUIContent("None"), cell.type == PrefabType.None, () =>
        {
            cell.type = PrefabType.None;
        });

        optionsMenu.ShowAsContext();
    }
    // Borra la matriz
    private void Clear()
    {
        _editorGrid = null;
    }
    #endregion
    #region Chiringuito
    // Crea dos obbjetos vacios para organizar mejor la Hierarchy e instancia dentro los prefab de las paredes y suelos con la posición de la matriz como position
    // (anotación: como pasamos de 2D a 3D, la Y de la matriz es la Z del mundo 3D)
    private void GenerateChiringuito()
    {
        EraseChiringuito();
        GameObject wallFolder = new GameObject();
        wallFolder.name = "Walls";
        
        GameObject floorFolder = new GameObject();
        floorFolder.name = "Floors";

        GameObject squareFolder = new GameObject();
        floorFolder.name = "Squares";

        for(int y = 0; y < _gridHeight; y++)
        {
            for(int x = 0; x < _gridWidth; x++)
            {
                if(_editorGrid[x,y].type == PrefabType.Wall)
                {
                    GameObject wall = Instantiate(_wallPrefab, new Vector3(x,0f, y), Quaternion.identity, wallFolder.transform);
                    wall.transform.rotation = Quaternion.Euler(-90,0,0);
                }
                else if(_editorGrid[x,y].type == PrefabType.Floor)
                {
                    GameObject floor = Instantiate(_floorPrefab, new Vector3(x, 0f, y), Quaternion.identity, floorFolder.transform);
                }
                else if(_editorGrid[x,y].type == PrefabType.Square)
                {
                    GameObject square = Instantiate(_squarePrefab, new Vector3(x,0f, y), Quaternion.identity, squareFolder.transform);
                    square.transform.rotation = Quaternion.Euler(-90,0,0);

                }
            }
        }
    }
    // Borra los objetos vacios creados en GenerateChiringuito con todo lo que hay dentro
    private void EraseChiringuito()
    {
        var walls = GameObject.Find("Walls");
        var floors = GameObject.Find("Floors");
        var squares = GameObject.Find("Squares");
        if (walls != null)
        {
            DestroyImmediate(walls);
        }
        if (floors != null)
        {
            DestroyImmediate(floors);
        }
        if  (squares != null)
        {
            DestroyImmediate(squares);
        }
    }
    #endregion
}