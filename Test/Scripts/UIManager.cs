using UnityEngine;
using UnityEngine.UI;
using TMPro;

// LƯU Ý: Đây chỉ là ví dụ để bạn hình dung cách xử lý UI
// Trong ứng dụng thực tế, bạn cần triển khai kiến trúc Singleton cho UIManager
// và kết nối các tham chiếu UI (health bars, text, v.v.) qua Inspector.

public class UIManager : MonoBehaviour
{
    // Tham chiếu đến thanh máu của Người chơi 1
    [Header("Player 1 UI (Fill Origin: Right)")]
    [Tooltip("Image Component của Health Bar P1. Fill Origin phải là Right.")]
    [SerializeField] private Image healthBarP1;

    // Tham chiếu đến thanh máu của Người chơi 2
    [Header("Player 2 UI (Fill Origin: Left)")]
    [Tooltip("Image Component của Health Bar P2. Fill Origin phải là Left.")]
    [SerializeField] private Image healthBarP2;

    [Header("Game Over UI Modal")]
    [Tooltip("Panel cha của màn hình Game Over.")]
    [SerializeField] private GameObject gameOverPanel;
    [Tooltip("Text để hiển thị người chiến thắng.")]
    [SerializeField] private TMP_Text winnerText;
    [Tooltip("Button để quay lại màn hình chọn nhân vật.")]
    [SerializeField] private Button backToSelectButton;

    public static UIManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (backToSelectButton != null)
        {
            backToSelectButton.onClick.AddListener(OnBackToSelectClicked);
        }
        else
        {
            Debug.LogError("BackToSelectButton chưa được gán trong UIManager!");
        }
    }

    public void InitializeHealthBar(float maxHP_P1, float maxHP_P2)
    {
        UpdateHealth(0, 1.0f);
        UpdateHealth(1, 1.0f);
        Debug.Log("UIManager: Health Bars Initialized to full.");
    }
    public void UpdateHealth(int playerIndex, float healthRatio)
    {
        healthRatio = Mathf.Clamp01(healthRatio);

        if (playerIndex == 0)
        {
            if (healthBarP1 != null)
            {
                healthBarP1.fillAmount = healthRatio;
            }
        }
        else if (playerIndex == 1)
        {
            if (healthBarP2 != null)
            {
                healthBarP2.fillAmount = healthRatio;
            }
        }
    }
    private void OnBackToSelectClicked()
    {
        if (GameSceneManager.Instance != null)
        {
            GameSceneManager.Instance.GoToCharacterSelect();
        }
    }

    public void ShowGameOverScreen(int winnerPlayerIndex)
    {
        if (gameOverPanel == null || winnerText == null)
        {
            Debug.LogError("Chưa gán Panel/Text cho Game Over Modal.");
            return;
        }

        string winnerName = (winnerPlayerIndex == 0) ? "Player 1" : "Player 2";
        winnerText.text = $"{winnerName} Win!";

        gameOverPanel.SetActive(true);

        Debug.Log($"UI: Đã hiển thị màn hình Game Over. {winnerName} WIN!");
    }
}