using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class StageSelectUI : MonoBehaviour
{
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button testStageButton;

    private string stageName;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("Cần có GameManager trong scene này!");
            return;
        }

        testStageButton.onClick.AddListener(testStage);
        startGameButton.onClick.AddListener(OnStartGame);
    }

    void testStage()
    {
        stageName = "TestStage";
        GameManager.Instance.OnSelectStage(stageName);
    }

    void OnStartGame()
    {
        GameManager.Instance.StartGame();
    }

    // Update is called once per frame
    void Update()
    {

    }
}
