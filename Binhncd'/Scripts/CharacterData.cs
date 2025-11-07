using UnityEngine;

// Đây là struct để định nghĩa một đòn tấn công
[System.Serializable]
public struct AttackData
{
    [Tooltip("Tên của Animation Trigger trong Animator. Ví dụ: 'Attack1', 'Hadouken'")]
    public string animationTriggerName;
    [Tooltip("Sát thương của đòn này")]
    public float damage;
    [Tooltip("Lực đẩy ngang (thrust) tác dụng lên BẢN THÂN khi ra đòn")]
    public float attackThrust;
    [Tooltip("Vị trí offset của hitbox")]
    public float offsetX;
    public float offsetY;
    [Tooltip("Độ rộng của hitbox")]
    public float sizeX;
    public float sizeY;
}

// Đây là file ScriptableObject chính
// Right-click trong Project -> Create -> Game/Character Data
[CreateAssetMenu(fileName = "NewCharacterData", menuName = "Game/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Info")]
    public string characterName = "New Character";

    [Header("Visuals & Animations")]
    [Tooltip("Prefab của nhân vật (bao gồm model, sprite...)")]
    public GameObject characterPrefab;

    [Tooltip("Bộ Animation Override cho nhân vật này")]
    public AnimatorOverrideController animatorOverride;

    [Header("Stats (Giống nhau cho mọi nhân vật)")]

    public float moveSpeed = 10f;
    public float jumpForce = 30f;
    public float maxHealth = 100f;

    [Header("Combat (Khác nhau)")]
    [Tooltip("Danh sách các đòn tấn công theo thứ tự combo. " +
             "Số lượng combo KHÔNG cố định, tùy vào độ dài của mảng này.")]
    public AttackData[] comboAttacks;
}
