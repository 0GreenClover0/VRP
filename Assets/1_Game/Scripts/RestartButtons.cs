using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartButtons : MonoBehaviour
{
    private List<string> gameSceneNames = new List<string>();

    private void Awake()
    {
        gameSceneNames = new List<string>() {"Game_Blockout"};
    }

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
        string randomLevel = gameSceneNames[Random.Range(0, gameSceneNames.Count)];

        SceneManager.LoadSceneAsync(randomLevel);
    }
}
