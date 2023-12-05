using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapCreator))]
public class MapCreatorInspector : Editor
{
    public MapCreator current
    {
        get
        {
            return (MapCreator)target;
        }
    }

    public override void OnInspectorGUI()
    {

        DrawDefaultInspector();
        if (GUILayout.Button("Generate"))
            current.GenerateMap();
        if (GUILayout.Button("Clear"))
            current.Clear();
        if (GUILayout.Button("Grow"))
            current.Grow();
        if (GUILayout.Button("Shrink"))
            current.Shrink();
        if (GUILayout.Button("Grow Area"))
            current.GrowArea();
        if (GUILayout.Button("Shrink Area"))
            current.ShrinkArea();
        if (GUILayout.Button("Save"))
            current.Save();
        if (GUILayout.Button("Load"))
            current.Load();

        if (GUI.changed) {
            if (current.autoUpdate)
                current.GenerateMap();
            current.UpdateMarker();
        }
    }
}
