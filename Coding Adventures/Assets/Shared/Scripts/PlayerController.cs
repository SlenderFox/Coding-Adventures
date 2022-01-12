using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public sealed class PlayerController : MonoBehaviour
{
    public static bool s_bInputEnabled { internal get; set; } = true;

    /// <summary>
    /// This class controls the players camera looking
    /// </summary>
    [System.Serializable]
    internal sealed class PlayerLook
    {
        public static bool s_bLookEnabled { internal get; set; } = true;

        [System.Serializable]
        internal struct Vec2
        {
            internal Vec2(float pX, float pY)
            {
                m_x = pX;
                m_y = pY;
            }

            [Range(0.0001f, 100f)]
            public float m_x;
            [Range(0.0001f, 100f)]
            public float m_y;
        }

        // ----------Visible variables----------
        [SerializeField]
        private bool m_bAllowInput = true;
        [SerializeField]
        private Vec2 m_v2Sensitivity = new Vec2(1, 1);

        // ----------Hidden variables----------
        internal PlayerController m_pcWrapper;

        /// <summary>
        /// Called once before the first frame
        /// </summary>
        internal void Start()
        {
            //s_bLookEnabled = true;

            // Lock the mouse in place and hide it
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        /// <summary>
        /// Called once per frame
        /// </summary>
        internal void Update()
        {
            if (s_bInputEnabled && s_bLookEnabled && m_bAllowInput)
                CameraUpdate();
        }

        /// <summary>
        /// Updates the camera movement
        /// </summary>
        private void CameraUpdate()
        {
            // Rotates the camera
            m_pcWrapper.m_tLocalTransform.Rotate(0, Input.GetAxis("Mouse X") * m_v2Sensitivity.m_x * 0.1f, 0);
            m_pcWrapper.m_tFirstChild.Rotate(Input.GetAxis("Mouse Y") * -m_v2Sensitivity.m_y * 0.1f, 0, 0);
        }
    } // PlayerLook

    /// <summary>
    /// This class controls the players movement
    /// </summary>
    [System.Serializable]
    internal sealed class PlayerMovement
    {
        public static bool s_bMovementEnabled { internal get; set; } = true;

        // ----------Visible variables----------
        //[SerializeField]
        //private KeyCode m_kcForward = KeyCode.W;
        //[SerializeField]
        //private KeyCode m_kcSprint = KeyCode.LeftShift;
        [SerializeField]
        private KeyCode m_kcJump = KeyCode.Space;
        [Space(8)]
        [SerializeField]
        private bool m_bAllowInput = true;
        [SerializeField]
        private bool m_bJumpEnabled = true;

        [Header("Movement Variables")]
        [SerializeField]
        private float m_fVelocityCutoff = 20;
        [SerializeField]
        private float m_fAcceleration = 5;
        [SerializeField]
        private float m_fForwardSpeed = 5;
        //[SerializeField]
        //private float m_fStrafeSpeed = 1;
        //[SerializeField]
        //private float m_fBackwardSpeed = 1;
        //[SerializeField]
        //private float m_fSprintSpeed = 1;
        [SerializeField]
        private float m_fJumpForce = 5;
        [SerializeField]
        private float m_fAerialManuverability = 3;

        [Header("Timers")]
        [SerializeField]
        private float m_fJumpCooldown = 0.01f;

        [Header("Raycasting")]
        [SerializeField]
        private float m_fGRayOrigin = 0.5f;
        [SerializeField]
        private float m_fGRayLength = 0.55f;
        [SerializeField]
        private float m_fGRaySpread = 0.45f;

        // ----------Hidden variables----------
        internal PlayerController m_pcWrapper;

        [SerializeField]
        private bool m_bGrounded = true;
        //[SerializeField]
        //private bool m_bSprinting = true;

        private float m_fJumpTimer = 0;
        private int m_iWalkableMask = 0;

        private Vector2 m_v2Input = new Vector2();
        private Vector3 m_v3Force = new Vector3();

        /// <summary>
        /// Called once before the first frame
        /// </summary>
        internal void Start()
        {
            //s_bMovementEnabled = true;

            m_fJumpTimer = m_fJumpCooldown;
            m_iWalkableMask = LayerMask.GetMask("WalkableStaticFlat", "WalkableStaticSloped", "WalkableDynamic");
        }

        /// <summary>
        /// Called once per frame
        /// </summary>
        internal void Update()
        {
            // Decrements the jump timer
            if (m_fJumpTimer > 0)
            {
                m_fJumpTimer -= Time.deltaTime;
                if (m_fJumpTimer < 0)
                    m_fJumpTimer = 0;
            }

            GroundCheck();
            DoMovement();
        }

        /// <summary>
        /// Looks below the player for valid ground
        /// </summary>
        private void GroundCheck()
        {
            if (m_pcWrapper.m_bDebug)
            {
                Debug.DrawRay(m_pcWrapper.m_tLocalTransform.position
                    + new Vector3(0, m_fGRayOrigin, m_fGRaySpread), new Vector3(0, -m_fGRayLength, 0), Color.red);
                Debug.DrawRay(m_pcWrapper.m_tLocalTransform.position
                    + new Vector3(m_fGRaySpread, m_fGRayOrigin, 0), new Vector3(0, -m_fGRayLength, 0), Color.red);
                Debug.DrawRay(m_pcWrapper.m_tLocalTransform.position
                    + new Vector3(0, m_fGRayOrigin, -m_fGRaySpread), new Vector3(0, -m_fGRayLength, 0), Color.red);
                Debug.DrawRay(m_pcWrapper.m_tLocalTransform.position
                    + new Vector3(-m_fGRaySpread, m_fGRayOrigin, 0), new Vector3(0, -m_fGRayLength, 0), Color.red);
            }

            // Checks if one of four downward facing rays collide with a surface
            if (Physics.Raycast(m_pcWrapper.m_tLocalTransform.position
                + new Vector3(0, m_fGRayOrigin, m_fGRaySpread), Vector3.down, m_fGRayLength, m_iWalkableMask)
                || Physics.Raycast(m_pcWrapper.m_tLocalTransform.position
                + new Vector3(0, m_fGRayOrigin, m_fGRaySpread), Vector3.down, m_fGRayLength, m_iWalkableMask)
                || Physics.Raycast(m_pcWrapper.m_tLocalTransform.position
                + new Vector3(0, m_fGRayOrigin, m_fGRaySpread), Vector3.down, m_fGRayLength, m_iWalkableMask)
                || Physics.Raycast(m_pcWrapper.m_tLocalTransform.position
                + new Vector3(0, m_fGRayOrigin, m_fGRaySpread), Vector3.down, m_fGRayLength, m_iWalkableMask))
            {
                m_bGrounded = true;
            }
            else
            {
                m_bGrounded = false;
                //m_bSprinting = false;
            }
        }

        /// <summary>
        /// All player movement is done here
        /// </summary>
        private void DoMovement()
        {
            //if (m_bGrounded && Input.GetKey(m_kcSprint) && Input.GetKey(m_kcForward))
            //    m_bSprinting = true;
            //if (!(!Input.GetKeyUp(m_kcSprint) && Input.GetKey(m_kcForward)))
            //    m_bSprinting = false;

            // Resets the force data
            m_v3Force -= m_v3Force;

            if (s_bInputEnabled && s_bMovementEnabled && m_bAllowInput)
            {
                // Caches the input to reduces calls
                m_v2Input = new Vector2(Input.GetAxis("Vertical"), Input.GetAxis("Horizontal"));

                // Converts the unit square to a unit circle as to maintain constant magnitude
                m_v2Input = new Vector2(m_v2Input.x * Mathf.Sqrt(1 - 0.5f * m_v2Input.y * m_v2Input.y),
                    m_v2Input.y * Mathf.Sqrt(1 - 0.5f * m_v2Input.x * m_v2Input.x));
            }
            else
            {
                m_v2Input = new Vector2();
            }

            if (m_bGrounded)
            {
                // The players velocity cant be reduced by trying to move in the same direction

                /*
                 * Could try a split system where when the current velocity is within the movement
                 * the players velocity is directly used but outside of the bounds it uses a regular apply force
                */

                if (m_pcWrapper.m_rbRigidbody.velocity.magnitude < m_fVelocityCutoff)
                {
                    /*Temp scaling both inputs by forward speed*/
                    m_v2Input *= m_fForwardSpeed;

                    // Rotates the force to match the player
                    m_v3Force += m_pcWrapper.m_tLocalTransform.forward * m_v2Input.x;
                    m_v3Force += m_pcWrapper.m_tLocalTransform.right * m_v2Input.y;

                    // Sets the horizontal velocity of the player directly to the input forces
                    m_pcWrapper.m_rbRigidbody.velocity = new Vector3(m_v3Force.x,
                        m_pcWrapper.m_rbRigidbody.velocity.y, m_v3Force.z);

                    // Reset the forces for jumping
                    m_v3Force -= m_v3Force;
                }
                else
                {
                    //// Multiplies speed based on moving direction
                    //if (m_v2Input.x > 0)
                    //{
                    //    // Multiplies forwards speed based on current sprinting status
                    //    if (m_bSprinting)
                    //        m_v2Input.x *= m_fSprintSpeed;
                    //    else
                    //        m_v2Input.x *= m_fForwardSpeed;
                    //}
                    //else if (m_v2Input.x < 0)
                    //{
                    //    m_v2Input.x *= m_fBackwardSpeed;
                    //}

                    //// Always scale the strafe speed
                    //m_v2Input.y *= m_fStrafeSpeed;

                    /*Temp scaling both inputs by forward speed*/
                    m_v2Input *= m_fForwardSpeed;

                    // Rotates the force to match the player
                    m_v3Force += m_pcWrapper.m_tLocalTransform.forward * m_v2Input.x;
                    m_v3Force += m_pcWrapper.m_tLocalTransform.right * m_v2Input.y;

                    // Finds the difference between the desired velocity and the current
                    Vector3 diff = new Vector3(m_v3Force.x - m_pcWrapper.m_rbRigidbody.velocity.x,
                        0, m_v3Force.z - m_pcWrapper.m_rbRigidbody.velocity.z);

                    m_v3Force = diff * m_fAcceleration;
                }
            }
            else
            {
                m_v2Input *= m_fAerialManuverability * Time.deltaTime * 100;

                // Rotates the force to match the player
                m_v3Force += m_pcWrapper.m_tLocalTransform.forward * m_v2Input.x;
                m_v3Force += m_pcWrapper.m_tLocalTransform.right * m_v2Input.y;
            }

            if (s_bInputEnabled && s_bMovementEnabled && m_bAllowInput)
            {
                // Does jumping
                if (m_fJumpTimer <= 0 && m_bJumpEnabled && m_bGrounded
                && Input.GetKeyDown(m_kcJump) && m_pcWrapper.m_rbRigidbody.velocity.y < m_fJumpForce)
                {
                    // Resets the jump cooldown
                    m_fJumpTimer = m_fJumpCooldown;

                    m_pcWrapper.m_rbRigidbody.velocity = new Vector3(m_pcWrapper.m_rbRigidbody.velocity.x,
                        m_fJumpForce, m_pcWrapper.m_rbRigidbody.velocity.z);

                    //m_v3Force.y = m_fJumpForce * m_pcWrapper.m_rbRigidbody.mass;
                }
            }

            m_pcWrapper.m_rbRigidbody.AddForce(m_v3Force * m_pcWrapper.m_rbRigidbody.mass);
        }
    } // PlayerMovement

    // ----------Visible variables----------
    [SerializeField]
    private KeyCode m_kcFocus = KeyCode.K;
    [SerializeField]
    private bool m_bDebug = false;
    [SerializeField]
    private bool m_bFocused = true;
    [SerializeField]
    private PlayerLook m_plLook = new PlayerLook();
    [SerializeField]
    private PlayerMovement m_pmMovement = new PlayerMovement();

    // ----------Hidden variables----------
    internal Rigidbody m_rbRigidbody;
    internal Transform m_tLocalTransform;
    internal Transform m_tFirstChild;

    private void Awake()
    {
        // Local caching of the transforms to reduce cpu load
        m_tLocalTransform = transform;
        m_tFirstChild = transform.GetChild(0);

        m_rbRigidbody = GetComponent<Rigidbody>();
        m_rbRigidbody.freezeRotation = true;

        m_plLook.m_pcWrapper = this;
        m_pmMovement.m_pcWrapper = this;

        //s_bInputEnabled = true;
    }

    private void Start()
    {
        m_plLook.Start();
        m_pmMovement.Start();
    }

    private void Update()
    {
        if (Input.GetKeyDown(m_kcFocus))
        {
            m_bFocused = !m_bFocused;
            s_bInputEnabled = m_bFocused;
            // Toggles cursor visibility
            Cursor.visible = !m_bFocused;
            Cursor.lockState = m_bFocused ? CursorLockMode.Locked : CursorLockMode.None;
        }

        m_plLook.Update();
        m_pmMovement.Update();
    }
} // PlayerController
