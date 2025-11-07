using UnityEngine;
using System.Collections; // Cần cho Coroutines (như Hit Stop)

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
    private ShadowTrailEffect shadowTrail;

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
    private bool isDashing = false;

    // --- COMBAT ---
    private float originalGravityScale = 1f;
    private int comboStep = 0;
    private float comboTimer = 0f;
    private float lastAttackTime = 0f;
    private AttackData[] currentComboArray;
    private static float P1_MAX_HEALTH = 0;
    private static float P2_MAX_HEALTH = 0;

    // --- CONSTANTS (HẰNG SỐ) ---
    private const float GROUND_CHECK_DISTANCE = 0.5f;
    private const float COMBO_WINDOW_TIME = 0.4f; // Thời gian chờ giữa các đòn combo
    private const float ATTACK_DEBOUNCE_DELAY = 0.2f; // Chống spam attack
    private const float DASH_DISTANCE = 10f; 
    private const float DASH_SPEED = 50f; // Tốc độ Dash

    // --- THAM CHIẾU (REFERENCES) ---
    [Tooltip("Transform chỉ ra vị trí để kiểm tra mặt đất (Dưới chân nhân vật).")]
    [SerializeField] private Transform groundCheckPoint;
    [Tooltip("Layer nào được coi là mặt đất.")]
    [SerializeField] private LayerMask groundLayer;
    [Tooltip("BoxCollider2D của Hitbox con duy nhất.")]
    [SerializeField] private BoxCollider2D childMeleeHitbox;

    // KNOCKBACK CONSTANTS (Tạm thời)
    private const float SOFT_KNOCKBACK_X = 0.5f;
    private const float SOFT_KNOCKBACK_Y = 1.0f;
    private const float HARD_KNOCKBACK_X = 5.0f;
    private const float HARD_KNOCKBACK_Y = 10.0f;

    #region Khởi tạo và Thiết lập

    // Awake được gọi ngay cả khi script bị disable
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        health = GetComponent<HealthComponent>();
        shadowTrail = GetComponent<ShadowTrailEffect>();

        originalGravityScale = rb.gravityScale;
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
        health.Initialize(characterData.maxHealth, index, this);

        if (index == 0) { P1_MAX_HEALTH = characterData.maxHealth; }
        else if (index == 1) { P2_MAX_HEALTH = characterData.maxHealth; }

        if (P1_MAX_HEALTH > 0 && P2_MAX_HEALTH > 0 && UIManager.Instance != null)
        {
            UIManager.Instance.InitializeHealthBar(P1_MAX_HEALTH, P2_MAX_HEALTH);
        }

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

        HandleMovement();
        HandleFlip();
        UpdateAnimationParameters();
    }

    private void HandleMovement()
    {
        if (isDashing || isStunned)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (isAttacking && !isGrounded)
        {
            // Ngừng ảnh hưởng của trọng lực
            rb.gravityScale = 0f;
            // Khóa vận tốc đứng (chỉ giữ lại vận tốc ngang do attackThrust)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            return;
        }
        

        if (rb.gravityScale != originalGravityScale)
        {
            rb.gravityScale = originalGravityScale;
        }

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
        if (!isAttacking || !isStunned)
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
        anim.SetBool("IsDashing", isDashing);
    }

    #endregion

    #region Xử lý Input (Được gọi từ PlayerInputHandler.cs)

    // Hàm này được gọi bởi PlayerInputHandler
    public void SetMoveInput(Vector2 input)
    {
        // DEBUG: Nhận input di chuyển
        Debug.Log($"P{playerIndex + 1} [INPUT-MOVE] Nhận: {input}. Trạng thái hiện tại: Attacking={isAttacking}, Guarding={isGuarding}, Stunned={isStunned}");
        lastMoveInput = input;

        if (isStunned || isDashing)
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

        if (isGuarding || isAttacking || isStunned || !isGrounded || isDashing)
        {
            string reason = isStunned ? "Stunned" : (isGuarding ? "Guarding" : (isAttacking ? "Attacking" : (!isGrounded ? "Not Grounded" : "Unknown")));
            Debug.LogWarning($"P{playerIndex + 1} [INPUT-JUMP] BỊ CHẶN. Lý do: {reason}.");
            return;
        }

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, characterData.jumpForce);
        anim.SetTrigger("Jump");

        Debug.Log($"P{playerIndex + 1} [INPUT-JUMP] KÍCH HOẠT Nhảy. Lực đẩy: {characterData.jumpForce}.");
    }

    public void OnDashPressed()
    {
        if (isStunned || isAttacking || isGuarding || isDashing)
        {
            return;
        }

        if (lastMoveInput.x > 0 && !isFacingRight) Flip();
        if (lastMoveInput.x < 0 && isFacingRight) Flip();

        StartCoroutine(DashCoroutine());
    }

    // Hàm này được gọi bởi PlayerInputHandler
    public void OnAttackPressed()
    {
        if (isDashing || isStunned) return;

        // DEBUG: Nhận input Attack
        Debug.Log($"P{playerIndex + 1} [INPUT-ATTACK] Nhấn Attack. Trạng thái hiện tại: Attacking={isAttacking}, Guarding={isGuarding}, Stunned={isStunned}, ComboTimer={comboTimer:F2}.");

        AttackData[] targetAttackArray = isGrounded ?
            characterData.groundNormalAttacks : characterData.airNormalAttacks;

        if (targetAttackArray == null || targetAttackArray.Length < 2) return;

        // Check các điều kiện không thể tấn công
        if (isStunned || isGuarding)
        {
            string reason = isStunned ? "Stunned" : (isGuarding ? "Guarding" : (!isGrounded ? "Not Grounded" : "Unknown"));
            Debug.LogWarning($"P{playerIndex + 1} [INPUT-ATTACK] BỊ CHẶN. Lý do: {reason}.");
            return;
        }

        currentComboArray = targetAttackArray;

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

        int nextComboStep = comboStep + 1;

        // Kiểm tra xem combo có thể tiếp tục không
        if (nextComboStep > currentComboArray.Length - 1)
        {
            if (!isGrounded)
            {
                Debug.Log($"P{playerIndex + 1}: Max air combo reached. Blocking further air attacks until landing.");
                ResetCombo();
                return;
            }
            // Logic cũ: Reset combo trên mặt đất
            else
            {
                Debug.Log($"P{playerIndex + 1}: Max combo reached. Restarting combo.");
                nextComboStep = 1;
            }
        }

        // Nếu hợp lệ, cập nhật bước combo
        comboStep = nextComboStep;

        // Lấy dữ liệu đòn đánh hiện tại (trừ 1 vì mảng bắt đầu từ 0)
        AttackData currentAttack = currentComboArray[comboStep];

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
        foreach (var attack in currentComboArray)
        {
            anim.ResetTrigger(attack.animationTriggerName);
        }
        // Kích hoạt trigger mới
        anim.SetTrigger(currentAttack.animationTriggerName);

        Debug.Log($"P{playerIndex + 1} [INPUT-ATTACK] KÍCH HOẠT Attack {comboStep} ({currentAttack.animationTriggerName}). Timer Reset: {COMBO_WINDOW_TIME:F2}.");
    }

    public void OnSkillPressed(int skillIndex)
    {
        // 1. Kiểm tra điều kiện chặn
        if (isStunned || isGuarding || isAttacking || isDashing) return;

        if (characterData.skills == null || skillIndex < 1 || skillIndex > characterData.skills.Length)
        {
            Debug.LogWarning($"P{playerIndex + 1}: Skill Index {skillIndex} không hợp lệ hoặc Skill chưa được định nghĩa.");
            return;
        }

        SkillAttackData skill = characterData.skills[skillIndex];

        // 2. Thiết lập trạng thái
        isAttacking = true;
        movementInput = Vector2.zero;
        lastAttackTime = Time.time;
        ResetCombo(); // Reset combo thường

        // 3. Kích hoạt Animation Skill
        anim.SetTrigger(skill.skillAnimationTriggerName);

        // 4. Bắt đầu Coroutine để quản lý chuỗi hitbox và lực đẩy của Skill
        StartCoroutine(SkillSequenceCoroutine(skill));

        Debug.Log($"P{playerIndex + 1}: KÍCH HOẠT Skill: {skill.skillName} (Index: {skillIndex}).");
    }

    // Hàm này được gọi bởi PlayerInputHandler
    // Đã thêm Debug Log chi tiết
    public void OnGuardPressed(bool isPressed)
    {
        // DEBUG: Nhận input Guard
        Debug.Log($"P{playerIndex + 1} [INPUT-GUARD] {(isPressed ? "Nhấn BẮT ĐẦU" : "Nhả KẾT THÚC")}. Trạng thái hiện tại: Attacking={isAttacking}, Stunned={isStunned}, Grounded={isGrounded}.");

        if (isAttacking || isStunned || isDashing)
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
        currentComboArray = null;
        Debug.Log($"P{playerIndex + 1}: Combo reset.");
    }

    #endregion

    #region Animation Events (Hàm này được gọi TỪ Animation)

    // GỌI HÀM NÀY TẠI KEYFRAME BẮT ĐẦU GÂY SÁT THƯƠNG
    public void Anim_ActivateHitbox()
    {
        if (meleeHitbox == null || hitboxTrigger == null || characterData == null) return;

        AttackData currentAttack;

        if (comboStep <= 0 || comboStep > currentComboArray.Length - 1)
        {
            Debug.LogError($"P{playerIndex + 1}: Lỗi Anim_ActivateHitbox. ComboStep không hợp lệ: {comboStep}");
            return;
        }

        // Lấy data của đòn đánh hiện tại
        currentAttack = currentComboArray[comboStep];

        meleeHitbox.size = new Vector2(currentAttack.sizeX, currentAttack.sizeY);
        meleeHitbox.offset = new Vector2(currentAttack.offsetX, currentAttack.offsetY);

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
    public void OnHit(Vector3 attackerPosition, bool isHardHit)
    {
        // Bị đánh sẽ ngắt mọi hành động
        isAttacking = false;
        isGuarding = false;
        isDashing = false;
        movementInput = Vector2.zero;

        // Hủy kích hoạt hitbox nếu đang mở
        Anim_DeactivateHitbox();
        ResetCombo();
        if (shadowTrail != null) shadowTrail.StopTrail();

        movementInput = Vector2.zero;
        rb.gravityScale = originalGravityScale;
        rb.linearVelocity = Vector2.zero;

        float knockbackDirection = (transform.position.x > attackerPosition.x) ? 1f : -1f;

        if (isHardHit)
        {
            isStunned = true;
            anim.SetTrigger("HardHit");

            // Áp dụng Knockback Mạnh
            Vector2 knockbackForce = new Vector2(knockbackDirection * HARD_KNOCKBACK_X, HARD_KNOCKBACK_Y);
            rb.AddForce(knockbackForce, ForceMode2D.Impulse);
            Debug.Log($"P{playerIndex + 1}: HARD HIT! Stunned for {health.recoveryDuration}s.");

        }
        else
        {
            anim.SetTrigger("Hit");

            // Áp dụng Knockback Nhẹ
            Vector2 knockbackForce = new Vector2(knockbackDirection * SOFT_KNOCKBACK_X, SOFT_KNOCKBACK_Y);
            rb.AddForce(knockbackForce, ForceMode2D.Impulse);
        }
    }

    public void OnRecoveryEnd()
    {
        if (isStunned)
        {
            anim.SetTrigger("Recovery");
        }
    }

    public void RevoveryEnd()
    {
        isStunned = false; // Tắt Stun Lock
        movementInput = lastMoveInput; // Phục hồi input di chuyển cuối cùng
        Debug.Log($"P{playerIndex + 1}: Hồi phục Stun hoàn tất. Mở khóa Input.");
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

    #region Coroutines
    private IEnumerator SkillSequenceCoroutine(SkillAttackData skill)
    {
        // Lặp qua từng đòn đánh nhỏ (hitbox) trong chuỗi
        for (int i = 0; i < skill.attackSequence.Length - 1; i++)
        {
            AttackData currentHit = skill.attackSequence[i];

            // 1. Áp dụng lực đẩy Skill (nếu cần)
            float direction = isFacingRight ? 1f : -1f;
            // Áp dụng lực đẩy (thrust) ngang của hit hiện tại
            rb.linearVelocity = new Vector2(direction * currentHit.attackThrust, rb.linearVelocity.y);

            // 2. Kích hoạt Hitbox
            ActivateSpecificHitbox(currentHit);
            Debug.Log($"P{playerIndex + 1}: [SKILL HIT {i + 1}] Hitbox Activated.");

            // 3. Chờ cho hitbox hoạt động
            // Sử dụng thời lượng được định nghĩa trong AttackData
            yield return new WaitForSeconds(currentHit.hitActiveDuration);

            // 4. Vô hiệu hóa Hitbox
            Anim_DeactivateHitbox();

            // 5. Chờ thêm một khoảng ngắn để Animation Skill tiếp diễn
            // Hoặc chờ đến khi Animation Skill hoàn thành (nếu không có hit tiếp theo)
            // Ví dụ: Chờ 0.1 giây giữa các hit nhanh
            if (i < skill.attackSequence.Length - 1)
            {
                yield return new WaitForSeconds(0.1f);
            }
        }

        // Đảm bảo Coroutine chờ cho đến khi Animation skill kết thúc 
        // trước khi gọi Anim_EndAttack để reset isAttacking (tạm thời không cần, vì Anim_EndAttack sẽ được gọi từ Animation)
    }

    private void ActivateSpecificHitbox(AttackData attackToUse)
    {
        if (meleeHitbox == null || hitboxTrigger == null || characterData == null) return;

        // Thiết lập Hitbox
        meleeHitbox.size = new Vector2(attackToUse.sizeX, attackToUse.sizeY);
        meleeHitbox.offset = new Vector2(attackToUse.offsetX, attackToUse.offsetY);

        hitboxTrigger.TargetLayer = targetLayer;
        hitboxTrigger.damage = attackToUse.damage;
        hitboxTrigger.ResetHitTargets();

        meleeHitbox.enabled = true;
    }

    private IEnumerator DashCoroutine()
    {
        isDashing = true;

        // Bắt đầu hiệu ứng bóng mờ
        if (shadowTrail != null)
        {
            shadowTrail.StartTrail();
        }

        anim.SetTrigger("Dash");

        float calculatedDuration = DASH_DISTANCE / DASH_SPEED;
        float startTime = Time.time;
        float direction = isFacingRight ? 1f : -1f;
        Vector2 startPosition = rb.position;
        Vector2 targetEndPosition = startPosition + new Vector2(direction * DASH_DISTANCE, 0f);
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;

        while (Time.time < startTime + calculatedDuration)
        {
            float t = (Time.time - startTime) / calculatedDuration;

            rb.MovePosition(Vector2.Lerp(startPosition, targetEndPosition, t));

            if (shadowTrail != null)
            {
                shadowTrail.UpdateTrail();
            }

            yield return null;
        }

        // Kết thúc Dash
        isDashing = false;

        // Dừng hiệu ứng bóng mờ
        if (shadowTrail != null)
        {
            shadowTrail.StopTrail();
        }

        rb.gravityScale = originalGravityScale;

        rb.MovePosition(targetEndPosition);

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        Debug.Log($"P{playerIndex + 1}: Dash Finished.");
    }
    #endregion
}
