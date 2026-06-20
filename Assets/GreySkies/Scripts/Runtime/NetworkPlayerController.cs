using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

namespace GreySkies
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkPlayerController : NetworkBehaviour
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 4.0f;
        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 6.0f;
        [Tooltip("Rotation speed of the character")]
        public float RotationSpeed = 1.0f;
        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;
        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again.")]
        public float JumpTimeout = 0.1f;
        [Tooltip("Time required to pass before entering the fall state.")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not.")]
        public bool Grounded = true;
        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;
        [Tooltip("The radius of the grounded check.")]
        public float GroundedRadius = 0.5f;
        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera")]
        public GameObject CinemachineCameraTarget;
        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 90.0f;
        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -90.0f;

        // Player movement/look state
        private float _speed;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // Timeout variables
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        private CharacterController _controller;
        private PlayerInput _playerInput;
        private SurvivalStats _survivalStats;
        private Vector2 _moveInput;
        private Vector2 _lookInput;
        private bool _jumpInput;
        private bool _sprintInput;

        private float _cinemachineTargetPitch;
        private const float _threshold = 0.01f;

        [HideInInspector] public bool DisableInput = false;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _playerInput = GetComponent<PlayerInput>();
            _survivalStats = GetComponent<SurvivalStats>();
        }

        private void Start()
        {
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        public bool IsSprinting()
        {
            // Sprint is only active if we have stamina remaining (server-authoritative check or local-responsive check)
            if (_survivalStats != null && _survivalStats.Stamina.Value < 5f)
            {
                return false;
            }
            return _sprintInput;
        }

        public bool IsMoving()
        {
            return _moveInput != Vector2.zero;
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                // Set up local camera follow
                CinemachineCamera virtualCamera = FindAnyObjectByType<CinemachineCamera>();
                if (virtualCamera != null)
                {
                    virtualCamera.Follow = CinemachineCameraTarget.transform;
                }
                
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                // Disable components that shouldn't run on remote clones
                if (_playerInput != null) _playerInput.enabled = false;
                // Leave CharacterController but don't move it directly from Update/Inputs on other clients
            }
        }

        private void Update()
        {
            if (!IsOwner) return;

            if (DisableInput)
            {
                _moveInput = Vector2.zero;
                _lookInput = Vector2.zero;
                _jumpInput = false;
                _sprintInput = false;
                // Apply simple gravity
                GroundedCheck();
                if (Grounded)
                {
                    if (_verticalVelocity < 0.0f)
                    {
                        _verticalVelocity = -2f;
                    }
                }
                else
                {
                    if (_verticalVelocity < _terminalVelocity)
                    {
                        _verticalVelocity += Gravity * Time.deltaTime;
                    }
                }
                _controller.Move(new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
                return;
            }

            JumpAndGravity();
            GroundedCheck();
            Move();
            HandleInteraction();
        }

        [Header("Interaction Settings")]
        [Tooltip("The range of the interaction raycast")]
        [SerializeField] private float _interactionRange = 2.5f;

        private void HandleInteraction()
        {
            Ray ray;
            if (Camera.main != null)
            {
                ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
            }
            else
            {
                ray = new Ray(transform.position + Vector3.up * 1.5f, transform.forward);
            }

            if (Physics.Raycast(ray, out RaycastHit hit, _interactionRange))
            {
                PickableItem pickable = hit.collider.GetComponentInParent<PickableItem>();
                if (pickable == null)
                {
                    pickable = hit.collider.GetComponent<PickableItem>();
                }

                if (pickable != null)
                {
                    // Print hover message (could be connected to UI)
                    // Debug.Log($"[Interaction] Hovering over PickableItem: {pickable.ItemID.Value}");

                    if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                    {
                        Debug.Log($"[Interaction] Pressed E to interact with item: {pickable.ItemID.Value}");
                        pickable.InteractServerRpc(NetworkManager.Singleton.LocalClientId);
                    }
                }
            }
        }

        private void LateUpdate()
        {
            if (!IsOwner) return;
            if (DisableInput) return;

            CameraRotation();
        }

        // New Input System callbacks (registered via PlayerInput events or SendMessages)
        public void OnMove(InputValue value)
        {
            if (!IsOwner) return;
            _moveInput = value.Get<Vector2>();
        }

        public void OnLook(InputValue value)
        {
            if (!IsOwner) return;
            _lookInput = value.Get<Vector2>();
        }

        public void OnJump(InputValue value)
        {
            if (!IsOwner) return;
            _jumpInput = value.isPressed;
        }

        public void OnSprint(InputValue value)
        {
            if (!IsOwner) return;
            _sprintInput = value.isPressed;
        }

        private void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
        }

        private void CameraRotation()
        {
            if (_lookInput.sqrMagnitude >= _threshold)
            {
                // Don't multiply mouse input by Time.deltaTime
                bool isMouse = true; // Defaulting to true for standard mouse control
                float deltaTimeMultiplier = isMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetPitch += _lookInput.y * RotationSpeed * deltaTimeMultiplier;
                _rotationVelocity = _lookInput.x * RotationSpeed * deltaTimeMultiplier;

                _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

                CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);
                transform.Rotate(Vector3.up * _rotationVelocity);
            }
        }

        private void Move()
        {
            float targetSpeed = _sprintInput ? SprintSpeed : MoveSpeed;

            if (_moveInput == Vector2.zero) targetSpeed = 0.0f;

            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = 1f;

            if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            Vector3 inputDirection = new Vector3(_moveInput.x, 0.0f, _moveInput.y).normalized;

            if (_moveInput != Vector2.zero)
            {
                inputDirection = transform.right * _moveInput.x + transform.forward * _moveInput.y;
            }

            _controller.Move(inputDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                _fallTimeoutDelta = FallTimeout;

                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                if (_jumpInput && _jumpTimeoutDelta <= 0.0f)
                {
                    // Verify stamina is sufficient (requires 10+ stamina to jump)
                    if (_survivalStats == null || _survivalStats.Stamina.Value >= 10.0f)
                    {
                        _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                        
                        if (_survivalStats != null && IsOwner)
                        {
                            _survivalStats.ApplyJumpStaminaDrainServerRpc();
                        }
                    }
                }

                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                _jumpTimeoutDelta = JumpTimeout;

                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }

                _jumpInput = false;
            }

            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }
    }
}