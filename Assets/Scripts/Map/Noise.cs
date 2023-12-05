using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Noise
{
    [SerializeField] private int seed;
    [Range(0.1f, 100f)][SerializeField] private float scale;
    [Range(0, 100)][SerializeField] private int octaves;
    [Range(0.01f, 1f)][SerializeField] private float startAmplitude;
    [Range(0f, 1f)][SerializeField] private float persistance;
    [Range(0.1f, 5f)][SerializeField] private float lacunarity;
    [SerializeField] private Vector2 offset;

    public float[,] GenerateNoiseMap(int mapWidth, int mapHeight)
    {
        if (mapWidth <= 0 || mapHeight <= 0 || scale <= 0)
        {
            return null;
        }
        float[,] noiseMap = new float[mapWidth + 1, mapHeight + 1];

        System.Random psuedoRandom = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i++)
        {
            float offsetX = psuedoRandom.Next(-100000, 100000) + offset.x;
            float offsetY = psuedoRandom.Next(-100000, 100000) + offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        float maxNoiseHeight = 1f;
        float minNoiseHeight = -1f;

        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;

        for (int i = 0; i < mapHeight * mapWidth; i++)
        {
            int x = i % mapWidth;
            int y = i / mapWidth;
            float amplitude = startAmplitude;
            float frequency = 1f;
            float noiseHeight = 0;

            for (int j = 0; j < octaves; j++)
            {
                float sampleX = (x - halfWidth) / scale * frequency + octaveOffsets[j].x;
                float sampleY = (y - halfHeight) / scale * frequency + octaveOffsets[j].y;

                float perlinValue = Mathf.Clamp(Mathf.PerlinNoise(sampleX, sampleY), 0f, 1f) * 2 - 1;
                noiseHeight += perlinValue * amplitude;

                amplitude *= persistance;
                frequency *= lacunarity;
            }

            if (noiseHeight > maxNoiseHeight)
            {
                noiseHeight = maxNoiseHeight;
            }
            else if (noiseHeight < minNoiseHeight)
            {
                noiseHeight = minNoiseHeight;
            }
            noiseMap[x, y] = noiseHeight;
        }
        return noiseMap;
    }
}
