using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitboxTrigger : MonoBehaviour
{
    public float damage { get; set; } = 0f;

    [Tooltip("Thời gian dừng game (Game Stutter) khi đòn đánh trúng.")]
    public float hit_stop_duration = 0.55f;
    public LayerMask TargetLayer { get; set; }
    private readonly HashSet<Collider2D> targets_hit = new HashSet<Collider2D>();
    private static Coroutine hitStopCoroutine = null;

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
        Debug.Log($"[--- CORE TRIGGER DEBUG ---] Va chạm xảy ra giữa {gameObject.name} (Hitbox) và {other.gameObject.name} (Mục tiêu).");
        Debug.Log($"[HitboxTrigger Debug] Target Layer Value: {TargetLayer.value}. Object's Layer: {other.gameObject.layer}.");

        HealthComponent health = other.GetComponent<HealthComponent>();

        if (health != null)
        {
            bool check = health.TakeDamage(damage, transform.position);

            if (hitStopCoroutine == null && check)
            {
                hitStopCoroutine = StartCoroutine(HitStop(hit_stop_duration));
            }
            Debug.Log($"[HitboxTrigger] SUCCESSFULLY hit {other.gameObject.name} for {damage} damage.");
        }
        else
        {
            Debug.LogWarning($"[HitboxTrigger] Hit {other.gameObject.name} but no HealthComponent found!");
        }

        targets_hit.Add(other);
        Debug.Log($"[Child Hitbox] Hit {other.gameObject.name} for {damage} damage.");
    }

    private IEnumerator HitStop(float duration)
    {
        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = 1f;
        hitStopCoroutine = null;
    }
}