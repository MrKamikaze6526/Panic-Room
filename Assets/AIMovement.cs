using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Speeds")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private float crouchSpeed = 2.5f;
   
    [Header("Jump and Gravity")]
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -19.62f;
    [SerializeField] private float groundDistance = 0.4f;
    [SerializeField] private LayerMask groundMask;
    //s
    [Header("Crouch Settings")]
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchTransitionSpeed = 10f;

    [Header("Footstep Audio")]
    [SerializeField] private AudioSource footstepAudio;
    [SerializeField] private float walkStepInterval = 0.5f;
    [SerializeField] private float sprintStepInterval = 0.3f;
    [SerializeField] private float crouchStepInterval = 0.7f;
    [SerializeField] private AudioClip[] footstepSounds;

    // Components
    private CharacterController controller;
    private Transform groundCheck;
    private Camera playerCamera;

    // Movement states
    private bool isSprinting;
    private bool isWalking;
    private bool isCrouching;
    private bool isGrounded;
    private Vector3 velocity;
    private float targetHeight;
    private float footstepTimer;

    // Public accessors for monster detection
    public bool IsSprinting => isSprinting;
    public bool IsWalking => isWalking;
    public bool IsCrouching => isCrouching;

    private void Start()
    {
        // Get components
        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
       
        // Create ground check object
        groundCheck = new GameObject("GroundCheck").transform;
        groundCheck.parent = transform;
        groundCheck.localPosition = Vector3.down * (standingHeight / 2f);

        // Set initial height
        targetHeight = standingHeight;
        controller.height = standingHeight;
       
        // Lock and hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleMovementInput();
        HandleJump();
        HandleCrouch();
        ApplyGravity();
        UpdateMovementStates();
        HandleFootsteps();
    }

    private void HandleMovementInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // Create movement vector relative to camera direction
        Vector3 forward = transform.forward * vertical;
        Vector3 right = transform.right * horizontal;
        Vector3 moveDirection = (forward + right).normalized;

        // Calculate current speed based on movement state
        float currentSpeed = GetCurrentSpeed();

        // Apply movement
        if (moveDirection.magnitude >= 0.1f)
        {
            controller.Move(moveDirection * currentSpeed * Time.deltaTime);
        }
    }

    private float GetCurrentSpeed()
    {
        if (isSprinting && !isCrouching && velocity.y >= 0) // Can't sprint while falling or crouching
        {
            return sprintSpeed;
        }
        else if (isCrouching)
        {
            return crouchSpeed;
        }
        return walkSpeed;
    }

    private void HandleJump()
    {
        // Check if grounded
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small negative value to maintain ground contact
        }

        // Jump logic
        if (Input.GetButtonDown("Jump") && isGrounded && !isCrouching)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    private void HandleCrouch()
    {
        // Toggle crouch state
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            isCrouching = !isCrouching;
            targetHeight = isCrouching ? crouchHeight : standingHeight;
        }

        // Smooth height transition
        if (controller.height != targetHeight)
        {
            UpdateControllerHeight();
        }
    }

    private void UpdateControllerHeight()
    {
        // Calculate new height
        float newHeight = Mathf.Lerp(controller.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);
        float heightDifference = newHeight - controller.height;

        // Adjust position to account for height change
        controller.height = newHeight;
        transform.position += new Vector3(0, heightDifference / 2, 0);

        // Update ground check position
        groundCheck.localPosition = Vector3.down * (controller.height / 2f);
    }

    private void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void UpdateMovementStates()
    {
        // Get input magnitude for walking detection
        float inputMagnitude = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).magnitude;

        // Update movement states
        isSprinting = Input.GetKey(KeyCode.LeftShift) && inputMagnitude > 0 && !isCrouching && isGrounded;
        isWalking = inputMagnitude > 0 && !isSprinting && !isCrouching;
    }

    private void HandleFootsteps()
    {
        if (!isGrounded || footstepSounds.Length == 0 || footstepAudio == null) return;

        // Get current step interval based on movement state
        float currentStepInterval = isSprinting ? sprintStepInterval :
                                  isCrouching ? crouchStepInterval :
                                  walkStepInterval;

        // Check if we should play a footstep
        if ((isWalking || isSprinting || (isCrouching && velocity.magnitude > 0.1f)))
        {
            footstepTimer += Time.deltaTime;

            if (footstepTimer >= currentStepInterval)
            {
                // Play random footstep sound
                AudioClip randomStep = footstepSounds[Random.Range(0, footstepSounds.Length)];
                footstepAudio.PlayOneShot(randomStep);
                footstepTimer = 0f;
            }
        }
        else
        {
            footstepTimer = 0f;
        }
    }

    // Optional: Add head bobbing
}