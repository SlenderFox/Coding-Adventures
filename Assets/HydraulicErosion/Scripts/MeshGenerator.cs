using UnityEngine;
using System.Runtime.InteropServices;

namespace HydraulicErosionProj
{
	[System.Serializable]
	public class MeshGenerator
	{
		[StructLayout(LayoutKind.Explicit, Size = 65025)]
		public class HeightMap
		{
			[FieldOffset(0)]
			public float[] heightMap;
			[FieldOffset(0)]
			public float[,] coords;

			public HeightMap(ushort _resolution)
			{
				heightMap = new float[_resolution * _resolution];
				coords = new float[_resolution, _resolution];
			}
		}

		[Min(2), Tooltip("The length of one edge in vertices," +
			" full map size will be this number squared")]
		public ushort m_resolution = 255;

		[Range(0.001f, 100f), Tooltip("How large the map will appear in the world")]
		public float m_horizontalScale = 20f;

		[Range(0.001f, 5f), Tooltip("The intensity of the Perlin noise")]
		public float m_verticalScale = 2f;

		[Range(0.1f, 2f), Tooltip("The size of the Perlin noise")]
		public float m_noiseScale = 0.7f;

		[Range(1, 20), Tooltip("How many times the perlin noise is sampled," +
			" each subsiquent sample has increased scale and reduced intensity")]
		public sbyte m_noiseLayers = 10;

		private Vector2[] m_noiseOffset; // The offset of the Perlin noise
		public HeightMap m_heightMap;    // Struct union of 1d and 2d heightmap data

		public void UpdateNoiseLayers()
		{
			Vector2[] temp = m_noiseOffset;
			m_noiseOffset = new Vector2[m_noiseLayers];
			for (int i = 0; i < m_noiseOffset.Length; ++i)
			{
				try
				{
					m_noiseOffset[i] = temp[i];
				}
				catch
				{
					m_noiseOffset[i].x = Random.Range(0, 1000);
					m_noiseOffset[i].y = Random.Range(0, 1000);
				}
			}
		}

		public void RandomiseOffset()
		{
			m_noiseOffset = new Vector2[m_noiseLayers];
			for (int i = 0; i < m_noiseLayers; ++i)
			{
				m_noiseOffset[i].x = Random.Range(0, 1000);
				m_noiseOffset[i].y = Random.Range(0, 1000);
			}
		}

		public void GenerateHeightMap()
		{
			m_heightMap = new HeightMap(m_resolution);
			for (int y = 0; y < m_resolution; ++y)
			{
				for (int x = 0; x < m_resolution; ++x)
				{
					for (int l = 0; l < m_noiseLayers; ++l)
					{
						m_heightMap.coords[x, y] += Mathf.PerlinNoise(
							m_noiseOffset[l].x + (x * (l + 1) / m_noiseScale) / m_resolution,
							m_noiseOffset[l].y + (y * (l + 1) / m_noiseScale) / m_resolution
						) * m_verticalScale / (l + 1);
					}
				}
			}
		}

		public Mesh GenerateMesh()
		{
			// Initialise required values

			Vector3[] vertices = new Vector3[m_resolution * m_resolution];
			int[] triangles = new int[(m_resolution - 1) * (m_resolution - 1) * 6];
			int triIndex = 0;
			Vector2[] uv = new Vector2[vertices.Length];

			// Loop through each position in the height map
			for (int y = 0; y < m_resolution; ++y)
			{
				for (int x = 0; x < m_resolution; ++x)
				{
					// Convert the 2d coord to 1d
					int i = x + y * m_resolution;
					// For each vertex the x and z values are offset the all values are scaled
					vertices[i] = new Vector3(
						(x - m_resolution * .5f + .5f) / (m_resolution - 1) * m_horizontalScale,
						m_heightMap.coords[x, y] * m_verticalScale - m_verticalScale,
						(y - m_resolution * .5f + .5f) / (m_resolution - 1) * m_horizontalScale
					);
					uv[i].y = m_heightMap.coords[x, y];  // Tbh I have no idea what this does

					// Stitches together the vertices into triangles
					if (x != m_resolution - 1 && y != m_resolution - 1)
					{
						triangles[triIndex] = i;
						triangles[triIndex + 1] = i + m_resolution;
						triangles[triIndex + 2] = i + m_resolution + 1;

						triangles[triIndex + 3] = i;
						triangles[triIndex + 4] = i + m_resolution + 1;
						triangles[triIndex + 5] = i + 1;
						triIndex += 6;
					}
				}
			}

			Mesh result = new Mesh();
			// Apply the new data to the mesh
			result.vertices = vertices;
			result.triangles = triangles;
			result.uv = uv;
			// Recalculate mesh data
			result.RecalculateNormals();
			result.RecalculateBounds();
			result.RecalculateTangents();
			result.Optimize();

			return result;
		}
	}
}
