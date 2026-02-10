using UnityEditor;
using UnityEngine;

public class GridToolEditor : EditorWindow
{
    [Header("Variables")]
    private int _width = 5;
    private int _height = 5; 
    private GameObject _wallPrefab;
    private GameObject _floorPrefab;
    [MenuItem("Tools/Grid Generator")]
    public static void Open()
    {
        GetWindow<GridToolEditor>("Grid Generator");
    }
    private void OnGUI()
    {
        GUILayout.Label("Grid Settings", EditorStyles.boldLabel);

        _width = EditorGUILayout.IntField("Ancho", _width);
        _height = EditorGUILayout.IntField("Alto", _height);
        _wallPrefab = (GameObject)EditorGUILayout.ObjectField("Prefab Pared", _wallPrefab, typeof(GameObject), false);
        _floorPrefab = (GameObject)EditorGUILayout.ObjectField("Prefab Suelo", _floorPrefab, typeof(GameObject), false);


    }
}
