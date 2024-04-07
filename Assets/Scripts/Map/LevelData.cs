﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class LevelData : ScriptableObject
{
    public List<Vector3> positions;
    public List<TerrainType> terrains;
    public bool is3D;
    public float tileHeightMultiplier;
    public float waterLevelMultiplier;
}
