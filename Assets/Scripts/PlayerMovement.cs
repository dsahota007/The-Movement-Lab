using UnityEngine;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 35f;
    public float sprintSpeed = 45f;
    public KeyCode sprintKey = KeyCode.LeftShift; //do keyCode so we dont have to write all this BS 
    public float jumpForce = 33f;

    [Header("Mouse Settings")]
    public float mouseSensitivity = 2.5f;

    [Header("Head Bobbing Settings")]
    public float walkBobSpeed = 14f;
    public float walkBobAmount = 0.05f;
    public float sprintBobSpeed = 18f;
    public float sprintBobAmount = 0.1f;

    [Header("FOV Settings")]
    public float normalFOV = 120f;
    public float sprintFOV = 145f;
    public float fovTransitionSpeed = 6f;

    [Header("Jump Settings")]
    public int maxJumps = 3; // Maximum number of jumps, including initial jump
    private int currentJumps; // Number of jumps remaining
    public float rechargeRate = 1.5f; // How long to recharge each jump
    private float rechargeTimer = 0f; // Timer for jump recharge

    [Header("UI Settings")]
    public Slider jumpChargeBar; // Slider to show jump charges (add a Slider UI in Unity)
    public float uiUpdateSpeed = 5f; // Speed for the gradual UI update

    private float targetJumpCharge; // Target value for the UI bar

    [Header("Dash Settings")]
    public float dashSpeed = 45f;
    public float dashDuration = 0.5f;
    public float dashCooldown = 1f;
    public float dashFOV = 145f;
    public float dashTiltAmount = 12f;

    // Animation curve for dash speed over time
    public AnimationCurve dashSpeedCurve;

    [Header("Camera Tilt Settings")]
    public float tiltTransitionSpeed = 3f;

    private bool isDashing = false;
    private Vector3 dashDirection;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private float targetTilt = 0f;
    private float currentTilt = 0f;
    private float dashElapsedTime = 0f; // Time since dash started

    [Header("Gravity Dive")]
    public bool gravityDive = false;
    public KeyCode DiveBtn = KeyCode.Mouse0;


    private Rigidbody rb;
    public float VerticalRotation { get; private set; } = 0f;

    public bool IsGrounded { get; private set; }
    public bool CanMove { get; set; } = true;
    public Rigidbody Rb => rb; // Provide access to the Rigidbody

    private Camera playerCamera;
    private Vector3 cameraOriginalLocalPos;
    private float headBobTimer = 0f;

    // Reference to PlayerWallRun script
    private PlayerWallRun wallRunScript;
    private PlayerGrappling playerGrapplingScript;

    private float targetFOV;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
        playerCamera = Camera.main;
        cameraOriginalLocalPos = playerCamera.transform.localPosition;
        playerCamera.fieldOfView = normalFOV;
        targetFOV = normalFOV;

        Physics.gravity = new Vector3(0, -25.62f, 0); // Adjusting gravity 

        wallRunScript = GetComponent<PlayerWallRun>();
        if (wallRunScript == null)
        {
            Debug.LogError("PlayerMovement requires PlayerWallRun component on the same GameObject.");
        }

        currentJumps = maxJumps; // Initialize with max jumps
        jumpChargeBar.maxValue = maxJumps;
        targetJumpCharge = maxJumps; // Initialize the target charge value
        jumpChargeBar.value = maxJumps; // Set the initial value of the slider

        // Initialize the dash speed curve if not set
        if (dashSpeedCurve == null || dashSpeedCurve.length == 0)
        {
            dashSpeedCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.5f, 1f),
                new Keyframe(1f, 0f)
            );
        }
    }

    void Update()
    {
        HandleLook();
        HandleMovement();
        HandleDash();
        HandleJump();
        CheckGroundStatus();
        HandleHeadBobbing();
        HandleFOV();
        HandleCameraTilt();
        HandleJumpRecharge();
        UpdateJumpChargeUI();
        TurnGravityDiveOn();
    }

    private void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        VerticalRotation -= mouseY;
        VerticalRotation = Mathf.Clamp(VerticalRotation, -90f, 90f);  //Mathf.Clamp(value, min, max)

        // Apply camera rotation for pitch (up/down)
        playerCamera.transform.localRotation = Quaternion.Euler(VerticalRotation, 0f, 0f);   //(x, y, z) creates rotation 
        transform.Rotate(Vector3.up * mouseX);
    }

    private void HandleMovement()
    {
        if (!CanMove || isDashing) return; // Disable movement if another script sets CanMove to false or if dashing

        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        Vector3 movement = transform.right * moveHorizontal + transform.forward * moveVertical;

        // Determine current speed based on whether the player is sprinting
        float currentSpeed = walkSpeed;

        if (Input.GetKey(sprintKey) && IsGrounded && movement.magnitude > 0)
        {//movMagnitude is to make sure u dont sprint when idling  
            currentSpeed = sprintSpeed;
        }

        Vector3 velocity = new Vector3(movement.x * currentSpeed, rb.velocity.y, movement.z * currentSpeed);

        rb.velocity = velocity;
    }

    private void HandleDash()
    {
        // Handle dash cooldown timer
        if (dashCooldownTimer > 0)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        if (!isDashing && dashCooldownTimer <= 0)
        {
            // Forward dash (scroll wheel up)
            if (Input.GetAxis("Mouse ScrollWheel") > 0)
            {
                StartDash(transform.forward);
            }
            // Backward dash (scroll wheel down)
            else if (Input.GetAxis("Mouse ScrollWheel") < 0)
            {
                StartDash(-transform.forward);
            }
            // Left dash (Q key)
            else if (Input.GetKeyDown(KeyCode.Q))
            {
                StartDash(-transform.right);
            }
            // Right dash (E key)
            else if (Input.GetKeyDown(KeyCode.E))
            {
                StartDash(transform.right);
            }
        }

        // Handle dash duration
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            dashElapsedTime += Time.deltaTime;

            // Calculate normalized time (0 to 1)
            float normalizedTime = dashElapsedTime / dashDuration;

            // Get the speed multiplier from the animation curve
            float speedMultiplier = dashSpeedCurve.Evaluate(normalizedTime);
            float currentDashSpeed = dashSpeed * speedMultiplier;

            // Apply the dash movement
            Vector3 dashVelocity = dashDirection * currentDashSpeed;
            dashVelocity.y = rb.velocity.y; // Preserve current vertical velocity
            rb.velocity = dashVelocity;

            if (dashTimer <= 0)
            {
                isDashing = false;
                dashDirection = Vector3.zero;

                // Reset FOV and tilt
                targetFOV = normalFOV;
                targetTilt = 0f;

                rb.useGravity = true;
            }
        }
    }

    private void StartDash(Vector3 direction)
    {
        isDashing = true;
        dashDirection = direction.normalized;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        dashElapsedTime = 0f;

        rb.velocity = Vector3.zero;
        rb.useGravity = false;

        // If dashing forward or backward, initiate FOV effect
        if (direction == transform.forward || direction == -transform.forward)
        {
            targetFOV = dashFOV;
        }

        // If dashing left or right, initiate screen tilt
        if (direction == transform.right)
        {
            targetTilt = -dashTiltAmount;
        }
        else if (direction == -transform.right)
        {
            targetTilt = dashTiltAmount;
        }
    }

    private void HandleFOV()
    {
        // Set default desired FOV
        float desiredFOV = normalFOV;

        if (Input.GetKey(sprintKey) && IsGrounded && rb.velocity.magnitude > 0.1f)
        {
            desiredFOV = sprintFOV;
        }

        if (wallRunScript != null && wallRunScript.IsWallRunning)
        {
            desiredFOV = sprintFOV;
        }

        if (isDashing && (dashDirection == transform.forward || dashDirection == -transform.forward))
        {
            desiredFOV = dashFOV;
        }

        targetFOV = desiredFOV;

        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * fovTransitionSpeed);    //(starting value, target value, interpolation from 0 to 1)
    }

    private void HandleCameraTilt()
    {
        // Handle camera tilt for dashing
        if (!isDashing)
        {
            targetTilt = 0f;
        }

        // Update current tilt
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * tiltTransitionSpeed);

        // Add wall run tilt if applicable
        float totalTilt = currentTilt;
        if (wallRunScript != null)
        {
            totalTilt += wallRunScript.WallRunTiltAngle;
        }

        // Apply tilt and pitch to camera
        Quaternion targetRotation = Quaternion.Euler(VerticalRotation, 0f, totalTilt);
        playerCamera.transform.localRotation = targetRotation;
    }

    private void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && currentJumps > 0)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);  //forceMOde just helps it be better somehow idk 
            currentJumps--; // Use a jump
            targetJumpCharge = currentJumps; // Set the target value for the UI to be decreased
        }
    }

    private void CheckGroundStatus()
    {
        IsGrounded = Physics.Raycast(transform.position, Vector3.down, 1.1f);  //(origin, the direction, distance)
    }

    private void HandleHeadBobbing()
    {
        if (!IsGrounded || !CanMove || isDashing)
        {
            ResetHeadBobbing();
            return;
        }

        float speed = (Input.GetKey(sprintKey) && rb.velocity.magnitude > 0.1f) ? sprintBobSpeed : walkBobSpeed;   //this will trigger sprint bobbing
        float amount = (Input.GetKey(sprintKey) && rb.velocity.magnitude > 0.1f) ? sprintBobAmount : walkBobAmount;

        if (rb.velocity.magnitude > 0.1f)
        {      //ensure bobbing when player moves
            headBobTimer += Time.deltaTime * speed;
            float bobbingOffset = Mathf.Sin(headBobTimer) * amount;      // (float f) --> generates a smooth, periodic value between -1 and 1.
            playerCamera.transform.localPosition = new Vector3(cameraOriginalLocalPos.x, cameraOriginalLocalPos.y + bobbingOffset, cameraOriginalLocalPos.z);   //(x,y,z) camera pos
        }
        else
        {    //is we aint sprinting just reset the bitch 
            ResetHeadBobbing();
        }
    }

    private void ResetHeadBobbing()
    {
        headBobTimer = 0f;
        playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, cameraOriginalLocalPos, Time.deltaTime * walkBobSpeed);    //walking/idle bobbing
    }

    private void HandleJumpRecharge()
    {
        // Recharge jumps whenever the player is not jumping
        if (currentJumps < maxJumps)
        {
            rechargeTimer += Time.deltaTime;

            if (rechargeTimer >= rechargeRate)
            {
                currentJumps++;
                targetJumpCharge = currentJumps; // Set the target value for the UI to be increased
                rechargeTimer = 0f;
            }
        }
    }

    private void UpdateJumpChargeUI()
    {
        // Smoothly transition the UI bar value to the target value
        jumpChargeBar.value = Mathf.Lerp(jumpChargeBar.value, targetJumpCharge, Time.deltaTime * uiUpdateSpeed);
    }

    private void TurnGravityDiveOn() {
        if (Input.GetKeyDown(DiveBtn)) {
            Physics.gravity = new Vector3(0, -200.62f, 0);
        }
        if (IsGrounded || (playerGrapplingScript != null && playerGrapplingScript.IsGrappling()) || Input.GetButtonDown("Jump")) {
            Physics.gravity = new Vector3(0, -25.62f, 0);
        }
    }
}


