using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class MapCreator : MonoBehaviour
{
    [SerializeField] GameObject tileSelectionIndicatorPrefab;
    [SerializeField] GameObject emptyTile;
    [SerializeField] GameObject waterLevelPrefab;

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
            if(_waterLevel == null)
            {
                GameObject instance = Instantiate(waterLevelPrefab);
                _waterLevel = instance.transform;
            }
            return _waterLevel;
        }
    }
    Transform _waterLevel;

    Dictionary<Point, Tile> tiles = new Dictionary<Point, Tile>();
    [SerializeField] int mapWidth;
    [SerializeField] int mapDepth;
    [SerializeField] int mapHeight;
    [Range(0.1f, 10f)][SerializeField] float tileHeightMultiplier;
    [Range(-2f, 2f)][SerializeField] float globalLift;
    [Range(0.1f, 10f)][SerializeField] float waterLevelMultiplier = 2.1f;
    public Noise[] noiseLayers;

    [SerializeField] Point pos;
    [SerializeField] LevelData levelData;
    [SerializeField] string saveName;

    public bool autoUpdate;

    public TerrainType[] regions;

    public void GenerateMap()
    {
        Clear();
        List<float[,]> layers = new List<float[,]>();
        foreach (Noise layer in noiseLayers)
        {
            layers.Add(layer.GenerateNoiseMap(mapWidth, mapHeight));
        }
        waterLevel.position = new Vector3(_waterLevel.position.x, tileHeightMultiplier * waterLevelMultiplier, _waterLevel.position.z);

        for (int i = 0; i < mapDepth * mapWidth; i++)
        {
            int x = i / mapDepth;
            int y = i % mapDepth;
            float currentHeight = 0;
            foreach (float[,] layer in layers)
            {
                currentHeight += layer[x, y];
            }
            currentHeight += globalLift;
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
    }

    private TerrainType GetTerrainFromHeight(float h)
    {
        for (int i = 0; i < regions.Length; i++)
        {
            if (h <= regions[i].height)
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
        TerrainType tt = GetTerrainFromHeight(h);
        Tile t = Instantiate(emptyTile).AddComponent<Tile>();
        t.transform.parent = transform;
        t.Load(p, h, tt, tileHeightMultiplier);
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
        if (t.height < mapHeight)
            t.Grow();
        t.terrain = GetTerrainFromHeight(t.height);
        t.UpdateVisual();
    }

    void ShrinkSingle(Point p)
    {
        if (!tiles.ContainsKey(p))
            return;

        Tile t = tiles[p];
        t.Shrink();
        t.terrain = GetTerrainFromHeight(t.height);
        t.UpdateVisual();


        // if (t.height <= 0)
        // {
        //     tiles.Remove(p);
        //     DestroyImmediate(t.gameObject);
        // }
    }

    public void Grow()
    {
        GrowSingle(pos);
    }

    public void Shrink()
    {
        ShrinkSingle(pos);
    }

    public void UpdateMarker()
    {
        Tile t = tiles.ContainsKey(pos) ? tiles[pos] : null;
        marker.localPosition = t != null ? t.center : new Vector3(pos.x, 0, pos.y);
    }

    public void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; --i)
            DestroyImmediate(transform.GetChild(i).gameObject);
        tiles.Clear();
    }

    public void Save()
    {
        string filePath = Application.dataPath + "/Resources/Levels";
        if (!Directory.Exists(filePath))
            CreateSaveDirectory();

        LevelData board = ScriptableObject.CreateInstance<LevelData>();
        board.positions = new List<Vector3>(tiles.Count);
        board.terrains = new List<TerrainType>(tiles.Count);
        foreach (Tile t in tiles.Values)
        {
            board.positions.Add(new Vector3(t.pos.x, t.height, t.pos.y));
            board.terrains.Add(t.terrain);
        }

        string fileName = string.Format("Assets/Resources/Levels/{1}.asset", filePath, saveName);
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
