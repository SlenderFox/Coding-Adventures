using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(DensityCalculation))]
[RequireComponent(typeof(MarchingCubes))]
public class MarchingMaster : MonoBehaviour
{
    // --------------------Public--------------------
    [SerializeField, Tooltip("Whether or not spheres are placed in the volume")]
    private bool m_bSpheres = false;
    [SerializeField]
    private bool m_bInterpolate = false;
    [SerializeField]
    private bool m_bRandomizeNoiseOffset = true;

    [Tooltip("At what value will point be considered above the surface and thus cause geometry to be placed")]
    public float m_fSurface = 0.5f;
    [Tooltip("The size of the random noise, lower values result in bigger shapes")]
    public float m_fNoiseScale = 0.05f;
    //public float m_fDetail = 1;

    [Tooltip("The desired scale of the volume generated")]
    public Vector3Int m_v3iBounds = new Vector3Int(1, 1, 1);

    [Space]
    [SerializeField]
    private GameObject m_goMeshObjPrefab = null;
    //[SerializeField]
    //private Material m_mMaterial = null;

    // --------------------Private--------------------

    private int m_iNumChunks = 1;
    private int m_iChunkSize = 18;

    // NOTE: Chunk can only be 18x18x18
    private Vector3Int m_v3iChunks = new Vector3Int(1, 1, 1);

    [HideInInspector]
    public Vector3 m_v3NoiseOffset;

    // Positional data for each point inside the volume
    [HideInInspector]
    public float[,,] m_fVolumeData;

    private List<GameObject> m_LgoMeshObjects = new List<GameObject>();

    // Script references
    private DensityCalculation m_dcDensityCalculation;
    private MarchingCubes m_mcMarchingCubes;
    //private MeshGenerator m_mgMeshGenerator;

    private void Awake()
    {
        // Gets reference to the appropriate scripts
        m_dcDensityCalculation = GetComponent<DensityCalculation>();
        m_mcMarchingCubes = GetComponent<MarchingCubes>();
        //m_mgMeshGenerator = GetComponent<MeshGenerator>();

        m_fVolumeData = new float[m_v3iBounds.x, m_v3iBounds.y, m_v3iBounds.z];
    }

    private void Start()
    {
        // Generate a mesh when the program starts
        GenerateNewMesh();
    }

    private void Update()
    {
        // Regenerates the density field on R press
        if (Input.GetKeyDown(KeyCode.R))
        {
            GenerateNewMesh();
        }
    }

    /// <summary>
    /// Determines the requires chunks to use
    /// </summary>
    private void UpdateChunks()
    {
        int xChunks = Mathf.CeilToInt(m_v3iBounds.x / m_iChunkSize);
        int yChunks = Mathf.CeilToInt(m_v3iBounds.y / m_iChunkSize);
        int zChunks = Mathf.CeilToInt(m_v3iBounds.z / m_iChunkSize);
        m_v3iChunks = new Vector3Int(xChunks, yChunks, zChunks);
        m_iNumChunks = xChunks * yChunks * zChunks;

        // Clears the gameobject list (this can be optimised)
        foreach (GameObject obj in m_LgoMeshObjects)
        {
            Destroy(obj);
        }
        m_LgoMeshObjects.Clear();

        // Creates a new gameobject for each chunk
        for (int i = 0; i < m_iNumChunks; i++)
        {
            GameObject newChunk = Instantiate(m_goMeshObjPrefab, gameObject.transform);
            //newChunk.GetComponent<MeshRenderer>().sharedMaterial = m_mMaterial;
            m_LgoMeshObjects.Add(newChunk);
        }

        // 5, 4, 6 = 120
        // 

        for (int z = 0; z < m_v3iChunks.z; z++)
        {
            for (int y = 0; y < m_v3iChunks.y; y++)
            {
                for (int x = 0; x < m_v3iChunks.x; x++)
                {
                    // size.x * size.y * pos.z + size.x * pos.y + pos.x
                    int listPos = m_v3iChunks.x * m_v3iChunks.y * z + m_v3iChunks.x * y + x;
                    m_LgoMeshObjects[listPos].transform.localPosition = new Vector3(
                        x * (m_iChunkSize - 1), y * (m_iChunkSize - 1), z * (m_iChunkSize - 1)); 
                }
            }
        }
    }

    /// <summary>
    /// Regenerates the mesh
    /// </summary>
    private void GenerateNewMesh()
    {
        // Updates the required chunks
        UpdateChunks();

        // Randomizes the Perlin noise offset
        if (m_bRandomizeNoiseOffset)
            m_dcDensityCalculation.RandomizeOffset(out m_v3NoiseOffset);

        // Calculates the density for each point in the volume
        m_dcDensityCalculation.GenerateDensity(this);

        // Places spheres below the surface level
        if (m_bSpheres)
            m_dcDensityCalculation.PlaceSpheres(m_fVolumeData, m_v3iBounds, m_fSurface);

        for (int c = 0; c < m_iNumChunks; c++)
        {
            int z = Mathf.FloorToInt(c / (m_v3iChunks.x * m_v3iChunks.y));
            int y = Mathf.FloorToInt(c / m_v3iChunks.x) - z * m_v3iChunks.y;
            int x = c - m_v3iChunks.x * m_v3iChunks.y * z - m_v3iChunks.x * y;
            Vector3Int chunkOffset = new Vector3Int(x * (m_iChunkSize - 1), y * (m_iChunkSize - 1), z * (m_iChunkSize - 1));

            // Marches the cubes
            if (m_bInterpolate)
                m_mcMarchingCubes.MarchWithInterpolation(chunkOffset, this);
            else
                m_mcMarchingCubes.MarchWithoutInterpolation(chunkOffset, this);

            // Converts from triangle struct to array
            Vector3[,] triangles = new Vector3[m_mcMarchingCubes.m_lTriangles.Count, 3];
            for (int t = 0; t < m_mcMarchingCubes.m_lTriangles.Count; t++)
            {
                triangles[t, 0] = m_mcMarchingCubes.m_lTriangles[t].vertexA;
                triangles[t, 1] = m_mcMarchingCubes.m_lTriangles[t].vertexB;
                triangles[t, 2] = m_mcMarchingCubes.m_lTriangles[t].vertexC;
            }
            m_LgoMeshObjects[c].GetComponent<MeshGenerator>().CreateMesh(triangles, m_mcMarchingCubes.m_lTriangles.Count);
        }
    }
}
