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

    // New: Record the drag start position (global drag start point)
    private Vector3 dragStartPosition;
    // New: Line for visualizing drag (connects drag start point to current pen tip)
    private LineRenderer dragLineRenderer;

    private Vector3 tipOffset = new Vector3(0.00904f, -0.07088f, -0.07374f); 

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // New: Create line for visualizing drag
        GameObject dragLineObj = new GameObject("DragLine");
        dragLineObj.transform.SetParent(transform);
        dragLineObj.transform.localPosition = Vector3.zero;
        dragLineRenderer = dragLineObj.AddComponent<LineRenderer>();
        dragLineRenderer.positionCount = 0;
        dragLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        dragLineRenderer.startColor = new Color(1f, 1f, 1f, 0f);  // 透明的白色
        dragLineRenderer.endColor = new Color(1f, 1f, 1f, 1f);    // 纯白色
        dragLineRenderer.widthMultiplier = 0.005f;
    }

    // Helper function to determine pen tip position based on the current input mode.
    private Vector3 GetPenTipPosition()
    {
        RightHandInputManager inputManager = FindObjectOfType<RightHandInputManager>();
        if (inputManager != null && inputManager.UseQuest3AtStart)
        {
            VrStylusHandler vrStylus = FindObjectOfType<VrStylusHandler>();
            if (vrStylus != null)
                return vrStylus.CurrentState.inkingPose.position;
        }
        // Fallback to controller position.
    // Get the local position and rotation of the right-hand controller
    Vector3 controllerPos = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
    Quaternion controllerRot = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch);
    
    // Calculate pen tip position: apply tipOffset to the controller's local coordinate system
    return controllerPos + (controllerRot * tipOffset);
    }

    // Start moving the selected points
    public void StartMoving()
    {
        isMoving = true;
        isMovingWithConfirm = true;  // Set flag indicating movement triggered via confirm long press
        Vector3 penTipPos = GetPenTipPosition();
        // Record the current drag start point
        dragStartPosition = penTipPos;

        // Calculate and store each point's offset relative to the pen tip
        selectableOffsets.Clear();
        foreach (ISelectable selectable in SelectionManager.Instance.selectedPoints)
        {
            if (selectable is MonoBehaviour mb)
            {
                Vector3 offset = mb.transform.position - penTipPos;
                selectableOffsets[selectable] = offset;
            }
        }

        // Apply highlight effect when drag begins
        foreach (ISelectable selectable in SelectionManager.Instance.selectedPoints)
        {
            if (selectable is PointObject pointObj)
            {
                pointObj.OnDragEnter();
            }
            else if (selectable is ControlPoint controlPoint)
            {
                controlPoint.OnDragEnter();
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
        if (dragLineRenderer != null)
        {
            dragLineRenderer.positionCount = 0;
        }
        // New: Apply restore effect when drag ends
        foreach (ISelectable selectable in SelectionManager.Instance.selectedPoints)
        {
            if (selectable is PointObject pointObj)
            {
                pointObj.OnDragExit();
            }
            else if (selectable is ControlPoint controlPoint)
            {
                controlPoint.OnDragExit();
            }
        }
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
        Vector3 penTipPos = GetPenTipPosition();
        // Calculate the projection to ensure it lies on the movement plane
        Vector3 toPoint = penTipPos - movementPlanePoint;
        float distance = Vector3.Dot(toPoint, movementPlaneNormal);
        verticalReferencePoint = penTipPos - distance * movementPlaneNormal;
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
        Vector3 penTipPos = GetPenTipPosition();

        // Update drag line and distance display: only show line between drag start point and current pen tip when moving (isMoving is true)
        if (isMoving && dragLineRenderer != null)
        {
            dragLineRenderer.positionCount = 2;
            dragLineRenderer.SetPosition(0, dragStartPosition);
            dragLineRenderer.SetPosition(1, penTipPos);
        }
        else if (dragLineRenderer != null)
        {
            dragLineRenderer.positionCount = 0;
        }

        Vector3 targetPos = penTipPos;

        // If in vertical movement mode
        if (verticalMovementMode)
        {
            Vector3 toPoint = penTipPos - movementPlanePoint;
            float distance = Vector3.Dot(toPoint, movementPlaneNormal);
            Vector3 currentProjected = penTipPos - distance * movementPlaneNormal;
            float deltaY = Vector3.Dot(currentProjected - verticalReferencePoint, Vector3.forward);
            float scale = verticalMappingScale;
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

        // If in plane constraint mode, perform horizontal movement
        if (constrainToPlane)
        {
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
