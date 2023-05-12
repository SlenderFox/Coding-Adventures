using UnityEngine;
using UnityEditor;

namespace HydraulicErosionProj
{
	[CustomEditor(typeof(HydraulicErosionMaster))]
	public class HydraulicErosionEditor: Editor
	{
		HydraulicErosionMaster m_heScript;

		private void OnEnable()
		{
			m_heScript = (HydraulicErosionMaster)target;
		}

		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			GUILayout.Space(8);

			if (GUILayout.Button("Generate Heightmap"))
			{
				m_heScript.Btn_GenerateHeightMap();
			}

			if (GUILayout.Button("Build Mesh"))
			{
				m_heScript.Btn_RebuildMesh();
			}

			string numDroplets = m_heScript.m_erosionScript.GetNumberOfDroplets().ToString();
			if (GUILayout.Button($"Run Erosion CPU ({numDroplets} droplets)"))
			{
				m_heScript.Btn_RunErosionCpu();
			}

			string numGroups = m_heScript.m_erosionScript.GetComputeShaderThreads().ToString();
			if (GUILayout.Button($"Run Erosion GPU ({numGroups} droplets)"))
			{
				m_heScript.Btn_RunErosionGpu();
			}
		}
	}
}
