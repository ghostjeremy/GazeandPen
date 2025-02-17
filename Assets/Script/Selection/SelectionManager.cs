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
            VrStylusHandler vrStylus = FindObjectOfType<VrStylusHandler>();
            if (vrStylus != null)
            {
                Vector3 penTipPos = vrStylus.CurrentState.inkingPose.position;

                // Update the visual selection sphere's position and scale (diameter = 2 * selectionSphereRadius)
                if (selectionVisual != null)
                {
                    if (!selectionVisual.activeSelf)
                        selectionVisual.SetActive(true);
                    selectionVisual.transform.position = penTipPos;
                    selectionVisual.transform.localScale = Vector3.one * (selectionSphereRadius * 2f);
                }

                // Update hover status: Get all objects within the selection sphere that implement the ISelectable interface
                Collider[] colliders = Physics.OverlapSphere(penTipPos, selectionSphereRadius);
                HashSet<ISelectable> hoveredSelectables = new HashSet<ISelectable>();
                foreach (Collider col in colliders)
                {
                    ISelectable selectable = col.GetComponentInParent<ISelectable>();
                    if (selectable != null)
                        hoveredSelectables.Add(selectable);
                }

                // Update the hover state of all objects in the scene that implement ISelectable
                // This is accomplished by finding all MonoBehaviour instances and then filtering for ISelectable instances
                ISelectable[] allSelectables = FindObjectsOfType<MonoBehaviour>().OfType<ISelectable>().ToArray();
                foreach (ISelectable selectable in allSelectables)
                {
                    // Trigger OnHoverEnter if within detection range; otherwise, trigger OnHoverExit
                    if (hoveredSelectables.Contains(selectable))
                    {
                        selectable.OnHoverEnter();
                    }
                    else
                    {
                        selectable.OnHoverExit();
                    }
                }
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
        if (active)
        {
            // Calculate the selection sphere radius based on pressure: within the range [0.1, 1], higher pressure yields a smaller radius
            float clampedPressure = Mathf.Clamp(pressure, 0.1f, 1.0f);
            float normalized = (clampedPressure - 0.1f) / (1.0f - 0.1f); // Obtain a value in the range 0 to 1
            float computedRadius = Mathf.Lerp(maxDynamicRadius, minDynamicRadius, normalized);

            // Update the global selection sphere radius
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
                {
                    selectable.OnHoverEnter();
                }
                else
                {
                    selectable.OnHoverExit();
                }
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
