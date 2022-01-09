using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MarchingMaster))]
public class MarchingCubes : MonoBehaviour
{
    public struct Triangle
    {
        public Vector3 vertexA;
        public Vector3 vertexB;
        public Vector3 vertexC;

        // Allows Triangle to be access via index
        public Vector3 this [int i]
        {
            get
            {
                return i switch
                {
                    0 => vertexA,
                    1 => vertexB,
                    _ => vertexC,
                };
            }
        }
    }

    public List<Triangle> m_lTriangles;

    // Script references
    private MarchTable m_mtMarchTable;

    private void Awake()
    {
        // Creates a march table object
        m_mtMarchTable = new MarchTable();
        m_lTriangles = new List<Triangle>();
    }

    /// <summary>
    /// Interpolates the position of the surface between two points
    /// </summary>
    /// <param name="pV1">First point</param>
    /// <param name="pV2">Second point</param>
    /// <param name="pSurface">The value of the surface</param>
    /// <returns>The estimated position of the surface between the two points</returns>
    Vector3 InterpolateVerts(Vector4 pV1, Vector4 pV2, float pSurface)
    {
        float t = (pSurface - pV1.w) / (pV2.w - pV1.w);
        //return v1.xyz + t * (v2.xyz - v1.xyz);
        // Essentially just a lerp between the two points as vector3
        //return new Vector3(pV1.x + t * (pV2.x - pV1.x), pV1.y + t * (pV2.y - pV1.y), pV1.z + t * (pV2.z - pV1.z));
        return Vector3.Lerp(pV1, pV2, t);
    }

