using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float gravity = -9.81f;

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
        }

        private void OnDisable()
        {
            _inputActions.Player.Move.performed -= OnMove;
            _inputActions.Player.Move.canceled -= OnMove;
            _inputActions.Player.Look.performed -= OnLook;
            _inputActions.Player.Look.canceled -= OnLook;
            _inputActions.Player.Disable();
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

        private void HandleMovement()
        {
            // Ground Check
            bool isGrounded = _characterController.isGrounded;
            if (isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f; // Small downward force to keep grounded
            }

            // Move
            Vector3 move = transform.right * _moveInput.x + transform.forward * _moveInput.y;
            _characterController.Move(move * moveSpeed * Time.deltaTime);

            // Gravity
            _velocity.y += gravity * Time.deltaTime;
            _characterController.Move(_velocity * Time.deltaTime);
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
    }
}
