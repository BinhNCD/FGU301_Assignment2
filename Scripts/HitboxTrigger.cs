using System.Collections.Generic;
using UnityEngine;

public class HitboxTrigger : MonoBehaviour
{
    public float damage { get; set; } = 0f;

    [Tooltip("Thời gian dừng game (Game Stutter) khi đòn đánh trúng.")]
    public float hit_stop_duration = 0.05f;
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

        HealthComponent health = other.GetComponent<HealthComponent>();

        if (health != null)
        {
            health.TakeDamage(damage, transform.position);

            Debug.Log($"[HitboxTrigger] SUCCESSFULLY hit {other.gameObject.name} for {damage} damage.");
        }
        else
        {
            Debug.LogWarning($"[HitboxTrigger] Hit {other.gameObject.name} but no HealthComponent found!");
        }

        targets_hit.Add(other);
        Debug.Log($"[Child Hitbox] Hit {other.gameObject.name} for {damage} damage.");
    }
}