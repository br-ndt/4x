using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public enum BorderType
{
    Neutral,
    Ocean,
    Land,
}

public enum RenderType
{
    Map2D,
    Map3D,
}

public class MapCreator : MonoBehaviour
{

    [SerializeField] GameObject tileSelectionIndicatorPrefab;
    [SerializeField] GameObject emptyTile2D;
    [SerializeField] GameObject emptyTile3D;
    [SerializeField] GameObject waterLevelPrefab;
    [SerializeField] TerrainType[] regions;

    [SerializeField] int mapWidth;
    [SerializeField] int mapDepth;
    [SerializeField] int mapHeight;
    public Noise[] noiseLayers;
    [Range(0.1f, 10f)][SerializeField] float globalHeightMultiplier = 1f;
    [Range(-2f, 2f)][SerializeField] float globalLift = 0f;
    [SerializeField] public BorderType borderType;
    [HideInInspector][Range(0, 30)] public int northBorderWidth;
    [HideInInspector][Range(0, 30)] public int southBorderWidth;
    [HideInInspector][Range(0, 30)] public int eastBorderWidth;
    [HideInInspector][Range(0, 30)] public int westBorderWidth;
    [HideInInspector][Range(0.1f, 2f)] public float borderIntensity = 2f;
    [SerializeField] public RenderType renderType;
    [HideInInspector][Range(0.1f, 10f)] public float waterLevelMultiplier = 2.1f;
    [HideInInspector][Range(0.1f, 10f)] public float tile3DHeightMultiplier = 1f;
    Transform marker
    {
        get
        {
            if (_marker == null)
            {
                GameObject instance = Instantiate(tileSelectionIndicatorPrefab);
                _marker = instance.transform;
            }
            return _marker;
        }
    }
    Transform _marker;

    Transform waterLevel
    {
        get
        {
            if (_waterLevel == null)
            {
                GameObject instance = Instantiate(waterLevelPrefab);
                _waterLevel = instance.transform;
            }
            return _waterLevel;
        }
    }
    Transform _waterLevel;

    Dictionary<Point, Tile> tiles = new Dictionary<Point, Tile>();

    [SerializeField] Point selectedPosition;

    public bool autoUpdate;
    [SerializeField] LevelData levelData;
    [SerializeField] string saveName;

    public void GenerateMap()
    {
        Clear();
        List<float[,]> layers = new List<float[,]>();
        foreach (Noise layer in noiseLayers)
        {
            layers.Add(layer.GenerateNoiseMap(mapWidth, mapHeight));
        }
        if (renderType == RenderType.Map3D)
        {
            waterLevel.position = new Vector3(_waterLevel.position.x, tile3DHeightMultiplier * waterLevelMultiplier, _waterLevel.position.z);
        }

        float borderMod = 0f;
        if (borderType == BorderType.Ocean)
        {
            borderMod = -borderIntensity;
        }
        else if (borderType == BorderType.Land)
        {
            borderMod = borderIntensity;
        }

        for (int i = 0; i < mapDepth * mapWidth; i++)
        {
            int x = i % mapWidth;
            int y = i / mapWidth;
            float currentHeight = 0;
            foreach (float[,] layer in layers)
            {
                currentHeight += layer[x, y];
            }
            currentHeight *= globalHeightMultiplier;
            currentHeight += globalLift;

            if (borderType != BorderType.Neutral)
            {
                if (northBorderWidth > 0 && y >= mapHeight - northBorderWidth)
                {
                    currentHeight += (float)(y - (mapHeight - northBorderWidth)) / northBorderWidth * borderMod;
                }
                else if (southBorderWidth > 0 && y <= southBorderWidth)
                {
                    currentHeight += (float)(southBorderWidth - y) / southBorderWidth * borderMod;
                }
                if (eastBorderWidth > 0 && x >= mapWidth - eastBorderWidth)
                {
                    currentHeight += (float)(x - (mapWidth - eastBorderWidth)) / eastBorderWidth * borderMod;
                }
                else if (westBorderWidth > 0 && x <= westBorderWidth)
                {
                    currentHeight += (float)(westBorderWidth - x) / westBorderWidth * borderMod;
                }
            }

            if (currentHeight < -1)
            {
                currentHeight = -1;
            }
            else if (currentHeight > 1)
            {
                currentHeight = 1;
            }
            for (int r = 0; r < regions.Length; r++)
            {
                if (currentHeight <= regions[r].height)
                {
                    GetOrCreate(new Vector3(x, currentHeight, y));
                    break;
                }
            }
        }
        Save();
    }

    private TerrainType GetTerrainFromHeight(float height)
    {
        for (int i = 0; i < regions.Length; i++)
        {
            if (height <= regions[i].height)
            {
                return regions[i];
            }
        }
        return regions[^1];
    }


    private void OnValidate()
    {
        if (mapWidth < 1)
            mapWidth = 1;
        if (mapDepth < 1)
            mapDepth = 1;
    }

    public void GrowArea()
    {
        Rect r = RandomRect();
        GrowRect(r);
    }

