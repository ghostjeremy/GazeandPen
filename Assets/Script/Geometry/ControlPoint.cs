using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlPoint : MonoBehaviour, ISelectable
{
    // Expandable properties, such as point color, size, etc.
    public float pointSize = 0.01f;

    public Color normalColor = Color.blue;
    public Color selectedColor = Color.yellow;
    public Color hoveredColor = Color.green;
    public bool IsSelected { get; set; }

    private Renderer rend;
    private bool _isHovered = false;
    // New: Indicates whether drag is active
    private bool isDragging = false;
    // New: Record the original transform.localScale
    private Vector3 originalScale;

    // New: Called when drag begins, applying a highlight effect (e.g., enlargement)
    public void OnDragEnter()
    {
         isDragging = true;
         // For example: enlarge by 20% based on the original scale
         transform.localScale = originalScale * 1.2f;
         // 或者启用 Outline 效果
         // GetComponent<Outline>()?.SetActive(true);
    }

    // New: Called when drag ends to revert to the default state
    public void OnDragExit()
    {
         isDragging = false;
         transform.localScale = originalScale;
         // Disable Outline effect
         // GetComponent<Outline>()?.SetActive(false);
    }

    // Dynamically load the ControlPointModel prefab from the Resources folder using Resources.Load
    // Ensure that ControlPointModel.prefab is placed in the Resources folder
    private void Start()
    {
        // Directly load the ControlPointModel prefab from the Resources folder
        GameObject prefab = Resources.Load<GameObject>("ControlPointModel");
        GameObject model = Instantiate(prefab, transform);
        model.transform.localPosition = Vector3.zero;
        model.transform.localScale = Vector3.one * pointSize;
        rend = model.GetComponent<Renderer>();
        rend.material.color = normalColor;
        // Store the initial scale
        originalScale = transform.localScale;
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
