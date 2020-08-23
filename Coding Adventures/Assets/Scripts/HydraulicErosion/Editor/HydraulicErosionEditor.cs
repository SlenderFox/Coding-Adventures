using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(HydraulicErosion))]
public class HydraulicErosionEditor : Editor
{
    HydraulicErosion m_heScript;

    private void OnEnable()
    {
        m_heScript = (HydraulicErosion)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(8);

        if (GUILayout.Button("Generate Heightmap"))
        {
            m_heScript.BtnGenerateHeightmap();
        }

        //if (GUILayout.Button("Generate Mesh"))
        //{
        //    m_heScript.BtnGenerateMesh();
        //}

        if (GUILayout.Button($"Run Erosion ({m_heScript.GetIterations()} iterations)"))
        {
            m_heScript.BtnRunErosion();
        }

        if (GUILayout.Button($"Run Erosion Compute Shader ({m_heScript.GetIterations()} iterations)"))
        {
            m_heScript.BtnRunErosionComputeShader();
        }
    }
}
