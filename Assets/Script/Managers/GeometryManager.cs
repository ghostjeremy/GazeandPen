using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class GeometryManager : MonoBehaviour
{
    public static GeometryManager Instance { get; private set; }

    
    
    // For example, used to manage all geometric objects, including splines and points
    public List<NURBSSpline> splines = new List<NURBSSpline>();
    public List<PointObject> points = new List<PointObject>();

    // New: The currently active spline being created (if in spline mode)
    public NURBSSpline activeSpline = null;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    // Existing CreateNURBSSpline method
    public void CreateNURBSSpline()
    {
        GameObject splineObj = new GameObject("NURBSSpline");
        NURBSSpline spline = splineObj.AddComponent<NURBSSpline>();
        spline.InitializeSpline(4);
        splines.Add(spline);
        Debug.Log("Created a new NURBS Spline");
    }

    // New: Create a point
    public void CreatePoint()
    {
        // Create a new point object
        GameObject pointObj = new GameObject("Point");
        PointObject point = pointObj.AddComponent<PointObject>();

        // Set the initial position to the pen tip position
        var vrStylusHandler = FindObjectOfType<VrStylusHandler>();
        if (vrStylusHandler != null)
            pointObj.transform.position = vrStylusHandler.CurrentState.inkingPose.position;
        else
            pointObj.transform.position = Vector3.zero;

        // Add to the management list
        points.Add(point);

        Debug.Log("Created a new point");
    }

    // New: Start creating a new spline
    public void StartSplineCreation()
    {
        GameObject splineObj = new GameObject("NURBSSpline");
        NURBSSpline spline = splineObj.AddComponent<NURBSSpline>();
        activeSpline = spline;
        splines.Add(spline);
        Debug.Log("Started new spline creation");
    }

    // New: Add a control point to the currently active spline
    public void AddSplineControlPointToActiveSpline()
    {
        if (activeSpline != null)
        {
            // Create a control point GameObject
            GameObject cpObj = new GameObject("ControlPoint_" + activeSpline.controlPoints.Count);
            cpObj.transform.parent = activeSpline.transform;
            
            var vrStylusHandler = FindObjectOfType<VrStylusHandler>();
            if (vrStylusHandler != null)
                cpObj.transform.position = vrStylusHandler.CurrentState.inkingPose.position;
            else
                cpObj.transform.position = Vector3.zero;
            ControlPoint cp = cpObj.AddComponent<ControlPoint>();
            activeSpline.controlPoints.Add(cp);
            activeSpline.UpdateSpline();
            Debug.Log("Added control point " + activeSpline.controlPoints.Count + " to active spline");
        }
    }

    // New: Finish the current spline creation
    public void FinishSplineCreation()
    {
        if (activeSpline != null)
        {
            // Update the final spline curve (without preview)
            activeSpline.UpdateSpline(false);
            Debug.Log("Finished spline creation");
            activeSpline = null;
        }
    }

    private void Update()
    {
        // If a spline is currently being edited, update its curve preview in real time
        if (activeSpline != null)
        {
            activeSpline.UpdateSpline();
        }
    }
}
