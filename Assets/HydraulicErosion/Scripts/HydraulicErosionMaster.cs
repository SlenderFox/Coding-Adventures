using UnityEngine;

namespace HydraulicErosionProj
{
	[ExecuteInEditMode, RequireComponent(typeof(MeshFilter))]
	public class HydraulicErosionMaster : MonoBehaviour
	{
		public MeshGenerator m_meshGenerator;
		public ErosionScript m_erosionScript;

		private void Awake()
		{
			m_meshGenerator.RandomiseOffset();
			m_meshGenerator.GenerateHeightMap();
			SetMesh(m_meshGenerator.GenerateMesh());
		}

		private void SetMesh(Mesh _mesh)
		{
			gameObject.GetComponent<MeshFilter>().sharedMesh = _mesh;
		}

		/// <summary>
		/// Samples the height from a position in the height map,
		/// values outside the range will return NaN
		/// </summary>
		/// <param name="_pos">The position on the heightmap in 2d space</param>
		/// <returns>The height at the point</returns>
		public float GetFromHeightMap(Vector2Int _pos)
		{
			if (_pos.x < 0
				|| _pos.y < 0
				|| _pos.x >= m_meshGenerator.m_resolution
				|| _pos.y >= m_meshGenerator.m_resolution
			)
			{	return float.PositiveInfinity; }

			return m_meshGenerator.m_heightMap.coords[_pos.x, _pos.y];
		}

		/// <summary>
		/// Does an addition operation to a position in the height map array,
		/// negative numbers can be used to subtract
		/// </summary>
		/// <param name="pPos">The position on the heightmap in 2d space</param>
		/// <param name="pValue">The value to be added (or subtracted)</param>
		public void ModifyHeightMap(ref ErosionScript.Droplet _droplet, float _amount)
		{
			if (_droplet.position.x < 0
				|| _droplet.position.y < 0
				|| _droplet.position.x >= m_meshGenerator.m_resolution
				|| _droplet.position.y >= m_meshGenerator.m_resolution
			)
			{	throw new UnityException("Droplet has gone out of bounds"); }

			//if (_amount >= 0)
			//{
				// Adding sediment to the terrain
				m_meshGenerator.m_heightMap.coords[_droplet.position.x, _droplet.position.y] += _amount;
				_droplet.sediment -= _amount;
			//}
			//else
			//{
			//	// Removing sediment from the terrain
			//	//Invert amount if subtraction to make code easier to understand
			//	_amount = -_amount;
			//	float terrainHeight = GetHeightFromHeightMap(_droplet.position);
			//	float difference = terrainHeight - _amount;
			//	// Prevent from removing too much sediment, creates "bedrock"
			//	if (difference < 0)
			//	{
			//		m_meshGenerator.m_heightMap.coords[_droplet.position.x, _droplet.position.y] -= terrainHeight;
			//		_droplet.sediment += terrainHeight;
			//	}
			//	else
			//	{
			//		m_meshGenerator.m_heightMap.coords[_droplet.position.x, _droplet.position.y] -= _amount;
			//		_droplet.sediment += _amount;
			//	}
			//}
		}

		public void Btn_GenerateHeightMap()
		{
			m_meshGenerator.RandomiseOffset();
			m_meshGenerator.GenerateHeightMap();
			SetMesh(m_meshGenerator.GenerateMesh());
		}

		public void Btn_RebuildMesh()
		{
			m_meshGenerator.GenerateHeightMap();
			SetMesh(m_meshGenerator.GenerateMesh());
		}

		public void Btn_RunErosionCpu()
		{
			m_erosionScript.RunErosion(this, m_meshGenerator.m_resolution);
			SetMesh(m_meshGenerator.GenerateMesh());
		}

		public void Btn_RunErosionGpu()
		{
			m_erosionScript.RunErosionComputeShader(this, m_meshGenerator.m_resolution);
			SetMesh(m_meshGenerator.GenerateMesh());
		}
	}
}
