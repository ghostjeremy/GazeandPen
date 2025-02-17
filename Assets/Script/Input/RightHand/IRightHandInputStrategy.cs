public enum PressureState
{
    Tap,              // Short press (tap)
    SustainedChange,  // Sustained change (pressure value continuously changes)
    SustainedPress,   // Sustained press (pressure maintained at a high value)
    Release           // Release (transition from pressed state)
}

public interface IRightHandInputStrategy
{
    void Initialize();
    void UpdateInput();
    void Deinitialize();

    // (Deprecated) Handles boolean buttons: Confirm and Cancel; now only handles short press and long press
    
    // New: Handle short press events (button pushed and released quickly)
    void OnButtonShortPress(RightHandButton button);
    // New: Handle long press events (button held down beyond a threshold)
    void OnButtonLongPress(RightHandButton button);

    // Handle analog buttons: Tip and Middle; pressure range is typically 0 ~ 1.
    // Handles changes in pressure-sensitive input state.
    // Parameter "button" specifies which pressure-sensitive button (Tip or Middle).
    // "state" indicates the current state (Tap, SustainedChange, SustainedPress, Release).
    // "pressure" is the current pressure value.
    void OnPressureStateChanged(RightHandButton button, PressureState state, float pressure);

    // New: Handle the state when transitioning into dragging mode due to sustained press (called only once)
    // Called when pressure meets the long press criteria; enters dragging mode until released.
    void OnPressureHoldState(RightHandButton button, PressureState state, float pressure);
}
