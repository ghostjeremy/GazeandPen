using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SelectionManager : MonoBehaviour
{
    // Singleton implementation, ensuring that InkPenInputStrategy can access this object via SelectionManager.Instance
    public static SelectionManager Instance { get; private set; }

    // The collection of currently selected objects (supports multi-selection)
    public List<ISelectable> selectedPoints = new List<ISelectable>();

    // The current radius of the selection sphere (also used for visualization), initial value is 0.05f
    public float selectionSphereRadius = 0.05f;

    // Dynamic parameters for the selection sphere radius:
    public float minDynamicRadius = 0.03f; // Minimum sphere radius when pressure is maximum (=1)
    public float maxDynamicRadius = 0.1f;  // Maximum sphere radius when pressure is minimum (=0.1)

    // Object used for visualizing the selection sphere; loads the SelectionModel prefab from the Resources folder
    private GameObject selectionVisual;

    // Indicates if selection mode is active; set via SetSelectionModeActive by InkPenInputStrategy
    public bool isSelectionModeActive = false;

    private Vector3 tipOffset = new Vector3(0.00904f, -0.07088f, -0.07374f); 

    // Helper function to determine pen tip position based on the current input mode.
    private Vector3 GetPenTipPosition()
    {
        // Look up the RightHandInputManager to check which input mode is active
        RightHandInputManager inputManager = FindObjectOfType<RightHandInputManager>();
        if (inputManager != null && inputManager.UseQuest3AtStart)
        {
            VrStylusHandler vrStylus = FindObjectOfType<VrStylusHandler>();
            if (vrStylus != null)
                return vrStylus.CurrentState.inkingPose.position;
        }
        // Fallback: use controller's position.
    // Get the local position and rotation of the right-hand controller
    Vector3 controllerPos = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
    Quaternion controllerRot = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch);
    
    // Calculate pen tip position: apply tipOffset to the controller's local coordinate system
    return controllerPos + (controllerRot * tipOffset);
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Load the SelectionModel prefab from the Resources folder for visualizing the selection sphere
        GameObject prefab = Resources.Load<GameObject>("SelectionModel");
        if (prefab != null)
        {
            selectionVisual = Instantiate(prefab);
            selectionVisual.name = "SelectionVisual";
            selectionVisual.SetActive(false);
        }
        else
        {
            Debug.LogError("SelectionModel prefab not found in Resources folder!");
        }
    }

    void Update()
    {
        if (isSelectionModeActive)
        {
            Vector3 penTipPos = GetPenTipPosition();

            // Update the visual selection sphere's position and scale (diameter = 2 * selectionSphereRadius)
            if (selectionVisual != null)
            {
                if (!selectionVisual.activeSelf)
                    selectionVisual.SetActive(true);
                selectionVisual.transform.position = penTipPos;
                selectionVisual.transform.localScale = Vector3.one * (selectionSphereRadius * 2f);
            }

            // Update hover status based on the pen tip position
            Collider[] colliders = Physics.OverlapSphere(penTipPos, selectionSphereRadius);
            HashSet<ISelectable> hoveredSelectables = new HashSet<ISelectable>();
            foreach (Collider col in colliders)
            {
                ISelectable selectable = col.GetComponentInParent<ISelectable>();
                if (selectable != null)
                    hoveredSelectables.Add(selectable);
            }

            ISelectable[] allSelectables = FindObjectsOfType<MonoBehaviour>().OfType<ISelectable>().ToArray();
            foreach (ISelectable selectable in allSelectables)
            {
                if (hoveredSelectables.Contains(selectable))
                    selectable.OnHoverEnter();
                else
                    selectable.OnHoverExit();
            }
        }
        else
        {
            // When selection mode is inactive, hide the selection sphere and cancel the hover state of all objects
            if (selectionVisual != null && selectionVisual.activeSelf)
            {
                selectionVisual.SetActive(false);
            }
            ISelectable[] allSelectables = FindObjectsOfType<MonoBehaviour>().OfType<ISelectable>().ToArray();
            foreach (ISelectable selectable in allSelectables)
            {
                selectable.OnHoverExit();
            }
        }
    }

    // External interface for setting the selection mode's active state and updating the initial position of the selection sphere
    public void SetSelectionModeActive(bool active, Vector3 penTipPos, float pressure)
    {
        isSelectionModeActive = active;
        // Override external penTipPos with the correct position based on the input mode.
        penTipPos = GetPenTipPosition();
        if (active)
        {
            // Calculate the selection sphere radius based on pressure: within the range [0.1, 1], higher pressure yields a smaller radius
            float clampedPressure = Mathf.Clamp(pressure, 0.1f, 1.0f);
            float normalized = (clampedPressure - 0.1f) / (1.0f - 0.1f);
            float computedRadius = Mathf.Lerp(maxDynamicRadius, minDynamicRadius, normalized);

            selectionSphereRadius = computedRadius;

            if (selectionVisual != null)
            {
                selectionVisual.SetActive(true);
                selectionVisual.transform.position = penTipPos;
                selectionVisual.transform.localScale = Vector3.one * (selectionSphereRadius * 2f);
            }
            // The auto-selection feature has been removed; here, only updating hover status
            Collider[] colliders = Physics.OverlapSphere(penTipPos, selectionSphereRadius);
            HashSet<ISelectable> hoveredSelectables = new HashSet<ISelectable>();
            foreach (Collider col in colliders)
            {
                ISelectable selectable = col.GetComponentInParent<ISelectable>();
                if (selectable != null)
                    hoveredSelectables.Add(selectable);
            }
            ISelectable[] allSelectables = FindObjectsOfType<MonoBehaviour>().OfType<ISelectable>().ToArray();
            foreach (ISelectable selectable in allSelectables)
            {
                if (hoveredSelectables.Contains(selectable))
                    selectable.OnHoverEnter();
                else
                    selectable.OnHoverExit();
            }
        }
        else
        {
            if (selectionVisual != null)
            {
                selectionVisual.SetActive(false);
            }
            ISelectable[] allSelectables = FindObjectsOfType<MonoBehaviour>().OfType<ISelectable>().ToArray();
            foreach (ISelectable selectable in allSelectables)
            {
                selectable.OnHoverExit();
            }
        }
    }

    // Modified to support multi-selection with toggling: iterate over all Colliders within the selection sphere
    public void SelectInRange(Vector3 center)
    {
        Collider[] colliders = Physics.OverlapSphere(center, selectionSphereRadius);
        foreach (Collider col in colliders)
        {
            ISelectable selectable = col.GetComponentInParent<ISelectable>();
            if (selectable != null)
            {
                if (selectedPoints.Contains(selectable))
                {
                    selectedPoints.Remove(selectable);
                    selectable.OnDeselected();
                }
                else
                {
                    selectedPoints.Add(selectable);
                    selectable.OnSelected();
                }
            }
        }
    }

    // Deselect all selections
    public void Deselect()
    {
        foreach (ISelectable selectable in selectedPoints)
        {
            selectable.OnDeselected();
        }
        selectedPoints.Clear();
    }
}
