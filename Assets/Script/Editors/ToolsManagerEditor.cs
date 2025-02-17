using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ToolsManager))]
public class ToolsManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ToolsManager manager = (ToolsManager)target;
        if (GUILayout.Button("Test Short Confirm Action"))
        {
            manager.OnConfirmShortPressed();
        }
    }
} 