using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ToolType
{
    None,
    CreatePoint,
    CreateSpline
}

public class ToolsManager : MonoBehaviour
{
    public static ToolsManager Instance { get; private set; }

    [Tooltip("Select the current tool type (None, CreatePoint, CreateSpline) for debugging purposes")]
    public ToolType currentTool = ToolType.None;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Optional: DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Called when the user selects the "Create Point" tool from the UI
    public void SelectCreatePointTool() 
    {
        currentTool = ToolType.CreatePoint;
        Debug.Log("Current tool set to CreatePoint");
    }

    public void OnTipTapPressed()
    {
        if (currentTool == ToolType.CreatePoint)
        {
            GeometryManager.Instance.CreatePoint();
            Debug.Log("Created a point via the Confirm button");
        }
        else if (currentTool == ToolType.CreateSpline)
        {
            // If there is no active spline, start new spline creation and immediately add a control point;
            // otherwise, simply add a control point
            if (GeometryManager.Instance.activeSpline == null)
            {
                GeometryManager.Instance.StartSplineCreation();
            }
            GeometryManager.Instance.AddSplineControlPointToActiveSpline();
        }
    }

    // Tool operation
    public void OnConfirmShortPressed()
    {
        if (currentTool == ToolType.CreatePoint)
        {
            GeometryManager.Instance.CreatePoint();
            Debug.Log("Created a point via the Confirm button");
        }
        else if (currentTool == ToolType.CreateSpline)
        {
            // If there is no active spline, start new spline creation and immediately add a control point;
            // otherwise, simply add a control point
            if (GeometryManager.Instance.activeSpline == null)
            {
                GeometryManager.Instance.StartSplineCreation();
            }
            GeometryManager.Instance.AddSplineControlPointToActiveSpline();
        }
    }

    // New: Use the Cancel button to finish spline creation in spline mode
    public void OnCancelShortPressed()
    {
        if (currentTool == ToolType.CreateSpline)
        {
            GeometryManager.Instance.FinishSplineCreation();
            Debug.Log("Spline creation finished");
        }
    }
}

