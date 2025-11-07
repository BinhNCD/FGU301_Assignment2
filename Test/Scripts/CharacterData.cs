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

    [Tooltip("Thời gian (giây) hitbox này được kích hoạt trước khi tự động tắt.")]
    public float hitActiveDuration;
    [Tooltip("Vị trí offset của hitbox")]
    public float offsetX;
    public float offsetY;
    [Tooltip("Độ rộng của hitbox")]
    public float sizeX;
    public float sizeY;
}

// Đây là struct định nghĩa một KỸ NĂNG hoặc COMBO (một chuỗi các đòn đánh liên tục/hitbox)
[System.Serializable]
public struct SkillAttackData
{
    [Tooltip("Tên Kỹ năng (chỉ để dễ quản lý trong Editor)")]
    public string skillName;

    [Tooltip("Tên của Animation Trigger CHUNG cho toàn bộ Skill/Combo. Ví dụ: 'Hadouken'")]
    public string skillAnimationTriggerName;

    [Tooltip("Thời gian hồi chiêu (Cooldown) của Kỹ năng này (giây)")]
    public float cooldownTime;

    [Tooltip("Mảng các đòn tấn công (hitbox) tạo nên Skill này. " +
             "Mỗi phần tử trong mảng đại diện cho một hitbox/phase riêng biệt của Skill.")]
    public AttackData[] attackSequence;
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
    public float maxHealth = 1000f;

    [Header("--- COMBAT: Đòn đánh Thường ---")]
    [Tooltip("Danh sách đòn đánh thường trên MẶT ĐẤT. " +
             "Dùng để tạo combo N-đòn khi ở trên đất (Ví dụ: Đòn 1, Đòn 2, Đòn 3).")]
    public AttackData[] groundNormalAttacks;

    [Tooltip("Danh sách đòn đánh thường trên KHÔNG. " +
             "Dùng để tạo combo N-đòn khi ở trên không.")]
    public AttackData[] airNormalAttacks;

    [Header("--- COMBAT: Kỹ năng (Skills) ---")]
    [Tooltip("Danh sách các KỸ NĂNG/COMBO KỸ NĂNG. " +
             "Mỗi Kỹ năng chứa một chuỗi hitbox riêng biệt.")]
    public SkillAttackData[] skills;
}
