using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{                       
    public void PlayGame()
    {
        SceneManager.LoadScene("Game");
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void OpenLeaderboard()
    {
        LeaderboardScreen.Show();
    }
}
