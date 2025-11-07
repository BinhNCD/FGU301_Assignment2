using UnityEngine;
using System.Collections;

// Component này chịu trách nhiệm tạo ra hiệu ứng bóng mờ (Afterimage/Ghosting)
// Nó cần được gắn vào GameObject chứa PlayerController.
public class ShadowTrailEffect : MonoBehaviour
{
    // Cần tham chiếu đến SpriteRenderer của nhân vật để sao chép hình ảnh
    [SerializeField] private SpriteRenderer playerSprite;

    [Header("Cấu hình Bóng mờ")]
    [Tooltip("Thời gian giữa mỗi lần tạo bóng mờ (giây)")]
    public float trailInterval = 0.05f;
    [Tooltip("Thời gian tồn tại của mỗi bóng mờ (giây)")]
    public float shadowDuration = 0.3f;
    [Tooltip("Màu sắc của bóng mờ (thường là màu tối và trong suốt)")]
    public Color shadowColor = new Color(0.5f, 0.5f, 0.5f, 0.4f);

    private float lastTrailTime;

    private void Awake()
    {
        if (playerSprite == null)
        {
            // Cố gắng tự động tìm SpriteRenderer trên cùng hoặc các đối tượng con
            playerSprite = GetComponentInChildren<SpriteRenderer>();
        }

        if (playerSprite == null)
        {
            Debug.LogError("ShadowTrailEffect: Không tìm thấy SpriteRenderer để tạo bóng mờ. Vui lòng gán trong Inspector.");
        }
    }

    // Hàm public được gọi TỪ PlayerController khi Dash bắt đầu
    public void StartTrail()
    {
        if (playerSprite == null) return;

        lastTrailTime = Time.time;
        // Tạo bóng mờ ngay lập tức để bắt đầu
        CreateShadow();
    }

    // Hàm public được gọi TỪ PlayerController trong mỗi Update khi Dash đang diễn ra
    public void UpdateTrail()
    {
        if (playerSprite == null) return;

        if (Time.time >= lastTrailTime + trailInterval)
        {
            CreateShadow();
            lastTrailTime = Time.time;
        }
    }

    // Hàm public được gọi TỪ PlayerController khi Dash kết thúc
    public void StopTrail()
    {
        // Dừng việc tạo bóng mờ (đã được quản lý trong PlayerController)
    }

    private void CreateShadow()
    {
        if (playerSprite == null || playerSprite.sprite == null) return;

        // 1. Tạo GameObject tạm thời cho bóng mờ
        GameObject shadowObject = new GameObject("ShadowTrail");
        shadowObject.transform.position = transform.position;
        shadowObject.transform.rotation = transform.rotation;
        shadowObject.transform.localScale = transform.localScale;

        // 2. Thêm SpriteRenderer và sao chép dữ liệu
        SpriteRenderer shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
        shadowRenderer.sprite = playerSprite.sprite;
        shadowRenderer.flipX = playerSprite.flipX;

        // Sắp xếp layer: Đảm bảo bóng mờ nằm sau nhân vật chính
        shadowRenderer.sortingLayerID = playerSprite.sortingLayerID;
        shadowRenderer.sortingOrder = playerSprite.sortingOrder - 1;

        // 3. Áp dụng màu sắc và độ trong suốt
        shadowRenderer.color = shadowColor;

        // 4. Bắt đầu quá trình làm mờ và xóa bóng mờ
        StartCoroutine(FadeAndDestroy(shadowRenderer));
    }

    private IEnumerator FadeAndDestroy(SpriteRenderer sr)
    {
        float startTime = Time.time;
        Color startColor = sr.color;

        // Sử dụng shader mặc định để thao tác màu sắc
        sr.material = new Material(Shader.Find("Sprites/Default"));

        while (Time.time < startTime + shadowDuration)
        {
            float t = (Time.time - startTime) / shadowDuration;
            // Làm mờ dần alpha về 0
            float newAlpha = Mathf.Lerp(startColor.a, 0f, t);
            sr.color = new Color(startColor.r, startColor.g, startColor.b, newAlpha);

            yield return null;
        }

        // Hủy GameObject bóng mờ sau khi làm mờ xong
        Destroy(sr.gameObject);
    }
}