using UnityEngine;

public class PlayerWallRun : MonoBehaviour
{
    [Header("Wall Run Settings")]
    public float wallRunSpeed = 33f;
    public float wallRunDuration = 1f;
    public float wallCheckDistance = 0.6f;      // Distance for raycast to check for walls
    public LayerMask wallLayer;

    [Header("Wall Jump Settings")]
    public float wallJumpSideForce = 15f;
    public float wallJumpUpForce = 15f;

    [Header("Wall Run Visual Settings")]
    public float wallRunBobSpeed = 14f;
    public float wallRunBobAmount = 0.05f;
    public float cameraTiltAngle = 15f;
    public float tiltTransitionSpeed = 10f;

    private PlayerMovement playerMovement;
    private Rigidbody rb;
    private bool isWallRunning = false;
    private bool isTouchingWall = false;
    private bool wallOnRightSide = false;
    private Vector3 wallNormal;             // Stores the normal of the wall you're touching so left or right 
    private float wallRunTimer = 0f;

    // Expose wall running status
    public bool IsWallRunning => isWallRunning;  //Provides read-only access to the isWallRunning variable from outside the class idk honestly

    // Head bobbing and camera tilt variables
    private Camera playerCamera;
    private Vector3 cameraOriginalLocalPos;
    private float wallRunHeadBobTimer = 0f;

    // Added WallRunTiltAngle property
    public float WallRunTiltAngle = 0f;    // { get; private set; }
    private float currentCameraTilt = 0f;

    void Start() {
        rb = GetComponent<Rigidbody>();
        playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement == null) {
            Debug.LogError("PlayerWallRun requires PlayerMovement component on the same GameObject.");
        }

        playerCamera = Camera.main;
        cameraOriginalLocalPos = playerCamera.transform.localPosition;
    }

    void Update() {
        HandleWallRun();
        HandleWallRunEffects();
    }

    private void HandleWallRun() {
        if (playerMovement.IsGrounded) {
            StopWallRun();
            return;                 //no wall run than 
        }

        CheckForWall();

        if (isTouchingWall && Input.GetAxis("Vertical") > 0) {
            if (!isWallRunning) {
                StartWallRun();   //If the player is not already wall-running (!isWallRunning), then call StartWallRun() to begin wall-running
            }

            ContinueWallRun();

            if (Input.GetButtonDown("Jump")) {
                WallJump();
            }

            // Update wall run timer
            wallRunTimer += Time.deltaTime;
            if (wallRunTimer >= wallRunDuration) {
                StopWallRun();
            }
        }
        else {
            StopWallRun();
        }
    }

    private void HandleWallRunEffects() {
        if (isWallRunning) {
            // Head Bobbing during wall running
            wallRunHeadBobTimer += Time.deltaTime * wallRunBobSpeed;
            float bobbingOffset = Mathf.Sin(wallRunHeadBobTimer) * wallRunBobAmount;

            Vector3 bobbingPosition = new Vector3(cameraOriginalLocalPos.x, cameraOriginalLocalPos.y + bobbingOffset, cameraOriginalLocalPos.z);
            playerCamera.transform.localPosition = bobbingPosition;

            // Screen tilt
            float targetTilt = wallOnRightSide ? cameraTiltAngle : -cameraTiltAngle;
            currentCameraTilt = Mathf.Lerp(currentCameraTilt, targetTilt, Time.deltaTime * tiltTransitionSpeed);

            // Set WallRunTiltAngle for external use
            WallRunTiltAngle = currentCameraTilt;
        }
        else {
            // Reset head bobbing
            wallRunHeadBobTimer = 0f;
            playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, cameraOriginalLocalPos, Time.deltaTime * wallRunBobSpeed);

            // Reset camera tilt
            currentCameraTilt = Mathf.Lerp(currentCameraTilt, 0f, Time.deltaTime * tiltTransitionSpeed);
            WallRunTiltAngle = currentCameraTilt;
        }

        // Removed direct camera rotation modification
        // The camera tilt is now handled in PlayerMovement script to combine with dash tilt
    }

    private void CheckForWall() {
        RaycastHit hitRight;
        RaycastHit hitLeft;
        bool wallOnRight = Physics.Raycast(transform.position, transform.right, out hitRight, wallCheckDistance, wallLayer); //(player pos, which side, empty box to put info smthn with "out", how far laser can go, detecting which walls)
        bool wallOnLeft = Physics.Raycast(transform.position, -transform.right, out hitLeft, wallCheckDistance, wallLayer);

        if (wallOnRight) {
            isTouchingWall = true;
            wallNormal = hitRight.normal;
            wallOnRightSide = true;
        }

        else if (wallOnLeft)  {
            isTouchingWall = true;
            wallNormal = hitLeft.normal;
            wallOnRightSide = false;
        }

        else {
            isTouchingWall = false;
        }
    }

    private void StartWallRun() {
        isWallRunning = true;
        playerMovement.CanMove = false;    // turn off normal normal movement
        rb.useGravity = false;            // turn off gravity
        wallRunTimer = 0f;
    }

    private void ContinueWallRun() {
        Vector3 wallForward = Vector3.Cross(wallNormal, Vector3.up);     //calc a direction that is perpendicular to two given vectors.  (direction that points directly out from the wall, direction  points up)
        wallForward.Normalize();                                         //This ensures the resulting vector (wallForward) has a length of 1. This is helpful for maintaining consistent movement speed

        if (wallOnRightSide) {               // Flip direction if wall is on the right side
            wallForward = -wallForward;
        }

        rb.velocity = wallForward * wallRunSpeed;

        // Optional: Rotate player to face along the wall
        Quaternion wallRunRotation = Quaternion.LookRotation(wallForward);  //creates a rotation that makes the player face the direction of wall running
        transform.rotation = Quaternion.Lerp(transform.rotation, wallRunRotation, Time.deltaTime * 10f);   //smoothly transitions the player's current rotation
    }

    private void StopWallRun() {
        if (isWallRunning) {
            isWallRunning = false;
            playerMovement.CanMove = true;  // Re-enable normal movement that isnt wall run
            rb.useGravity = true; // Re-enable gravity
            wallRunTimer = 0f;

            // Reset WallRunTiltAngle
            WallRunTiltAngle = 0f;
        }
    }

    private void WallJump()  {
        isWallRunning = false;
        playerMovement.CanMove = true;      // turn on normal movement
        rb.useGravity = true;               // turn gravity back on

        // calc jump direction
        Vector3 jumpDirection = wallNormal * wallJumpSideForce + Vector3.up * wallJumpUpForce;   //(wallNormal * wallJumpSideForce) --> gets away from wall,  Vector3.up points upwarsds so were jumping away 
        rb.velocity = jumpDirection;
    }
}
