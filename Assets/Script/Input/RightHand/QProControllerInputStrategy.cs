using UnityEngine;

public class QProControllerInputStrategy : IRightHandInputStrategy
{
    // Used for Confirm (front cluster) state detection
    private bool _lastPenConfirm = false;
    private float _penConfirmDownTime = 0f;
    private bool _penConfirmHeld = false;
    private bool _penConfirmLongPressTriggered = false;

    // Used for Cancel (back cluster) state detection
    private bool _lastPenCancel = false;
    private float _penCancelDownTime = 0f;
    private bool _penCancelHeld = false;
    private bool _penCancelLongPressTriggered = false;

    private float _longPressThreshold = 0.5f;

    // New: State variable for detecting pen tip tap events
    private bool _tipWasPressed = false;
    private float _tipPressStartTime = 0f;
    // New: Marks whether sustained press state (drag mode) has been entered to prevent continuous updates
    private bool _isTipSustainedActive = false;

    // New: Used to record whether the middle button is pressed
    private bool _middleWasPressed = false;

    private Vector3 tipOffset = new Vector3(0.00904f, -0.07088f, -0.07374f); 

    // Helper method to get the pen tip position from the scene object "right_tip_position"
    private Vector3 GetPenTipPosition()
    {
        // Get the local position and rotation of the right-hand controller
        Vector3 controllerPos = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
        Quaternion controllerRot = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch);
        
