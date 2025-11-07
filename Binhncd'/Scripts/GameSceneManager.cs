using UnityEngine;
using UnityEngine.SceneManagement;

// Script này nằm trong Scene Game chính ("GameScene")
// Nhiệm vụ: Đọc lựa chọn từ GameManager và tạo (spawn) nhân vật
public class GameSceneManager : MonoBehaviour
{
    [Header("Spawn Points")]
    [SerializeField] private Transform player1SpawnPoint;
    [SerializeField] private Transform player2SpawnPoint;

    [Header("Player Layers")]
    [Tooltip("Layer của Player 1 (ví dụ: 'Player1')")]
    [SerializeField] private LayerMask player1Layer;
    [Tooltip("Layer của Player 2 (ví dụ: 'Player2')")]
    [SerializeField] private LayerMask player2Layer;

    [Header("Map")]
    [SerializeField] private string stageName; // Tên stage để load

    void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("Không tìm thấy GameManager! Bạn có đang chạy từ GameScene không?" +
                           " Hãy chạy từ CharacterSelectScene.");
            // TODO: Có thể thêm code để tải lại scene chọn nhân vật
            return;
        }

        loadStage();
        SpawnPlayers();
    }

    //load stage trong folder Stages cua Scene
    void loadStage()
    {
        string stage = GameManager.Instance.selectedStageName;

        if (string.IsNullOrEmpty(stage))
        {
            Debug.LogError("Stage name is null. Did you click test/select stage?");
            return;
        }

        //load the stage
        SceneManager.LoadScene(stage, LoadSceneMode.Additive);
        Debug.Log("Stage loaded: " + stage);
    }

    void SpawnPlayers()
    {
        // 1. Lấy dữ liệu đã chọn
        CharacterData p1Data = GameManager.Instance.player1SelectedData;
        CharacterData p2Data = GameManager.Instance.player2SelectedData;

        if (p1Data == null || p2Data == null)
        {
            Debug.LogError("Dữ liệu nhân vật bị thiếu!");
            return;
        }

        // 2. Tạo Player 1
        // Lấy Layer của P1 và P2 từ LayerMask
        int p1LayerValue = GetLayerFromMask(player1Layer);
        int p2LayerValue = GetLayerFromMask(player2Layer);

        // Tạo P1
        GameObject p1_GO = Instantiate(p1Data.characterPrefab, player1SpawnPoint.position, Quaternion.identity);
        p1_GO.name = $"Player 1 ({p1Data.characterName})";
        p1_GO.layer = p1LayerValue; // Gán layer cho P1

        // Cấu hình P1
        PlayerController p1_Controller = p1_GO.GetComponent<PlayerController>();
        if (p1_Controller != null)
        {
            // Gán dữ liệu cho P1, và nói cho P1 biết "đối thủ" là Layer P2
            p1_Controller.Initialize(p1Data, 0, player2Layer);
        }

        // Cấu hình Input cho P1
        ConfigurePlayerInput(p1_GO, "Player1"); // "Player1" là tên Control Scheme


        // 3. Tạo Player 2
        GameObject p2_GO = Instantiate(p2Data.characterPrefab, player2SpawnPoint.position, Quaternion.identity);
        p2_GO.name = $"Player 2 ({p2Data.characterName})";
        p2_GO.layer = p2LayerValue; // Gán layer cho P2

        // Cấu hình P2
        PlayerController p2_Controller = p2_GO.GetComponent<PlayerController>();
        if (p2_Controller != null)
        {
            // Gán dữ liệu cho P2, và nói cho P2 biết "đối thủ" là Layer P1
            p2_Controller.Initialize(p2Data, 1, player1Layer);
        }

        // Cấu hình Input cho P2
        ConfigurePlayerInput(p2_GO, "Player2"); // "Player2" là tên Control Scheme
    }

    // Hàm này tìm PlayerInput và gán đúng Control Scheme
    private void ConfigurePlayerInput(GameObject playerGO, string schemeName)
    {
        var playerInput = playerGO.GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (playerInput != null)
        {
            playerInput.SwitchCurrentControlScheme(schemeName, UnityEngine.InputSystem.Keyboard.current);
        }
        else
        {
            Debug.LogError($"Không tìm thấy PlayerInput component trên prefab {playerGO.name}!");
        }
    }

    // Hàm tiện ích để lấy số (int) của Layer từ LayerMask
    private int GetLayerFromMask(LayerMask mask)
    {
        int value = mask.value;
        for (int i = 0; i < 32; i++)
        {
            if ((value & (1 << i)) != 0)
            {
                return i;
            }
        }
        return 0; // Trả về Default nếu không tìm thấy
    }
}
