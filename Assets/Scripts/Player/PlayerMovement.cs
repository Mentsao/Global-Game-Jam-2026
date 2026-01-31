using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float sprintSpeed = 8f;
        [SerializeField] private float crouchSpeed = 2.5f;
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -9.81f;

        [Header("Crouch Settings")]
        [SerializeField] private float standingHeight = 2f;
        [SerializeField] private float crouchHeight = 1f;
        [SerializeField] private float crouchTransitionSpeed = 10f;
        [SerializeField] private Vector3 standingCenter = new Vector3(0, 0, 0);
        [SerializeField] private Vector3 crouchCenter = new Vector3(0, -0.5f, 0);

        [Header("Look Settings")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float mouseSensitivity = 100f;
        [SerializeField] private float topClamp = 90f;
        [SerializeField] private float bottomClamp = -90f;

        private CharacterController _characterController;
        private InputSystem_Actions _inputActions;
        private Vector2 _moveInput;
        private Vector2 _lookInput;
        private float _xRotation = 0f;
        private Vector3 _velocity;
        private float _stepTimer = 0f;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _inputActions = new InputSystem_Actions();

            // Lock cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnEnable()
        {
            _inputActions.Player.Enable();
            _inputActions.Player.Move.performed += OnMove;
            _inputActions.Player.Move.canceled += OnMove;
            _inputActions.Player.Look.performed += OnLook;
            _inputActions.Player.Look.canceled += OnLook;
            _inputActions.Player.Jump.performed += OnJump;
        }

        private void OnDisable()
        {
            _inputActions.Player.Move.performed -= OnMove;
            _inputActions.Player.Move.canceled -= OnMove;
            _inputActions.Player.Look.performed -= OnLook;
            _inputActions.Player.Look.canceled -= OnLook;
            _inputActions.Player.Jump.performed -= OnJump;
            _inputActions.Player.Disable();
        }

        public void SetControlActive(bool active)
        {
            if (active)
            {
                _inputActions.Player.Enable();
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                _inputActions.Player.Disable();
                // Optionally unlock cursor if needed for QTE UI, but QTE is keyboard mashing so keeps locked usually.
                // If specific actions need to remain enabled (like Pause), we might need to be more granular. 
                // For now, disabling all Player movement/look is what was asked.
                _moveInput = Vector2.zero;
                _lookInput = Vector2.zero;
            }
        }

        private void Update()
        {
            HandleMovement();
            HandleRotation();
        }

        private void OnMove(InputAction.CallbackContext context)
        {
            _moveInput = context.ReadValue<Vector2>();
        }

        private void OnLook(InputAction.CallbackContext context)
        {
            _lookInput = context.ReadValue<Vector2>();
        }

        private void OnJump(InputAction.CallbackContext context)
        {
            if (_characterController.isGrounded)
            {
                _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayJump();
                }
            }
        }

        private void HandleMovement()
        {
            // Ground Check
            bool isGrounded = _characterController.isGrounded;
            if (isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f; // Small downward force to keep grounded
            }

            // --- CROUCH LOGIC ---
            bool isCrouching = false;
            if (Keyboard.current.ctrlKey.isPressed)
            {
                isCrouching = true;
            }

            // Target Height/Center
            float targetHeight = isCrouching ? crouchHeight : standingHeight;
            Vector3 targetCenter = isCrouching ? crouchCenter : standingCenter;

            // Smoothly adjust height
            _characterController.height = Mathf.Lerp(_characterController.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);
            _characterController.center = Vector3.Lerp(_characterController.center, targetCenter, Time.deltaTime * crouchTransitionSpeed);

            // Also adjust Camera position if needed (optional since CC height changes might push it?)
            // Usually camera is child of player, so if player shrinks, camera might need to move down locally?
            // Actually, usually you move the camera pivot. Let's simpler: rely on CC shrink or move camera localPos if needed.
            // For now, let's keep it simple: CC shrink usually handles collision, but visual camera might need to drop.
            Vector3 camPos = cameraTransform.localPosition;
            float camTargetY = isCrouching ? (crouchHeight * 0.8f) : (standingHeight * 0.8f); // Eye level approximation
            if (isCrouching) camTargetY = 0.5f; else camTargetY = 0.8f; // Hardcoded tweaks based on typical capsule

            // Let's NOT hardcode visual camera Y yet unless requested, simpler to let Physics handle it or just rely on hitboxes for gameplay.
            // User requested: "lower the camera". Ok.
            
            // Re-calc specific camera height targets
            // float standCamY = 0.6f; // Unused
            
            // We'll define specific camera offsets based on standard Unity Capsule (2m tall)
            // Stand Eye: 1.6m (or 0.6m local if pivot is center?)
            // Let's just use the Input to decide speed first.

            // --- SPRINT LOGIC ---
            float currentSpeed = walkSpeed;
            if (isCrouching) 
            {
                currentSpeed = crouchSpeed;
            }
            else if (Keyboard.current.shiftKey.isPressed)
            {
                currentSpeed = sprintSpeed;
            }

            // Move
            Vector3 move = transform.right * _moveInput.x + transform.forward * _moveInput.y;
            bool isMoving = move.sqrMagnitude > 0;
            _characterController.Move(move * currentSpeed * Time.deltaTime);

            // Footsteps Logic
            if (isMoving && isGrounded)
            {
                _stepTimer -= Time.deltaTime;
                if (_stepTimer <= 0f)
                {
                     bool isRunning = (currentSpeed == sprintSpeed);
                     AudioManager.Instance.PlayFootstep(isRunning, isCrouching);
                     
                     // Reset timer based on speed (simulated stride)
                     float strideDuration = isRunning ? 0.35f : (isCrouching ? 0.6f : 0.5f);
                     _stepTimer = strideDuration;
                }
            }
            else
            {
                 // Reset timer so next step is immediate when starting to move
                 _stepTimer = 0f;
            }

            // Gravity
            _velocity.y += gravity * Time.deltaTime;
            _characterController.Move(_velocity * Time.deltaTime);

            // Adjust Camera Height Smoothly
            if (cameraTransform != null)
            {
                Vector3 currentCamPos = cameraTransform.localPosition;
                float targetCamY = isCrouching ? 0.3f : 0.6f; // 0.6 is typical 'Head' pos for 2m capsule center 0
                
                // If user changed inspector values, 0.6 might be wrong. Let's verify standard height.
                // Standard Unity Capsule Height = 2. Camera usually at y=0.5 or 0.6 relative to center.
                // Crouch Height = 1. Camera should be at y=0 or 0.1?
                
                currentCamPos.y = Mathf.Lerp(currentCamPos.y, targetCamY, Time.deltaTime * crouchTransitionSpeed);
                cameraTransform.localPosition = currentCamPos;
            }
        }

        private void HandleRotation()
        {
            float mouseX = _lookInput.x * mouseSensitivity * Time.deltaTime;
            float mouseY = _lookInput.y * mouseSensitivity * Time.deltaTime;

            _xRotation -= mouseY;
            _xRotation = Mathf.Clamp(_xRotation, bottomClamp, topClamp);

            if (cameraTransform != null)
            {
                cameraTransform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
            }
            
            transform.Rotate(Vector3.up * mouseX);
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            // Debug.Log($"Player hit: {hit.collider.name}"); // Uncomment if needed

            // Check if we hit a Police NPC
            PoliceNPC police = hit.collider.GetComponent<PoliceNPC>();
            if (police != null)
            {
                police.CheckForDocument(this.gameObject);
            }
        }
    }
}
