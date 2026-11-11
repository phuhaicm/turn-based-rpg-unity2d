using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SmoothBarUI : HaiMonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField]
    private RectTransform fillRect;

    [SerializeField]
    private Text valueText;

    [SerializeField]
    private string labelPrefix = "HP";

    [Header("Smooth Animation")]
    [SerializeField, Min(0.5f)]
    private float slideSpeed = 2.5f;

    private float displayedRatio = 1f;
    private float targetRatio = 1f;
    private Coroutine slideCoroutine;

    protected override void LoadComponents()
    {
        base.LoadComponents();

        if (fillRect == null)
        {
            Transform foundFill = transform.Find("Fill");
            if (foundFill != null)
            {
                fillRect = foundFill.GetComponent<RectTransform>();
            }
        }

        if (valueText == null)
        {
            valueText = GetComponentInChildren<Text>(true);
        }
    }

    public void ConfigureReferences(RectTransform fill, Text textLabel, string prefix)
    {
        fillRect = fill;
        valueText = textLabel;
        labelPrefix = prefix;
    }

    public void SetValueImmediate(int current, int max)
    {
        targetRatio = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
        displayedRatio = targetRatio;

        ApplyFillRatio(displayedRatio);
        UpdateTextLabel(current, max);
    }

    public void UpdateBarValue(int current, int max)
    {
        targetRatio = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;

        // Chỉ cập nhật chuỗi Text đúng 1 lần khi nhận sự kiện thay đổi máu/mana
        UpdateTextLabel(current, max);

        if (!gameObject.activeInHierarchy)
        {
            displayedRatio = targetRatio;
            ApplyFillRatio(displayedRatio);
            return;
        }

        if (slideCoroutine != null)
        {
            StopCoroutine(slideCoroutine);
        }

        // Chỉ chạy Coroutine trượt thanh máu trong khoảng thời gian máu biến thiên, không chạy Update mỗi frame
        slideCoroutine = StartCoroutine(SlideBarRoutine());
    }

    private IEnumerator SlideBarRoutine()
    {
        while (Mathf.Abs(displayedRatio - targetRatio) > 0.001f)
        {
            displayedRatio = Mathf.MoveTowards(
                displayedRatio,
                targetRatio,
                slideSpeed * Time.deltaTime
            );

            ApplyFillRatio(displayedRatio);
            yield return null;
        }

        displayedRatio = targetRatio;
        ApplyFillRatio(displayedRatio);
        slideCoroutine = null;
    }

    private void ApplyFillRatio(float ratio)
    {
        if (fillRect == null)
        {
            return;
        }

        Vector2 anchorMax = fillRect.anchorMax;
        anchorMax.x = Mathf.Clamp01(ratio);
        fillRect.anchorMax = anchorMax;
    }

    private void UpdateTextLabel(int current, int max)
    {
        if (valueText != null)
        {
            valueText.text = $"{labelPrefix}: {current}/{max}";
        }
    }
}