        // Calculate pen tip position: apply tipOffset to the controller's local coordinate system
        return controllerPos + (controllerRot * tipOffset);
    }

    public void Initialize()
    {
        Debug.Log("InkPenInputStrategy initialized");
        // Initialize the ink pen device and register events as needed
    }

    public void UpdateInput()
    {
        // Optional: Call OVRInput.Update() if OVRInput state needs updating
        // OVRInput.Update();
        
        // ----- Confirm (front cluster) processing using GetDown, Get, and GetUp -----
        if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
        {
            _penConfirmDownTime = Time.time;
            _penConfirmLongPressTriggered = false;
        }

        // When button is held, check if duration exceeds long press threshold
        if (OVRInput.Get(OVRInput.Button.Two, OVRInput.Controller.RTouch))
        {
            float duration = Time.time - _penConfirmDownTime;
            if (duration >= _longPressThreshold && !_penConfirmLongPressTriggered)
            {
                OnButtonLongPress(RightHandButton.Confirm);
                _penConfirmLongPressTriggered = true;
            }
        }

        // 按钮释放时，根据是否已经触发过长按进行不同处理
        if (OVRInput.GetUp(OVRInput.Button.Two, OVRInput.Controller.RTouch))
        {
            if (_penConfirmLongPressTriggered)
            {
                // Stop movement after long press release
                MovementController.Instance.StopMoving();
            }
            else
            {
                OnButtonShortPress(RightHandButton.Confirm);
            }
        }

        // ----- Cancel (back cluster) processing using GetDown and GetUp -----
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
        {
            _penCancelDownTime = Time.time;
        }
        if (OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.RTouch))
        {
            float duration = Time.time - _penCancelDownTime;
            if (duration >= _longPressThreshold)
            {
                OnButtonLongPress(RightHandButton.Cancel);
            }
            else
            {
                OnButtonShortPress(RightHandButton.Cancel);
            }
        }

        // Retrieve tip pressure using controller's raw axis for stylus force
        float penTipPressure = OVRInput.Get(OVRInput.RawAxis1D.RStylusForce);
        if (penTipPressure > 0.4f)
        {
            // If pressure is detected for the first time, record the press start time
            if (!_tipWasPressed)
            {
                _tipWasPressed = true;
                _tipPressStartTime = Time.time;
            }
            // If the press duration exceeds the long press threshold, enter SustainedPress mode, calling OnPressureHoldState only once
            if (Time.time - _tipPressStartTime >= _longPressThreshold)
            {
                if (!_isTipSustainedActive)
                {
                    // Trigger tip long press, consistent with Confirm: begin free movement
                    OnPressureHoldState(RightHandButton.Tip, PressureState.SustainedPress, penTipPressure);
                    _isTipSustainedActive = true;
                }
            }
            else
            {
                // If long press threshold not reached, send sustained change state (if needed)
                OnPressureStateChanged(RightHandButton.Tip, PressureState.SustainedChange, penTipPressure);
            }
        }
        else
        {
            // When pressure falls below threshold and was previously pressed, detect as release
            if (_tipWasPressed)
            {
                float pressDuration = Time.time - _tipPressStartTime;
                if (pressDuration < _longPressThreshold)
                {
                    // Short press (tap) event
                    OnPressureStateChanged(RightHandButton.Tip, PressureState.Tap, penTipPressure);
                }
                // Send release notification
                OnPressureStateChanged(RightHandButton.Tip, PressureState.Release, 0f);
                // Stop movement (consistent with Confirm long press release)
                MovementController.Instance.StopMoving();
                // Reset flags
                _isTipSustainedActive = false;
                _tipWasPressed = false;
            }
        }

        // Retrieve middle button pressure using controller's raw axis for thumb rest force
        float penMiddlePressure = OVRInput.Get(OVRInput.RawAxis1D.RThumbRestForce);
        if (penMiddlePressure > 0.3f)
        {
            if (!_middleWasPressed)
            {
                _middleWasPressed = true;
            }
            OnPressureStateChanged(RightHandButton.Middle, PressureState.SustainedChange, penMiddlePressure);
        }
        else
        {
            if (_middleWasPressed)
            {
                OnPressureStateChanged(RightHandButton.Middle, PressureState.Release, penMiddlePressure);
                _middleWasPressed = false;
            }
        }
    }

    public void OnButtonShortPress(RightHandButton button)
    {
        Debug.Log("Short Press: " + button);
        
        if (button == RightHandButton.Confirm)
        {
            
            if (MovementController.Instance != null && MovementController.Instance.IsMoving)
            {
                MovementController.Instance.ToggleVerticalMode();
            }
            else if (SelectionManager.Instance != null && SelectionManager.Instance.isSelectionModeActive)
            {
                Vector3 penTipPos = GetPenTipPosition();
                SelectionManager.Instance.SelectInRange(penTipPos);
            }
            else if (SelectionManager.Instance == null || SelectionManager.Instance.selectedPoints.Count == 0)
            {
                ToolsManager.Instance.OnConfirmShortPressed();
            }
        }
        else if (button == RightHandButton.Cancel)
        {
            if (SelectionManager.Instance != null && SelectionManager.Instance.selectedPoints.Count > 0)
            {
                SelectionManager.Instance.Deselect();
            }
            else
            {
                ToolsManager.Instance.OnCancelShortPressed();
            }
        }
    }

    public void OnButtonLongPress(RightHandButton button)
    {
        Debug.Log("Long Press: " + button);
        if (button == RightHandButton.Confirm)
        {
            if (SelectionManager.Instance != null && SelectionManager.Instance.selectedPoints.Count > 0)
            {
                // If already in plane constraint (tip hold is active), enable vertical movement
                if (MovementController.Instance.IsPlaneConstraintActive)
                {
                    MovementController.Instance.EnableVerticalMode();
                }
                else
                {
                    MovementController.Instance.StartMoving();
                }
            }
        }
    }

    public void OnPressureStateChanged(RightHandButton button, PressureState state, float pressure)
    {
        //Debug.Log($"InkPenInputStrategy PressureStateChanged: {button}, State: {state}, Pressure: {pressure}");
        
        // The sustained press state for the tip is now handled by OnPressureHoldState; handle other states here
        if (button == RightHandButton.Tip && state == PressureState.Tap)
        {
            // Only create a new object if no points are selected
            if (SelectionManager.Instance == null || SelectionManager.Instance.selectedPoints.Count == 0)
            {
                ToolsManager.Instance.OnTipTapPressed();
            }
        }

        if (button == RightHandButton.Middle)
        {
            // When a sustained middle button press is detected, activate selection mode
            if (state == PressureState.SustainedChange)
            {
                Vector3 penTipPos = GetPenTipPosition();
                // Pass penMiddlePressure (i.e., the pressure parameter) to SelectionManager
                SelectionManager.Instance.SetSelectionModeActive(true, penTipPos, pressure);
            }
            // When the middle button is released, cancel selection mode
            else if (state == PressureState.Release)
            {
                Vector3 penTipPos = GetPenTipPosition();
                SelectionManager.Instance.SetSelectionModeActive(false, penTipPos, pressure);
            }
        }
    }

    public void Deinitialize()
    {
        Debug.Log("InkPenInputStrategy deinitialized");
        // Perform ink pen device cleanup as needed
    }

    public void OnPressureHoldState(RightHandButton button, PressureState state, float pressure)
    {
        Debug.Log($"InkPenInputStrategy OnPressureHoldState: {button}, State: {state}, Pressure: {pressure}");
        if (button == RightHandButton.Tip && SelectionManager.Instance != null && SelectionManager.Instance.selectedPoints.Count > 0)
        {
            MovementController.Instance.StartMoving();
            MovementController.Instance.SetPlaneConstraint(true);
            // If the last used mode was vertical, automatically enable vertical mode
            if (MovementController.Instance.LastUsedVerticalMode)
            {
                MovementController.Instance.EnableVerticalMode();
            }
        }
    }
}
