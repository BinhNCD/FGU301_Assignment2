using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Script này gắn vào Canvas của Scene Chọn Nhân Vật
public class CharacterSelectUI : MonoBehaviour
{
    [Header("Player 1 UI")]
    [SerializeField] private TMP_Text p1_NameText;
    [SerializeField] private Button p1_NextButton;
    [SerializeField] private Button p1_PrevButton;

    [Header("Player 2 UI")]
    [SerializeField] private TMP_Text p2_NameText;
    [SerializeField] private Button p2_NextButton;
    [SerializeField] private Button p2_PrevButton;

    [Header("General")]
    [SerializeField] private Button startGameButton;

    private int p1_currentIndex = 0;
    private int p2_currentIndex = 0;

    private CharacterData[] characters;

    void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("Cần có GameManager trong scene này!");
            return;
        }

        // Lấy danh sách nhân vật từ GameManager
        characters = GameManager.Instance.availableCharacters;
        if (characters == null || characters.Length == 0)
        {
            Debug.LogError("GameManager chưa có danh sách 'Available Characters'!");
            return;
        }

        // Gán sự kiện cho các nút
        p1_NextButton.onClick.AddListener(P1_Next);
        p1_PrevButton.onClick.AddListener(P1_Prev);

        p2_NextButton.onClick.AddListener(P2_Next);
        p2_PrevButton.onClick.AddListener(P2_Prev);

        startGameButton.onClick.AddListener(OnStartGame);

        // Cập nhật UI lần đầu
        UpdateUI(0); // Cập nhật P1
        UpdateUI(1); // Cập nhật P2
    }

    private void P1_Next() { p1_currentIndex = (p1_currentIndex + 1) % characters.Length; UpdateUI(0); }
    private void P1_Prev() { p1_currentIndex = (p1_currentIndex - 1 + characters.Length) % characters.Length; UpdateUI(0); }

    private void P2_Next() { p2_currentIndex = (p2_currentIndex + 1) % characters.Length; UpdateUI(1); }
    private void P2_Prev() { p2_currentIndex = (p2_currentIndex - 1 + characters.Length) % characters.Length; UpdateUI(1); }

    // Cập nhật tên và gửi lựa chọn đến GameManager
    private void UpdateUI(int playerIndex)
    {
        if (playerIndex == 0)
        {
            CharacterData selected = characters[p1_currentIndex];
            p1_NameText.text = selected.characterName;
            GameManager.Instance.SelectCharacter(0, selected);
        }
        else
        {
            CharacterData selected = characters[p2_currentIndex];
            p2_NameText.text = selected.characterName;
            GameManager.Instance.SelectCharacter(1, selected);
        }
    }

    private void OnStartGame()
    {
        GameManager.Instance.StartGame();
    }
}
