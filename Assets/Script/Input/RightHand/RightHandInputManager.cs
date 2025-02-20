using UnityEngine;

public class RightHandInputManager : MonoBehaviour
{
    private IRightHandInputStrategy currentStrategy;

    [SerializeField]
    private bool useQuest3AtStart = false; // Set initial input device in Inspector

    // Expose the input mode for external checks
    public bool UseQuest3AtStart => useQuest3AtStart;

    private void Start()
    {
        if (useQuest3AtStart)
            SwitchToPen();
        else
            SwitchToController();
    }

    private void Update()
    {
        currentStrategy?.UpdateInput();
    }

    public void SwitchToPen()
    {
        currentStrategy?.Deinitialize();
        currentStrategy = new InkPenInputStrategy();
        currentStrategy.Initialize();
    }

    public void SwitchToController()
    {
        currentStrategy?.Deinitialize();
        currentStrategy = new QProControllerInputStrategy();
        currentStrategy.Initialize();
    }
}
