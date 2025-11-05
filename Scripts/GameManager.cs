using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // Singleton Pattern: Cho phép truy cập ở mọi nơi
    public static GameManager Instance { get; private set; }

    // Dữ liệu nhân vật được chọn
    public CharacterData player1SelectedData { get; private set; }
    public CharacterData player2SelectedData { get; private set; }

    // Mảng chứa TẤT CẢ các nhân vật có thể chọn trong game
    [Tooltip("Gán tất cả các file CharacterData (KosmosData, KenData...) vào đây")]
    public CharacterData[] availableCharacters;

    void Awake()
    {
        // --- Setup Singleton ---
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Đây là mấu chốt!
        }
        else
        {
            // Nếu đã có 1 GameManager, tự hủy
            Destroy(gameObject);
            return;
        }
    }

    // Hàm này được gọi bởi UI Chọn nhân vật (Scene 1)
    public void SelectCharacter(int playerIndex, CharacterData selectedData)
    {
        if (playerIndex == 0)
        {
            player1SelectedData = selectedData;
            Debug.Log($"Player 1 đã chọn: {selectedData.characterName}");
        }
        else
        {
            player2SelectedData = selectedData;
            Debug.Log($"Player 2 đã chọn: {selectedData.characterName}");
        }
    }

    // Hàm này được gọi khi nhấn nút "Start Game" (Scene 1)
    public void StartGame()
    {
        // Kiểm tra xem 2 người chơi đã chọn nhân vật chưa
        if (player1SelectedData == null || player2SelectedData == null)
        {
            Debug.LogError("Chưa chọn đủ nhân vật! Không thể bắt đầu game.");
            // TODO: Hiển thị thông báo lỗi trên UI
            return;
        }

        // Tải scene game chính (ví dụ: tên là "GameScene")
        // Đảm bảo bạn đã Add Scene này vào Build Settings!
        SceneManager.LoadScene("GameScene");
    }

    // Hàm này để quay lại màn hình chọn nhân vật
    public void BackToMenu()
    {
        player1SelectedData = null;
        player2SelectedData = null;
        SceneManager.LoadScene("CharacterSelectScene");
    }
}
