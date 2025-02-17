using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISelectable
{
    bool IsSelected { get; set; }
    // Called when object is selected
    void OnSelected();
    // Called when object is deselected
    void OnDeselected();
    // Called when object enters hover (detection range)
    void OnHoverEnter();
    // Called when object exits hover (detection range)
    void OnHoverExit();
} 