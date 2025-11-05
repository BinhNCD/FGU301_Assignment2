using UnityEngine;
using System.Collections; // Cần cho Coroutines (như Hit Stop)
using UnityEngine.InputSystem; // Cần thiết cho CallbackContext nếu dùng Send Messages

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(HealthComponent))]
public class PlayerController : MonoBehaviour
{
    // --- DỮ LIỆU NHÂN VẬT ---
    // Đây là biến quan trọng nhất.
    // Hệ thống bên ngoài (GameSceneManager) sẽ gán CharacterData vào đây.
    public CharacterData characterData;

    // --- CÁC THÀNH PHẦN (COMPONENTS) ---
    private Rigidbody2D rb;
    private Animator anim;
    private HealthComponent health;
    private BoxCollider2D meleeHitbox;
    private HitboxTrigger hitboxTrigger;

    // --- CẤU HÌNH INPUT & PLAYER ---
    [HideInInspector] public int playerIndex;
    [HideInInspector] public LayerMask targetLayer; // Layer của đối thủ

    // --- TRẠNG THÁI (STATE) ---
    private Vector2 movementInput = Vector2.zero;
    private Vector2 lastMoveInput = Vector2.zero;
    private bool isFacingRight = true;
    private bool isGrounded = true;
    private bool isGuarding = false;
    private bool isAttacking = false;
    private bool isStunned = false; // Trạng thái khi bị dính đòn

    // --- COMBAT ---
    private int comboStep = 0;
    private float comboTimer = 0f;
    private float lastAttackTime = 0f;

    // --- CONSTANTS (HẰNG SỐ) ---
    private const float GROUND_CHECK_DISTANCE = 0.5f;
    private const float COMBO_WINDOW_TIME = 0.4f; // Thời gian chờ giữa các đòn combo
    private const float ATTACK_DEBOUNCE_DELAY = 0.2f; // Chống spam attack

    // --- THAM CHIẾU (REFERENCES) ---
    [Tooltip("Transform chỉ ra vị trí để kiểm tra mặt đất (Dưới chân nhân vật).")]
    [SerializeField] private Transform groundCheckPoint;
    [Tooltip("Layer nào được coi là mặt đất.")]
    [SerializeField] private LayerMask groundLayer;
    [Tooltip("BoxCollider2D của Hitbox con duy nhất.")]
    [SerializeField] private BoxCollider2D childMeleeHitbox;

    #region Khởi tạo và Thiết lập

