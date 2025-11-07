using UnityEngine;
using System.Collections;

public class HealthComponent : MonoBehaviour
{
    [Tooltip("Máu tối đa của nhân vật.")]
    [SerializeField] private float maxHealth = 100f;
    [Tooltip("Cân bằng tối đa của nhân vật. Khi về 0, đòn tiếp theo gây Hard Hit.")]
    [SerializeField] private float maxBalance = 20f;
    [Tooltip("Mức cân bằng bị mất sau mỗi đòn đánh thường.")]
    [SerializeField] private float balanceDamagePerHit = 4f;
    [Tooltip("Thời gian hồi phục sau Hard Hit, cũng là thời gian bất khả xâm phạm.")]
    public float recoveryDuration = 2.0f;

    [HideInInspector] public int playerIndex; // Index của người chơi (0 hoặc 1)

    // --- Private fields ---
    private float currentHealth;
    private float currentBalance;
    private bool isHardHit = false;
    private bool isDead = false;

    // Tham chiếu đến Controller chung
    private PlayerController playerController;

    // Hàm này được gọi bởi PlayerController.Initialize()
    public void Initialize(float maxHP, int index, PlayerController controller)
    {
        this.maxHealth = maxHP;
        this.currentHealth = maxHP;
        this.playerIndex = index;
        // --- SỬA LỖI QUAN TRỌNG: Khởi tạo currentBalance ---
        this.currentBalance = this.maxBalance;

        this.playerController = controller;
        isDead = false;
        this.isHardHit = false;
        Debug.Log($"{gameObject.name} initialized with {maxHealth} HP and {maxBalance} Balance.");

        if (UIManager.Instance != null)
        {
            UpdateHealthUI();
        }
    }

    public bool TakeDamage(float damage, Vector3 attackerPosition)
    {
        if (isDead || isHardHit)
        {
            // Nếu Hard Hit = true, chặn sát thương
            Debug.Log($"{gameObject.name} Blocked damage. isHardHit: {isHardHit}");
            return false;
        }

        currentHealth -= damage;
        Debug.Log($"{gameObject.name} nhận {damage} sát thương. Máu còn lại: {currentHealth}");

        if (UIManager.Instance != null)
        {
            UpdateHealthUI();
        }

        // --- BƯỚC 1: Xử lý Cân Bằng ---
        Debug.Log($"Balance trước: {currentBalance:F2}. Sát thương: {balanceDamagePerHit:F2}");

        currentBalance -= balanceDamagePerHit;
        bool triggerHardHit = false;

        // Nếu cân bằng nhỏ hơn HOẶC bằng 0, kích hoạt Hard Hit
        if (currentBalance <= 0)
        {
            currentBalance = 0; // Đảm bảo không âm
            triggerHardHit = true;
            // Dòng log này sẽ hiển thị khi điều kiện được đáp ứng
            Debug.LogError($"!!! BALANCE REACHED 0 (or below). triggerHardHit = TRUE. Hiện tại: {currentBalance:F2}");
        }

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // --- BƯỚC 3: Xử lý Trạng thái Bị đánh ---
            // Nếu chưa chết, gọi hàm OnHit của controller
            // để xử lý bị đánh (anim, stun, knockback)
            if (playerController != null)
            {
                playerController.OnHit(attackerPosition, triggerHardHit);
            }

            // Nếu Hard Hit được kích hoạt, BẮT ĐẦU VÔ ĐỊCH/HỒI PHỤC CỨNG
            if (triggerHardHit)
            {
                Debug.LogError($"Kích hoạt StartHardHitRecovery()");
                StartHardHitRecovery();
            }
        }

        return true;
    }

    private void UpdateHealthUI()
    {
        // Tính toán tỷ lệ máu hiện tại
        float healthRatio = currentHealth / maxHealth;

        // Giả sử bạn có một UIManager quản lý UI của cả 2 người chơi
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealth(playerController.playerIndex, healthRatio);
        }
    }

    private void StartHardHitRecovery()
    {
        // RẤT QUAN TRỌNG: Thiết lập trạng thái isHardHit = TRUE
        isHardHit = true;
        // Dùng Invoke để lên lịch gọi hàm EndHardHitRecovery sau recoveryDuration
        CancelInvoke(nameof(EndHardHitRecovery));
        Invoke(nameof(EndHardHitRecovery), recoveryDuration);
        Debug.Log($"{gameObject.name}: HARD HIT - isHardHit = TRUE. Bắt đầu hồi phục trong {recoveryDuration}s.");
    }

    private void EndHardHitRecovery()
    {
        isHardHit = false;
        currentBalance = maxBalance;
        Debug.Log($"{gameObject.name}: Hard Hit Recovery END. isHardHit = FALSE. Balance restored to {maxBalance}.");

        // Báo cho PlayerController để thoát trạng thái stun và bắt đầu di chuyển lại
        if (playerController != null)
        {
            playerController.OnRecoveryEnd();
        }
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

        if (GameSceneManager.Instance != null)
        {
            int winnerIndex = 1 - this.playerIndex;
            GameSceneManager.Instance.EndRound(winnerIndex);
        }
    }
}