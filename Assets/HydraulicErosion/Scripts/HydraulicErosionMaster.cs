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
			m_erosionScript.SetMaster(this);
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
		public void ModifyHeightMap(Vector2Int _pos, float _amount)
		{
			Mathf.Clamp(_pos.x, 0, m_meshGenerator.m_resolution);
			Mathf.Clamp(_pos.y, 0, m_meshGenerator.m_resolution);

			m_meshGenerator.m_heightMap.coords[_pos.x, _pos.y] += _amount;
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
			m_erosionScript.RunErosion(m_meshGenerator.m_resolution);
			SetMesh(m_meshGenerator.GenerateMesh());
		}

		public void Btn_RunErosionGpu()
		{
			m_erosionScript.RunErosionComputeShader(this, m_meshGenerator.m_resolution);
			SetMesh(m_meshGenerator.GenerateMesh());
		}
	}
}
