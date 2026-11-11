using UnityEngine;

[RequireComponent(typeof(TextMesh))]
[RequireComponent(typeof(PooledObject))]
public class BattleFloatingDamageText : HaiMonoBehaviour, IPoolable
{
    [Header("Components")]
    [SerializeField]
    private TextMesh textMesh;

    [SerializeField]
    private MeshRenderer meshRenderer;

    [SerializeField]
    private PooledObject pooledObject;

    [Header("Animation Settings")]
    [SerializeField, Min(0.2f)]
    private float lifeDuration = 0.85f;

    [SerializeField]
    private float floatUpSpeed = 1.6f;

    [SerializeField]
    private Color damageColor = new Color(1f, 0.3f, 0.2f, 1f);

    [SerializeField]
    private Color healColor = new Color(0.2f, 0.95f, 0.45f, 1f);

    private float elapsedTime;
    private Color activeColor;

    protected override void LoadComponents()
    {
        base.LoadComponents();

        if (textMesh == null)
        {
            textMesh = GetComponent<TextMesh>();
        }

        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }

        if (pooledObject == null)
        {
            pooledObject = GetComponent<PooledObject>();
        }
    }

    protected override void Awake()
    {
        base.Awake();

        if (meshRenderer != null)
        {
            // Đảm bảo chữ sát thương nổi lên trên toàn bộ Sprite nhân vật và thanh máu
            meshRenderer.sortingOrder = 50;
        }
    }

    public void OnSpawnFromPool()
    {
        elapsedTime = 0f;
        transform.localScale = Vector3.one;
    }

    public void OnReturnToPool()
    {
        elapsedTime = 0f;
    }

    public void Setup(int amount, bool isHeal)
    {
        elapsedTime = 0f;
        activeColor = isHeal ? healColor : damageColor;

        if (textMesh != null)
        {
            // Chỉ gán chuỗi đúng 1 lần khi vừa sinh ra, tuyệt đối không nối chuỗi trong Update
            textMesh.text = isHeal ? $"+{amount}" : $"-{amount}";
            textMesh.color = activeColor;
        }
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;

        transform.position += Vector3.up * (floatUpSpeed * Time.deltaTime);

        if (textMesh != null)
        {
            float normalizedTime = Mathf.Clamp01(elapsedTime / lifeDuration);
            Color fadedColor = activeColor;
            fadedColor.a = 1f - normalizedTime;
            textMesh.color = fadedColor;
        }

        if (elapsedTime >= lifeDuration)
        {
            if (pooledObject != null)
            {
                pooledObject.ReturnToPool();
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}

