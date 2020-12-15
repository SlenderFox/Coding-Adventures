using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MeshGenerator : MonoBehaviour
{
    // Data for triangles to be pushed to the mesh
    Vector3[] m_v3Vertices;
    int[] m_iTriangles;

    // Script references
    private MeshFilter m_mfMeshFilter;
    private Mesh m_mMesh;

    /// <summary>
    /// Called when the script is created
    /// </summary>
    private void Awake()
    {
        // Gets reference to the appropriate scripts
        m_mfMeshFilter = GetComponent<MeshFilter>();

        // Creates the mesh used
        m_mMesh = new Mesh();
        m_mfMeshFilter.mesh = m_mMesh;
    }

    /// <summary>
    /// Using a list of verts and triangles, create a mesh
    /// </summary>
    /// <param name="pMarchingCubes"></param>
    public void CreateMesh(Vector3[,] pTriangles, int pCount)
    {
        // Clears the mesh, ready for a new one
        m_mMesh.Clear();

        m_v3Vertices = new Vector3[pCount * 3];
        m_iTriangles = new int[pCount * 3];

        // Loads the triangle list into a vector 3 array and int array
        for (int i = 0; i < pCount; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                m_iTriangles[i * 3 + j] = i * 3 + j;
                m_v3Vertices[i * 3 + j] = pTriangles[i, j];
            }
        }

        // Pushes the new mesh into the mesh object
        m_mMesh.vertices = m_v3Vertices;
        m_mMesh.triangles = m_iTriangles;

        m_mMesh.RecalculateNormals();
    }
}
