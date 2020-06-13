using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyCam : MonoBehaviour
{
    [SerializeField]
    private bool m_bEnabled = true;

    [SerializeField]
    private float m_fMovementSpeed = 1;

    [SerializeField]
    private float m_fSprintMult = 2;

    [SerializeField]
    private Vector2 m_v2Sensitivity = new Vector2(1, 1);

    private Transform m_tLocalTransform;
    private Transform m_tChild;

    /// <summary>
    /// Called at the beginning of the first frame
    /// </summary>
    private void Start()
    {
        m_tLocalTransform = transform;
        m_tChild = transform.GetChild(0);

        if (m_bEnabled)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    /// <summary>
    /// Called once per frame
    /// </summary>
    private void Update()
    {
        if (!m_bEnabled)
            return;

        // Quick end button
        if (Input.GetKey(KeyCode.End))
        {
            #if UNITY_STANDALONE
                Application.Quit();
            #endif

            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }

        // Rotates the camera
        m_tLocalTransform.Rotate(0, Input.GetAxis("Mouse X") * m_v2Sensitivity.x, 0);
        m_tChild.Rotate(Input.GetAxis("Mouse Y") * -m_v2Sensitivity.y, 0, 0);

        // Translates the camera
        if (Input.GetKey(KeyCode.LeftShift))
        {
            m_tLocalTransform.Translate(
                Input.GetAxis("Horizontal") * Time.deltaTime * m_fMovementSpeed * m_fSprintMult,
                Input.GetAxis("Actual Vertical") * Time.deltaTime * m_fMovementSpeed * m_fSprintMult,
                Input.GetAxis("Vertical") * Time.deltaTime * m_fMovementSpeed * m_fSprintMult);
        }
        else
        {
            m_tLocalTransform.Translate(
                Input.GetAxis("Horizontal") * Time.deltaTime * m_fMovementSpeed,
                Input.GetAxis("Actual Vertical") * Time.deltaTime * m_fMovementSpeed,
                Input.GetAxis("Vertical") * Time.deltaTime * m_fMovementSpeed);
        }
    }
}
