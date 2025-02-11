using UnityEngine;
using System.Collections.Generic;
using static OVRInput;

public class PointDrawing : MonoBehaviour
{
    [SerializeField]
    private StylusHandler _stylusHandler;

    // Mode switching: false indicates drawing mode (default), true indicates selection mode.
    [SerializeField]
    private bool _isSelectionMode = false;

    // Used to detect the rising edge of cluster_front_value (used in drawing mode).
    private bool _previousClusterFrontValue = false;

    // Used to detect the rising edge of tip_value (used in drawing mode to create a sphere when the pen touches the table).
    private bool _previousTipActive = false;

    // Force threshold for the pen tip (e.g., if tip force > 0.5, it is considered as a strong touch).
    [SerializeField]
    private float _tipForceThreshold = 0.5f;

    // ------------------------- Selection Mode Fields -------------------------

    // Records the starting time when cluster_front_value is pressed.
    private float _selectionPressStartTime = 0f;

    // List of currently selected objects (allows multiple objects to be selected).
    private List<GameObject> _selectedObjects = new List<GameObject>();

    // Sets the distance threshold between the pen tip and objects for selection.
    // A smaller value means the pen must be very close to a sphere to select/deselect it.
    [SerializeField]
    private float _selectionDistanceThreshold = 0.01f;

    // Sets the visual multiplier for the selection range indicator so that the displayed range matches the effective selection range.
    [SerializeField]
    private float _selectionVisualMultiplier = 1.0f;

    // Maximum range multiplier that controls the maximum effective selection range when cluster_middle_value is 0.
    // For example, when cluster_middle_value is 0, the effective range will be _selectionDistanceThreshold * (1 + _maxRangeMultiplier).
    [SerializeField]
    private float _maxRangeMultiplier = 2.0f;

    // Threshold time (in seconds) for long pressing cluster_front_value to initiate object dragging.
    [SerializeField]
    private float _moveTriggerDuration = 0.2f;

    // ------------------------- Dragging Fields -------------------------

    // Indicates whether we are currently in dragging mode.
    private bool _isDragging = false;

    // Records the pen tip's position at the start of a drag operation.
    private Vector3 _dragStartPenPosition;

    // Stores initial offsets for each selected object's position relative to the pen tip when dragging starts.
    private Dictionary<GameObject, Vector3> _dragOffsets = new Dictionary<GameObject, Vector3>();

    // ------------------------- Visual Indicator -------------------------

    // Visual indicator (a semi-transparent sphere) that shows the selection range at the pen tip.
    private GameObject _selectionIndicator;

    // Used to detect the rising edge of cluster_back_value.
    // When pressed, it cancels all currently selected objects.
    private bool _previousClusterBackValue = false;

    void Update()
    {
        // ------------------------- Cancel Selection -------------------------
        // Detect rising edge of cluster_back_value to clear all selections.
        bool currentClusterBack = _stylusHandler.CurrentState.cluster_back_value;
        if (currentClusterBack && !_previousClusterBackValue)
        {
            foreach (GameObject obj in _selectedObjects)
            {
                Renderer r = obj.GetComponent<Renderer>();
                if (r != null)
                {
                    r.material = new Material(Shader.Find("Standard"));
                    r.material.color = Color.red;
                }
            }
            _selectedObjects.Clear();
            Debug.Log("All selections cleared.");
        }
        _previousClusterBackValue = currentClusterBack;

        // ------------------------- Mode Handling -------------------------
        if (!_isSelectionMode)
        {
            // Drawing mode: create points when cluster_front_value or tip_value is pressed.
            bool currentClusterFrontValue = _stylusHandler.CurrentState.cluster_front_value;
            if (currentClusterFrontValue && !_previousClusterFrontValue)
            {
                DrawPoint();
            }
            _previousClusterFrontValue = currentClusterFrontValue;

            bool currentTipActive = _stylusHandler.CurrentState.tip_value > _tipForceThreshold;
            if (currentTipActive && !_previousTipActive)
            {
                DrawPoint();
            }
            _previousTipActive = currentTipActive;
        }
        else
        {
            // Selection mode:
            // Pressing cluster_front_value attempts to select/deselect nearby spheres (multiple selection allowed).
            // A long press triggers dragging of the selected objects using relative offsets.
            bool clusterPressed = _stylusHandler.CurrentState.cluster_front_value;
            if (clusterPressed)
            {
                if (_selectionPressStartTime == 0f)
                {
                    _selectionPressStartTime = Time.time;
                    TrySelectObject();
                }
                else
                {
                    float pressDuration = Time.time - _selectionPressStartTime;
                    if (pressDuration >= _moveTriggerDuration)
                    {
                        if (!_isDragging && _selectedObjects.Count > 0)
                        {
                            // Start dragging: record the pen tip position and each object's offset relative to the pen.
                            _isDragging = true;
                            _dragStartPenPosition = _stylusHandler.CurrentState.inkingPose.position;
                            _dragOffsets.Clear();
                            foreach (var obj in _selectedObjects)
                            {
                                _dragOffsets[obj] = obj.transform.position - _dragStartPenPosition;
                            }
                        }
                        else if (_isDragging)
                        {
                            // During dragging: update each selected object's position based on the current pen tip position and the initial offset.
                            Vector3 currentPenPosition = _stylusHandler.CurrentState.inkingPose.position;
                            foreach (var obj in _selectedObjects)
                            {
                                obj.transform.position = currentPenPosition + _dragOffsets[obj];
                            }
                        }
                    }
                }
            }
            else
            {
                _selectionPressStartTime = 0f;
                _isDragging = false;
                _dragOffsets.Clear();
            }
        }

        // ------------------------- Compute Effective Selection Range -------------------------
        // Calculate the effective selection range based on the current cluster_middle_value.
        // When pressure is high (cluster_middle_value near 1), the effective range is small.
        // When pressure is low (cluster_middle_value near 0), the effective range is larger.
        float effectiveRange = _selectionDistanceThreshold * (1f + (1f - _stylusHandler.CurrentState.cluster_middle_value) * _maxRangeMultiplier);

        // Update the visual selection indicator's position and size (visible only in selection mode)
        // so that it matches the current effective selection range.
        if (_selectionIndicator != null)
        {
            _selectionIndicator.transform.position = _stylusHandler.CurrentState.inkingPose.position;
            _selectionIndicator.transform.localScale = new Vector3(effectiveRange * _selectionVisualMultiplier * 2,
                                                                   effectiveRange * _selectionVisualMultiplier * 2,
                                                                   effectiveRange * _selectionVisualMultiplier * 2);
            _selectionIndicator.SetActive(_isSelectionMode);
        }

        // ------------------------- Update Sphere Colors -------------------------
        // In selection mode, update the colors of all spheres (tagged "Point"):
        // - If a sphere is within the effective range, set its color to green.
        // - Otherwise, if a sphere is selected, set its color to yellow; if not selected, set it to red.
        if (_isSelectionMode)
        {
            GameObject[] allPoints = GameObject.FindGameObjectsWithTag("Point");
            Vector3 penPos = _stylusHandler.CurrentState.inkingPose.position;
            foreach (GameObject pt in allPoints)
            {
                Renderer rend = pt.GetComponent<Renderer>();
                if (rend != null)
                {
                    if (Vector3.Distance(pt.transform.position, penPos) <= effectiveRange)
                        rend.material.color = Color.green;
                    else
                        rend.material.color = (_selectedObjects.Contains(pt)) ? Color.yellow : Color.red;
                }
            }
        }
    }

