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
        if (current.renderType == RenderType.Map3D)
        {
            current.waterLevelMultiplier = EditorGUILayout.Slider("Water Level Multiplier", current.waterLevelMultiplier, 0.1f, 10f);
            current.tile3DHeightMultiplier = EditorGUILayout.Slider("Tile3D Height Multiplier", current.tile3DHeightMultiplier, 0.1f, 10f);
        }
        if (current.borderType != BorderType.Neutral)
        {
            current.northBorderWidth = EditorGUILayout.IntSlider("Border Width (N)", current.northBorderWidth, 0, 30);
            current.southBorderWidth = EditorGUILayout.IntSlider("Border Width (S)", current.southBorderWidth, 0, 30);
            current.eastBorderWidth = EditorGUILayout.IntSlider("Border Width (E)", current.eastBorderWidth, 0, 30);
            current.westBorderWidth = EditorGUILayout.IntSlider("Border Width (W)", current.westBorderWidth, 0, 30);
            current.borderIntensity = EditorGUILayout.Slider("Border Intensity", current.borderIntensity, 0f, 2f);
        }
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
            current.Save(manual: true);
        if (GUILayout.Button("Load"))
            current.Load();

        if (GUI.changed)
        {
            if (current.autoUpdate)
                current.GenerateMap();
            current.UpdateMarker();
        }
        serializedObject.ApplyModifiedProperties();
    }

    private void OnValidate()
    {
        if (current.mapWidth < 1)
            current.mapWidth = 1;
        if (current.mapDepth < 1)
            current.mapDepth = 1;
    }

}
