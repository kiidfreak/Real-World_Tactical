using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace RealWorldTactical.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class FPSController : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float walkSpeed = 5f;
        public float runSpeed = 8f;
        public float jumpHeight = 3f;
        public float gravity = -9.81f;
        public float mouseSensitivity = 2f;
        
        [Header("Camera Settings")]
        public Camera playerCamera;
        public float cameraHeight = 1.8f;
        
        [Header("Audio")]
        public AudioSource footstepAudio;
        public AudioClip[] footstepSounds;
        
        // Private variables
        private CharacterController controller;
        private Vector3 velocity;
        private bool isGrounded;
        private float xRotation = 0f;
        private bool isRunning = false;
        private float currentSpeed;
        
        // Input System
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction jumpAction;
        private InputAction runAction;
        private InputAction interactAction;
        
        // Data Collection
        private PlayerDataCollector dataCollector;
        private Vector3 lastPosition;
        private float lastFootstepTime;
        private float footstepInterval = 0.5f;
        
        // Movement tracking for data collection
        private List<Vector3> movementPath = new List<Vector3>();
        private float pathUpdateInterval = 0.1f;
        private float lastPathUpdate;
        
        void Start()
        {
            // Get components
            controller = GetComponent<CharacterController>();
            dataCollector = GetComponent<PlayerDataCollector>();
            
            // Setup camera
            if (playerCamera == null)
                playerCamera = Camera.main;
                
            // Lock cursor to center of screen
            Cursor.lockState = CursorLockMode.Locked;
            
            // Initialize position tracking
            lastPosition = transform.position;
            
            // Setup input actions
            SetupInputActions();
        }
        
        void SetupInputActions()
        {
            var inputActions = new InputActionMap("Player");
            
            moveAction = inputActions.AddAction("Move", InputActionType.Value, "<Keyboard>/wasd");
            lookAction = inputActions.AddAction("Look", InputActionType.Value, "<Mouse>/delta");
            jumpAction = inputActions.AddAction("Jump", InputActionType.Button, "<Keyboard>/space");
            runAction = inputActions.AddAction("Run", InputActionType.Button, "<Keyboard>/leftShift");
            interactAction = inputActions.AddAction("Interact", InputActionType.Button, "<Keyboard>/e");
            
            // Enable input actions
            inputActions.Enable();
        }
        
        void Update()
        {
            HandleMovement();
            HandleMouseLook();
            HandleJump();
            HandleRunning();
            HandleInteraction();
            UpdateDataCollection();
        }
        
        void HandleMovement()
        {
            // Check if grounded
            isGrounded = controller.isGrounded;
            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }
            
            // Get input
            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            
            // Calculate movement direction
            Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
            
            // Apply speed
            currentSpeed = isRunning ? runSpeed : walkSpeed;
            controller.Move(move * currentSpeed * Time.deltaTime);
            
            // Apply gravity
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
            
            // Play footstep sounds
            if (moveInput.magnitude > 0.1f && isGrounded && Time.time - lastFootstepTime > footstepInterval)
            {
                PlayFootstepSound();
                lastFootstepTime = Time.time;
            }
        }
        
        void HandleMouseLook()
        {
            Vector2 lookInput = lookAction.ReadValue<Vector2>();
            
            // Rotate player body
            transform.Rotate(Vector3.up * lookInput.x * mouseSensitivity);
            
            // Rotate camera
            xRotation -= lookInput.y * mouseSensitivity;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
            playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
        
        void HandleJump()
        {
            if (jumpAction.WasPressedThisFrame() && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                
                // Log jump event for data collection
                dataCollector?.LogEvent("jump", new Dictionary<string, object>
                {
                    {"position", transform.position},
                    {"timestamp", Time.time}
                });
            }
        }
        
        void HandleRunning()
        {
            isRunning = runAction.IsPressed();
        }
        
        void HandleInteraction()
        {
            if (interactAction.WasPressedThisFrame())
            {
                // Raycast to find interactable objects
                Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
                RaycastHit hit;
                
                if (Physics.Raycast(ray, out hit, 3f))
                {
                    var interactable = hit.collider.GetComponent<IInteractable>();
                    if (interactable != null)
                    {
                        interactable.Interact();
                        
                        // Log interaction for data collection
                        dataCollector?.LogEvent("interact", new Dictionary<string, object>
                        {
                            {"object_name", hit.collider.name},
                            {"position", transform.position},
                            {"target_position", hit.point},
                            {"timestamp", Time.time}
                        });
                    }
                }
            }
        }
        
        void UpdateDataCollection()
        {
            // Track movement path
            if (Time.time - lastPathUpdate > pathUpdateInterval)
            {
                movementPath.Add(transform.position);
                if (movementPath.Count > 1000) // Limit path size
                {
                    movementPath.RemoveAt(0);
                }
                lastPathUpdate = Time.time;
            }
            
            // Log movement data
            if (Vector3.Distance(transform.position, lastPosition) > 0.1f)
            {
                dataCollector?.LogMovement(transform.position, currentSpeed, isRunning);
                lastPosition = transform.position;
            }
        }
        
        void PlayFootstepSound()
        {
            if (footstepAudio != null && footstepSounds.Length > 0)
            {
                AudioClip clip = footstepSounds[Random.Range(0, footstepSounds.Length)];
                footstepAudio.PlayOneShot(clip);
            }
        }
        
        // Public methods for data collection
        public Vector3 GetPosition() => transform.position;
        public float GetSpeed() => currentSpeed;
        public bool IsRunning() => isRunning;
        public bool IsGrounded() => isGrounded;
        public List<Vector3> GetMovementPath() => new List<Vector3>(movementPath);
        
        void OnDestroy()
        {
            // Cleanup input actions
            moveAction?.Disable();
            lookAction?.Disable();
            jumpAction?.Disable();
            runAction?.Disable();
            interactAction?.Disable();
        }
    }
    
    // Interface for interactable objects
    public interface IInteractable
    {
        void Interact();
    }
}
