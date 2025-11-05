using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneControll : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    //Exit the game
    public void ExitGame()
    {
        Console.WriteLine("Game exited");
        Debug.Log("Game exited");
        Application.Quit();
        
    }
}
