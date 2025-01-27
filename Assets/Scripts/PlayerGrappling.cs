

using UnityEngine;
using UnityEngine.UI;

public class PlayerGrappling : MonoBehaviour
{
    [Header("Grappling Settings")]
    public float maxGrappleDistance = 155f;         // Maximum distance the grapple can reach
    public KeyCode grappleKey = KeyCode.Mouse1;    // Key to activate the grapple (right-click)
    public float grappleSpeed = 150f;               // Speed at which the player moves towards the grapple point
    public float hookTravelSpeed = 70f;            // Speed at which the hook extends to the grapple point

    [Header("References")]
    public Transform gunTip;                       // The point where the grappling hook fires from
    public Camera playerCamera;                    // Reference to the player's camera
    public LineRenderer lineRenderer;              // Line renderer to visualize the grappling hook
    public Image grappleReticle;                   // UI Image for grapple indicator

    private PlayerMovement playerMovement;         // Reference to the PlayerMovement script
    private Rigidbody rb;                          // Reference to the player's Rigidbody

    private Vector3 grapplePoint;                  // The point in space where the grappling hook attaches
    private bool isGrappling = false;              // Indicates if the player is currently grappling
    private bool hookTraveling = false;            // Indicates if the hook is currently extending
    private float hookProgress = 0f;               // Progress of the hook extension (0 to 1)

    void Start() {
        rb = GetComponent<Rigidbody>();

        playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement == null)
        {
            Debug.LogError("PlayerGrappling requires PlayerMovement component on the same GameObject.");
        }

        // Ensure the LineRenderer is initialized
        lineRenderer.positionCount = 0;

        // Set default reticle color
        if (grappleReticle != null)
        {
            grappleReticle.color = Color.white;  // Default color for not in range
        }
    }

    void Update() {
        UpdateGrappleReticle(); // Check if player can grapple and update reticle

        // Start grappling when the grapple key is pressed down
        if (Input.GetKeyDown(grappleKey) && !isGrappling && !hookTraveling)
        {
            StartGrapple();
        }

        // Stop grappling when the grapple key is released
        if (Input.GetKeyUp(grappleKey))
        {
            StopGrapple();
        }

        // Update the grappling hook line
        DrawGrappleLine();
    }

    void FixedUpdate() {
        if (hookTraveling)
        {
            ExtendHook();
        }
        else if (isGrappling)
        {
            MoveTowardsGrapplePoint();
        }
    }

    void StartGrapple() {
        RaycastHit hit;
        // Perform a raycast from the camera to detect any surface
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, maxGrappleDistance))
        {
            // Set the grapple point
            grapplePoint = hit.point;

            // Initialize hook travel variables
            hookTraveling = true;
            hookProgress = 0f;

            // Initialize the line renderer
            lineRenderer.positionCount = 2;

            // Disable player movement during grappling
            playerMovement.CanMove = false;

            // Gravity will be re-enabled when the player starts moving
        }
    }

    void UpdateGrappleReticle() {
        RaycastHit hit;
        // Perform a raycast from the camera to detect any surface within range
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, maxGrappleDistance))
        {
            // If a valid surface is detected, change reticle color to indicate grapple is available
            if (grappleReticle != null)
            {
                grappleReticle.color = Color.green; // Indicate that the player can grapple
            }
        }
        else
        {
            // If no valid surface is in range, set the reticle color to default
            if (grappleReticle != null)
            {
                grappleReticle.color = Color.white; // Default color indicating no grapple available
            }
        }
    }

    void ExtendHook() {
        // Calculate the distance between the gun tip and the grapple point
        float distance = Vector3.Distance(gunTip.position, grapplePoint);

        // Calculate the step size based on hook travel speed
        float step = hookTravelSpeed * Time.fixedDeltaTime;

        // Update hook progress
        hookProgress += step / distance;

        // Clamp hook progress to 1
        hookProgress = Mathf.Clamp01(hookProgress);

        // Calculate the current position of the hook tip
        Vector3 currentHookPosition = Vector3.Lerp(gunTip.position, grapplePoint, hookProgress);

        // Update the line renderer positions
        lineRenderer.SetPosition(0, gunTip.position);
        lineRenderer.SetPosition(1, currentHookPosition);

        // Check if the hook has reached the grapple point
        if (hookProgress >= 1f)
        {
            hookTraveling = false;
            isGrappling = true;

            // Disable gravity during grappling
            rb.useGravity = false;
        }
    }

    void MoveTowardsGrapplePoint() {
        // Calculate the direction towards the grapple point
        Vector3 direction = (grapplePoint - transform.position).normalized;

        // Apply a force towards the grapple point to create a yank effect
        float distanceToPoint = Vector3.Distance(transform.position, grapplePoint);

        // Calculate the pulling force gradually, with the force decreasing as the player gets closer
        float pullStrength = Mathf.Lerp(grappleSpeed * 2f, 0f, 1f - Mathf.Clamp01(distanceToPoint / maxGrappleDistance));

        // Apply the pulling force
        rb.AddForce(direction * pullStrength, ForceMode.Acceleration);

        // Optional: Stop grappling when close enough to the grapple point
        if (distanceToPoint < 1f)
        {
            StopGrapple();
        }

        // Update the line renderer positions
        lineRenderer.SetPosition(0, gunTip.position);
        lineRenderer.SetPosition(1, grapplePoint);
    }

    void StopGrapple() {
        if (isGrappling || hookTraveling)
        {
            isGrappling = false;
            hookTraveling = false;

            // Re-enable gravity and player movement
            rb.useGravity = true;
            playerMovement.CanMove = true;

            // Allow momentum to carry over by not resetting the velocity
            // Reset the line renderer
            lineRenderer.positionCount = 0;
        }
    }

    void DrawGrappleLine() {
        if (!hookTraveling && !isGrappling) {
            lineRenderer.positionCount = 0;
            return;
        }
    }

    public bool IsGrappling() {
        return isGrappling;
    }
}
