using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Current task (e.g., HomePage, FreeTask, TaskTask, etc.)
    public string CurrentTask { get; private set; } = "HomePage";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeGame();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeGame()
    {
        Debug.Log("GameManager initialization complete");
        // Initialize global settings etc.
    }

    // Switch task and publish task switch event
    public void SwitchTask(string newTask)
    {
        CurrentTask = newTask;
        EventBus.Publish("OnSwitchTask", newTask);
    }
}
