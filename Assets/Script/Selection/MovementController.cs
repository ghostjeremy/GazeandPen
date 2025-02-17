using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementController : MonoBehaviour
{
    // Singleton pattern
    public static MovementController Instance { get; private set; }

    // Indicates whether movement is currently active
    private bool isMoving = false;
    // New: Public property to check if movement is ongoing
    public bool IsMoving { get { return isMoving; } }

    // Records the initial offset of each selected point relative to the pen tip
    private Dictionary<ISelectable, Vector3> selectableOffsets = new Dictionary<ISelectable, Vector3>();

    // Records the initial normal vector and point for the movement plane (used for planar movement)
    private Vector3 movementPlaneNormal;
    private Vector3 movementPlanePoint;

    // Whether movement is constrained to a plane
    private bool constrainToPlane = false;

    // Whether movement is constrained vertically
    private bool isMovingWithConfirm = false;  // New: Marks if movement was triggered via a confirm long press

    // Records the initial position for vertical movement
    private Vector3 verticalStartPosition;
    private float verticalStartHeight;

    // New: Indicates whether vertical movement mode is active
    private bool verticalMovementMode = false;
    // Records the reference point for vertical movement (the projection of the pen tip onto the movement plane)
    private Vector3 verticalReferencePoint;
    // Records the base height for vertical movement of each selected point
    private Dictionary<ISelectable, float> verticalBaseY = new Dictionary<ISelectable, float>();

    // New: Adjustable vertical mapping scale parameter
    [SerializeField] private float verticalMappingScale = 5.0f;

    // New: Records the last movement mode (true: vertical mode, false: planar mode)
    private bool lastUsedVerticalMode = false;

    // Public property to determine externally if plane constraint is active
    public bool IsPlaneConstraintActive { get { return constrainToPlane; } }
    // New: Public property to indicate if the last used mode was vertical
    public bool LastUsedVerticalMode { get { return lastUsedVerticalMode; } }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Start moving the selected points
    public void StartMoving()
    {
        isMoving = true;
        isMovingWithConfirm = true;  // Set flag indicating movement triggered via confirm long press
        // Get the initial pen tip position
        VrStylusHandler vrStylus = UnityEngine.Object.FindObjectOfType<VrStylusHandler>();
        if (vrStylus != null)
        {
            Vector3 penTipPos = vrStylus.CurrentState.inkingPose.position;
            
            // Calculate and store the offset of each point relative to the pen tip
            selectableOffsets.Clear();
            foreach (ISelectable selectable in SelectionManager.Instance.selectedPoints)
            {
                if (selectable is MonoBehaviour mb)
                {
                    // 计算并存储每个点相对于笔尖的偏移
                    Vector3 offset = mb.transform.position - penTipPos;
                    selectableOffsets[selectable] = offset;
                }
            }
        }
    }

    // Stop moving
    public void StopMoving()
    {
        isMoving = false;
        isMovingWithConfirm = false;
        constrainToPlane = false; // Reset plane constraint
        verticalMovementMode = false; // Disable vertical mode, but preserve lastUsedVerticalMode
        verticalBaseY.Clear();
        selectableOffsets.Clear();
    }

    // Set whether movement is constrained to a plane (called by InkPenInputStrategy when a sustained tip press is detected)
    public void SetPlaneConstraint(bool enable)
    {
        constrainToPlane = enable;
        if (enable && isMoving)
        {
            // Define the movement plane using the position of a selected point
            if (SelectionManager.Instance.selectedPoints.Count > 0)
            {
                var firstPoint = SelectionManager.Instance.selectedPoints[0] as MonoBehaviour;
                if (firstPoint != null)
                {
                    movementPlanePoint = firstPoint.transform.position;
                    movementPlaneNormal = Vector3.up; // Assume an upward normal vector
                    
                    // Record the initial state for vertical movement
                    verticalStartPosition = firstPoint.transform.position;
                    verticalStartHeight = verticalStartPosition.y;
                }
            }
        }
    }

    // Enable vertical movement mode
    public void EnableVerticalMode()
    {
        verticalMovementMode = true;
        // Get the projection of the current pen tip position as a reference
        VrStylusHandler vrStylus = UnityEngine.Object.FindObjectOfType<VrStylusHandler>();
        if (vrStylus != null)
        {
            Vector3 penTipPos = vrStylus.CurrentState.inkingPose.position;
            // Calculate the projection to ensure it lies on the movement plane
            Vector3 toPoint = penTipPos - movementPlanePoint;
            float distance = Vector3.Dot(toPoint, movementPlaneNormal);
            verticalReferencePoint = penTipPos - distance * movementPlaneNormal;
        }
        // Record the current Y coordinate of each selected point as the base height
        verticalBaseY.Clear();
        foreach (ISelectable selectable in SelectionManager.Instance.selectedPoints)
        {
            if (selectable is MonoBehaviour mb)
            {
                verticalBaseY[selectable] = mb.transform.position.y;
            }
        }
    }

    // Toggle vertical movement mode
    public void ToggleVerticalMode()
    {
        if (constrainToPlane) // Only works in planar mode
        {
            if (!verticalMovementMode)
            {
                EnableVerticalMode();
                lastUsedVerticalMode = true;
            }
            else
            {
                // Disable vertical mode and revert to planar movement
                verticalMovementMode = false;
                lastUsedVerticalMode = false;
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (isMoving && SelectionManager.Instance != null && SelectionManager.Instance.selectedPoints.Count > 0)
        {
            // Get the latest pen tip position each frame
            VrStylusHandler vrStylus = UnityEngine.Object.FindObjectOfType<VrStylusHandler>();
            if (vrStylus != null)
            {
                Vector3 penTipPos = vrStylus.CurrentState.inkingPose.position;
                Vector3 targetPos = penTipPos;

                // If in vertical movement mode, update the Y coordinate (map in-plane displacement to vertical movement)
                if (verticalMovementMode)
                {
                    // Get the projection of the current pen tip onto the movement plane
                    Vector3 toPoint = penTipPos - movementPlanePoint;
                    float distance = Vector3.Dot(toPoint, movementPlaneNormal);
                    Vector3 currentProjected = penTipPos - distance * movementPlaneNormal;
                    // Calculate the horizontal displacement between the reference point and the current projected point in the specified direction (using Vector3.forward) as the basis for vertical movement
                    float deltaY = Vector3.Dot(currentProjected - verticalReferencePoint, Vector3.forward);
                    float scale = verticalMappingScale; // Use the adjustable vertical mapping scale parameter
                    deltaY *= scale;
                    foreach (ISelectable selectable in SelectionManager.Instance.selectedPoints)
                    {
                        if (selectable is MonoBehaviour mb)
                        {
                            if (verticalBaseY.TryGetValue(selectable, out float baseY))
                            {
                                Vector3 pos = mb.transform.position;
                                mb.transform.position = new Vector3(pos.x, baseY + deltaY, pos.z);
                            }
                        }
                    }
                    return;
                }

                // If not in vertical mode but plane constraint is enabled, perform planar (horizontal) movement
                if (constrainToPlane)
                {
                    // Calculate the projection of the pen tip position onto the plane
                    Vector3 toPoint = penTipPos - movementPlanePoint;
                    float distance = Vector3.Dot(toPoint, movementPlaneNormal);
                    targetPos = penTipPos - distance * movementPlaneNormal;
                    
                    foreach (ISelectable selectable in SelectionManager.Instance.selectedPoints)
                    {
                        if (selectable is MonoBehaviour mb)
                        {
                            if (selectableOffsets.TryGetValue(selectable, out Vector3 offset))
                            {
                                Vector3 constrainedPos = new Vector3(
                                    targetPos.x + offset.x,
                                    movementPlanePoint.y,
                                    targetPos.z + offset.z
                                );
                                mb.transform.position = constrainedPos;
                            }
                        }
                    }
                    return;
                }

                // Otherwise, perform free movement
                foreach (ISelectable selectable in SelectionManager.Instance.selectedPoints)
                {
                    if (selectable is MonoBehaviour mb)
                    {
                        if (selectableOffsets.TryGetValue(selectable, out Vector3 offset))
                        {
                            mb.transform.position = penTipPos + offset;
                        }
                    }
                }
            }
        }
    }
}
