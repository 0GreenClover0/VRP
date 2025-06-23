using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartButtons : MonoBehaviour
{
    public void RestartLevel()
    {
        SceneManager.LoadSceneAsync("Game_Blockout");
    }

    public void RestartGame()
    {
    }
}
