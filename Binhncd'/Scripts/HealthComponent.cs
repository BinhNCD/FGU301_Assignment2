using UnityEngine;

public class HealthComponent : MonoBehaviour
{
    [Tooltip("Máu tối đa của nhân vật.")]
    [SerializeField] private float maxHealth = 100f;
    [Tooltip("Thời gian (giây) nhân vật không thể nhận sát thương sau khi bị đánh.")]
    public float invulnerabilityDuration = 0.5f;

    // --- Private fields ---
    private float currentHealth;
    private bool isInvulnerable = false;
    private bool isDead = false;

    // Tham chiếu đến Controller chung
    private PlayerController playerController;

    // Hàm này được gọi bởi PlayerController.Initialize()
    public void Initialize(float maxHP, PlayerController controller)
    {
        this.maxHealth = maxHP;
        this.currentHealth = maxHP;
        this.playerController = controller;
        isDead = false;

        // TODO: Cập nhật UI thanh máu ở đây
        // UIManager.Instance.UpdateHealth(playerController.playerIndex, currentHealth, maxHealth);
    }

    public void TakeDamage(float damage, Vector3 attackerPosition)
    {
        if (isDead || isInvulnerable)
        {
            return;
        }

        currentHealth -= damage;
        Debug.Log($"{gameObject.name} nhận {damage} sát thương. Máu còn lại: {currentHealth}");

        // TODO: Cập nhật UI thanh máu
        // UIManager.Instance.UpdateHealth(playerController.playerIndex, currentHealth, maxHealth);

        StartInvulnerability();

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Nếu chưa chết, gọi hàm OnHit của controller
            // để xử lý bị đánh (anim, stun, knockback)
            if (playerController != null)
            {
                playerController.OnHit(attackerPosition);
            }
        }
    }

    private void StartInvulnerability()
    {
        isInvulnerable = true;
        Invoke(nameof(EndInvulnerability), invulnerabilityDuration);
    }

    private void EndInvulnerability()
    {
        isInvulnerable = false;
    }

    private void Die()
    {
        isDead = true;
        Debug.Log($"{gameObject.name} đã bị tiêu diệt!");

        // Gọi hàm OnDeath của controller
        if (playerController != null)
        {
            playerController.OnDeath();
        }

        // TODO: Thêm logic xử lý chết (hiển thị UI Game Over,...)
        // GameManager.Instance.CheckEndGame(playerController.playerIndex);
    }
}