    public void ShrinkArea()
    {
        Rect r = RandomRect();
        ShrinkRect(r);
    }

    Rect RandomRect()
    {
        int x = Random.Range(0, mapWidth);
        int y = Random.Range(0, mapDepth);
        int w = Random.Range(1, mapWidth - x + 1);
        int h = Random.Range(1, mapDepth - y + 1);
        return new Rect(x, y, w, h);
    }

    void GrowRect(Rect rect)
    {
        for (int y = (int)rect.yMin; y < (int)rect.yMax; ++y)
        {
            for (int x = (int)rect.xMin; x < (int)rect.xMax; ++x)
            {
                Point p = new Point(x, y);
                GrowSingle(p);
            }
        }
    }

    void ShrinkRect(Rect rect)
    {
        for (int y = (int)rect.yMin; y < (int)rect.yMax; ++y)
        {
            for (int x = (int)rect.xMin; x < (int)rect.xMax; ++x)
            {
                Point p = new Point(x, y);
                ShrinkSingle(p);
            }
        }
    }

    Tile CreateTile(Point p, float h)
    {
        Tile t;
        if (renderType == RenderType.Map2D)
            t = Instantiate(emptyTile2D).AddComponent<Tile2D>();
        else
        {
            t = Instantiate(emptyTile3D).AddComponent<Tile3D>();
            ((Tile3D)t).stepHeight = tile3DHeightMultiplier;
        }
        t.transform.parent = transform;
        t.Position = p;
        t.Height = h;
        t.Terrain = GetTerrainFromHeight(h);
        tiles.Add(p, t);

        return t;
    }

    Tile GetOrCreate(Vector3 position)
    {
        Point p = new Point((int)position.x, (int)position.z);
        if (tiles.ContainsKey(p))
            return tiles[p];

        Tile t = CreateTile(p, position.y);

        return t;
    }

    Tile GetOrCreate(Point p)
    {
        if (tiles.ContainsKey(p))
            return tiles[p];

        Tile t = CreateTile(p, 0);

        return t;
    }

    void GrowSingle(Point p)
    {
        Tile t = GetOrCreate(p);
        if (t.Height < mapHeight)
            t.Grow();
        t.Terrain = GetTerrainFromHeight(t.Height);
    }

    void ShrinkSingle(Point p)
    {
        if (!tiles.ContainsKey(p))
            return;

        Tile t = tiles[p];
        t.Shrink();
        t.Terrain = GetTerrainFromHeight(t.Height);
    }

    public void Grow()
    {
        GrowSingle(selectedPosition);
    }

    public void Shrink()
    {
        ShrinkSingle(selectedPosition);
    }

    public void UpdateMarker()
    {
        Tile t = tiles.ContainsKey(selectedPosition) ? tiles[selectedPosition] : null;
        marker.localPosition = t != null ? t.Center : new Vector3(selectedPosition.x, 0, selectedPosition.y);
    }

    public void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; --i)
            DestroyImmediate(transform.GetChild(i).gameObject);
        DestroyImmediate(waterLevel.gameObject);
        tiles.Clear();
    }

    public void Save(bool manual = false)
    {
        string filePath = "Assets/Resources/Levels";
        string random = Randomizer.RandomString(length: 6);
        if (!Directory.Exists(filePath) || !Directory.Exists(string.Format("{0}/Random", filePath)))
            CreateSaveDirectory();


        LevelData board = ScriptableObject.CreateInstance<LevelData>();
        board.positions = new List<Vector3>(tiles.Count);
        board.terrains = new List<TerrainType>(tiles.Count);
        foreach (Tile t in tiles.Values)
        {
            board.positions.Add(new Vector3(t.Position.x, t.Height, t.Position.y));
            board.terrains.Add(t.Terrain);
        }
        board.is3D = renderType == RenderType.Map3D;
        board.tileHeightMultiplier = board.is3D ? tile3DHeightMultiplier : 0;
        board.waterLevelMultiplier = waterLevelMultiplier;

        string fileName = manual ? string.Format("{0}/{1}.asset", filePath, saveName) : string.Format("{0}/Random/{1}.asset", filePath, random);
        AssetDatabase.CreateAsset(board, fileName);
    }

    void CreateSaveDirectory()
    {
        string filePath = Application.dataPath + "/Resources";
        if (!Directory.Exists(filePath))
            AssetDatabase.CreateFolder("Assets", "Resources");
        filePath += "/Levels";
        if (!Directory.Exists(filePath))
            AssetDatabase.CreateFolder("Assets/Resources", "Levels");
        filePath += "/Random";
        if (!Directory.Exists(filePath))
            AssetDatabase.CreateFolder("Assets/Resources/Levels", "Random");
        AssetDatabase.Refresh();
    }

    public void Load()
    {
        Clear();
        if (levelData == null)
            return;
        for (int i = 0; i < levelData.positions.Count; ++i)
        {
            Debug.Log(i);
            GetOrCreate(levelData.positions[i]);
        }
    }
}
