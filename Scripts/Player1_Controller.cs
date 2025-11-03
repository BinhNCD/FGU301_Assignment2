using System;
using UnityEditor.Timeline.Actions;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public struct HitboxData
{
    [Tooltip("Mức sát thương gây ra.")]
    public float Damage;
}

public class Player1_Controller : MonoBehaviour
{
    [Tooltip("Tốc độ di chuyển ngang (tốc độ đi bộ) của nhân vật.")]
    private float move_speed = 10f;
    [Tooltip("Lực đẩy ban đầu khi nhân vật nhảy.")]
    private float jump_force = 30f;
    [Tooltip("Khoảng cách Raycast kiểm tra chạm đất.")]
    private float ground_check_distance = 0.1f;
    [Tooltip("Hiển thị player sử dụng input system.")]
    private int player_index;
    [Tooltip("Thời gian cho phép giữa các đòn đánh để duy trì combo.")]
    private float combo_window = 0.4f;
    private int combo_step = 0;
    private float combo_timer = 0f;
    private float last_attack_time = 0f;
    private readonly float debounce_delay = 0.2f;
    [Tooltip("Lực đẩy ngang được áp dụng khi nhân vật bắt đầu đòn tấn công (tạo hiệu ứng nhích về phía trước).")]
    private float attack_thrust = 10f;

    private Rigidbody2D rb;
    private Animator anim;
    private PlayerInput player_input;
    private InputAction guard_action;
    private InputAction attack_action;
    [Tooltip("Layer nào được coi là mặt đất.")]
    public LayerMask ground_layer;
    [Tooltip("Transform chỉ ra vị trí chính xác để kiểm tra mặt đất (Dưới chân nhân vật).")]
    public Transform ground_check_point;
    [Tooltip("LayerMask xác định những đối tượng (như Player khác hoặc Enemy) mà Hitbox này có thể gây sát thương.")]
    public LayerMask target_layer;
    public HitboxData[] combo_hitbox_data = new HitboxData[5];
    [Tooltip("BoxCollider2D của Hitbox con duy nhất. Vị trí và kích thước của nó sẽ được điều chỉnh bằng keyframe trong Animation.")]
    public BoxCollider2D melee_hitbox;
    private HitboxTrigger hitbox_trigger;
    private HealthComponent health_component;

    private Vector2 movement_input = Vector2.zero;
    private Vector2 last_move_input = Vector2.zero;

    private bool is_facing_right = true;
    private bool is_grounded = true;
    private bool is_guarding = false;
    private bool is_attacking = false;
    private bool is_stunned = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        player_input = GetComponent<PlayerInput>();
        anim = GetComponent<Animator>();

