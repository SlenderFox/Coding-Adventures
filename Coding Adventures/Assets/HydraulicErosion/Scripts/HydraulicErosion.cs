using UnityEngine;
using System.Runtime.InteropServices;

[ExecuteInEditMode]
public class HydraulicErosion : MonoBehaviour
{
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
    [SerializeField, Min(1), Tooltip("The maximum amount of iterations done per droplet")]
    private short m_sMaxLifetime = 90;
    [SerializeField, Range(0.00001f, 0.5f), Tooltip("How fast each droplet collects sediment from the ground")]
    private float m_fErosionSpeed = 0.01f;
    [SerializeField, Min(0.01f), Tooltip("How much water the droplets will start with," +
        " more = more sediment carried")]
    private float m_fStartWater = 2;
    [SerializeField, Tooltip("How much water is lost each step")]
    private float m_fEvaporationRate = 0.01f;

    [Header("Program Settings")]
    [SerializeField]
    private ComputeShader m_cpErosion = null;
    [SerializeField, Range(1, 100000), Tooltip("The amount of droplets sequentially simulated on the cpu. VERY SLOW.")]
    private int m_iNumDroplets = 10000;
    [SerializeField, Range(1, 100), Tooltip("The amount of groups requested for the gpu")]
    private int m_iNumGroups = 10;

    private HeightMap m_hmHeightMap;    // Struct union of 1d and 2d heightmap data
    private Vector2[] m_v2Offset;       // The offset of the Perlin noise
    private Mesh m_mMesh;               // Reference to the mesh used to display the terrain

    [StructLayout(LayoutKind.Explicit, Size = 65025)]
    internal struct HeightMap
    {
        [FieldOffset(0)]
        internal float[,] m_v2Coords;
        [FieldOffset(0)]
        internal float[] m_fHeightMap;

        internal HeightMap(short pResolution) : this()
        {
            m_fHeightMap = new float[pResolution * pResolution];
            m_v2Coords = new float[pResolution, pResolution];
        }
    }

    /// <summary>
    /// The Droplet struct handles all the data for each droplet
    /// </summary>
    internal struct Droplet
    {
        internal Droplet(Vector2Int pPosition, float pHeight, float pWater)
        {
            position = pPosition;
            height = pHeight;
            water = pWater;
            prevHeight = float.PositiveInfinity;
            sediment = 0;
            inertia = 0;    // Inertia is just the delta height
        }

        internal Vector2Int position;
        internal float height;
        internal float water;
        internal float prevHeight;
        internal float sediment;
        internal float inertia;

        // Sediment capacity is how much water is left
    }

    /// <summary>
    /// Start is called once before the first frame
    /// </summary>
    private void Start()
    {
        m_mMesh = new Mesh();
        gameObject.GetComponent<MeshFilter>().sharedMesh = m_mMesh;
        m_hmHeightMap = new HeightMap(m_sResolution);
        m_v2Offset = new Vector2[m_sbNoiseLayers];
        RandomiseOffset();
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
        m_hmHeightMap = new HeightMap(m_sResolution);
        for (int y = 0; y < m_sResolution; y++)
        {
            for (int x = 0; x < m_sResolution; x++)
            {
                int i = x + y * m_sResolution;
                for (int l = 0; l < m_sbNoiseLayers; l++)
                {
                    m_hmHeightMap.m_fHeightMap[i] += Mathf.PerlinNoise(
                        m_v2Offset[l].x + (x * m_fNoiseScalar * (l + 1)) / m_sResolution,
                        m_v2Offset[l].y + (y * m_fNoiseScalar * (l + 1)) / m_sResolution)
                        * m_fHeightScalar / (l + 1);
                }
            }
        }
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
                    m_hmHeightMap.m_fHeightMap[i] * m_sMapScale,
                    (y - m_sResolution * .5f + .5f) / (m_sResolution - 1) * m_sMapScale);
                uv[i].y = m_hmHeightMap.m_fHeightMap[i];  // Tbh I have no idea what this does

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
    /// Samples the height from a position in the height map,
    /// values outside the range will return NaN
    /// </summary>
    /// <param name="pPos">The position on the heightmap in 2d space</param>
    /// <returns>The height at the point</returns>
    private float GetHeightFromHeightMap(Vector2Int pPos)
    {
        if (pPos.x < 0 || pPos.x >= m_sResolution || pPos.y < 0 || pPos.y >= m_sResolution)
            return float.PositiveInfinity;

        return m_hmHeightMap.m_v2Coords[pPos.x, pPos.y];
    }

