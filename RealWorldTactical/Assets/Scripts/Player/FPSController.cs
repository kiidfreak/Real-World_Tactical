using UnityEngine;
using UnityEngine.InputSystem;

public class FPSController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 3f;
    public float runSpeed = 5f;
    public float jumpHeight = 3f;
    public float gravity = -9.81f;
    public float mouseSensitivity = 0.5f;
    
    [Header("Camera Settings")]
    public Camera playerCamera;
    
    [Header("Input Actions")]
    public InputActionAsset inputActions;
    
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
    
    void Start()
    {
        // Get components
        controller = GetComponent<CharacterController>();
        
        // Setup camera
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();
            
        // Lock cursor to center of screen
        Cursor.lockState = CursorLockMode.Locked;
        
        // Setup input actions
        SetupInputActions();
    }
    
    void SetupInputActions()
    {
        if (inputActions != null)
        {
            // Use the Input Actions asset
            moveAction = inputActions.FindAction("Move");
            lookAction = inputActions.FindAction("Look");
            jumpAction = inputActions.FindAction("Jump");
            runAction = inputActions.FindAction("Run");
            
            // Enable input actions
            inputActions.Enable();
        }
        else
        {
            // Fallback to manual setup
            var inputMap = new InputActionMap("Player");
            
            moveAction = inputMap.AddAction("Move", InputActionType.Value, "<Keyboard>/wasd");
            lookAction = inputMap.AddAction("Look", InputActionType.Value, "<Mouse>/delta");
            jumpAction = inputMap.AddAction("Jump", InputActionType.Button, "<Keyboard>/space");
            runAction = inputMap.AddAction("Run", InputActionType.Button, "<Keyboard>/leftShift");
            
            // Enable input actions
            inputMap.Enable();
        }
    }
    
    void Update()
    {
        HandleMovement();
        HandleMouseLook();
        HandleJump();
        HandleRunning();
        HandleCursorToggle();
    }
    
    void HandleCursorToggle()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }
    
    void HandleMovement()
    {
        // Check if grounded
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        
        // Get input using Unity's built-in Input class
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        
        // Calculate movement direction
        Vector3 move = transform.right * x + transform.forward * z;
        
        // Apply speed
        currentSpeed = isRunning ? runSpeed : walkSpeed;
        controller.Move(move * currentSpeed * Time.deltaTime);
        
        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
    
    void HandleMouseLook()
    {
        // Only process mouse input when the cursor is locked
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
            
            // Rotate player body
            transform.Rotate(Vector3.up * mouseX);
            
            // Rotate camera
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
            playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
    }
    
    void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }
    
    void HandleRunning()
    {
        isRunning = Input.GetKey(KeyCode.LeftShift);
    }
    
    void OnDestroy()
    {
        // Cleanup input actions
        moveAction?.Disable();
        lookAction?.Disable();
        jumpAction?.Disable();
        runAction?.Disable();
    }
}