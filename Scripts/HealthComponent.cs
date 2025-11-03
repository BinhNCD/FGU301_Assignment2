using UnityEngine;

public class HealthComponent : MonoBehaviour
{
    [Tooltip("Máu tối đa của nhân vật.")]
    public float max_health = 100f;
    [Tooltip("Máu hiện tại của nhân vật.")]
    public float current_health;
    [Tooltip("Thời gian (giây) nhân vật không thể nhận sát thương sau khi bị đánh.")]
    public float invulnerability_duration = 0.5f;

    [Header("Cấu hình Phản ứng Sát thương")]
    [Tooltip("Lực đẩy ngang (Knockback X) khi bị đánh.")]
    public float knockback_force_x = 10f;
    [Tooltip("Lực đẩy dọc (Knockback Y) khi bị đánh.")]
    public float knockback_force_y = 5f;

    private Rigidbody2D rb;
    private Animator anim;
    private bool is_invulnerable = false;

    private Player1_Controller player_controller;
    void Awake()
    {
        current_health = max_health;
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        player_controller = GetComponent<Player1_Controller>();

        if (rb == null)
        {
            Debug.LogError("HealthComponent requires a Rigidbody2D component on the same GameObject.");
        }
    }

    public void TakeDamage(float damage, Vector3 attacker_position)
    {
        if (current_health <= 0 || is_invulnerable)
        {
            return;
        }

        current_health -= damage;
        Debug.Log($"{gameObject.name} nhận {damage} sát thương. Máu còn lại: {current_health}");

        if (rb != null)
        {
            ApplyKnockback(attacker_position);
        }

        StartInvulnerability();

        anim?.SetTrigger("Hit");

        if (current_health <= 0)
        {
            Die();
        }
    }

    private void ApplyKnockback(Vector3 attacker_position)
    {
        float knockback_direction = (transform.position.x > attacker_position.x) ? 1f : -1f;

        rb.linearVelocity = Vector2.zero;

        Vector2 knockback_force = new Vector2(knockback_direction * knockback_force_x, knockback_force_y);
        rb.AddForce(knockback_force, ForceMode2D.Impulse);

        Debug.Log($"Knockback applied to {gameObject.name}: {knockback_force}");

        player_controller?.LockMovementDuringHit();
    }

    private void StartInvulnerability()
    {
        is_invulnerable = true;
        Invoke(nameof(EndInvulnerability), invulnerability_duration);
    }

    private void EndInvulnerability()
    {
        is_invulnerable = false;
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} đã bị tiêu diệt!");
        anim?.SetTrigger("Die");

        player_controller?.UnlockMovementAfterHit();
        if (player_controller != null)
        {
            player_controller.enabled = false;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        // TODO: Thêm logic xử lý chết (hiển thị UI Game Over,...)
    }
}
