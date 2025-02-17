

## Features

- **Geometry Creation:**  
  Create and modify NURBS splines using control points. The splines are generated using complex mathematical algorithms and are visualized in real time.

- **Point Creation:**  
  Dynamically create individual points with adjustable properties, such as size and color.

- **Input Strategies:**  
  Utilize diverse input methods:
  - **InkPenInputStrategy:** For digital pen devices.
  - **QProControllerInputStrategy:** For VR controllers (e.g., Meta Quest Touch Controller Pro).

- **Selection and Movement:**  
  Select objects using a visual selection sphere that dynamically adapts based on pressure input. Move selected objects via free movement or constrained (planar or vertical) mode.

- **Tool Management:**  
  Switch between different tool modes (e.g., CreatePoint or CreateSpline) using the Tools Manager. This allows quick toggling between creating points and splines, with a simple UI integration.

- **Task Switching:**  
  Move between different project tasks or scenes using the HomePageController, which integrates with the overall GameManager and SceneLoader systems.

## Project Structure

- **Script/Geometry:**
  - `NURBSSpline.cs` - Implements NURBS spline computation and visualization.
  - `ControlPoint.cs` - Contains the definition for a control point that influences spline shape.
  - `PointObject.cs` - Represents an individual, manipulatable point in the scene.

- **Script/Input/RightHand:**
  - `IRightHandInputStrategy.cs` - Defines the interface for right-hand input strategies.
  - `InkPenInputStrategy.cs` - Provides input handling for digital ink pens.
  - `QProControllerInputStrategy.cs` - Handles VR controller input.
  - `RightHandButton.cs` - Enumerates the available input buttons.
  - `RightHandInputManager.cs` - Manages switching between different input strategies.

- **Script/Managers:**
  - `GeometryManager.cs` - Manages all geometric objects (splines and points) within the scene.
  - `ToolsManager.cs` - Oversees the current tool state (e.g., creating points or splines) and provides methods to perform actions based on the selected tool.

- **Script/Selection:**
  - `SelectionManager.cs` - Controls the selection mechanism using a dynamic sphere, supporting multi-selection and hover state management.
  - `MovementController.cs` - Facilitates movement (both free and constrained) of selected objects.

- **Script/Tasks:**
  - `HomePageController.cs` - Provides functionality to switch between tasks/scenes, such as moving into a test task.

- **Script/Core:**
  - Contains essential systems like `GameManager`, `EventBus`, and `SceneLoader` for overarching project control.

- **Script/Editors:**
  - `ToolsManagerEditor.cs` - Custom editor script to aid in testing and debugging of tool actions in the Unity Inspector.

## Getting Started

1. **Prerequisites:**  
   Ensure you have a Unity version compatible with the project requirements.

2. **Project Setup:**  
   - Import the project into the Unity Editor.
   - Review the scenes and configure the initial input device via the Inspector (e.g., select between pen or controller).

3. **Usage:**  
   - Use the provided UI elements to select the desired tool (e.g., CreatePoint or CreateSpline).
   - Interact with the scene using the corresponding input strategy.
   - Utilize the selection sphere to hover and select objects; then use movement handlers to reposition them.

## Build and Deployment

- The project follows Unity's component-based architecture.  
- Use Unity's Build Settings to target the desired platform (e.g., PC, VR devices).
- Ensure all prefabs (e.g., `SelectionModel`, `PointModel`) are correctly placed under the **Resources** folder.

## Additional Information

- **Modular Design:**  
  Each system (Geometry, Input, Selection, Tools, Tasks) is implemented in a modular way to facilitate easy maintenance and extensions.

- **Best Practices:**  
  The code adheres to Unity and C# best practices, such as the use of singletons for managers, component-based architecture for game logic, and clear separation of concerns.

- **Customization:**  
  Modify properties (such as `verticalMappingScale`, selection sphere radii, and movement constraints) via the Inspector to tailor the project's behavior to your needs.