    public void MarchWithoutInterpolation(Vector3Int pOffset, MarchingMaster pMarchingMaster)
    {
        m_lTriangles = new List<Triangle>();

        for (int z = 0; z < 17; z++)
        {
            for (int y = 0; y < 17; y++)
            {
                for (int x = 0; x < 17; x++)
                {
                    // Construct a cube from this point outwards
                    // Stores the position and density of each of the eight points
                    float[,] wireCube = new float[8, 4];

                    // Fills the cube with data
                    wireCube[0, 0] = x + 0; wireCube[0, 1] = y + 0; wireCube[0, 2] = z + 0; wireCube[0, 3] = pMarchingMaster.m_fVolumeData[x + pOffset.x + 0, y + pOffset.y + 0, z + pOffset.z + 0];
                    wireCube[1, 0] = x + 1; wireCube[1, 1] = y + 0; wireCube[1, 2] = z + 0; wireCube[1, 3] = pMarchingMaster.m_fVolumeData[x + pOffset.x + 1, y + pOffset.y + 0, z + pOffset.z + 0];
                    wireCube[2, 0] = x + 1; wireCube[2, 1] = y + 0; wireCube[2, 2] = z + 1; wireCube[2, 3] = pMarchingMaster.m_fVolumeData[x + pOffset.x + 1, y + pOffset.y + 0, z + pOffset.z + 1];
                    wireCube[3, 0] = x + 0; wireCube[3, 1] = y + 0; wireCube[3, 2] = z + 1; wireCube[3, 3] = pMarchingMaster.m_fVolumeData[x + pOffset.x + 0, y + pOffset.y + 0, z + pOffset.z + 1];
                    wireCube[4, 0] = x + 0; wireCube[4, 1] = y + 1; wireCube[4, 2] = z + 0; wireCube[4, 3] = pMarchingMaster.m_fVolumeData[x + pOffset.x + 0, y + pOffset.y + 1, z + pOffset.z + 0];
                    wireCube[5, 0] = x + 1; wireCube[5, 1] = y + 1; wireCube[5, 2] = z + 0; wireCube[5, 3] = pMarchingMaster.m_fVolumeData[x + pOffset.x + 1, y + pOffset.y + 1, z + pOffset.z + 0];
                    wireCube[6, 0] = x + 1; wireCube[6, 1] = y + 1; wireCube[6, 2] = z + 1; wireCube[6, 3] = pMarchingMaster.m_fVolumeData[x + pOffset.x + 1, y + pOffset.y + 1, z + pOffset.z + 1];
                    wireCube[7, 0] = x + 0; wireCube[7, 1] = y + 1; wireCube[7, 2] = z + 1; wireCube[7, 3] = pMarchingMaster.m_fVolumeData[x + pOffset.x + 0, y + pOffset.y + 1, z + pOffset.z + 1];

                    // Determines the unique index for each cube
                    byte cubeIndex = 0;
                    if (wireCube[0, 3] < pMarchingMaster.m_fSurface) cubeIndex |= 1;        // Point 0 is below the isosurface
                    if (wireCube[1, 3] < pMarchingMaster.m_fSurface) cubeIndex |= 2;        // Point 1 is below the isosurface
                    if (wireCube[2, 3] < pMarchingMaster.m_fSurface) cubeIndex |= 4;        // Point 2 is below the isosurface
                    if (wireCube[3, 3] < pMarchingMaster.m_fSurface) cubeIndex |= 8;        // Point 3 is below the isosurface
                    if (wireCube[4, 3] < pMarchingMaster.m_fSurface) cubeIndex |= 16;       // Point 4 is below the isosurface
                    if (wireCube[5, 3] < pMarchingMaster.m_fSurface) cubeIndex |= 32;       // Point 5 is below the isosurface
                    if (wireCube[6, 3] < pMarchingMaster.m_fSurface) cubeIndex |= 64;       // Point 6 is below the isosurface
                    if (wireCube[7, 3] < pMarchingMaster.m_fSurface) cubeIndex |= 128;      // Point 7 is below the isosurface

                    // Create triangles for current cube configuration without interpolation
                    for (int i = 0; m_mtMarchTable.triTable[cubeIndex, i] != -1; i += 3)
                    {
                        Triangle tri;

                        // Gets the edge number based on what triangle should be constructed
                        int a = m_mtMarchTable.triTable[cubeIndex, i + 1];
                        int b = m_mtMarchTable.triTable[cubeIndex, i];
                        int c = m_mtMarchTable.triTable[cubeIndex, i + 2];

                        // Converts the edge numbers to positions then pushes to triangle

                        // Brute forcing a
                        Vector3 aOffSet = a switch
                        {
                            0 => new Vector3(0.5f, 0, 0),
                            1 => new Vector3(1, 0, 0.5f),
                            2 => new Vector3(0.5f, 0, 1),
                            3 => new Vector3(0, 0, 0.5f),
                            4 => new Vector3(0.5f, 1, 0),
                            5 => new Vector3(1, 1, 0.5f),
                            6 => new Vector3(0.5f, 1, 1),
                            7 => new Vector3(0, 1, 0.5f),
                            8 => new Vector3(0, 0.5f, 0),
                            9 => new Vector3(1, 0.5f, 0),
                            10 => new Vector3(1, 0.5f, 1),
                            11 => new Vector3(0, 0.5f, 1),
                            _ => new Vector3(0, 0, 0),
                        };

                        // Brute forcing b
                        Vector3 bOffSet = b switch
                        {
                            0 => new Vector3(0.5f, 0, 0),
                            1 => new Vector3(1, 0, 0.5f),
                            2 => new Vector3(0.5f, 0, 1),
                            3 => new Vector3(0, 0, 0.5f),
                            4 => new Vector3(0.5f, 1, 0),
                            5 => new Vector3(1, 1, 0.5f),
                            6 => new Vector3(0.5f, 1, 1),
                            7 => new Vector3(0, 1, 0.5f),
                            8 => new Vector3(0, 0.5f, 0),
                            9 => new Vector3(1, 0.5f, 0),
                            10 => new Vector3(1, 0.5f, 1),
                            11 => new Vector3(0, 0.5f, 1),
                            _ => new Vector3(0, 0, 0),
                        };

                        // Brute forcing c
                        Vector3 cOffSet = c switch
                        {
                            0 => new Vector3(0.5f, 0, 0),
                            1 => new Vector3(1, 0, 0.5f),
                            2 => new Vector3(0.5f, 0, 1),
                            3 => new Vector3(0, 0, 0.5f),
                            4 => new Vector3(0.5f, 1, 0),
                            5 => new Vector3(1, 1, 0.5f),
                            6 => new Vector3(0.5f, 1, 1),
                            7 => new Vector3(0, 1, 0.5f),
                            8 => new Vector3(0, 0.5f, 0),
                            9 => new Vector3(1, 0.5f, 0),
                            10 => new Vector3(1, 0.5f, 1),
                            11 => new Vector3(0, 0.5f, 1),
                            _ => new Vector3(0, 0, 0),
                        };

                        tri.vertexA = new Vector3(wireCube[0, 0], wireCube[0, 1], wireCube[0, 2]) + aOffSet;
                        tri.vertexB = new Vector3(wireCube[0, 0], wireCube[0, 1], wireCube[0, 2]) + bOffSet;
                        tri.vertexC = new Vector3(wireCube[0, 0], wireCube[0, 1], wireCube[0, 2]) + cOffSet;

                        m_lTriangles.Add(tri);
                    }
                }
            }
        }
    }

