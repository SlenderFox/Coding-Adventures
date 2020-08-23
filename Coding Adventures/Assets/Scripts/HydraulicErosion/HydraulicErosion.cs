using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class HydraulicErosion : MonoBehaviour
{
    [Space(6)]
    [Header("Mesh Settings")]
    [SerializeField]
    private bool m_bLiveUpdate = false;
    [SerializeField, Range(2, 256), Tooltip("The length of one edge, size will be squared")]
    private short m_iResolution = 12;
    [SerializeField, Range(1, 50)]
    private float m_fScale = 1;
    [SerializeField, Range(0.01f, 10)]
    private float m_fHeightScale = 1;
    [SerializeField, Range(1, 20)]
    private sbyte m_sbNoiseLayers = 4;

    [Header("Erosion Settings")]
    [SerializeField]
    private ComputeShader m_cpErosion = null;
    [SerializeField, Min(1024)]
    private int m_inumDroplets = 1024;
    [SerializeField, Min(1)]
    private int m_iMaxLifetime = 90;
    [SerializeField, Range(0.00001f, 0.5f)]
    private float m_fErosionSpeed = 0.01f;
    [SerializeField, Min(0)]
    private float m_fMaxSedimentCapacity = 3;
    [SerializeField, Min(0.01f)]
    private float m_fStartWater = 2;
    [SerializeField]
    private float m_fEvaporationRate = 0.01f;

    private float[] m_fHeightMap;
    private Vector2[] m_v2Offset;
    private Mesh m_mMesh;

    internal struct Droplet
    {
        internal Droplet(Vector2Int pPosition, float pWater, float pSedimentCapacity)
        {
            position = pPosition;
            water = pWater;
            height = 0;
            prevHeight = float.PositiveInfinity;
            sediment = 0;
            sedimentCapacity = pSedimentCapacity;
            speed = 0;
        }

        internal Vector2Int position;
        internal float water;
        internal float height;
        internal float prevHeight;
        internal float sediment;
        internal float sedimentCapacity;
        internal float speed;
    }

    private void Start()
    {
        m_mMesh = new Mesh();
        gameObject.GetComponent<MeshFilter>().sharedMesh = m_mMesh;
        m_fHeightMap = new float[m_iResolution * m_iResolution];
        m_v2Offset = new Vector2[m_sbNoiseLayers];
        GenerateHeightMap();
        GenerateMesh();
    }

    private void Update()
    {
        if (m_bLiveUpdate)
        {
            UpdateNoiseLayers();
            GenerateHeightMap();
            GenerateMesh();
        }
    }

    private void UpdateNoiseLayers()
    {
        Vector2[] temp = m_v2Offset;
        m_v2Offset = new Vector2[m_sbNoiseLayers];
        for (int i = 0; i < m_v2Offset.Length; i++)
        {
            try
            {
                m_v2Offset[i] = temp[i];
            }
            catch
            {
                m_v2Offset[i].x = Random.Range(0, 1000);
                m_v2Offset[i].y = Random.Range(0, 1000);
            }
        }
    }

    private void RandomiseOffset()
    {
        m_v2Offset = new Vector2[m_sbNoiseLayers];
        for (int i = 0; i < m_sbNoiseLayers; i++)
        {
            m_v2Offset[i].x = Random.Range(0, 1000);
            m_v2Offset[i].y = Random.Range(0, 1000);
        }
    }

    private void GenerateHeightMap()
    {
        m_fHeightMap = new float[m_iResolution * m_iResolution];
        for (int y = 0; y < m_iResolution; y++)
        {
            for (int x = 0; x < m_iResolution; x++)
            {
                int i = x + y * m_iResolution;
                for (int l = 0; l < m_sbNoiseLayers; l++)
                {
                    m_fHeightMap[i] += Mathf.PerlinNoise(
                        m_v2Offset[l].x + (x * m_fScale * (l + 1)) / m_iResolution,
                        m_v2Offset[l].y + (y * m_fScale * (l + 1)) / m_iResolution)
                        * m_fHeightScale / (l + 1);
                }
            }
        }
    }
    
    private float GetHeightFromHeightMap(Vector2Int pPos)
    {
        if (pPos.x < 0 || pPos.x >= m_iResolution || pPos.y < 0 || pPos.y >= m_iResolution)
            return float.NaN;

        try
        { 
            return m_fHeightMap[pPos.x + pPos.y * m_iResolution];
        }
        catch
        { 
            Debug.LogError($"Error at {pPos.x}, {pPos.y}");
            return float.NaN;
        }
    }

    private void AddHeightOnHeightMap(Vector2Int pPos, float value)
    {
        if (pPos.x < 0 || pPos.x >= m_iResolution || pPos.y < 0 || pPos.y >= m_iResolution)
            return;

        m_fHeightMap[pPos.x + pPos.y * m_iResolution] += value;
    }

    private void GenerateMesh()
    {
        Vector3[] vertices = new Vector3[m_iResolution * m_iResolution];
        int[] triangles = new int[(m_iResolution - 1) * (m_iResolution - 1) * 6];
        int triIndex = 0;
        Vector2[] uv = (m_mMesh.uv.Length == vertices.Length) ? m_mMesh.uv : new Vector2[vertices.Length];

        for (int y = 0; y < m_iResolution; y++)
        {
            for (int x = 0; x < m_iResolution; x++)
            {
                int i = x + y * m_iResolution;
                vertices[i] = new Vector3(
                    (x + .5f - m_iResolution * .5f) / (m_iResolution - 1) * 20,
                    m_fHeightMap[i],
                    (y + .5f - m_iResolution * .5f) / (m_iResolution - 1) * 20);
                uv[i].y = m_fHeightMap[i];

                if (x != m_iResolution - 1 && y != m_iResolution - 1)
                {
                    triangles[triIndex] = i;
                    triangles[triIndex + 1] = i + m_iResolution;
                    triangles[triIndex + 2] = i + m_iResolution + 1;

                    triangles[triIndex + 3] = i;
                    triangles[triIndex + 4] = i + m_iResolution + 1;
                    triangles[triIndex + 5] = i + 1;
                    triIndex += 6;
                }
            }
        }
        // Recalibrate the mesh
        m_mMesh.Clear();
        m_mMesh.vertices = vertices;
        m_mMesh.triangles = triangles;
        m_mMesh.uv = uv;
        m_mMesh.RecalculateNormals();
        m_mMesh.MarkModified();
        m_mMesh.Optimize();
    }

    private void RunErosion()
    {
        // Verifies there is a heightmap to work with
        if (m_mMesh.vertexCount == 0)
        {
            RandomiseOffset();
            GenerateHeightMap();
            GenerateMesh();
        }

        for (int i = 0; i < m_inumDroplets; i++)
        {
            Droplet drop = new Droplet(new Vector2Int(Random.Range(0, m_iResolution), Random.Range(0, m_iResolution)),
                m_fStartWater, m_fMaxSedimentCapacity);
            drop.height = GetHeightFromHeightMap(drop.position);
            //float sedimentTaken = m_fErosionSpeed;
            //drop.sediment += sedimentTaken;
            //AddHeightOnHeightMap(drop.position, -sedimentTaken);

            for (int j = 0; j < m_iMaxLifetime; j++)
            {
                drop.prevHeight = drop.height;

                // Calculate the lowest point next to the droplet and update the position to it
                Vector2Int lowestPos = new Vector2Int(int.MaxValue, int.MaxValue);
                for (int g = 0; g < 8; g++)
                {
                    float height = 0;
                    // Rotates counter-clockwise
                    switch (g)
                    {
                        case 0:
                            height = GetHeightFromHeightMap(new Vector2Int(drop.position.x - 1, drop.position.y - 1));
                            if (!float.IsNaN(height) && height < drop.height)
                                lowestPos = new Vector2Int(drop.position.x - 1, drop.position.y - 1);
                            break;
                        case 1:
                            height = GetHeightFromHeightMap(new Vector2Int(drop.position.x, drop.position.y - 1));
                            if (!float.IsNaN(height) && height < drop.height)
                                lowestPos = new Vector2Int(drop.position.x, drop.position.y - 1);
                            break;
                        case 2:
                            height = GetHeightFromHeightMap(new Vector2Int(drop.position.x + 1, drop.position.y - 1));
                            if (!float.IsNaN(height) && height < drop.height)
                                lowestPos = new Vector2Int(drop.position.x + 1, drop.position.y - 1);
                            break;
                        case 3:
                            height = GetHeightFromHeightMap(new Vector2Int(drop.position.x + 1, drop.position.y));
                            if (!float.IsNaN(height) && height < drop.height)
                                lowestPos = new Vector2Int(drop.position.x + 1, drop.position.y);
                            break;
                        case 4:
                            height = GetHeightFromHeightMap(new Vector2Int(drop.position.x + 1, drop.position.y + 1));
                            if (!float.IsNaN(height) && height < drop.height)
                                lowestPos = new Vector2Int(drop.position.x + 1, drop.position.y + 1);
                            break;
                        case 5:
                            height = GetHeightFromHeightMap(new Vector2Int(drop.position.x, drop.position.y + 1));
                            if (!float.IsNaN(height) && height < drop.height)
                                lowestPos = new Vector2Int(drop.position.x, drop.position.y + 1);
                            break;
                        case 6:
                            height = GetHeightFromHeightMap(new Vector2Int(drop.position.x - 1, drop.position.y + 1));
                            if (!float.IsNaN(height) && height < drop.height)
                                lowestPos = new Vector2Int(drop.position.x - 1, drop.position.y + 1);
                            break;
                        case 7:
                            height = GetHeightFromHeightMap(new Vector2Int(drop.position.x - 1, drop.position.y));
                            if (!float.IsNaN(height) && height < drop.height)
                                lowestPos = new Vector2Int(drop.position.x - 1, drop.position.y);
                            break;
                    }
                }

                // Droplet is at the lowest position of the surrounding spots
                if (GetHeightFromHeightMap(drop.position) <= GetHeightFromHeightMap(lowestPos))
                {
                    AddHeightOnHeightMap(drop.position, drop.sediment);
                    break;
                }

                drop.position = lowestPos;
                drop.height = GetHeightFromHeightMap(drop.position);
                float deltaHeight = drop.prevHeight - drop.height;
                float deltaSpeed = deltaHeight - drop.speed;
                drop.speed += deltaSpeed; // FIX THIS
                drop.sedimentCapacity = drop.water * drop.speed;
                if (drop.sediment > drop.sedimentCapacity)
                {
                    // Deposit some soil
                    AddHeightOnHeightMap(drop.position, drop.sediment - drop.sedimentCapacity); // FIX THIS
                    drop.sediment = drop.sedimentCapacity;
                }
                else
                {
                    // Erode some soil
                    float sedimentTaken = (m_fErosionSpeed < deltaHeight) ? m_fErosionSpeed : deltaHeight;
                    drop.sediment += sedimentTaken;
                    AddHeightOnHeightMap(drop.position, -sedimentTaken);
                }
                // Evaporate some water
                drop.water -= (m_fEvaporationRate < drop.water) ? m_fEvaporationRate : drop.water;
            }
        }
    }

    private void RunErosionComputeShader()
    {
        // Verifies there is a heightmap to work with
        if (m_mMesh.vertexCount == 0)
        {
            RandomiseOffset();
            GenerateHeightMap();
            GenerateMesh();
        }

        ComputeBuffer buffer = new ComputeBuffer(m_fHeightMap.Length, sizeof(float));
        buffer.SetData(m_fHeightMap);
        m_cpErosion.SetBuffer(0, "heightMap", buffer);
        m_cpErosion.SetInt("resolution", m_iResolution);

        int numGroups = m_inumDroplets / 1024;
        m_cpErosion.Dispatch(0, numGroups, 1, 1);

        buffer.GetData(m_fHeightMap);
        buffer.Release();
    }

    public int GetIterations() { return m_inumDroplets; }

    public void BtnGenerateHeightmap()
    {
        RandomiseOffset();
        GenerateHeightMap();
        GenerateMesh();
    }

    public void BtnGenerateMesh()
    {
        GenerateMesh();
    }

    public void BtnRunErosion()
    {
        RunErosion();
        GenerateMesh();
    }

    public void BtnRunErosionComputeShader()
    {
        RunErosionComputeShader();
        GenerateMesh();
    }
}
