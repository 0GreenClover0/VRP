using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartButtons : MonoBehaviour
{
    // Do not change this name.
    public void RestartLevel()
    {
        SceneManager.LoadSceneAsync("Game_Blockout");
    }

    public void RestartGame()
    {
        SceneManager.LoadSceneAsync("Menu");
    }

    public void RandomLevel()
    {
        string randomLevel = GameManager.Instance.gameSceneNames[Random.Range(0, GameManager.Instance.gameSceneNames.Count)];

        SceneManager.LoadSceneAsync(randomLevel);
    }
}
