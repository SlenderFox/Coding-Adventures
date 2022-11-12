using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RayTracingMaster))]
public class RayTracingMasterEditor : Editor
{
    RayTracingMaster m_rtmScript;

    private void OnEnable()
    {
        m_rtmScript = (RayTracingMaster)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Re-generate spheres"))
        {
            m_rtmScript.SetUpScene();
        }
    }
}