        InitializeHitboxes();
    }

    void Start()
    {
        if (rb == null)
        {
            Debug.LogError("Rigidbody2D component không được tìm thấy trên đối tượng này.");
        }
        if (health_component == null)
        {
            Debug.LogError("HealthComponent component không được tìm thấy. Vui lòng gắn HealthComponent.cs vào GameObject này.");
        }

        if (player_input == null)
        {
            Debug.LogError("PlayerInput component không được tìm thấy. Vui lòng gắn Player Input component vào GameObject này.");
            return;
        }

        player_index = player_input.playerIndex;
        Debug.Log($"Player {player_index + 1} ({gameObject.name}) đã tham gia.");

        if (player_input.actions != null)
        {
            player_input.actions.FindActionMap("Player")?.Enable();

            guard_action = player_input.actions.FindAction("Crouch");
            if (guard_action != null)
            {
                guard_action.performed += OnGuardPerformed;
                guard_action.canceled += OnGuardCanceled;
            }

            attack_action = player_input.actions.FindAction("Attack");
            if (attack_action != null)
            {
                attack_action.performed += OnAttackPerformed;
            }
        }
    }

    private void OnDestroy()
    {
        if (guard_action != null)
        {
            guard_action.performed -= OnGuardPerformed;
            guard_action.canceled -= OnGuardCanceled;
        }

        if (attack_action != null)
        {
            attack_action.performed -= OnAttackPerformed;
        }
    }

    public void OnMove(InputValue value)
    {
        Vector2 current_input = value.Get<Vector2>();
        last_move_input = current_input;

        if (is_guarding || is_attacking)
        {
            movement_input = Vector2.zero;
        }
        else
        {
            movement_input = current_input;
        }
    }

    public void OnJump(InputValue value)
    {
        if (is_guarding || is_attacking) return;

        if (value.isPressed && is_grounded && rb != null)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jump_force);

            if (anim != null)
            {
                anim.SetTrigger("Jump");
            }
        }
    }

    private void OnAttack()
    {
        int max_combo_steps = combo_hitbox_data != null ? combo_hitbox_data.Length - 1 : 0;
        if (max_combo_steps <= 0 || is_stunned) return;

        if (Time.time < last_attack_time + debounce_delay)
        {
            return;
        }

        if (is_guarding || !is_grounded) return;

        if (combo_step > 0 && combo_timer <= 0)
        {
            combo_step = 0;
        }

        combo_step++;
        if (combo_step > max_combo_steps)
        {
            combo_step = 1;
        }

        is_attacking = true;
        combo_timer = combo_window;
        last_attack_time = Time.time;
        movement_input = Vector2.zero;

        if (rb != null && (combo_step == 1 || combo_step == 2))
        {
            float direction = is_facing_right ? 1f : -1f;
            rb.linearVelocity = new Vector2(direction * attack_thrust, rb.linearVelocity.y);

            Debug.Log($"[ATTACK THRUST] Applying horizontal thrust: {direction * attack_thrust}");
        }
        else
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            Debug.Log($"[ATTACK THRUST] Horizontal velocity reset to 0 for combo step {combo_step}.");
        }

        if (anim != null)
        {
            for (int i = 1; i <= max_combo_steps; i++)
            {
                anim.ResetTrigger("Attack" + i);
            }

            anim.SetTrigger("Attack" + combo_step);
        }

        Debug.Log($"[ATTACK START] Attack {combo_step} triggered at Time: {Time.time}. Movement locked.");
    }

    public void EndAttack()
    {
        float duration = Time.time - last_attack_time;
        Debug.Log($"[ATTACK END] EndAttack called at Time: {Time.time}. Duration: {duration:F3}s. Movement unlocked.");

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        is_attacking = false;
        movement_input = last_move_input;
    }

    private void OnGuardPerformed(InputAction.CallbackContext ctx)
    {
        SetGuard(true);
    }
    private void OnGuardCanceled(InputAction.CallbackContext ctx)
    {
        SetGuard(false);
    }
    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        OnAttack();
    }

    void Update()
    {
        is_grounded = IsGrounded();
        Debug.Log($"P{player_index + 1} isGrounded: {is_grounded}");

        if (combo_timer > 0)
        {
            combo_timer -= Time.deltaTime;
            if (combo_timer <= 0 && is_attacking == false)
            {
                combo_step = 0;
                Debug.Log("Combo timed out. Resetting combo_step.");
            }
        }
    }

    void FixedUpdate()
    {
        if (rb != null)
        {
            if (is_stunned) return;

            Debug.Log("Tan cong debug" + is_attacking);
            float current_horizontal_input = movement_input.x;
            float actual_move_speed = move_speed;
            float input_for_flip = is_guarding ? last_move_input.x : current_horizontal_input;

            if (is_guarding || is_attacking)
            {
                actual_move_speed *= 0f;
            }

            if (!is_attacking)
            {
                Vector2 movement = new Vector2(current_horizontal_input * actual_move_speed, rb.linearVelocity.y);
                rb.linearVelocity = movement;

                if (current_horizontal_input != 0)
                {
                    Debug.Log($"P{player_index + 1} Applying Velocity: {movement.x}");
                }
            }

            if (input_for_flip > 0 && !is_facing_right)
            {
                Flip();
            }
            else if (input_for_flip < 0 && is_facing_right)
            {
                Flip();
            }

            UpdateAnimation(current_horizontal_input);
        }
    }

    private void UpdateAnimation(float horizontal_input)
    {
        if (anim != null)
        {
            float speed = Mathf.Abs(horizontal_input);
            anim.SetFloat("Speed", speed);
            anim.SetBool("IsJumping", !is_grounded);
            anim.SetFloat("VerticalSpeed", rb.linearVelocity.y);
        }
    }

    private void Flip()
    {
        is_facing_right = !is_facing_right;

        Vector3 scale = transform.localScale;

        scale.x *= -1;
        transform.localScale = scale;
    }

    private bool IsGrounded()
    {
        if (rb == null || ground_check_point == null) return false;

        if (ground_layer.value == 0)
        {
            Debug.LogError("Ground Layer chưa được chọn! Vui lòng chọn Layer mặt đất (ví dụ: Ground) trong Inspector.");
            return false;
        }

        Vector2 raycast_origin = ground_check_point.position;

        RaycastHit2D hit = Physics2D.Raycast(raycast_origin, Vector2.down, ground_check_distance, ground_layer);
        float debugRayLength = ground_check_distance * 10f;
        Debug.DrawRay(transform.position, Vector2.down * debugRayLength, hit.collider != null ? Color.green : Color.red);

        return hit.collider != null;
    }

    private void SetGuard(bool guard)
    {
        if (is_attacking) return;

        Debug.Log("Crouch check: " + guard + " " + is_guarding + " " + is_grounded);
        if (is_guarding == guard) return;
        if (!is_grounded && guard) return;

        is_guarding = guard;
        if (anim != null)
        {
            anim.SetBool("IsGuarding", guard);
        }

        if (guard)
        {
            movement_input = Vector2.zero;
        }
        else
        {
            movement_input = last_move_input;
            Debug.Log($"[GUARD] Restored movement input to: {movement_input.x}");
        }
    }

    private void InitializeHitboxes()
    {
        if (melee_hitbox == null)
        {
            melee_hitbox = GetComponentInChildren<BoxCollider2D>();

            if (melee_hitbox == null)
            {
                Debug.LogError("FATAL ERROR: melee_hitbox Collider chưa được gán trong Inspector và không tìm thấy BoxCollider2D nào trên các đối tượng con.");
                return;
            }

            Debug.Log("[Hitbox Init] Tự động tìm thấy BoxCollider2D con. Vui lòng kiểm tra đã đặt Collider là Is Trigger.");
        }

        hitbox_trigger = melee_hitbox.GetComponent<HitboxTrigger>();
        if (hitbox_trigger == null)
        {
            Debug.LogError("HitboxTrigger.cs script is missing on the melee_hitbox Collider component!");
            return;
        }

        melee_hitbox.enabled = false;

        Debug.Log("[Hitbox Init] Single Hitbox setup complete.");
    }

    public void ActivateHitbox(int step)
    {
        if (melee_hitbox == null || hitbox_trigger == null) return;

        if (step <= 0 || step >= combo_hitbox_data.Length)
        {
            Debug.LogError($"Invalid combo step: {step}. Hitbox data index out of bounds.");
            return;
        }

        hitbox_trigger.TargetLayer = target_layer;

        hitbox_trigger.damage = combo_hitbox_data[step].Damage;

        hitbox_trigger.ResetHitTargets();

        melee_hitbox.enabled = true;

        Debug.Log($"[Single Hitbox] Attack {step} activated. Collider is ON.");
    }

    public void DeactivateHitbox()
    {
        if (melee_hitbox != null)
        {
            melee_hitbox.enabled = false;
        }

        Debug.Log("[Single Hitbox] Deactivated.");
    }

    public void LockMovementDuringHit()
    {
        is_attacking = false;
        is_guarding = false;
        is_stunned = true;
        movement_input = Vector2.zero;

        float stun_duration = health_component != null ? health_component.invulnerability_duration + 0.1f : 0.6f;
        Invoke(nameof(UnlockMovementAfterHit), stun_duration);

        if (anim != null)
        {
            anim.SetBool("IsGuarding", false);
        }
    }

    public void UnlockMovementAfterHit()
    {
        is_stunned = false;
        movement_input = last_move_input;
        Debug.Log("[STUN END] Movement unlocked.");
    }
}