    public void MarchWithInterpolation(Vector3Int pOffset, MarchingMaster pMarchingMaster)
    {
        m_lTriangles = new List<Triangle>();

        for (int z = 0; z < 17; z++)
        {
            for (int y = 0; y < 17; y++)
            {
                for (int x = 0; x < 17; x++)
                {
                    // Construct a cube from this point outwards
                    // Stores the position and density of each of the eight points
                    float[,] wireCube = new float[8, 4];

                    // Fills the cube with data
                    wireCube[0, 0] = x + 0; wireCube[0, 1] = y + 0; wireCube[0, 2] = z + 0; wireCube[0, 3] = pMarchingMaster.m_fVolumeData[x + pOffset.x + 0, y + pOffset.y + 0, z + pOffset.z + 0];
                    wireCube[1, 0] = x + 1; wireCube[1, 1] = y + 0; wireCube[1, 2] = z + 0; wireCube[1, 3] = pMarchingMaster.m_fVolumeData[x + pOffset.x + 1, y + pOffset.y + 0, z + pOffset.z + 0];
                    wireCube[2, 0] = x + 1; wireCube[2, 1] = y + 0; wireCube[2, 2] = z + 1; wireCube[2, 3] = pMarchingMaster.m_fVolumeData[x + pOffset.x + 1, y + pOffset.y + 0, z + pOffset.z + 1];
                    wireCube[3, 0] = x + 0; wireCube[3, 1] = y + 0; wireCube[3, 2] = z + 1; wireCube[3, 3] = pMarchingMaster.m_fVolumeData[x + pOffset.x + 0, y + pOffset.y + 0, z + pOffset.z + 1];
                    wireCube[4, 0] = x + 0; wireCube[4, 1] = y + 1; wireCube[4, 2] = z + 0; wireCube[4, 3] = pMarchingMaster.m_fVolumeData[x + pOffset.x + 0, y + pOffset.y + 1, z + pOffset.z + 0];
                    wireCube[5, 0] = x + 1; wireCube[5, 1] = y + 1; wireCube[5, 2] = z + 0; wireCube[5, 3] = pMarchingMaster.m_fVolumeData[x + pOffset.x + 1, y + pOffset.y + 1, z + pOffset.z + 0];
                    wireCube[6, 0] = x + 1; wireCube[6, 1] = y + 1; wireCube[6, 2] = z + 1; wireCube[6, 3] = pMarchingMaster.m_fVolumeData[x + pOffset.x + 1, y + pOffset.y + 1, z + pOffset.z + 1];
                    wireCube[7, 0] = x + 0; wireCube[7, 1] = y + 1; wireCube[7, 2] = z + 1; wireCube[7, 3] = pMarchingMaster.m_fVolumeData[x + pOffset.x + 0, y + pOffset.y + 1, z + pOffset.z + 1];

                    // Determines the unique index for each cube
                    byte cubeIndex = 0;
                    if (wireCube[0, 3] < pMarchingMaster.m_fSurface) cubeIndex |= 1;        // Point 0 is below the isosurface
                    if (wireCube[1, 3] < pMarchingMaster.m_fSurface) cubeIndex |= 2;        // Point 1 is below the isosurface
                    if (wireCube[2, 3] < pMarchingMaster.m_fSurface) cubeIndex |= 4;        // Point 2 is below the isosurface
                    if (wireCube[3, 3] < pMarchingMaster.m_fSurface) cubeIndex |= 8;        // Point 3 is below the isosurface
                    if (wireCube[4, 3] < pMarchingMaster.m_fSurface) cubeIndex |= 16;      // Point 4 is below the isosurface
                    if (wireCube[5, 3] < pMarchingMaster.m_fSurface) cubeIndex |= 32;      // Point 5 is below the isosurface
                    if (wireCube[6, 3] < pMarchingMaster.m_fSurface) cubeIndex |= 64;      // Point 6 is below the isosurface
                    if (wireCube[7, 3] < pMarchingMaster.m_fSurface) cubeIndex |= 128;    // Point 7 is below the isosurface

                    // Create triangles for current cube configuration with interpolation
                    for (int i = 0; m_mtMarchTable.triTable[cubeIndex, i] != -1; i += 3)
                    {
                        int a1 = m_mtMarchTable.cornerIndexAFromEdge[m_mtMarchTable.triTable[cubeIndex, i + 1]];
                        int a2 = m_mtMarchTable.cornerIndexBFromEdge[m_mtMarchTable.triTable[cubeIndex, i + 1]];

                        int b1 = m_mtMarchTable.cornerIndexAFromEdge[m_mtMarchTable.triTable[cubeIndex, i]];
                        int b2 = m_mtMarchTable.cornerIndexBFromEdge[m_mtMarchTable.triTable[cubeIndex, i]];

                        int c1 = m_mtMarchTable.cornerIndexAFromEdge[m_mtMarchTable.triTable[cubeIndex, i + 2]];
                        int c2 = m_mtMarchTable.cornerIndexBFromEdge[m_mtMarchTable.triTable[cubeIndex, i + 2]];

                        // Interpolates the verts
                        Triangle tri;
                        tri.vertexA = InterpolateVerts(new Vector4(wireCube[a1, 0], wireCube[a1, 1], wireCube[a1, 2], wireCube[a1, 3]),
                            new Vector4(wireCube[a2, 0], wireCube[a2, 1], wireCube[a2, 2], wireCube[a2, 3]), pMarchingMaster.m_fSurface);
                        tri.vertexB = InterpolateVerts(new Vector4(wireCube[b1, 0], wireCube[b1, 1], wireCube[b1, 2], wireCube[b1, 3]),
                            new Vector4(wireCube[b2, 0], wireCube[b2, 1], wireCube[b2, 2], wireCube[b2, 3]), pMarchingMaster.m_fSurface);
                        tri.vertexC = InterpolateVerts(new Vector4(wireCube[c1, 0], wireCube[c1, 1], wireCube[c1, 2], wireCube[c1, 3]),
                            new Vector4(wireCube[c2, 0], wireCube[c2, 1], wireCube[c2, 2], wireCube[c2, 3]), pMarchingMaster.m_fSurface);
                        m_lTriangles.Add(tri);
                    }
                }
            }
        }
    }
}
