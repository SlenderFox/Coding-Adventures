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

        public Vector3 this [int i]
        {
            get
            {
                switch (i)
                {
                    case 0:
                        return vertexA;
                    case 1:
                        return vertexB;
                    default:
                        return vertexC;
                }
            }
        }
    }

    public List<Triangle> m_lTriangles;

    // Script references
    private MarchTable m_mtMarchTable;

    /// <summary>
    /// Called when the script is created
    /// </summary>
    private void Awake()
    {
        // Gets reference to the appropriate scripts

        // Creates a march table object
        m_mtMarchTable = new MarchTable();
        m_lTriangles = new List<Triangle>();
    }

    Vector3 InterpolateVerts(Vector4 pV1, Vector4 pV2, float pSurface)
    {
        float t = (pSurface - pV1.w) / (pV2.w - pV1.w);
        //return v1.xyz + t * (v2.xyz - v1.xyz);
        return new Vector3(pV1.x + t * (pV2.x - pV1.x), pV1.y + t * (pV2.y - pV1.y), pV1.z + t * (pV2.z - pV1.z));
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
                    if (wireCube[4, 3] < pMarchingMaster.m_fSurface) cubeIndex |= 16;      // Point 4 is below the isosurface
                    if (wireCube[5, 3] < pMarchingMaster.m_fSurface) cubeIndex |= 32;      // Point 5 is below the isosurface
                    if (wireCube[6, 3] < pMarchingMaster.m_fSurface) cubeIndex |= 64;      // Point 6 is below the isosurface
                    if (wireCube[7, 3] < pMarchingMaster.m_fSurface) cubeIndex |= 128;    // Point 7 is below the isosurface

                    // Create triangles for current cube configuration without interpolation
                    for (int i = 0; m_mtMarchTable.triTable[cubeIndex, i] != -1; i += 3)
                    {
                        Triangle tri;

                        // Gets the edge number based on what triangle should be constructed
                        int a = m_mtMarchTable.triTable[cubeIndex, i + 1];
                        int b = m_mtMarchTable.triTable[cubeIndex, i];
                        int c = m_mtMarchTable.triTable[cubeIndex, i + 2];

                        // Converts the edge numbers to positions then pushes to triangle
                        Vector3 aOffSet;
                        Vector3 bOffSet;
                        Vector3 cOffSet;

                        // Brute forcing a
                        switch (a)
                        {
                            case 0: aOffSet = new Vector3(0.5f, 0, 0); break;
                            case 1: aOffSet = new Vector3(1, 0, 0.5f); break;
                            case 2: aOffSet = new Vector3(0.5f, 0, 1); break;
                            case 3: aOffSet = new Vector3(0, 0, 0.5f); break;
                            case 4: aOffSet = new Vector3(0.5f, 1, 0); break;
                            case 5: aOffSet = new Vector3(1, 1, 0.5f); break;
                            case 6: aOffSet = new Vector3(0.5f, 1, 1); break;
                            case 7: aOffSet = new Vector3(0, 1, 0.5f); break;
                            case 8: aOffSet = new Vector3(0, 0.5f, 0); break;
                            case 9: aOffSet = new Vector3(1, 0.5f, 0); break;
                            case 10: aOffSet = new Vector3(1, 0.5f, 1); break;
                            case 11: aOffSet = new Vector3(0, 0.5f, 1); break;
                            default: aOffSet = new Vector3(0, 0, 0); break;
                        }

                        // Brute forcing b
                        switch (b)
                        {
                            case 0: bOffSet = new Vector3(0.5f, 0, 0); break;
                            case 1: bOffSet = new Vector3(1, 0, 0.5f); break;
                            case 2: bOffSet = new Vector3(0.5f, 0, 1); break;
                            case 3: bOffSet = new Vector3(0, 0, 0.5f); break;
                            case 4: bOffSet = new Vector3(0.5f, 1, 0); break;
                            case 5: bOffSet = new Vector3(1, 1, 0.5f); break;
                            case 6: bOffSet = new Vector3(0.5f, 1, 1); break;
                            case 7: bOffSet = new Vector3(0, 1, 0.5f); break;
                            case 8: bOffSet = new Vector3(0, 0.5f, 0); break;
                            case 9: bOffSet = new Vector3(1, 0.5f, 0); break;
                            case 10: bOffSet = new Vector3(1, 0.5f, 1); break;
                            case 11: bOffSet = new Vector3(0, 0.5f, 1); break;
                            default: bOffSet = new Vector3(0, 0, 0); break;
                        }

                        // Brute forcing c
                        switch (c)
                        {
                            case 0: cOffSet = new Vector3(0.5f, 0, 0); break;
                            case 1: cOffSet = new Vector3(1, 0, 0.5f); break;
                            case 2: cOffSet = new Vector3(0.5f, 0, 1); break;
                            case 3: cOffSet = new Vector3(0, 0, 0.5f); break;
                            case 4: cOffSet = new Vector3(0.5f, 1, 0); break;
                            case 5: cOffSet = new Vector3(1, 1, 0.5f); break;
                            case 6: cOffSet = new Vector3(0.5f, 1, 1); break;
                            case 7: cOffSet = new Vector3(0, 1, 0.5f); break;
                            case 8: cOffSet = new Vector3(0, 0.5f, 0); break;
                            case 9: cOffSet = new Vector3(1, 0.5f, 0); break;
                            case 10: cOffSet = new Vector3(1, 0.5f, 1); break;
                            case 11: cOffSet = new Vector3(0, 0.5f, 1); break;
                            default: cOffSet = new Vector3(0, 0, 0); break;
                        }

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
