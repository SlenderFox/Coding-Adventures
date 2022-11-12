using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayTracingMaster : MonoBehaviour
{
    private struct Sphere
    {
        internal Vector3 position;
        internal float radius;
        internal Vector3 albedo;
        internal Vector3 specular;
    }

    [SerializeField]
    private uint m_uiPlacementAttempts = 100;

    [SerializeField]
    private float m_fValidRegionSize = 100.0f;

    [SerializeField]
    private Vector2 m_v2SphereRadiusBounds = new Vector2(1.0f, 8.0f);

    [SerializeField]
    private ComputeShader m_csRayTracingShader = null;

    [SerializeField]
    private Light m_lDirectionalLight = null;

    [SerializeField]
    private Texture m_tSkybox = null;
    
    private Camera m_cCamera = null;

    private ComputeBuffer _sphereBuffer = null;

    private RenderTexture _target = null;

    private void Awake()
    {
        m_cCamera = GetComponent<Camera>();
        SetUpScene();
    }

    private void OnDisable()
    {
        if (_sphereBuffer != null)
            _sphereBuffer.Release();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
            SetUpScene();
    }

    public void SetUpScene()
    {
        List<Sphere> sphereList = new List<Sphere>();

        // Add a number of random spheres
        for (int i = 0; i < m_uiPlacementAttempts; i++)
        {
            Sphere newSphere = new Sphere();

            // Radius and radius
            newSphere.radius = m_v2SphereRadiusBounds.x + Random.value * (m_v2SphereRadiusBounds.y - m_v2SphereRadiusBounds.x);
            Vector2 randomPos = Random.insideUnitCircle * m_fValidRegionSize;
            newSphere.position = new Vector3(randomPos.x, newSphere.radius, randomPos.y);

            // Reject spheres that are intersecting others
            foreach (Sphere other in sphereList)
            {
                float minDist = newSphere.radius + other.radius;
                if (Vector3.SqrMagnitude(newSphere.position - other.position) < minDist * minDist)
                    goto SkipSphere;
            }

            // Albedo and specular color
            Color sphereColour = Random.ColorHSV();
            bool metal = Random.value < 0.5f;
            newSphere.albedo = metal ? Vector3.zero : new Vector3(sphereColour.r, sphereColour.g, sphereColour.b);
            newSphere.specular = metal ? new Vector3(sphereColour.r, sphereColour.g, sphereColour.b) : Vector3.one * 0.02f;

            // Add the sphere to the list
            sphereList.Add(newSphere);

        SkipSphere:
            continue;
        }

        // Assign to compute buffer
        _sphereBuffer = new ComputeBuffer(sphereList.Count, 40);
        _sphereBuffer.SetData(sphereList);
    }

    private void SetShaderParameters()
    {
        m_csRayTracingShader.SetMatrix("_CameraToWorld", m_cCamera.cameraToWorldMatrix);
        m_csRayTracingShader.SetMatrix("_CameraInverseProjection", m_cCamera.projectionMatrix.inverse);
        m_csRayTracingShader.SetTexture(0, "_SkyboxTexture", m_tSkybox);
        Vector3 light = m_lDirectionalLight.transform.forward;
        m_csRayTracingShader.SetVector("_DirectionalLight", new Vector4(light.x, light.y, light.z, m_lDirectionalLight.intensity));
        m_csRayTracingShader.SetBuffer(0, "_Spheres", _sphereBuffer);
    }

    private void InitRenderTexture()
    {
        if (_target == null || _target.width != Screen.width || _target.height != Screen.height)
        {
            // Release the render texture if we already have one
            if (_target != null)
                _target.Release();

            // Get a render target for ray tracing
            _target = new RenderTexture(Screen.width, Screen.height, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
            { enableRandomWrite = true };
            _target.Create();
        }
    }

    private void Render(RenderTexture pDestination)
    {
        // Make sure we have a current render target
        InitRenderTexture();

        // Set the target and dispatch the compute shader
        m_csRayTracingShader.SetTexture(0, "Result", _target);
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
        m_csRayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

        // Blit the result texture to the screen
        Graphics.Blit(_target, pDestination);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    { 
        SetShaderParameters();
        Render(destination);
    }
}
