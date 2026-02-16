using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;


public class GridToolEditor : EditorWindow
{
    #region Variables
    private int _fieldWidth = 5;
    private int _fieldHeight = 5;
    private int _gridWidth;
    private int _gridHeight;
    
    private int _fieldWarehouseStartY = 9;
    private int _fieldWarehouseEndY = 9;

    private GridCell[,] _editorGrid;

    private GridData gridData;

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

        _fieldWarehouseStartY = EditorGUILayout.IntField("Start Warehouse Y",_fieldWarehouseStartY);
        _fieldWarehouseEndY = EditorGUILayout.IntField("End Warehouse Y",_fieldWarehouseEndY);

        GUILayout.Space(10);

        gridData= (GridData)EditorGUILayout.ObjectField(
            "Grid Data",
            gridData,
            typeof(GridData),
            false
        );

        if(GUILayout.Button("Load Grid"))
        {
            if(gridData == null)
            {
                Debug.LogWarning("No GridData assigned");
                return;
            }
            LoadGridFromData(gridData);
            Repaint();
        }
        
        if(GUILayout.Button("Create New Grid"))
        {
            if(_fieldWidth > 0 && _fieldHeight > 0)
            {
                _gridWidth =_fieldWidth;
                _gridHeight = _fieldHeight;
                CreateGrid();
                Repaint();
            }
        }
        if (GUILayout.Button("Erase Grid"))
        {
            EraseGrid();
        }
        GUILayout.Space(10);

        GUILayout.Label("Grid", EditorStyles.boldLabel);

        GUILayout.Space(10);

        DrawGrid();

        GUILayout.Space(10);
        if(GUILayout.Button("Empty Occuped Grid"))
        {
        if(_editorGrid != null && _editorGrid.Length > 0 && _editorGrid.GetLength(0) > 0)
            {
                EmptyOccupiedGrid(gridData);
                Repaint();
            }        
        }
        if(GUILayout.Button("Save Grid"))
        {
            if(_editorGrid != null && _editorGrid.Length > 0 && _editorGrid.GetLength(0) > 0)
            {
                SaveGrid(gridData);
            }
        }
    }
    #endregion
    #region Grid
    //  Genera la matriz en el editor con los valores proporcionados por el usuario
    private void CreateGrid()
    {
        _editorGrid = new GridCell[_gridWidth, _gridHeight];

        for (int y = _gridHeight - 1; y >= 0; y--)
        {
            for (int x = 0; x < _gridWidth; x++)
            {
                _editorGrid[x, y] = new GridCell();
            }
        }
    }
    //  Guarda el grid en el scriptable object
    private void SaveGrid(GridData data)
    {
        data.widht = _gridWidth;
        data.height = _gridHeight;
        data.gridWarehouseStartY = _fieldWarehouseStartY;
        data.gridWarehouseEndY = _fieldWarehouseEndY;
        data._cells = new GridCell [_gridWidth * _gridHeight];
        for (int i = 0; i < data._cells.Length; i++)
        {
            data._cells[i] = new GridCell();
        }
        for(int y = 0; y < _gridHeight; y++)
        {
            for(int x = 0; x < _gridWidth; x++)
            {
                if(y >= _fieldWarehouseStartY && y <= _fieldWarehouseEndY)
                {
                    data.SetIsWarehouse(x,y,true);
                }
                data.SetType(x,y,_editorGrid[x,y].type);
            }
        }
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
    }
    //  Carga en el visualizador el grid
    private void LoadGridFromData(GridData data)
    {
        _gridWidth = data.widht;
        _gridHeight = data.height;
        _fieldWidth = _gridWidth;
        _fieldHeight = _gridHeight;
        _fieldWarehouseStartY = data.gridWarehouseStartY;
        _fieldWarehouseEndY = data.gridWarehouseEndY;
        _editorGrid = new GridCell[_gridWidth, _gridHeight];

        for(int y = 0; y < _gridHeight; y++)
        {
            for(int x = 0; x < _gridWidth; x++)
            {
                _editorGrid[x,y] = new GridCell
                {
                    type = data.GetType(x,y),
                    isWarehouse = data.GetIsWarehouse(x,y)
                };
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
        string label = "E";
        if(cell.type == CellType.Blocked)
        {
            label = "B";
        }else if(cell.type == CellType.Empty)
        {
            label = "E";
        }
        else if(cell.type == CellType.Occupied)
        {
            label = "O";
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

        for (int y = _gridHeight - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < _gridWidth; x++)
            {
                DrawCell(x, y);
            }
            EditorGUILayout.EndHorizontal();
        }
    }
    // Opciones disponibles en los botones de la matriz
    private void ShowOptionsMenu(GridCell cell)
    {
        GenericMenu optionsMenu = new GenericMenu();

        optionsMenu.AddItem(new GUIContent("Blocked"), cell.type == CellType.Blocked, () =>
        {
            cell.type = CellType.Blocked;
        });

        optionsMenu.AddItem(new GUIContent("Occupied"), cell.type == CellType.Occupied, () =>
        {
            cell.type = CellType.Occupied;
        });

        optionsMenu.AddSeparator("");

        optionsMenu.AddItem(new GUIContent("Empty"), cell.type == CellType.Empty, () =>
        {
            cell.type = CellType.Empty;
        });

        optionsMenu.ShowAsContext();
    }
    //  Libera las celdas ocupadas
    private void EmptyOccupiedGrid(GridData gridData)
    {
        for (int y = _gridHeight - 1; y >= 0; y--)
        {
            for (int x = 0; x < _gridWidth; x++)
            {
                if(_editorGrid[x,y].type == CellType.Occupied)
                    _editorGrid[x,y].type = CellType.Empty;
            }
        }    
    }

    // Borra la matriz
    private void EraseGrid()
    {
        _editorGrid = null;
    }
    #endregion
}