using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 6f;
    public float sprintSpeed = 10f;
    public float gravity = -19.62f; 
    public float jumpHeight = 1.5f;
    
    [Header("Custom Ground Check")]
    [Tooltip("The highest Y position the player can be at and still be allowed to jump.")]
    public float groundYThreshold = 1.2f; 

    [Header("Look Settings")]
    public Transform playerCamera;
    public float mouseSensitivity = 0.15f; 
    public float maxLookAngle = 80f;

    private CharacterController controller;
    private Vector3 velocity;
    private float verticalRotation = 0f;

    private InputSystem_Actions inputActions;
    
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        inputActions = new InputSystem_Actions();

        moveAction = inputActions.Player.Move;
        lookAction = inputActions.Player.Look;
        jumpAction = inputActions.Player.Jump;
        sprintAction = inputActions.Player.Sprint;
    }

    void OnEnable()
    {
        inputActions.Player.Enable();
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnDisable()
    {
        inputActions.Player.Disable();
    }

    void Update()
    {
        HandleMouseLook();
        HandleMovement();
    }

    void HandleMouseLook()
    {
        Vector2 lookInput = lookAction.ReadValue<Vector2>();

        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -maxLookAngle, maxLookAngle);

        if (playerCamera != null)
        {
            playerCamera.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }
    }

    void HandleMovement()
    {
        // 1. Reset downward velocity if resting on the ground
        if (transform.position.y <= groundYThreshold && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // 2. Handle WASD movement
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;

        float currentSpeed = sprintAction.IsPressed() ? sprintSpeed : walkSpeed;
        controller.Move(move * currentSpeed * Time.deltaTime);

        // 3. Jump logic using the Y-axis position check
        if (jumpAction.WasPerformedThisFrame() && transform.position.y <= groundYThreshold)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // 4. Apply gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}