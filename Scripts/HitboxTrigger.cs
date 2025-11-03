using System.Collections.Generic;
using UnityEngine;

public class HitboxTrigger : MonoBehaviour
{
    public float damage { get; set; } = 0f;
    public LayerMask TargetLayer { get; set; }
    private readonly HashSet<Collider2D> targets_hit = new HashSet<Collider2D>();

    public void ResetHitTargets()
    {
        targets_hit.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!GetComponent<Collider2D>().enabled)
        {
            return;
        }

        if (((1 << other.gameObject.layer) & TargetLayer.value) == 0)
        {
            return;
        }

        if (targets_hit.Contains(other))
        {
            return;
        }

        // --- Logic Gây Sát Thương ---

        // ********************************************************************
        // THỰC HIỆN GỌI HÀM NHẬN SÁT THƯƠNG Ở ĐÂY
        // ********************************************************************

        // Ví dụ: other.GetComponent<HealthComponent>()?.TakeDamage(Damage);
        // Bạn sẽ cần thay thế 'HealthComponent' bằng tên script quản lý máu của kẻ địch/player khác.

        // Debug tạm thời 
        targets_hit.Add(other);
        Debug.Log($"[Child Hitbox] Hit {other.gameObject.name} for {damage} damage.");
    }
}
