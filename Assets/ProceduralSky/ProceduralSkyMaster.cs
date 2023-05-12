using UnityEngine;

[ExecuteInEditMode]
public class ProceduralSkyMaster : MonoBehaviour
{
    [SerializeField]
    private bool m_bEnabled = false;

    [SerializeField, Range(0.3f, 5)]
    private float m_fAtmosphereScale = 1;

    [SerializeField, Range(-1, 10)]
    private float m_fDensityFalloff = 1;
    [SerializeField, Range(0.001f, 2)]
    private float m_fIntensity = 0.5f;

    [SerializeField, Range(1, 20)]
    private int m_iNumInScatterPoints = 4;
    [SerializeField, Range(1, 20)]
    private int m_iNumOpticalDepthPoints = 4;

    [SerializeField]
    private Transform m_tPlanet = null;
    [SerializeField]
    private Transform m_tSun = null;

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

    private void Start()
    {
        if (m_tPlanet != null && m_tSun != null)
            m_bEnabled = true;
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!m_bEnabled)
        {
            Graphics.Blit(source, destination);
            return;
        }

        if (m_mShaderMat == null)
            m_mShaderMat = new Material(Shader.Find("Hidden/ProceduralSkyShader"));

        UpdateShaderParameters();

        Graphics.Blit(source, destination, m_mShaderMat);
    }

    private void UpdateShaderParameters()
    {
        Vector4 dirToSun = new Vector4(-m_tSun.forward.x, -m_tSun.forward.y, -m_tSun.forward.z);
        Vector4 planetCentre = new Vector4(m_tPlanet.position.x, m_tPlanet.position.y, m_tPlanet.position.z);
        m_mShaderMat.SetVector("sunPos", dirToSun);
        m_mShaderMat.SetVector("planetCentre", planetCentre);
        m_mShaderMat.SetFloat("planetRadius", m_tPlanet.localScale.magnitude);
        m_mShaderMat.SetFloat("atmosphereRadius", m_tPlanet.localScale.magnitude * m_fAtmosphereScale);

        m_mShaderMat.SetInt("numInScatterPoints", m_iNumInScatterPoints);
        m_mShaderMat.SetInt("numOpticalDepthPoints", m_iNumOpticalDepthPoints);
        m_mShaderMat.SetFloat("densityFalloff", m_fDensityFalloff);
        m_mShaderMat.SetFloat("intensity", m_fIntensity);
    }
}
