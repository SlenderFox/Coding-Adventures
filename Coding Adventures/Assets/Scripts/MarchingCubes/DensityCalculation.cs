using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MarchingMaster))]
public class DensityCalculation : MonoBehaviour
{
    // A reference to the sphere prefab
    [SerializeField]
    private GameObject m_goSpherePrefab = null;

    // A list for all spheres currently in the scene
    // Could use object pooling
    private List<GameObject> m_lSphereList = new List<GameObject>();

    /// <summary>
    /// Sets the Perlin noise offset to a random value between -100 and +100
    /// </summary>
    public void RandomizeOffset(out Vector3 pOffset)
    {
        pOffset = new Vector3(Random.Range(-100, 100), Random.Range(-100, 100), Random.Range(-100, 100));
    }

    /// <summary>
    /// Iterates through each point in the bounds to find the density
    /// </summary>
    public void GenerateDensity(MarchingMaster pMaster)
    {
        // Iterates through every position in a set area to generate a density
        for (int z = 0; z < pMaster.m_v3iBounds.z; z++)
        {
            for (int y = 0; y < pMaster.m_v3iBounds.y; y++)
            {
                for (int x = 0; x < pMaster.m_v3iBounds.x; x++)
                {
                    pMaster.m_fVolumeData[x, y, z] = DensityAtPoint(
                        pMaster.m_v3NoiseOffset.x + x * pMaster.m_fNoiseScale,
                        pMaster.m_v3NoiseOffset.y + y * pMaster.m_fNoiseScale,
                        pMaster.m_v3NoiseOffset.z + z * pMaster.m_fNoiseScale);
                }
            }
        }
    }

    /// <summary>
    /// Calculates the density for a point in 3D space 
    /// </summary>
    /// <param name="pX">Position in the x axis</param>
    /// <param name="pY">Position in the y axis</param>
    /// <param name="pZ">Position in the z axis</param>
    /// <returns>The density of the given positon</returns>
    public static float DensityAtPoint(float pX, float pY, float pZ)
    {
        float xy = Mathf.PerlinNoise(pX, pY);
        float xz = Mathf.PerlinNoise(pX, pZ);
        float yx = Mathf.PerlinNoise(pY, pX);
        float yz = Mathf.PerlinNoise(pY, pZ);
        float zx = Mathf.PerlinNoise(pZ, pX);
        float zy = Mathf.PerlinNoise(pZ, pY);

        return (xy + xz + yx + yz + zx + zy) / 6;
    }

    /// <summary>
    /// Places spheres at every position in a given volume
    /// </summary>
    public void PlaceSpheres(float [,,]pVolume, Vector3Int pBounds, float pSurface)
    {
        // Resets the sphere list before generating a new one
        foreach (GameObject sphere in m_lSphereList)
            Destroy(sphere);
        m_lSphereList.Clear();

        // Iterates through all points to place a sphere
        for (int z = 0; z < pBounds.x; z++)
        {
            for (int y = 0; y < pBounds.y; y++)
            {
                for (int x = 0; x < pBounds.z; x++)
                {
                    if (pVolume[x, y, z] >= pSurface)
                    {
                        m_lSphereList.Add(Instantiate(m_goSpherePrefab, new Vector3(x, y, z), Quaternion.identity));
                    }
                }
            }
        }
    }
}
