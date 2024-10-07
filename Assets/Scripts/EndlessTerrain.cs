using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class EndlessTerrain : MonoBehaviour
{
    public const float maxViewDist = 500;
    public Transform viewer;
    public Material mapMaterial;

    public static Vector2 viewerPosition;
    public static MapGenerator mapGenerator; 
    int chunkSize;
    int chunkVisibleInViewDst;

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionnary = new();
    List<TerrainChunk> TerrainChunksVisibleLastUpdate = new();

    public void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();
        chunkSize = MapGenerator.mapChunkSize - 1;
        chunkVisibleInViewDst = Mathf.RoundToInt(maxViewDist / chunkSize);
    }

    public void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);
        UpdateVisibleChunk();
    }

    void UpdateVisibleChunk()
    {
        for(int i = 0; i < TerrainChunksVisibleLastUpdate.Count; i++)
        {
            TerrainChunksVisibleLastUpdate[i].SetVisible(false);
        }
        TerrainChunksVisibleLastUpdate.Clear();

        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for (int yOffset = -chunkVisibleInViewDst; yOffset <= chunkVisibleInViewDst; yOffset++)
        {
            for (int xOffset = -chunkVisibleInViewDst; xOffset <= chunkVisibleInViewDst; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if(terrainChunkDictionnary.ContainsKey(viewedChunkCoord))
                {
                    terrainChunkDictionnary[viewedChunkCoord].UpdateTerrainChunk();
                    if (terrainChunkDictionnary[viewedChunkCoord].IsVisible())
                    {
                        TerrainChunksVisibleLastUpdate.Add(terrainChunkDictionnary[viewedChunkCoord]);
                    }
                }
                else
                {
                    terrainChunkDictionnary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, transform, mapMaterial));
                }
            }
        }
    }

    public class TerrainChunk
    {
        GameObject meshObject;
        Vector2 position;
        Bounds bounds;

        MapGenerator.MapData mapData;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        public TerrainChunk(Vector2 coord, int size , Transform parent, Material material)
        {
            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();    
            meshRenderer.material = material;
            meshObject.transform.position = positionV3;
            meshObject.transform.parent = parent;
            SetVisible(false);

            mapGenerator.RequestMapData(OnMapDataReceived);
        }

        void OnMapDataReceived(MapGenerator.MapData mapData)
        {
            mapGenerator.RequestMeshData(mapData, OnMeshDataReceived);
        }

        void OnMeshDataReceived(MeshData meshData)
        {
            meshFilter.mesh = meshData.CreateMesh();
        }

        public void UpdateTerrainChunk()
        {
            float viewerDstFromNearestEdge =  Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
            bool visible = viewerDstFromNearestEdge <= maxViewDist;
            SetVisible(visible);
        }

        public void SetVisible(bool visibile)
        {
            meshObject.SetActive(visibile);
        }

        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }
    }
}
