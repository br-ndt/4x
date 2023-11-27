using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class MapCreator : MonoBehaviour
{
    [SerializeField] GameObject tileSelectionIndicatorPrefab;
    [SerializeField] GameObject emptyTile;

    Transform marker
    {
        get
        {
            if (_marker == null)
            {
                GameObject instance = Instantiate(tileSelectionIndicatorPrefab) as GameObject;
                _marker = instance.transform;
            }
            return _marker;
        }
    }
    Transform _marker;

    Dictionary<Point, Tile> tiles = new Dictionary<Point, Tile>();
    [SerializeField] int mapWidth;
    [SerializeField] int mapDepth;
    [SerializeField] int mapHeight;
    [SerializeField] float noiseScale;

    [SerializeField] int octaves;
    [Range(0, 1)]
    public float persistance;
    [SerializeField] float lacunarity;

    [SerializeField] int seed;
    [SerializeField] Vector2 offset;

    [SerializeField] float tileHeightMultiplier;
    [SerializeField] AnimationCurve tileHeightCurve;

    [SerializeField] Point pos;
    [SerializeField] LevelData levelData;
    [SerializeField] string saveName;

    public bool autoUpdate;

    public TerrainType[] regions;

    public void GenerateMap()
    {
        Clear();
        float[,] noiseMap = Noise.GenerateNoiseMap(mapWidth, mapDepth, seed, noiseScale, octaves, persistance, lacunarity, offset);

        for (int i = 0; i < mapDepth * mapWidth; i++)
        {
            int x = i / mapDepth;
            int y = i % mapDepth;
            float currentHeight = noiseMap[x, y];
            for (int j = 0; j < regions.Length; j++)
            {
                if (currentHeight <= regions[j].height)
                {
                    float scaledHeight = tileHeightCurve.Evaluate(currentHeight) * tileHeightMultiplier;
                    Point point = new Point(x, y);
                    tiles.Add(point, CreateTile(point, currentHeight, regions[j]));
                    break;
                }
            }
        }
    }

    private Tile CreateTile(Point p, float h, TerrainType tt)
    {
        Tile thisTile = Instantiate(emptyTile).AddComponent<Tile>();
        thisTile.transform.parent = transform;
        thisTile.Load(p, h, tt);
        return thisTile;
    }

    private void OnValidate()
    {
        if (mapWidth < 1)
            mapWidth = 1;
        if (mapDepth < 1)
            mapDepth = 1;
        if (lacunarity < 1)
            lacunarity = 1;
        if (octaves < 0)
            octaves = 0;
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
        int x = UnityEngine.Random.Range(0, mapWidth);
        int y = UnityEngine.Random.Range(0, mapDepth);
        int w = UnityEngine.Random.Range(1, mapWidth - x + 1);
        int h = UnityEngine.Random.Range(1, mapDepth - y + 1);
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

    Tile Create()
    {
        Tile instance = Instantiate(emptyTile).AddComponent<Tile>();
        instance.transform.parent = transform;
        return instance;
    }

    Tile GetOrCreate(Point p)
    {
        if (tiles.ContainsKey(p))
            return tiles[p];

        Tile t = Create();
        t.Load(p, 0, regions[0]);
        tiles.Add(p, t);

        return t;
    }

    void GrowSingle(Point p)
    {
        Tile t = GetOrCreate(p);
        if (t.height < mapHeight)
            t.Grow();
    }

    void ShrinkSingle(Point p)
    {
        if (!tiles.ContainsKey(p))
            return;

        Tile t = tiles[p];
        t.Shrink();

        if (t.height <= 0)
        {
            tiles.Remove(p);
            DestroyImmediate(t.gameObject);
        }
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
            Tile t = Create();
            t.Load(levelData.positions[i], levelData.terrains[i]);
            tiles.Add(t.pos, t);
        }
    }
}
