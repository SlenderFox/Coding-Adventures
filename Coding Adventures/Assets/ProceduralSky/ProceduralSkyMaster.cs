using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ProceduralSkyMaster : MonoBehaviour
{
    [SerializeField]
    private bool m_bEnabled = true;
    [SerializeField, Range(1, 100)]
    private float m_fMaxDepthValue = 20;
    [SerializeField, Range(1, 20)]
    private float m_fAtmosphereRadius = 1;
    [SerializeField]
    private Vector3 m_v3PlanetCentre = new Vector3(0, 0, 0);
    
    private Camera m_cCamera = null;
    private Material m_mShaderMat = null;

    private void Awake()
    {
        if (m_cCamera == null)
        {
            m_cCamera = GetComponent<Camera>();
            m_cCamera.depthTextureMode = DepthTextureMode.Depth;
        }

        if (m_mShaderMat == null)
            m_mShaderMat = new Material(Shader.Find("Hidden/ProceduralSkyShader"));
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (m_mShaderMat == null)
            m_mShaderMat = new Material(Shader.Find("Hidden/ProceduralSkyShader"));

        if (!m_bEnabled)
            Graphics.Blit(source, destination);
        else
        {
            Graphics.Blit(source, destination, m_mShaderMat);
        }
    }

    private void OnValidate()
    {
        if (m_mShaderMat == null)
            m_mShaderMat = new Material(Shader.Find("Hidden/ProceduralSkyShader"));

        m_mShaderMat.SetFloat("_DepthMax", m_fMaxDepthValue);
        m_mShaderMat.SetFloat("atmosphereRadius", m_fAtmosphereRadius);
        float[] bruh = new float[3];
        bruh[0] = m_v3PlanetCentre.x;
        bruh[1] = m_v3PlanetCentre.y;
        bruh[2] = m_v3PlanetCentre.z;
        m_mShaderMat.SetFloatArray("planetCentre", bruh);
    }
}
