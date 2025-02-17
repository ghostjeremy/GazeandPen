using UnityEngine;

public class QProControllerInputStrategy : IRightHandInputStrategy
{
    // Used for tracking Confirm button state
    private float _confirmDownTime = 0f;
    private bool _confirmHeld = false;
    private bool _confirmLongPressTriggered = false;

    // Used for tracking Cancel button state
    private float _cancelDownTime = 0f;
    private bool _cancelHeld = false;
    private bool _cancelLongPressTriggered = false;

    // Long press threshold (in seconds)
    private float _longPressThreshold = 0.5f;

    // New: Marks whether Tip sustained pressure hold state has been entered to prevent continuous updates
    private bool _isPressureHoldActive = false;

    public void Initialize()
    {
        // No explicit initialization is needed for OVRInput.
        Debug.Log("QProControllerInputStrategy using OVRInput for Meta Quest Touch Controller Pro.");
    }

    public void UpdateInput()
    {
        // ----- Confirm button processing -----
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
        {
            _confirmDownTime = Time.time;
            _confirmHeld = true;
            _confirmLongPressTriggered = false;
        }
        if (_confirmHeld)
        {
            if (OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.RTouch))
            {
                if (!_confirmLongPressTriggered && (Time.time - _confirmDownTime >= _longPressThreshold))
                {
                    OnButtonLongPress(RightHandButton.Confirm);
                    _confirmLongPressTriggered = true;
                }
                else if (_confirmLongPressTriggered)
                {
                    // Optional: Call long press event every frame during long press
                    OnButtonLongPress(RightHandButton.Confirm);
                }
            }
            if (OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.RTouch))
            {
                if (!_confirmLongPressTriggered)
                {
                    OnButtonShortPress(RightHandButton.Confirm);
                }
                _confirmHeld = false;
                _confirmLongPressTriggered = false;
            }
        }

        // ----- Cancel button processing -----
        if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
        {
            _cancelDownTime = Time.time;
            _cancelHeld = true;
            _cancelLongPressTriggered = false;
        }
        if (_cancelHeld)
        {
            if (OVRInput.Get(OVRInput.Button.Two, OVRInput.Controller.RTouch))
            {
                if (!_cancelLongPressTriggered && (Time.time - _cancelDownTime >= _longPressThreshold))
                {
                    OnButtonLongPress(RightHandButton.Cancel);
                    _cancelLongPressTriggered = true;
                }
                else if (_cancelLongPressTriggered)
                {
                    OnButtonLongPress(RightHandButton.Cancel);
                }
            }
            if (OVRInput.GetUp(OVRInput.Button.Two, OVRInput.Controller.RTouch))
            {
                if (!_cancelLongPressTriggered)
                {
                    OnButtonShortPress(RightHandButton.Cancel);
                }
                _cancelHeld = false;
                _cancelLongPressTriggered = false;
            }
        }

        // Tip button (analog input) -> Using trigger analog input.
        float tipValue = OVRInput.Get(OVRInput.RawAxis1D.RStylusForce, OVRInput.Controller.RTouch);
        if (tipValue > 0.1f)
        {
            // 简单示例：将状态设为 SustainedChange
            OnPressureStateChanged(RightHandButton.Tip, PressureState.SustainedChange, tipValue);
        }

        // Middle button (analog input) -> Using grip analog input.
        float middleValue = OVRInput.Get(OVRInput.RawAxis1D.RThumbRestForce, OVRInput.Controller.RTouch);
        if (middleValue > 0.1f)
        {
            OnPressureStateChanged(RightHandButton.Middle, PressureState.SustainedChange, middleValue);
        }
    }

    // Removed OnButtonPressed method; only short press and long press events are handled
    public void OnButtonShortPress(RightHandButton button)
    {
        if (button == RightHandButton.Confirm)
        {
            Debug.Log("QProControllerInputStrategy Short Press Confirm");
            ToolsManager.Instance.OnConfirmShortPressed();
        }
        else if (button == RightHandButton.Cancel)
        {
            Debug.Log("QProControllerInputStrategy Short Press Cancel");
            // 可添加对应 Cancel 逻辑
        }
    }

    public void OnButtonLongPress(RightHandButton button)
    {
        if (button == RightHandButton.Confirm)
        {
            Debug.Log("QProControllerInputStrategy Long Press Confirm - Ignored for point creation.");
            // Do not create points on long press; point creation is triggered only by short press.
        }
        else if (button == RightHandButton.Cancel)
        {
            Debug.Log("QProControllerInputStrategy Long Press Cancel");
            // 可添加 Cancel 对应的其他逻辑
        }
    }

    public void OnPressureStateChanged(RightHandButton button, PressureState state, float pressure)
    {
        //Debug.Log($"QProControllerInputStrategy PressureStateChanged: {button}, State: {state}, Pressure: {pressure}");
        // Retain the original handling logic as needed, for example:
        if (button == RightHandButton.Tip && SelectionManager.Instance != null && SelectionManager.Instance.selectedPoints.Count > 0)
        {
            if (state == PressureState.Release && _isPressureHoldActive)
            {
                MovementController.Instance.SetPlaneConstraint(false);
                _isPressureHoldActive = false;
            }
        }
    }

    // New: Handle sustained press (Hold) to enter dragging mode; called only once
    // when pressure meets the long press criteria; enters dragging mode (plane movement) until released.
    public void OnPressureHoldState(RightHandButton button, PressureState state, float pressure)
    {
        Debug.Log($"QProControllerInputStrategy OnPressureHoldState: {button}, State: {state}, Pressure: {pressure}");
        if (button == RightHandButton.Tip && SelectionManager.Instance != null && SelectionManager.Instance.selectedPoints.Count > 0)
        {
            // 进入拖拽模式（平面移动），此处只执行一次
            MovementController.Instance.SetPlaneConstraint(true);
            _isPressureHoldActive = true;
        }
    }

    public void Deinitialize()
    {
        Debug.Log("QProControllerInputStrategy deinitialized");
    }
}
