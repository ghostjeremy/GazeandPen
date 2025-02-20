using UnityEngine;

public class HomePageController : MonoBehaviour
{
    // Called when the user clicks the test task button
    public void OnFreeTaskButtonClicked()
    {
        // Switch to the test task scene
        GameManager.Instance.SwitchTask("TestTask");
    }
}

