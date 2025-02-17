using UnityEngine;

public class InkPenInputStrategy : IRightHandInputStrategy
{
    // Define action names consistent with VrStylusHandler
    private const string MX_Ink_TipForce = "tip";
    private const string MX_Ink_MiddleForce = "middle";
    private const string MX_Ink_ClusterFront = "front";
    private const string MX_Ink_ClusterBack = "back";
    
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

    public void Initialize()
    {
        Debug.Log("InkPenInputStrategy initialized");
        // Initialize the ink pen device and register events as needed
    }

    public void UpdateInput()
    {
        // Optional: Call OVRInput.Update() if OVRInput state needs updating
        // OVRInput.Update();
        
        // ----- Confirm (front cluster) processing -----
        bool penConfirm = false;
        if (!OVRPlugin.GetActionStateBoolean(MX_Ink_ClusterBack, out penConfirm))
        {
            Debug.LogError("InkPenInputStrategy: Failed to get Confirm input: " + MX_Ink_ClusterBack);
        }
        // Detect rising edge
        if (penConfirm && !_lastPenConfirm)
        {
            _penConfirmDownTime = Time.time;
            _penConfirmHeld = true;
            _penConfirmLongPressTriggered = false;
        }
        if (_penConfirmHeld)
        {
            if (penConfirm)
            {
                if (!_penConfirmLongPressTriggered && (Time.time - _penConfirmDownTime >= _longPressThreshold))
                {
                    OnButtonLongPress(RightHandButton.Confirm);
                    _penConfirmLongPressTriggered = true;
                }
            }
            if (!penConfirm && _lastPenConfirm)
            {
                if (!_penConfirmLongPressTriggered)
                {
                    OnButtonShortPress(RightHandButton.Confirm);
                }
                else
                {
                    // Stop moving when long press is released
                    MovementController.Instance.StopMoving();
                }
                _penConfirmHeld = false;
                _penConfirmLongPressTriggered = false;
            }
        }
        _lastPenConfirm = penConfirm;

        // ----- Cancel (back cluster) processing -----
        bool penCancel = false;
        if (!OVRPlugin.GetActionStateBoolean(MX_Ink_ClusterFront, out penCancel))
        {
            Debug.LogError("InkPenInputStrategy: Failed to get Cancel input: " + MX_Ink_ClusterFront);
        }
        if (penCancel && !_lastPenCancel)
        {
            _penCancelDownTime = Time.time;
            _penCancelHeld = true;
            _penCancelLongPressTriggered = false;
        }
        if (_penCancelHeld)
        {
            if (penCancel)
            {
                if (!_penCancelLongPressTriggered && (Time.time - _penCancelDownTime >= _longPressThreshold))
                {
                    OnButtonLongPress(RightHandButton.Cancel);
                    _penCancelLongPressTriggered = true;
                }
                else if (_penCancelLongPressTriggered)
                {
                    OnButtonLongPress(RightHandButton.Cancel);
                }
            }
            if (!penCancel && _lastPenCancel)
            {
                if (!_penCancelLongPressTriggered)
                {
                    // If any points are selected (regardless of selection mode), deselect all; otherwise, invoke default Cancel handling
                    if (SelectionManager.Instance != null && SelectionManager.Instance.selectedPoints.Count > 0)
                    {
                        SelectionManager.Instance.Deselect();
                    }
                    else
                    {
                        ToolsManager.Instance.OnCancelShortPressed();
                    }
                }
                _penCancelHeld = false;
                _penCancelLongPressTriggered = false;
            }
        }
        _lastPenCancel = penCancel;

        // Retrieve pen tip pressure (Tip Force)
        float penTipPressure = 0f;
        if (!OVRPlugin.GetActionStateFloat(MX_Ink_TipForce, out penTipPressure))
        {
            Debug.LogError("InkPenInputStrategy: Failed to get Confirm input: " + MX_Ink_TipForce);
        }
        if (penTipPressure > 0.3f)
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
                // If the long press threshold is not reached, send sustained change state (if needed)
                OnPressureStateChanged(RightHandButton.Tip, PressureState.SustainedChange, penTipPressure);
            }
        }
        else
        {
            // When pressure falls below the threshold and was previously pressed, treat it as a release
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
                // Stop moving (consistent with Confirm long press release)
                MovementController.Instance.StopMoving();
                // Reset flag
                _isTipSustainedActive = false;
                _tipWasPressed = false;
            }
        }

        // Retrieve middle button pressure (Middle Force)
        float penMiddlePressure = 0f;
        if (!OVRPlugin.GetActionStateFloat(MX_Ink_MiddleForce, out penMiddlePressure))
        {
            Debug.LogError("InkPenInputStrategy: Failed to get Cancel input: " + MX_Ink_MiddleForce);
        }
        if (penMiddlePressure > 0.1f)
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
        Debug.Log("InkPenInputStrategy Short Press: " + button);
        if (button == RightHandButton.Confirm)
        {
            // If in moving state (points selected and movement initiated), toggle mode
            if (MovementController.Instance != null && MovementController.Instance.IsMoving)
            {
                MovementController.Instance.ToggleVerticalMode();
            }
            else if (SelectionManager.Instance != null && SelectionManager.Instance.isSelectionModeActive)
            {
                // Enter selection mode
                VrStylusHandler vrStylus = UnityEngine.Object.FindObjectOfType<VrStylusHandler>();
                if (vrStylus != null)
                {
                    Vector3 penTipPos = vrStylus.CurrentState.inkingPose.position;
                    SelectionManager.Instance.SelectInRange(penTipPos);
                }
            }
            else if (SelectionManager.Instance == null || SelectionManager.Instance.selectedPoints.Count == 0)
            {
                ToolsManager.Instance.OnConfirmShortPressed();
            }
        }
        else if (button == RightHandButton.Cancel)
        {
            // If any points are selected (regardless of selection mode), deselect all; otherwise, invoke default Cancel handling
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
        Debug.Log("InkPenInputStrategy Long Press: " + button);
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
                VrStylusHandler vrStylus = UnityEngine.Object.FindObjectOfType<VrStylusHandler>();
                if (vrStylus != null)
                {
                    Vector3 penTipPos = vrStylus.CurrentState.inkingPose.position;
                    // Pass penMiddlePressure (i.e., the pressure parameter) to SelectionManager
                    SelectionManager.Instance.SetSelectionModeActive(true, penTipPos, pressure);
                }
            }
            // When the middle button is released, cancel selection mode
            else if (state == PressureState.Release)
            {
                VrStylusHandler vrStylus = UnityEngine.Object.FindObjectOfType<VrStylusHandler>();
                Vector3 penTipPos = vrStylus != null ? vrStylus.CurrentState.inkingPose.position : Vector3.zero;
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
