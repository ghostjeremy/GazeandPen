# PointDrawing.cs - Feature Overview

This document outlines the features and functionality implemented in the `PointDrawing.cs` script. This Unity script handles both drawing and selection behaviors using a stylus input.

## Modes

The system operates in two distinct modes:
- **Drawing Mode** (default): Creates points when the user presses specific input buttons.
- **Selection Mode**: Enables the user to select, deselect, and drag points.

## Drawing Mode Features

- **Point Creation**  
  - In drawing mode, a red sphere is created at the stylus tip (inking position) when either:
    - `cluster_front_value` is pressed.
    - `tip_value` exceeds a defined threshold (`_tipForceThreshold`), indicating a strong pen touch.
  - Created points are scaled (diameter = 0.01, corresponding to a radius of 0.005) and tagged as **"Point"** for future reference during selection.

## Selection Mode Features

- **Dynamic Selection Range**  
  The effective selection range is computed based on a base threshold and the current stylus pressure:
  - **Base Threshold**: `_selectionDistanceThreshold` (e.g., 0.01).
  - **Pressure Adjustment**:  
    The effective range decreases when the stylus pressure (`cluster_middle_value`) is high and increases when the pressure is low.
  - **Calculation Formula**:  
    ```plaintext
    effectiveRange = _selectionDistanceThreshold * (1 + (1 - cluster_middle_value) * _maxRangeMultiplier)
    ```
    - When `cluster_middle_value` = 1 (high pressure):  
      `effectiveRange` equals `_selectionDistanceThreshold`.
    - When `cluster_middle_value` = 0 (low pressure):  
      `effectiveRange` equals `_selectionDistanceThreshold * (1 + _maxRangeMultiplier)`.

- **Visual Indicator**  
  - A semi-transparent cyan sphere serves as a visual indicator for the current effective selection range.
  - The indicator updates in real time to follow the pen tip's position and adjust in size accordingly.
  - The displayed range uses a visual multiplier (`_selectionVisualMultiplier`), which is typically set so the visual range exactly matches the computed effective range.

- **Selection/Deselection Logic**  
  - **Toggling**:  
    - When pressing `cluster_front_value` in selection mode, an OverlapSphere is used to detect all nearby points (using the computed `effectiveRange` as the radius).
    - If a point is already in the selection list, it is deselected (its color reverts to red).
    - If a point is not selected, it is added to the selection (its color changes to yellow).
  - **Color Coding**:
    - **Within effective range**: Points are displayed in **green**.
    - **Outside effective range**:
      - Selected points are **yellow**.
      - Unselected points remain **red**.

- **Dragging**  
  - A long press of `cluster_front_value` (beyond the threshold `_moveTriggerDuration`) initiates a drag operation.
  - At the beginning of dragging, the script records the pen tip position and computes relative offsets for each selected object.
  - As the pen moves, the selected objects are repositioned according to these relative offsets, maintaining their initial distance from the pen tip.

- **Cancel Selection**  
  - Pressing `cluster_back_value` clears all selections by resetting the color of selected objects to red and emptying the selection list.

## Input Handling

- **StylusHandler**  
  The script relies on a `StylusHandler` component that provides current:
  - `cluster_front_value`
  - `tip_value`
  - `cluster_back_value`
  - `cluster_middle_value`  
  These inputs determine actions such as drawing points, selecting/deselecting, and dragging.

## Mode Switching

- The mode is toggled via the public method `ToggleMode()`, which switches between drawing and selection modes.
- When switching modes, relevant detection states are reset to prevent accidental triggers.

## Technical Details

- **Tagging**:  
  Every created point is tagged as **"Point"** to facilitate efficient lookup during selection operations.
  
- **Material Handling**:  
  New material instances are created when updating point colors to avoid modifying shared assets.

- **Real-time Updates**:  
  In every frame (`Update()`), the script recalculates:
  - The effective selection range based on the current `cluster_middle_value`.
  - The visual indicator's position and scale.
  - The colors of all points depending on their distance from the pen tip and their selection status.

## Summary

The `PointDrawing.cs` script provides a robust solution for both point creation (drawing) and point manipulation (selection/deselection and dragging) in VR/AR environments. The dynamic adjustment of the selection range through stylus pressure, coupled with clear visual feedback, creates an intuitive and responsive user experience.

*Feel free to adjust the serialized field values in the Unity Inspector to fine-tune the interactions (e.g., `_tipForceThreshold`, `_selectionDistanceThreshold`, `_maxRangeMultiplier`, and `_moveTriggerDuration`).* 