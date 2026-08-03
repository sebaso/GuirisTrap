using UnityEditor;
using UnityEngine;

public class VoxelGridToolEditor : EditorWindow
{
    private VoxelGridData _voxelData;
    private int _width = 12;
    private int _height = 3;
    private int _depth = 7;

    [MenuItem("Tools/Voxel Grid Tool")]
    public static void Open()
    {
        GetWindow<VoxelGridToolEditor>("Voxel Grid Tool");
    }

    private void OnGUI()
    {
        GUILayout.Label("Voxel Grid Data", EditorStyles.boldLabel);

        _voxelData = (VoxelGridData)EditorGUILayout.ObjectField(
            "Voxel Grid Data", _voxelData, typeof(VoxelGridData), false);

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
            EditorGUILayout.HelpBox("Asigna un VoxelGridData para poder inicializarlo.", MessageType.Info);
    }
}