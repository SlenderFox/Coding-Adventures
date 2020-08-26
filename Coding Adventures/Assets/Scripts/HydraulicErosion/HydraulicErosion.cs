using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class HydraulicErosion : MonoBehaviour
{
    [Space(6)]
    [Header("Mesh Settings")]
    [SerializeField, Tooltip("The rendered map will update to setting changes in real-time")]
    private bool m_bLiveUpdate = false;
    [SerializeField, Range(2, 255), Tooltip("The length of one edge in vertices," +
        " full map size will be this number squared")]
    private short m_sResolution = 32;
    [SerializeField, Range(1, 50), Tooltip("How large the map will appear in the world")]
    private short m_sMapScale = 1;
    [SerializeField, Range(1, 50), Tooltip("The size of the Perlin noise")]
    private float m_fNoiseScalar = 1.7f;
    [SerializeField, Range(0.0001f, 1), Tooltip("The intensity of the Perlin noise")]
    private float m_fHeightScalar = 0.3f;
    [SerializeField, Range(1, 20), Tooltip("How many times the perlin noise is sampled," +
        " each subsiquent sample has increased scale and reduced intensity")]
    private sbyte m_sbNoiseLayers = 10;

    [Header("Erosion Settings")]
    [SerializeField]
    private ComputeShader m_cpErosion = null;
    [SerializeField, /*Min(1024),*/ Tooltip("The number of droplets simulated, more droplets = more erosion")]
    private int m_inumDroplets = 1024;
    [SerializeField, Min(1), Tooltip("The maximum amount of iterations done per droplet")]
    private short m_sMaxLifetime = 90;
    [SerializeField, Range(0.00001f, 0.5f), Tooltip("How fast each droplet collects sediment from the ground")]
    private float m_fErosionSpeed = 0.01f;
    [SerializeField, Min(0), Tooltip("The limit to how much sediment a single droplet can hold")]
    private float m_fMaxSedimentCapacity = 3;
    [SerializeField, Min(0.01f), Tooltip("How much water the droplets will start with," +
        " more = more sediment carried")]
    private float m_fStartWater = 2;
    [SerializeField, Tooltip("How much water is lost each step")]
    private float m_fEvaporationRate = 0.01f;

    private float[] m_fHeightMap;   // 1d array for all the heights in the terrain
    private Vector2[] m_v2Offset;   // The offset of the Perlin noise
    private Mesh m_mMesh;           // Reference to the mesh used to display the terrain

    /// <summary>
    /// The Droplet struct handles all the data for each droplet
    /// </summary>
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

    /// <summary>
    /// Start is called once before the first frame
    /// </summary>
    private void Start()
    {
        m_mMesh = new Mesh();
        gameObject.GetComponent<MeshFilter>().sharedMesh = m_mMesh;
        m_fHeightMap = new float[m_sResolution * m_sResolution];
        m_v2Offset = new Vector2[m_sbNoiseLayers];
        GenerateHeightMap();
        GenerateMesh();
    }

    /// <summary>
    /// Update is called once per frame
    /// </summary>
    private void Update()
    {
        // This could be optimised to only be called when a setting is changed
        if (m_bLiveUpdate)
        {
            UpdateNoiseLayers();
            GenerateHeightMap();
            GenerateMesh();
        }
    }

    /// <summary>
    /// When new noise layers are added or removed the array needs to be resized
    /// </summary>
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

    /// <summary>
    /// Shifts the perlin noise offset to a random position for each noise layer
    /// </summary>
    private void RandomiseOffset()
    {
        m_v2Offset = new Vector2[m_sbNoiseLayers];
        for (int i = 0; i < m_sbNoiseLayers; i++)
        {
            m_v2Offset[i].x = Random.Range(0, 1000);
            m_v2Offset[i].y = Random.Range(0, 1000);
        }
    }

    /// <summary>
    /// Loops through each point in the height map array and samples the height from Perlin noise
    /// </summary>
    private void GenerateHeightMap()
    {
        m_fHeightMap = new float[m_sResolution * m_sResolution];
        for (int y = 0; y < m_sResolution; y++)
        {
            for (int x = 0; x < m_sResolution; x++)
            {
                int i = x + y * m_sResolution;
                for (int l = 0; l < m_sbNoiseLayers; l++)
                {
                    m_fHeightMap[i] += Mathf.PerlinNoise(
                        m_v2Offset[l].x + (x * m_fNoiseScalar * (l + 1)) / m_sResolution,
                        m_v2Offset[l].y + (y * m_fNoiseScalar * (l + 1)) / m_sResolution)
                        * m_fHeightScalar / (l + 1);
                }
            }
        }
    }
    
    /// <summary>
    /// Samples the height from a position in the height map,
    /// values outside the range will return NaN
    /// </summary>
    /// <param name="pPos">The position on the heightmap in 2d space</param>
    /// <returns>The height at the point</returns>
    private float GetHeightFromHeightMap(Vector2Int pPos)
    {
        if (pPos.x < 0 || pPos.x >= m_sResolution || pPos.y < 0 || pPos.y >= m_sResolution)
            return 0;

        return m_fHeightMap[pPos.x + pPos.y * m_sResolution];
    }

    /// <summary>
    /// Does an addition operation to a position in the height map array,
    /// negative numbers can be used to subtract
    /// </summary>
    /// <param name="pPos">The position on the heightmap in 2d space</param>
    /// <param name="pValue">The value to be added (or subtracted)</param>
    private void AddHeightOnHeightMap(Vector2Int pPos, float pValue)
    {
        if (pPos.x < 0 || pPos.x >= m_sResolution || pPos.y < 0 || pPos.y >= m_sResolution)
            return;

        m_fHeightMap[pPos.x + pPos.y * m_sResolution] += pValue;
        if (m_fHeightMap[pPos.x + pPos.y * m_sResolution] < 0)
		    m_fHeightMap[pPos.x + pPos.y * m_sResolution] = 0;
    }

    /// <summary>
    /// Generates the 3d mesh based on the height map array
    /// </summary>
    private void GenerateMesh()
    {
        // Initialise required values
        Vector3[] vertices = new Vector3[m_sResolution * m_sResolution];
        int[] triangles = new int[(m_sResolution - 1) * (m_sResolution - 1) * 6];
        int triIndex = 0;
        // Either store the previous uv or create a new one
        Vector2[] uv = (m_mMesh.uv.Length == vertices.Length) ? m_mMesh.uv : new Vector2[vertices.Length];

        // Loop through each position in the height map
        for (int y = 0; y < m_sResolution; y++)
        {
            for (int x = 0; x < m_sResolution; x++)
            {
                // Convert the 2d coord to 1d
                int i = x + y * m_sResolution;
                // For each vertex the x and z values are offset the all values are scaled
                vertices[i] = new Vector3(
                    (x - m_sResolution * .5f + .5f) / (m_sResolution - 1) * m_sMapScale,
                    m_fHeightMap[i] * m_sMapScale,
                    (y - m_sResolution * .5f + .5f) / (m_sResolution - 1) * m_sMapScale);
                uv[i].y = m_fHeightMap[i];  // Tbh I have no idea what this does

                // Stitches together the vertices into triangles
                if (x != m_sResolution - 1 && y != m_sResolution - 1)
                {
                    triangles[triIndex] = i;
                    triangles[triIndex + 1] = i + m_sResolution;
                    triangles[triIndex + 2] = i + m_sResolution + 1;

                    triangles[triIndex + 3] = i;
                    triangles[triIndex + 4] = i + m_sResolution + 1;
                    triangles[triIndex + 5] = i + 1;
                    triIndex += 6;
                }
            }
        }
        // Clear all data in the mesh
        m_mMesh.Clear();
        // Apply the new data to the mesh
        m_mMesh.vertices = vertices;
        m_mMesh.triangles = triangles;
        m_mMesh.uv = uv;
        // Recalculate mesh data
        m_mMesh.RecalculateNormals();
        m_mMesh.RecalculateBounds();
        m_mMesh.RecalculateTangents();
        // Optimise and mark as modified (for good measure)
        m_mMesh.Optimize();
        m_mMesh.MarkModified();
    }

    /// <summary>
    /// Simulates water erosion, this function does it on the cpu
    /// </summary>
    private void RunErosion()
    {
        // Verifies there is a heightmap to work with
        if (m_mMesh.vertexCount == 0)
        {
            RandomiseOffset();
            GenerateHeightMap();
            GenerateMesh();
        }

        // To save memory allocations allocate the droplet outside the for loop
        Droplet drop;

        // Loop through each and every droplet (slow)
        for (int i = 0; i < m_inumDroplets; i++)
        {
            // Initialise the droplet
            drop = new Droplet(new Vector2Int(Random.Range(0, m_sResolution), Random.Range(0, m_sResolution)),
                m_fStartWater, m_fMaxSedimentCapacity);
            drop.height = GetHeightFromHeightMap(drop.position);
            //float sedimentTaken = m_fErosionSpeed;
            //drop.sediment += sedimentTaken;
            //AddHeightOnHeightMap(drop.position, -sedimentTaken);

            // Each loop is a step in the droplets life (slow)
            for (int j = 0; j < m_sMaxLifetime; j++)
            {
                // Update the previous height
                drop.prevHeight = drop.height;

                // Calculate the lowest adjacent point and update the droplet position to it
                // There might be a better way to do this
                Vector2Int lowestPos = new Vector2Int(int.MaxValue, int.MaxValue);
                float lowestHeight = int.MaxValue;
                Vector2Int posCycle;
                float cycleHeight;
                for (int g = 0; g < 8; g++)
                {
                    // Rotates counter-clockwise
                    switch (g)
                    {
                        case 0:
                            posCycle = new Vector2Int(drop.position.x - 1, drop.position.y - 1);
                            break;
                        case 1:
                            posCycle = new Vector2Int(drop.position.x, drop.position.y - 1);
                            break;
                        case 2:
                            posCycle = new Vector2Int(drop.position.x + 1, drop.position.y - 1);
                            break;
                        case 3:
                            posCycle = new Vector2Int(drop.position.x + 1, drop.position.y);
                            break;
                        case 4:
                            posCycle = new Vector2Int(drop.position.x + 1, drop.position.y + 1);
                            break;
                        case 5:
                            posCycle = new Vector2Int(drop.position.x, drop.position.y + 1);
                            break;
                        case 6:
                            posCycle = new Vector2Int(drop.position.x - 1, drop.position.y + 1);
                            break;
                        case 7:
                        default:
                            posCycle = new Vector2Int(drop.position.x - 1, drop.position.y);
                            break;
                    }

                    cycleHeight = GetHeightFromHeightMap(posCycle);
                    if (cycleHeight < drop.height && cycleHeight < lowestHeight)
                    {
                        lowestPos = posCycle;
                        lowestHeight = cycleHeight;
                    }
                }

                // Droplet is at the lowest position of the surrounding spots
                if (GetHeightFromHeightMap(drop.position) <= GetHeightFromHeightMap(lowestPos))
                {
                    AddHeightOnHeightMap(drop.position, drop.sediment);
                    break;
                }

                // Update the droplets position to the lowest adjacent point
                drop.position = lowestPos;
                // Update the droplets height (I could get this when finding the lowest adjacent point)
                drop.height = GetHeightFromHeightMap(drop.position);
                // Find the delta height
                float deltaHeight = drop.prevHeight - drop.height;
                // This is all a mess
                // Find the delta speed (possibly wrong)
                float deltaSpeed = deltaHeight * drop.speed;
                // Update the speed
                drop.speed += deltaSpeed; // FIX THIS
                // Calculate the new sediment capacity based on remaining water and droplet speed
                drop.sedimentCapacity = (drop.water * drop.speed < m_fMaxSedimentCapacity)
                    ? drop.water * drop.speed : m_fMaxSedimentCapacity;

                if (drop.sediment > drop.sedimentCapacity)
                {
                    // Deposit some soil
                    AddHeightOnHeightMap(drop.position, drop.sediment - drop.sedimentCapacity);
                    drop.sediment = drop.sedimentCapacity;
                }
                else
                {
                    // Erode some soil
                    float sedimentTaken = Mathf.Min(m_fErosionSpeed, deltaHeight);
                    drop.sediment += sedimentTaken;
                    AddHeightOnHeightMap(drop.position, -sedimentTaken);
                }
                // Evaporate some water
                drop.water -= Mathf.Min(m_fEvaporationRate, drop.water);
            }
        }
    }

    /// <summary>
    /// Simulates water erosion, this function does it on the gpu
    /// </summary>
    private void RunErosionComputeShader()
    {
        // Verifies there is a heightmap to work with
        if (m_mMesh.vertexCount == 0)
        {
            RandomiseOffset();
            GenerateHeightMap();
            GenerateMesh();
        }

        // Sets the compute shader data
        ComputeBuffer buffer = new ComputeBuffer(m_fHeightMap.Length, sizeof(float));
        buffer.SetData(m_fHeightMap);
        m_cpErosion.SetBuffer(0, "heightMap", buffer);
        m_cpErosion.SetInt("resolution", m_sResolution);
        m_cpErosion.SetInt("maxLifetime", m_sMaxLifetime);
        m_cpErosion.SetFloat("erosionSpeed", m_fErosionSpeed);
        m_cpErosion.SetFloat("maxSedimentCapacity", m_fMaxSedimentCapacity);
        m_cpErosion.SetFloat("water", m_fStartWater);
        m_cpErosion.SetFloat("evaporationRate", m_fEvaporationRate);

        // Calculate the amount of groups requested
        int numGroups = m_inumDroplets / 1024 + 1;
        // Dispach the compute shader
        m_cpErosion.Dispatch(0, numGroups, 1, 1);

        // Retrieve the data and release the buffer
        buffer.GetData(m_fHeightMap);
        buffer.Release();
    }

    /// <summary>
    /// Returns how many droplets are going to be simulated
    /// </summary>
    /// <returns>How many droplets are going to be simulated</returns>
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
        m_bLiveUpdate = false;
        RunErosion();
        GenerateMesh();
    }

    public void BtnRunErosionComputeShader()
    {
        m_bLiveUpdate = false;
        RunErosionComputeShader();
        GenerateMesh();
    }
}
