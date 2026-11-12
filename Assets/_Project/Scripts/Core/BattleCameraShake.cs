using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class BattleCameraShake : HaiMonoBehaviour
{
    private static BattleCameraShake instance;
    public static BattleCameraShake Instance => instance;

    [Header("Default Shake Settings")]
    [SerializeField, Range(0.05f, 0.6f)]
    private float defaultDuration = 0.16f;

    [SerializeField, Range(0.02f, 0.8f)]
    private float defaultMagnitude = 0.16f;

    private Vector3 originalLocalPosition;
    private Coroutine shakeCoroutine;

    protected override void Awake()
    {
        base.Awake();

        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        instance = this;
        originalLocalPosition = transform.localPosition;
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
        }

        transform.localPosition = originalLocalPosition;
    }

    public void ShakeDefault()
    {
        Shake(defaultDuration, defaultMagnitude);
    }

    public void Shake(float duration, float magnitude)
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            transform.localPosition = originalLocalPosition;
        }

        shakeCoroutine = StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;

        // Chỉ tính toán rung lắc trong thời gian ngắn bằng Coroutine, không chạy Update mỗi frame
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float damper = 1f - Mathf.Clamp01(elapsed / duration);
            Vector2 offset2D = Random.insideUnitCircle * (magnitude * damper);

            transform.localPosition = new Vector3(
                originalLocalPosition.x + offset2D.x,
                originalLocalPosition.y + offset2D.y,
                originalLocalPosition.z
            );

            yield return null;
        }

        transform.localPosition = originalLocalPosition;
        shakeCoroutine = null;
    }
}