    /// <summary>
    /// Creates a red sphere at the pen tip (invoked in drawing mode).
    /// </summary>
    private void DrawPoint()
    {
        // Create a sphere to represent the pen tip point.
        GameObject point = GameObject.CreatePrimitive(PrimitiveType.Sphere);

        // Set the sphere's position to the pen tip's inkingPose position.
        point.transform.position = _stylusHandler.CurrentState.inkingPose.position;

        // Set sphere scale: radius is 0.005 (diameter = 0.01).
        point.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        // Set the sphere color to red.
        Renderer renderer = point.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = Color.red;
        }
        // Tag the sphere as "Point" for later lookup.
        point.tag = "Point";
    }

    /// <summary>
    /// Attempts to select or deselect spheres near the pen tip using an OverlapSphere.
    /// If a sphere is detected, it is added to or removed from the selection list.
    /// When selected, spheres change to yellow; when deselected, they revert to red.
    /// </summary>
    private void TrySelectObject()
    {
        float effectiveRange = _selectionDistanceThreshold * (1f + (1f - _stylusHandler.CurrentState.cluster_middle_value) * _maxRangeMultiplier);
        Collider[] colliders = Physics.OverlapSphere(_stylusHandler.CurrentState.inkingPose.position, effectiveRange);
        if (colliders.Length > 0)
        {
            foreach (Collider col in colliders)
            {
                GameObject obj = col.gameObject;
                if (_selectedObjects.Contains(obj))
                {
                    // Deselect the object if it is already selected (toggle off), and reset its color to red.
                    _selectedObjects.Remove(obj);
                    Renderer renderer = obj.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material = new Material(Shader.Find("Standard"));
                        renderer.material.color = Color.red;
                    }
                    Debug.Log("Deselected object: " + obj.name);
                }
                else
                {
                    // Select the object and change its color to yellow.
                    _selectedObjects.Add(obj);
                    Renderer renderer = obj.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material = new Material(Shader.Find("Standard"));
                        renderer.material.color = Color.yellow;
                    }
                    Debug.Log("Selected object: " + obj.name);
                }
            }
        }
        else
        {
            Debug.Log("No object near pen tip for selection.");
        }
    }

    /// <summary>
    /// Toggles between drawing and selection modes.
    /// </summary>
    public void ToggleMode()
    {
        _isSelectionMode = !_isSelectionMode;
        // Reset detection states to avoid accidental triggers when switching modes.
        _previousClusterFrontValue = false;
        _previousTipActive = false;
        Debug.Log("Mode switched to " + (_isSelectionMode ? "Selection" : "Drawing"));
    }

    private void Start()
    {
        // Create a visual indicator (a sphere) for the selection range.
        _selectionIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        // Disable its collider so that it does not interfere with physics checks.
        Collider indicatorCollider = _selectionIndicator.GetComponent<Collider>();
        if (indicatorCollider != null)
        {
            indicatorCollider.enabled = false;
        }
        // Set the visual indicator's material to a semi-transparent cyan color.
        Renderer indicatorRenderer = _selectionIndicator.GetComponent<Renderer>();
        if (indicatorRenderer != null)
        {
            indicatorRenderer.material = new Material(Shader.Find("Standard"));
            Color indicatorColor = Color.cyan;
            indicatorColor.a = 0.1f;
            indicatorRenderer.material.color = indicatorColor;
            indicatorRenderer.material.SetFloat("_Mode", 3);
            indicatorRenderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            indicatorRenderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            indicatorRenderer.material.SetInt("_ZWrite", 0);
            indicatorRenderer.material.DisableKeyword("_ALPHATEST_ON");
            indicatorRenderer.material.EnableKeyword("_ALPHABLEND_ON");
            indicatorRenderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            indicatorRenderer.material.renderQueue = 3000;
        }
    }
}
