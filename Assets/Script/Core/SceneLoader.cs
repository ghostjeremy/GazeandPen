using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    private void OnEnable()
    {
        EventBus.Subscribe("OnSwitchTask", OnSwitchTask);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe("OnSwitchTask", OnSwitchTask);
    }

    private void OnSwitchTask(object taskObj)
    {
        if (taskObj is string taskName && !string.IsNullOrEmpty(taskName))
        {
            Debug.Log("Switching scene to: " + taskName);
            SceneManager.LoadScene(taskName);
        }
        else
        {
            Debug.LogWarning("Switching scene failed, invalid task name!");
        }
    }
}
