using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SaveManager))]
public class SaveManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Los campos de _data (day, money, grids) se dibujan aquí: se pueden
        // editar a mano y luego persistir con "Save Edited Values".
        DrawDefaultInspector();

        SaveManager manager = (SaveManager)target;

        GUILayout.Space(10);

        // Sincroniza con MoneyManager y las grids de la escena: solo en Play.
        GUI.enabled = Application.isPlaying;
        if (GUILayout.Button("Force Save (sync live state)"))
            manager.ForceSave();
        GUI.enabled = true;

        // Persiste los valores editados a mano sin sincronizar nada más.
        // Funciona también fuera de Play Mode.
        if (GUILayout.Button("Save Edited Values"))
        {
            manager.WriteCurrentData();
            AssetDatabase.Refresh();
        }

        GUILayout.Space(5);

        // Reset destructivo, con confirmación.
        Color prev = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f);
        if (GUILayout.Button("Reset Save"))
        {
            if (EditorUtility.DisplayDialog(
                    "Reset Save",
                    "This will clear all saved data and delete the save file on disk. This cannot be undone.",
                    "Reset", "Cancel"))
            {
                manager.ResetSave();
            }
        }
        GUI.backgroundColor = prev;

        GUILayout.Space(5);

        EditorGUILayout.LabelField("Save file", manager.HasSaveFile ? "exists" : "none");
        EditorGUILayout.LabelField("Path", manager.SaveFilePath);

        if (!Application.isPlaying)
            EditorGUILayout.HelpBox(
                "Force Save (which pulls live money/grids) only works in Play Mode. " +
                "Use 'Save Edited Values' to persist the fields above, or 'Reset Save' to wipe the file.",
                MessageType.Info);
    }
}