    // Awake được gọi ngay cả khi script bị disable
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        health = GetComponent<HealthComponent>(); // Lấy HealthComponent
    }

    // Hàm này sẽ được gọi TỪ BÊN NGOÀI (Bởi GameSceneManager)
    // ngay sau khi nhân vật được tạo ra.
    public void Initialize(CharacterData data, int index, LayerMask opponentLayer)
    {
        this.characterData = data;
        this.playerIndex = index;
        this.targetLayer = opponentLayer;

        if (characterData == null)
        {
            Debug.LogError($"Player {playerIndex}: CharacterData bị thiếu!");
            return;
        }

        // 1. Áp dụng chỉ số
        // (Lấy từ HealthComponent thay vì CharacterData để đồng bộ)
        health.Initialize(characterData.maxHealth, this);

        // 2. Áp dụng Animations
        if (characterData.animatorOverride != null)
        {
            anim.runtimeAnimatorController = characterData.animatorOverride;
        }
        else
        {
            Debug.LogWarning($"Player {playerIndex}: Không tìm thấy AnimatorOverrideController.");
        }

        // 3. Thiết lập Hitbox
        InitializeHitboxes();

        // 4. Thiết lập hướng ban đầu (P1 bên trái, P2 bên phải)
        if (playerIndex == 1)
        {
            Flip();
        }

        Debug.Log($"P{playerIndex + 1} ({characterData.characterName}) đã được khởi tạo.");
    }

    private void InitializeHitboxes()
    {
        // Sử dụng tham chiếu đã gán từ Inspector
        meleeHitbox = childMeleeHitbox;

        if (meleeHitbox == null)
        {
            Debug.LogError($"FATAL ERROR: Melee Hitbox Collider chưa được gán trong Inspector cho Player {playerIndex}!");
            return;
        }

        hitboxTrigger = meleeHitbox.GetComponent<HitboxTrigger>();
        if (hitboxTrigger == null)
        {
            Debug.LogError("HitboxTrigger.cs script is missing on the meleeHitbox Collider!");
            return;
        }

        meleeHitbox.enabled = false;
    }

    #endregion

    #region Vòng lặp Update

    void Update()
    {
        isGrounded = IsGrounded();
        Debug.Log($"P{playerIndex + 1} isGrounded: {isGrounded}");

        // Đếm ngược thời gian combo
        if (comboTimer > 0)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0 && !isAttacking)
            {
                ResetCombo();
                Debug.Log("Combo timed out. Resetting combo_step.");
            }
        }
    }

    void FixedUpdate()
    {
        if (isStunned || characterData == null) return;

        // Xử lý di chuyển
        HandleMovement();

        // Xử lý lật mặt
        HandleFlip();

        // Cập nhật Animator
        UpdateAnimationParameters();
    }

    private void HandleMovement()
    {
        float currentHorizontalInput = movementInput.x;
        float actualMoveSpeed = characterData.moveSpeed;

        // Không di chuyển khi đang đỡ hoặc tấn công
        if (isGuarding || isAttacking)
        {
            actualMoveSpeed *= 0f;
        }

        // Áp dụng di chuyển
        if (!isAttacking)
        {
            Vector2 movement = new Vector2(currentHorizontalInput * actualMoveSpeed, rb.linearVelocity.y);
            rb.linearVelocity = movement;
        }
    }

    private void HandleFlip()
    {
        // Chỉ lật mặt khi không tấn công hoặc đỡ đòn
        if (!isAttacking)
        {
            if (movementInput.x > 0 && !isFacingRight)
            {
                Flip();
                Debug.Log("[FLIP] Is Flip");
            }
            else if (movementInput.x < 0 && isFacingRight)
            {
                Flip();
                Debug.Log("[FLIP] Is Flip");
            }
        }
    }

    private void UpdateAnimationParameters()
    {
        anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        anim.SetBool("IsJumping", !isGrounded);
        anim.SetFloat("VerticalSpeed", rb.linearVelocity.y);
        anim.SetBool("IsGuarding", isGuarding);
    }

    #endregion

    #region Xử lý Input (Được gọi từ PlayerInputHandler.cs)

    // Hàm này được gọi bởi PlayerInputHandler
    public void SetMoveInput(Vector2 input)
    {
        // DEBUG: Nhận input di chuyển
        Debug.Log($"P{playerIndex + 1} [INPUT-MOVE] Nhận: {input}. Trạng thái hiện tại: Attacking={isAttacking}, Guarding={isGuarding}, Stunned={isStunned}");
        lastMoveInput = input;

        if (isStunned)
        {
            movementInput = Vector2.zero;
            Debug.LogWarning($"P{playerIndex + 1} [INPUT-MOVE] BỊ CHẶN: Đang Stunned.");
            return;
        }

        if (isAttacking)
        {
            movementInput = Vector2.zero;
            // Di chuyển bị chặn nhưng không cần warning vì đây là logic game.
        }
        else
        {
            movementInput = input;
        }
    }

    // Hàm này được gọi bởi PlayerInputHandler
    public void OnJumpPressed()
    {
        // DEBUG: Nhận input Jump
        Debug.Log($"P{playerIndex + 1} [INPUT-JUMP] Nhấn Jump. Trạng thái hiện tại: Attacking={isAttacking}, Guarding={isGuarding}, Stunned={isStunned}, Grounded={isGrounded}");

        if (isGuarding || isAttacking || isStunned || !isGrounded)
        {
            string reason = isStunned ? "Stunned" : (isGuarding ? "Guarding" : (isAttacking ? "Attacking" : (!isGrounded ? "Not Grounded" : "Unknown")));
            Debug.LogWarning($"P{playerIndex + 1} [INPUT-JUMP] BỊ CHẶN. Lý do: {reason}.");
            return;
        }

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, characterData.jumpForce);
        anim.SetTrigger("Jump");

        Debug.Log($"P{playerIndex + 1} [INPUT-JUMP] KÍCH HOẠT Nhảy. Lực đẩy: {characterData.jumpForce}.");
    }

    // Hàm này được gọi bởi PlayerInputHandler
    public void OnAttackPressed()
    {
        // DEBUG: Nhận input Attack
        Debug.Log($"P{playerIndex + 1} [INPUT-ATTACK] Nhấn Attack. Trạng thái hiện tại: Attacking={isAttacking}, Guarding={isGuarding}, Stunned={isStunned}, ComboTimer={comboTimer:F2}.");

        if (characterData.comboAttacks == null || characterData.comboAttacks.Length < 2) return;

        // Check các điều kiện không thể tấn công
        if (isStunned || isGuarding || !isGrounded)
        {
            string reason = isStunned ? "Stunned" : (isGuarding ? "Guarding" : (!isGrounded ? "Not Grounded" : "Unknown"));
            Debug.LogWarning($"P{playerIndex + 1} [INPUT-ATTACK] BỊ CHẶN. Lý do: {reason}.");
            return;
        }

        // Check debounce (tránh spam)
        if (Time.time < lastAttackTime + ATTACK_DEBOUNCE_DELAY)
        {
            Debug.LogWarning($"P{playerIndex + 1} [INPUT-ATTACK] BỊ CHẶN: Debounce ({ATTACK_DEBOUNCE_DELAY:F2}s) chưa hết.");
            return;
        }

        // Xử lý Reset Combo nếu quá thời gian
        if (comboStep > 0 && comboTimer <= 0)
        {
            ResetCombo();
        }

        int nextComboStep =  comboStep + 1;

        // Kiểm tra xem combo có thể tiếp tục không
        if (nextComboStep > characterData.comboAttacks.Length - 1)
        {
            nextComboStep = 1;
        }

        // Nếu hợp lệ, cập nhật bước combo
        comboStep = nextComboStep;

        // Lấy dữ liệu đòn đánh hiện tại (trừ 1 vì mảng bắt đầu từ 0)
        AttackData currentAttack = characterData.comboAttacks[comboStep];

        // --- BẮT ĐẦU TẤN CÔNG ---
        isAttacking = true;
        comboTimer = COMBO_WINDOW_TIME;
        lastAttackTime = Time.time;
        movementInput = Vector2.zero; // Ngừng di chuyển khi tấn công

        // 1. Đẩy nhân vật về phía trước (Attack Thrust)
        float direction = isFacingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(direction * currentAttack.attackThrust, rb.linearVelocity.y);

        // 2. Kích hoạt Animation Trigger
        // Reset tất cả trigger cũ (để an toàn)
        foreach (var attack in characterData.comboAttacks)
        {
            anim.ResetTrigger(attack.animationTriggerName);
        }
        // Kích hoạt trigger mới
        anim.SetTrigger(currentAttack.animationTriggerName);

        Debug.Log($"P{playerIndex + 1} [INPUT-ATTACK] KÍCH HOẠT Attack {comboStep} ({currentAttack.animationTriggerName}). Timer Reset: {COMBO_WINDOW_TIME:F2}.");
    }

    // Hàm này được gọi bởi PlayerInputHandler
    // Đã thêm Debug Log chi tiết
    public void OnGuardPressed(bool isPressed)
    {
        // DEBUG: Nhận input Guard
        Debug.Log($"P{playerIndex + 1} [INPUT-GUARD] {(isPressed ? "Nhấn BẮT ĐẦU" : "Nhả KẾT THÚC")}. Trạng thái hiện tại: Attacking={isAttacking}, Stunned={isStunned}, Grounded={isGrounded}.");

        if (isAttacking || isStunned)
        {
            string reason = isStunned ? "Stunned" : "Attacking";
            Debug.LogWarning($"P{playerIndex + 1} [INPUT-GUARD] BỊ CHẶN. Lý do: {reason}.");
            return;
        }

        // Không thể bắt đầu đỡ đòn trên không
        if (isPressed && !isGrounded)
        {
            Debug.LogWarning($"P{playerIndex + 1} [INPUT-GUARD] BỊ CHẶN: Không thể Guard trên không.");
            return;
        }

        isGuarding = isPressed;

        if (isGuarding)
        {
            movementInput = Vector2.zero;
            Debug.Log($"P{playerIndex + 1} [INPUT-GUARD] KÍCH HOẠT Guard.");
        }
        else
        {
            movementInput = lastMoveInput;
            Debug.Log($"P{playerIndex + 1} [INPUT-GUARD] KẾT THÚC Guard.");
        }
    }

    #endregion

    #region Hàm hỗ trợ (Utilities)

    private bool IsGrounded()
    {
        if (groundCheckPoint == null) return false;

        RaycastHit2D hit = Physics2D.Raycast(groundCheckPoint.position, Vector2.down, GROUND_CHECK_DISTANCE, groundLayer);
        Debug.DrawRay(groundCheckPoint.position, Vector2.down * GROUND_CHECK_DISTANCE, hit.collider != null ? Color.green : Color.red);
        return hit.collider != null;
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    private void ResetCombo()
    {
        comboStep = 0;

        Debug.Log($"P{playerIndex + 1}: Combo reset.");
    }

    #endregion

    #region Animation Events (Hàm này được gọi TỪ Animation)

    // GỌI HÀM NÀY TẠI KEYFRAME BẮT ĐẦU GÂY SÁT THƯƠNG
    public void Anim_ActivateHitbox()
    {
        if (meleeHitbox == null || hitboxTrigger == null) return;
        if (comboStep <= 0 || comboStep > characterData.comboAttacks.Length - 1)
        {
            Debug.LogError($"P{playerIndex + 1}: Lỗi Anim_ActivateHitbox. ComboStep không hợp lệ: {comboStep}");
            return;
        }

        // Lấy data của đòn đánh hiện tại
        AttackData currentAttack = characterData.comboAttacks[comboStep];

        hitboxTrigger.TargetLayer = targetLayer;
        hitboxTrigger.damage = currentAttack.damage; // Gán sát thương từ CharacterData
        hitboxTrigger.ResetHitTargets();

        meleeHitbox.enabled = true;
        Debug.Log($"P{playerIndex + 1}: [ANIM-EVENT] Hitbox Activated (Step {comboStep}, Damage: {currentAttack.damage}).");
    }

    // GỌI HÀM NÀY TẠI KEYFRAME KẾT THÚC GÂY SÁT THƯƠNG
    public void Anim_DeactivateHitbox()
    {
        if (meleeHitbox != null)
        {
            meleeHitbox.enabled = false;
        }
        Debug.Log($"P{playerIndex + 1}: [ANIM-EVENT] Hitbox Deactivated.");
    }

    // GỌI HÀM NÀY TẠI KEYFRAME CUỐI CÙNG CỦA ANIMATION TẤN CÔNG
    public void Anim_EndAttack()
    {
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        isAttacking = false;
        movementInput = lastMoveInput;
    }

    #endregion

    #region Xử lý Trạng thái (Bị đánh, Chết)

    // Hàm này được gọi bởi HealthComponent
    public void OnHit(Vector3 attackerPosition)
    {
        // Bị đánh sẽ ngắt mọi hành động
        isAttacking = false;
        isGuarding = false;
        isStunned = true;
        movementInput = Vector2.zero;

        // Hủy kích hoạt hitbox nếu đang mở
        Anim_DeactivateHitbox();
        ResetCombo();

        anim.SetTrigger("Hit"); // Kích hoạt anim bị đánh

        // Tính toán và áp dụng Knockback
        float knockbackDirection = (transform.position.x > attackerPosition.x) ? 1f : -1f;
        rb.linearVelocity = Vector2.zero;

        // Tạm thời hardcode knockback (có thể lấy từ HealthComponent)
        // Lấy từ HealthComponent để đồng bộ
        float knockbackX = 1.25f;
        float knockbackY = 5f;

        Vector2 knockbackForce = new Vector2(knockbackDirection * knockbackX, knockbackY);
        rb.AddForce(knockbackForce, ForceMode2D.Impulse);

        Debug.Log($"P{playerIndex + 1}: BỊ ĐÁNH. Kích hoạt Stun/Khóa Di chuyển. Knockback: {knockbackForce}.");

        // Tự động hồi phục sau khi hết stun
        StartCoroutine(StunRecoveryCoroutine());
    }

    private IEnumerator StunRecoveryCoroutine()
    {
        // Đợi hết thời gian stun (lấy từ HealthComponent)
        yield return new WaitForSeconds(health.invulnerabilityDuration);
        isStunned = false;
        Debug.Log($"P{playerIndex + 1}: Stun recovered. Mở khóa Di chuyển.");
    }

    // Hàm này được gọi bởi HealthComponent
    public void OnDeath()
    {
        isStunned = true; // Coi như bị stun vĩnh viễn
        this.enabled = false; // Tắt script này
        anim.SetTrigger("Die");

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false; // Tắt vật lý
        }

        // Tắt hitbox
        Anim_DeactivateHitbox();

        Debug.Log($"P{playerIndex + 1} ĐÃ BỊ TIÊU DIỆT!");
        // TODO: Gọi Game Over Logic từ GameManager
    }

    #endregion
}
