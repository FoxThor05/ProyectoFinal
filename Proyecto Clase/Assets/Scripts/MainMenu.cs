using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public void StartGame()
    {
        GameManager.Instance.StartNewGame();
    }

    public void QuitGame()
    {
        GameManager.Instance.QuitGame();
    }

    public void OpenSettings()
    {
        GameManager.Instance.OpenSettings();
    }

    public void OpenLogin()
    {
        GameManager.Instance.OpenLoginMenu();
    }
}
