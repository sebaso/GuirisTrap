using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SaveManager))]
public class SaveManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);

        GUI.enabled = Application.isPlaying;
        if (GUILayout.Button("Force Save"))
            ((SaveManager)target).ForceSave();
        GUI.enabled = true;

        if (!Application.isPlaying)
            EditorGUILayout.HelpBox("Force Save only available in Play Mode.", MessageType.Info);
    }
}
