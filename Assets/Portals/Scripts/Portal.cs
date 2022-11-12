using UnityEngine;
using System.Collections.Generic;

public class Portal : MonoBehaviour
{
    [SerializeField]
    private Portal m_pLinkedPortal = null;
    [SerializeField]
    private Camera m_cPortalCamera = null;
    [SerializeField]
    private MeshRenderer m_mrScreen = null;

    // ---------- Hidden ----------

    private List<PortalTraveller> m_LptTravellers = new List<PortalTraveller>();
    private RenderTexture m_rtViewTexture = null;
    private Camera m_cPlayerCamera = null;
    // Local transform caches
    private Transform m_tTransform;
    private Transform m_tPlayerCamera;
    private Transform m_tPortalCamera;
    private Transform m_tOtherPortal;
    private Transform m_tScreen;

    private void Awake()
    {
        m_cPlayerCamera = Camera.main;

        // Caches the transforms
        m_tTransform = transform;
        m_tPlayerCamera = m_cPlayerCamera.transform;
        m_tPortalCamera = m_cPortalCamera.transform;
        m_tOtherPortal = m_pLinkedPortal.transform;
        m_tScreen = m_mrScreen.transform;

        m_cPortalCamera.enabled = false;
    }

    static bool VisibleFromCamera(Renderer pRenderer, Camera pCamera)
    {
        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(pCamera);
        return GeometryUtility.TestPlanesAABB(frustumPlanes, pRenderer.bounds);
    }

    private void CreateViewTexture()
    {
        if (m_rtViewTexture == null || m_rtViewTexture.width != Screen.width
            || m_rtViewTexture.height != Screen.height)
        {
            if (m_rtViewTexture != null)
                m_rtViewTexture.Release();

            m_rtViewTexture = new RenderTexture(Screen.width, Screen.height, 0);
            m_cPortalCamera.targetTexture = m_rtViewTexture;
            m_pLinkedPortal.m_mrScreen.material.SetTexture("_MainTex", m_rtViewTexture);
        }
    }

    private void ProtectScreenFromClipping()
    {
        float halfHeight = m_cPlayerCamera.nearClipPlane * Mathf.Tan(m_cPlayerCamera.fieldOfView
            * 0.5f * Mathf.Deg2Rad);
        float halfWidth = halfHeight * m_cPlayerCamera.aspect;
        float dstToNearClipPlaneCorner = new Vector3(halfWidth, halfHeight,
            m_cPlayerCamera.nearClipPlane).magnitude;

        bool camFacingSameDirAsPortal = Vector3.Dot(m_tTransform.forward, m_tTransform.position
            - m_tPlayerCamera.position) > 0;
        m_tScreen.localScale = new Vector3(m_tScreen.localScale.x, m_tScreen.localScale.y,
            dstToNearClipPlaneCorner);
        m_tScreen.localPosition = Vector3.forward * dstToNearClipPlaneCorner
            * (camFacingSameDirAsPortal ? 0.5f : -0.5f);
    }

    private void SetNearClipPlane()
    {
        Transform clipPlane = m_tTransform;
        int dot = (int)Mathf.Sign(Vector3.Dot(clipPlane.forward,
            m_tTransform.position - m_tPortalCamera.position));

        Vector3 camSpacePos = m_cPortalCamera.worldToCameraMatrix.MultiplyPoint(clipPlane.position);
        Vector3 camSpaceNormal = m_cPortalCamera.worldToCameraMatrix.MultiplyVector(clipPlane.forward) * dot;
        float camSpaceDst = -Vector3.Dot(camSpacePos, camSpaceNormal);
        Vector4 clipPlaneCameraSpace = new Vector4(camSpaceNormal.x, camSpaceNormal.y,
            camSpaceNormal.z, camSpaceDst);

        // Update projection based on new clip plane
        // Calculate matrix with player cam so that player cam settings are used
        m_cPortalCamera.projectionMatrix = m_cPlayerCamera.CalculateObliqueMatrix(clipPlaneCameraSpace);
    }

    public void Update()
    {
        if (!VisibleFromCamera(m_pLinkedPortal.m_mrScreen, m_cPlayerCamera))
            return;

        m_mrScreen.enabled = false;

        Matrix4x4 m = m_tTransform.localToWorldMatrix * m_tOtherPortal.worldToLocalMatrix
            * m_tPlayerCamera.localToWorldMatrix;
        m_tPortalCamera.SetPositionAndRotation(m.GetColumn(3), m.rotation);

        ProtectScreenFromClipping();
        SetNearClipPlane();
        CreateViewTexture();

        m_cPortalCamera.Render();
        m_mrScreen.enabled = true;
    }

    private void LateUpdate()
    {
        for (int i = 0; i < m_LptTravellers.Count; i++)
        {
            PortalTraveller traveller = m_LptTravellers[i];
            Transform travTrans = traveller.transform;
            Vector3 offsetFromPortal = travTrans.position - m_tTransform.position;
            int portalSide = (int)Mathf.Sign(Vector3.Dot(offsetFromPortal, m_tTransform.forward));
            int portalSideOld = (int)Mathf.Sign(Vector3.Dot(traveller.previousOffsetFromPortal, m_tTransform.forward));

            if (portalSide != portalSideOld)
            {
                Matrix4x4 m = m_tTransform.localToWorldMatrix * m_tOtherPortal.worldToLocalMatrix
                    * m_tPlayerCamera.localToWorldMatrix;
                traveller.Teleport(m_tTransform, m_tOtherPortal, m.GetColumn(3), m.rotation);

                m_pLinkedPortal.OnTravellerEnterPortal(traveller);
                m_LptTravellers.RemoveAt(i);
                --i;
            }
            else
                traveller.previousOffsetFromPortal = offsetFromPortal;
        }
    }

    private void OnTravellerEnterPortal(PortalTraveller pTraveller)
    {
        if (!m_LptTravellers.Contains(pTraveller))
        {
            pTraveller.EnterPortalThreshold();
            pTraveller.previousOffsetFromPortal = pTraveller.transform.position - m_tTransform.position;
            m_LptTravellers.Add(pTraveller);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var traveller = other.GetComponent<PortalTraveller>();
        if (traveller)
            OnTravellerEnterPortal(traveller);
    }

    private void OnTriggerExit(Collider other)
    {
        var traveller = other.GetComponent<PortalTraveller>();
        if (traveller && m_LptTravellers.Contains(traveller))
        {
            traveller.ExitPortalThreshold();
            m_LptTravellers.Remove(traveller);
        }
    }
}
