using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointObject : MonoBehaviour, ISelectable
{
    // Expandable properties, such as point color, size, etc.
    public float pointSize = 0.01f;

    public Color normalColor = Color.red;
    public Color selectedColor = Color.yellow;
    public Color hoveredColor = Color.green;
    public bool IsSelected { get; set; }

    private Renderer rend;
    private bool _isHovered = false;

    // Dynamically load the PointModel prefab from the Resources folder using Resources.Load
    // Ensure that PointModel.prefab is placed in the Resources folder
    private void Start()
    {
        // Directly load the PointModel prefab from the Resources folder
        GameObject prefab = Resources.Load<GameObject>("PointModel");
        GameObject model = Instantiate(prefab, transform);
        model.transform.localPosition = Vector3.zero;
        model.transform.localScale = Vector3.one * pointSize;
        rend = model.GetComponent<Renderer>();
        rend.material.color = normalColor;
    }

    public void OnSelected()
    {
        IsSelected = true;
        _isHovered = false; // Cancel hover state after selection
        if (rend != null)
        {
            rend.material.color = selectedColor;
        }
    }

    public void OnDeselected()
    {
        IsSelected = false;
        if (rend != null)
        {
            // If still hovered, display hoveredColor; otherwise, display normalColor
            rend.material.color = _isHovered ? hoveredColor : normalColor;
        }
    }

    public void OnHoverEnter()
    {
        _isHovered = true;
        // Change color only if not selected
        if (!IsSelected && rend != null)
        {
            rend.material.color = hoveredColor;
        }
    }

    public void OnHoverExit()
    {
        _isHovered = false;
        // Revert to normal color only if not selected
        if (!IsSelected && rend != null)
        {
            rend.material.color = normalColor;
        }
    }

    // Expandable: add drag, selection, and other interactive logic
} 