using LingBoCanteen;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class LockColliderWhileScaleSprite : MonoBehaviour
{
    [Header("统一图片显示边界（世界单位）")]
    public float BoundWidth = 3.2f;
    public float BoundHeight = 2.2f;
    
    [Header("缩放倍数")]
    public float ScaleMultiplier = 1f;

    private SpriteRenderer m_SpriteRender;
    private BoxCollider2D m_BoxCol;
    private Vector2 m_FixedColliderSize; // 初始锁定的碰撞尺寸，有碰撞体才生效
    private Camera m_MainCam;
    private CustomerEntity m_CustomerEntity;

    private void Awake()
    {
        m_SpriteRender = GetComponent<SpriteRenderer>();
        m_MainCam = Camera.main;

        // 尝试获取碰撞体，不存在则置空
        m_BoxCol = GetComponent<BoxCollider2D>();
        if (m_BoxCol != null)
        {
            // 【核心】读取初始碰撞大小，永久锁定
            m_FixedColliderSize = m_BoxCol.size;
        }
        
        // 检查是否是 CustomerEntity
        m_CustomerEntity = GetComponent<CustomerEntity>();
    }

    private void LateUpdate()
    {
        Sprite sprite = m_SpriteRender.sprite;
        if (sprite == null || m_MainCam == null) return;

        // 1. 根据贴图PPU计算原始世界尺寸
        float pxW = sprite.rect.width;
        float pxH = sprite.rect.height;
        float ppu = sprite.pixelsPerUnit;
        float worldW = pxW / ppu;
        float worldH = pxH / ppu;

        // 2. 等比缩放图片到统一边界
        float scaleW = BoundWidth / worldW;
        float scaleH = BoundHeight / worldH;
        float finalScale = Mathf.Min(scaleW, scaleH);
        
        // 如果是 CustomerEntity，乘以 3.4
        if (m_CustomerEntity != null)
        {
            finalScale *= 3.4f;
        }
        
        // 应用额外的缩放倍数（例如 Ghost 为 0.7）
        finalScale *= ScaleMultiplier;
        
        transform.localScale = Vector3.one * finalScale;

        // 3. 有碰撞体才执行反向补偿，抵消缩放维持原始碰撞大小
        if (m_BoxCol != null)
        {
            m_BoxCol.size = new Vector2(
                m_FixedColliderSize.x / finalScale,
                m_FixedColliderSize.y / finalScale
            );
        }
    }

    // 外部切换图片接口
    public void ChangeSprite(Sprite newSprite)
    {
        m_SpriteRender.sprite = newSprite;
    }
}