    /// <summary>
    /// Does an addition operation to a position in the height map array,
    /// negative numbers can be used to subtract
    /// </summary>
    /// <param name="pPos">The position on the heightmap in 2d space</param>
    /// <param name="pValue">The value to be added (or subtracted)</param>
    private void AddHeightOnHeightMap(ref Droplet pDroplet, float pAmount)
    {
        if (pDroplet.position.x < 0 || pDroplet.position.x >= m_sResolution || 
            pDroplet.position.y < 0 || pDroplet.position.y >= m_sResolution)
            throw new UnityException("Droplet has gone out of bounds");

        if (pAmount >= 0)
        {
            // Adding sediment to the terrain
            m_hmHeightMap.m_v2Coords[pDroplet.position.x, pDroplet.position.y] += pAmount;
            pDroplet.sediment -= pAmount;
        }
        else
        {
            // Removing sediment from the terrain
            //Invert amount if subtraction to make code easier to understand
            pAmount = -pAmount;
            float terrainHeight = GetHeightFromHeightMap(pDroplet.position);
            float difference = terrainHeight - pAmount;
            // Prevent from removing too much sediment, creates "bedrock"
            if (difference < 0)
            {
                m_hmHeightMap.m_v2Coords[pDroplet.position.x, pDroplet.position.y] -= terrainHeight;
                pDroplet.sediment += terrainHeight;
            }
            else
            {
                m_hmHeightMap.m_v2Coords[pDroplet.position.x, pDroplet.position.y] -= pAmount;
                pDroplet.sediment += pAmount;
            }
        }
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

        Droplet drop;

        // Loop through each and every droplet (slow)
        for (int i = 0; i < m_iNumDroplets; i++)
        {
            // Initialise a new droplet
            {
                Vector2Int startPosition = new Vector2Int(Random.Range(0, m_sResolution), Random.Range(0, m_sResolution));
                drop = new Droplet(startPosition, m_fStartWater, GetHeightFromHeightMap(startPosition));
            }

            // Each loop is a step in the droplets life (slow)
            // A break in this loop is equivelant to the droplet dying
            for (int j = 0; j < m_sMaxLifetime; j++)
            {
                // Update the previous height
                drop.prevHeight = drop.height;

                // Calculate the lowest adjacent point and update the droplet position to it
                Vector2Int lowestPos = new Vector2Int(int.MaxValue, int.MaxValue);
                float lowestHeight = int.MaxValue;
                Vector2Int cyclePos;
                float cycleHeight;

                for (int g = 0; g < 8; g++)
                {
                    // Rotates counter-clockwise
                    cyclePos = g switch
                    {
                        0 => new Vector2Int(drop.position.x - 1, drop.position.y - 1),
                        1 => new Vector2Int(drop.position.x,     drop.position.y - 1),
                        2 => new Vector2Int(drop.position.x + 1, drop.position.y - 1),
                        3 => new Vector2Int(drop.position.x + 1, drop.position.y    ),
                        4 => new Vector2Int(drop.position.x + 1, drop.position.y + 1),
                        5 => new Vector2Int(drop.position.x,     drop.position.y + 1),
                        6 => new Vector2Int(drop.position.x - 1, drop.position.y + 1),
                        _ => new Vector2Int(drop.position.x - 1, drop.position.y    ),
                    };
                    cycleHeight = GetHeightFromHeightMap(cyclePos);
                    if (cycleHeight < drop.height && cycleHeight < lowestHeight)
                    {
                        lowestPos = cyclePos;
                        lowestHeight = cycleHeight;
                    }
                }

                // Droplet is at the lowest position of the surrounding spots
                if (drop.height <= lowestHeight)
                {
                    // Disbtribute the sediment to the surrounding points aswell
                    AddHeightOnHeightMap(ref drop, drop.sediment);
                    break;
                }

                // Update the droplets position to the lowest adjacent point
                drop.position = lowestPos;
                // Update the droplets height
                drop.height = lowestHeight;
                // inertia is the delta height
                drop.inertia = drop.prevHeight - drop.height;

                // This is all a mess
                // Find the delta speed (possibly wrong)
                //deltaSpeed = -deltaHeight;
                //// Update the speed
                //drop.speed += deltaSpeed; // FIX THIS
                //// Calculate the new sediment capacity based on remaining water and droplet speed
                //drop.sedimentCapacity = (drop.water * drop.speed < m_fMaxSedimentCapacity)
                //    ? drop.water * drop.speed : m_fMaxSedimentCapacity;

                // Evaporate some water
                drop.water -= Mathf.Min(m_fEvaporationRate, drop.water);

                // TODO: Do the shit where soil is taken and deposited in an area around a point
                if (drop.sediment != drop.water)
                {
                    if (drop.sediment > drop.water)
                    {
                        // Deposit some soil
                        AddHeightOnHeightMap(ref drop, drop.water - drop.sediment);
                    }
                    else
                    {
                        // Erode some soil
                        float sedimentTaken = Mathf.Min(m_fErosionSpeed, drop.inertia);
                        AddHeightOnHeightMap(ref drop, -sedimentTaken);
                    }
                }
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
        ComputeBuffer heightBuffer = new ComputeBuffer(m_hmHeightMap.m_fHeightMap.Length, sizeof(float));
        heightBuffer.SetData(m_hmHeightMap.m_fHeightMap);
        m_cpErosion.SetBuffer(0, "heightMap", heightBuffer);
        m_cpErosion.SetInt("resolution", m_sResolution);
        m_cpErosion.SetInt("maxLifetime", m_sMaxLifetime);
        m_cpErosion.SetFloat("erosionSpeed", m_fErosionSpeed);
        m_cpErosion.SetFloat("evaporationRate", m_fEvaporationRate);
        m_cpErosion.SetFloat("water", m_fStartWater);

        // Dispach the compute shader
        m_cpErosion.Dispatch(0, m_iNumGroups, 1, 1);

        // Retrieve the data and release the buffer
        heightBuffer.GetData(m_hmHeightMap.m_fHeightMap);
        heightBuffer.Release();
    }

    /// <summary>
    /// Returns how many droplets are going to be simulated on the cpu
    /// </summary>
    /// <returns>How many droplets are going to be simulated on the cpu</returns>
    public int GetNumberOfDroplets() { return m_iNumDroplets; }

    /// <summary>
    /// Returns how many droplets are going to be simulated on the gpu
    /// </summary>
    /// <returns>How many droplets are going to be simulated on the gpu</returns>
    public int GetComputeShaderThreads() { return m_iNumGroups * 1024; }

    public void BtnGenerateNewHeightmap()
    {
        RandomiseOffset();
        GenerateHeightMap();
        GenerateMesh();
    }

    public void BtnRebuildMesh()
    {
        GenerateHeightMap();
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
