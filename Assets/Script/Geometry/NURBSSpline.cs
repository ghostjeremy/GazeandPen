using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NURBSSpline : MonoBehaviour
{
    public List<ControlPoint> controlPoints = new List<ControlPoint>();
    public int degree = 3; // The degree of the NURBS curve (typically 3, i.e., cubic)
    // New: Sampling resolution used to control the number of sample points on the curve; adjustable in the Inspector
    [SerializeField] private int resolution = 50;
    
    // Knot vector
    private List<float> knots = new List<float>();

    // LineRenderer used to display the spline
    private LineRenderer lineRenderer;
    // New: LineRenderer for connecting control points with a dashed line
    private LineRenderer controlPointsConnector;

    // New: Stores the computed NURBS curve points
    private List<Vector3> splinePoints = new List<Vector3>();

    private void Awake()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
        // Set line properties, such as color and width
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        // Set the curve color from dark blue to light blue
        lineRenderer.startColor = new Color(0f, 0f, 0.5f, 1f);  // Dark blue
        lineRenderer.endColor = new Color(0.5f, 0.5f, 1f, 1f);    // Light blue
        lineRenderer.widthMultiplier = 0.01f;

        // Create the dashed LineRenderer for connecting control points
        GameObject connectorObj = new GameObject("ControlPointsConnector");
        connectorObj.transform.SetParent(transform);
        connectorObj.transform.localPosition = Vector3.zero;
        controlPointsConnector = connectorObj.AddComponent<LineRenderer>();
        controlPointsConnector.positionCount = 0;
        controlPointsConnector.material = new Material(Shader.Find("Sprites/Default"));
        // Set the dashed line color to be whiter and more transparent (using white with alpha 0.6)
        controlPointsConnector.startColor = new Color(1f, 1f, 1f, 0.6f);
        controlPointsConnector.endColor = new Color(1f, 1f, 1f, 0.6f);
        controlPointsConnector.widthMultiplier = 0.0025f;
        // Use Tile mode so the texture repeats to achieve a dashed effect
        controlPointsConnector.textureMode = LineTextureMode.Tile;
        // If a dashed line texture is available, load and assign it (e.g., Resources.Load<Texture>("DashedLineTexture"))
    }

    // Initialize the spline by creating n control points
    public void InitializeSpline(int numControlPoints)
    {
        for (int i = 0; i < numControlPoints; i++)
        {
            // Create a control point GameObject
            GameObject cpObj = new GameObject("ControlPoint_" + i);
            cpObj.transform.parent = transform;
            cpObj.transform.localPosition = new Vector3(i * 0.5f, 0, 0); // Sample position
            ControlPoint cp = cpObj.AddComponent<ControlPoint>();
            controlPoints.Add(cp);
        }
        GenerateKnotVector();
        UpdateSpline();
    }

    // Generate a clamped knot vector
    private void GenerateKnotVector()
    {
        int n = controlPoints.Count;
        // Dynamically adjust the degree: effective degree is control point count minus one, but no greater than the degree field (e.g., 3)
        int p = Mathf.Min(degree, n - 1);
        int m = n + p + 1;
        knots.Clear();
        for (int i = 0; i < m; i++)
        {
            // Set the first p+1 knots to 0
            if(i < p + 1)
                knots.Add(0f);
            // Set the last p+1 knots to 1
            else if(i >= m - p - 1)
                knots.Add(1f);
            // Distribute the middle knots evenly
            else
                knots.Add((float)(i - p) / (m - 2 * p));
        }
    }

    // Update the spline curve (recalculate LineRenderer points)
    public void UpdateSpline(bool includePreview = true)
    {
        // Build a list of effective control point positions
        List<Vector3> effectiveCP = new List<Vector3>();
        foreach (var cp in controlPoints)
        {
            effectiveCP.Add(cp.transform.position);
        }
        // If preview is enabled, add the current pen tip position
        if(includePreview)
        {
            VrStylusHandler vrStylusHandler = FindObjectOfType<VrStylusHandler>();
            if(vrStylusHandler != null)
            {
                effectiveCP.Add(vrStylusHandler.CurrentState.inkingPose.position);
            }
        }

        int nEffective = effectiveCP.Count;
        if (nEffective < 1)
        {
            lineRenderer.positionCount = 0;
            return;
        }
        int samplesPerSegment = 20;
        int sampleCount = Mathf.Max((nEffective - 1) * samplesPerSegment + 1, 1);
        Vector3[] positions = new Vector3[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / (sampleCount - 1);
            positions[i] = EvaluateCurveFromPositions(effectiveCP, t, degree);
        }
        lineRenderer.positionCount = sampleCount;
        lineRenderer.SetPositions(positions);

        // Update the dashed line connecting control points (use effectiveCP)
        UpdateControlPointsConnector(includePreview);
    }

    // Evaluate the point on the NURBS curve at parameter t (0 to 1) using the Cox-de Boor formula
    public Vector3 EvaluateCurve(float t)
    {
        int n = controlPoints.Count;
        if (n == 0)
            return Vector3.zero;
        if (n == 1)
            return controlPoints[0].transform.position;
        // At the boundaries, directly return the endpoint to ensure the curve passes through the control points
        if(t >= 1f)
            return controlPoints[n - 1].transform.position;

        // Dynamically adjust the effective degree: effective degree = min(degree, n - 1)
        int p = Mathf.Min(degree, n - 1);

        // Regenerate the knot vector to ensure the latest knots
        GenerateKnotVector();

        float u = t; // u ∈ [0,1]，由于使用 clamped knot vector

        Vector3 numerator = Vector3.zero;
        float denominator = 0f;
        for (int i = 0; i < n; i++)
        {
            // For each control point, the default weight is 1. Custom weights can be added via ControlPoint.
            float weight = 1f;
            float N = CoxDeBoor(i, p, u);
            numerator += N * weight * controlPoints[i].transform.position;
            denominator += N * weight;
        }

        if (Mathf.Approximately(denominator, 0f))
            return Vector3.zero;
        return numerator / denominator;
    }

    // Recursively compute the basis function N_i,p(u) using the Cox-de Boor formula
    private float CoxDeBoor(int i, int p, float u)
    {
        // Base case: if p == 0
        if (p == 0)
        {
            if ((knots[i] <= u && u < knots[i+1]) || (u == knots[knots.Count-1] && i == knots.Count - 2))
                return 1f;
            return 0f;
        }

        float denom1 = knots[i + p] - knots[i];
        float term1 = 0f;
        if (!Mathf.Approximately(denom1, 0f))
            term1 = ((u - knots[i]) / denom1) * CoxDeBoor(i, p - 1, u);

        float denom2 = knots[i + p + 1] - knots[i + 1];
        float term2 = 0f;
        if (!Mathf.Approximately(denom2, 0f))
            term2 = ((knots[i + p + 1] - u) / denom2) * CoxDeBoor(i + 1, p - 1, u);

        return term1 + term2;
    }

    // Expandable: add listeners for control point dragging to update the spline in real-time

    // New: Update the dashed line connecting control points to display a gray dashed line between them
    private void UpdateControlPointsConnector(bool includePreview = true)
    {
        // Build the sequence of control point positions for the dashed line, starting with the definite control points:
        List<Vector3> cpPositions = new List<Vector3>();
        foreach (var cp in controlPoints)
        {
            cpPositions.Add(cp.transform.position);
        }
        // If in preview mode and the pen tip position differs from the last control point, add the pen tip as an extra point
        if (includePreview)
        {
            VrStylusHandler vrStylusHandler = FindObjectOfType<VrStylusHandler>();
            if(vrStylusHandler != null)
            {
                Vector3 penTip = vrStylusHandler.CurrentState.inkingPose.position;
                Vector3 lastCP = cpPositions[cpPositions.Count - 1];
                if ((penTip - lastCP).sqrMagnitude > 1e-4)
                {
                    cpPositions.Add(penTip);
                }
            }
        }

        int count = cpPositions.Count;
        if(count < 2)
        {
            controlPointsConnector.positionCount = 0;
            return;
        }
        controlPointsConnector.positionCount = count;
        controlPointsConnector.SetPositions(cpPositions.ToArray());

        // Adjust the material's texture tiling to simulate a dashed effect (assuming a dash length of 0.1 units)
        float totalLength = 0f;
        for (int i = 1; i < count; i++)
        {
            totalLength += Vector3.Distance(cpPositions[i - 1], cpPositions[i]);
        }
        float dashLength = 0.1f;
        float tiling = totalLength / dashLength;
        controlPointsConnector.material.mainTextureScale = new Vector2(tiling, 1f);
    }

    // New: Evaluate a point on the NURBS curve from a provided list of control point positions using the Cox-de Boor algorithm
    private Vector3 EvaluateCurveFromPositions(List<Vector3> cps, float t, int maxDegree)
    {
        int n = cps.Count;
        if (n == 0)
            return Vector3.zero;
        if (n == 1)
            return cps[0];
        if (t >= 1f)
            return cps[n - 1];

        int p = Mathf.Min(maxDegree, n - 1);
        // Construct a clamped knot vector
        int m = n + p + 1;
        List<float> localKnots = new List<float>();
        for (int i = 0; i < m; i++)
        {
            if(i < p + 1)
                localKnots.Add(0f);
            else if(i >= m - p - 1)
                localKnots.Add(1f);
            else
                localKnots.Add((float)(i - p) / (m - 2 * p));
        }

        float u = t;
        Vector3 numerator = Vector3.zero;
        float denominator = 0f;
        for (int i = 0; i < n; i++)
        {
            float weight = 1f;
            float N = CoxDeBoorLocal(i, p, u, localKnots);
            numerator += N * weight * cps[i];
            denominator += N * weight;
        }
        if (Mathf.Approximately(denominator, 0f))
            return Vector3.zero;
        return numerator / denominator;
    }

    // New: Helper method similar to CoxDeBoor but using a local knots array
    private float CoxDeBoorLocal(int i, int p, float u, List<float> localKnots)
    {
        if (p == 0)
        {
            if ((localKnots[i] <= u && u < localKnots[i+1]) || (u == localKnots[localKnots.Count - 1] && i == localKnots.Count - 2))
                return 1f;
            return 0f;
        }

        float denom1 = localKnots[i + p] - localKnots[i];
        float term1 = 0f;
        if (!Mathf.Approximately(denom1, 0f))
            term1 = ((u - localKnots[i]) / denom1) * CoxDeBoorLocal(i, p - 1, u, localKnots);

        float denom2 = localKnots[i + p + 1] - localKnots[i + 1];
        float term2 = 0f;
        if (!Mathf.Approximately(denom2, 0f))
            term2 = ((localKnots[i + p + 1] - u) / denom2) * CoxDeBoorLocal(i + 1, p - 1, u, localKnots);

        return term1 + term2;
    }

    // New: Helper method that evaluates the point on a NURBS curve at parameter u given control points, degree p, and a local knot vector
    private Vector3 EvaluateNURBSAtParameter(Vector3[] cps, int p, List<float> localKnots, float u)
    {
        int n = cps.Length;
        // If u is very close to the start or end value, directly return the corresponding control point position
        if (Mathf.Approximately(u, localKnots[0]))
            return cps[0];
        if (Mathf.Approximately(u, localKnots[localKnots.Count - 1]))
            return cps[n - 1];

        Vector3 numerator = Vector3.zero;
        float denominator = 0f;
        for (int i = 0; i < n; i++)
        {
            float N = CoxDeBoorLocal(i, p, u, localKnots);
            numerator += N * cps[i];
            denominator += N;
        }
        if (Mathf.Approximately(denominator, 0f))
            return cps[n - 1];
        return numerator / denominator;
    }

    // New: Evaluate the NURBS curve given control points, degree p, knot vector, and resolution, returning a list of discrete sample points along the curve
    private List<Vector3> NURBSEvaluate(Vector3[] cps, int p, List<float> knots, int resolution)
    {
        int n = cps.Length;
        // Construct a local clamped knot vector with length m = n + p + 1
        int m = n + p + 1;
        List<float> localKnots = new List<float>();
        for (int i = 0; i < m; i++)
        {
            if (i < p + 1)
                localKnots.Add(0f);
            else if (i >= m - p - 1)
                localKnots.Add(1f);
            else
                localKnots.Add((float)(i - p) / (m - 2 * p));
        }

        List<Vector3> result = new List<Vector3>();
        // Uniformly sample 'resolution' points within the parameter range [0, 1]
        for (int r = 0; r <= resolution; r++)
        {
            float u = (float)r / resolution;
            Vector3 pt = EvaluateNURBSAtParameter(cps, p, localKnots, u);
            result.Add(pt);
        }
        return result;
    }

    void Update()
    {
        // Recalculate the spline every frame to ensure the curve updates when control point positions change
        CalculateSpline();
    }
    
    // Compute the NURBS curve based on cpPositions, degree, knots, and resolution
    // Preserve the original curve calculation logic unchanged
    void CalculateSpline()
    {
        // Dynamically read the latest positions of all control points
        Vector3[] cpPositions = new Vector3[controlPoints.Count];
        for (int i = 0; i < controlPoints.Count; i++)
        {
            cpPositions[i] = controlPoints[i].transform.position;
        }
        
        // 依据 cpPositions、degree、knots 和 resolution 计算 NURBS 曲线
        // 此处保留原有的曲线计算逻辑，保持计算公式不变
        splinePoints = NURBSEvaluate(cpPositions, degree, knots, resolution);
        
        // If using a LineRenderer for visualization, update it here
        if(lineRenderer != null)
        {
            lineRenderer.positionCount = splinePoints.Count;
            lineRenderer.SetPositions(splinePoints.ToArray());
        }
        
        // Also update the dashed line connecting the control points, ensuring only actual control points are connected
        if (controlPointsConnector != null)
        {
            Vector3[] cpPosForConnector = new Vector3[controlPoints.Count];
            for (int i = 0; i < controlPoints.Count; i++)
            {
                cpPosForConnector[i] = controlPoints[i].transform.position;
            }
            controlPointsConnector.positionCount = cpPosForConnector.Length;
            controlPointsConnector.SetPositions(cpPosForConnector);
        }
    }
}
