using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode { NoiseMap, ColorMap, Mesh}
    public DrawMode drawMode;

    public const int mapChunkSize = 241;
    [Range(0,6)]
    public int levelOfDetail;
    public float noiseScale;

    public int octaves;
    [Range(0f, 1)]
    public float persistance;
    public float lacunarity;

    public int seed;
    public Vector2 offset;
    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;
    
    public bool autoUpdate;

    public TerrainType[] regions;

    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new();

    public void DrawMapInEditor()
    {
        MapData mapData = GenerateMapData();

        MapDisplay display = FindObjectOfType<MapDisplay>();
        switch (drawMode)
        {
            case DrawMode.NoiseMap:
                display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
                break;
            case DrawMode.ColorMap:
                display.DrawTexture(TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
                break;
            case DrawMode.Mesh:
                display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail), TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
                break;
        }
    }

    public void RequestMapData(Action<MapData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(callback);
        };

        new Thread(threadStart).Start();
    }

    void MapDataThread(Action<MapData> callback)
    {
        MapData mapData = GenerateMapData();
        lock(mapDataThreadInfoQueue)
        {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    public void RequestMeshData(MapData mapdata, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(mapdata, callback);
        };

        new Thread(threadStart).Start();
    }

    void MeshDataThread(MapData mapdata, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapdata.heightMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail);
        lock(meshDataThreadInfoQueue)
        {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    private void Update()
    {
        if(mapDataThreadInfoQueue.Count > 0)
        {
            for(int i = 0; i < mapDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                threadInfo.callBack(threadInfo.parameter);
            }
        }
        if (meshDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < meshDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callBack(threadInfo.parameter);
            }
        }
    }

    MapData GenerateMapData()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistance, lacunarity, offset);

        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];

        for (int y = 0; y < mapChunkSize; y++)
        {
            for (int x = 0; x < mapChunkSize; x++)
            {
                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < regions.Length; i++)
                {
                    if (currentHeight <= regions[i].height)
                    {
                        colorMap[y * mapChunkSize + x] = regions[i].color;
                        break;
                    }
                }
            }
        }
        return new MapData(noiseMap, colorMap);
    }

    private void OnValidate()
    {
        if(lacunarity < 1)
        {
            lacunarity = 1;
        }
        if(octaves < 0)
        {
            octaves = 0;
        }
    }

    struct MapThreadInfo<T>
    {
        public readonly Action<T> callBack;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callBack, T parameter)
        {
            this.callBack = callBack;
            this.parameter = parameter;
        }
    }

    [System.Serializable]
    public struct TerrainType
    {
        public readonly string name;
        public readonly float height;
        public readonly Color color;
    }

    public struct MapData
    {
        public readonly float[,] heightMap;
        public readonly Color[] colorMap;

        public MapData(float[,] heightMap, Color[] colorMap)
        {
            this.heightMap = heightMap;
            this.colorMap = colorMap;
        }

    }
}
