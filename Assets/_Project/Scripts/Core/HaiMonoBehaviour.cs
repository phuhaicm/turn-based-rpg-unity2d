using UnityEngine;

public abstract class HaiMonoBehaviour : MonoBehaviour
{
    protected virtual void Reset()
    {
        LoadComponents();
        ResetValue();

#if UNITY_EDITOR
        // Lưu lại các tham chiếu vừa tự động tìm được vào Scene/Prefab trong Editor
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    protected virtual void Awake()
    {
        // Gọi lại làm chốt chặn an toàn nếu quên bấm Reset trong Inspector
        LoadComponents();
    }

    protected virtual void OnEnable()
    {
    }

    protected virtual void Start()
    {
    }

    protected virtual void OnDisable()
    {
    }

    protected virtual void LoadComponents()
    {
        // Các lớp con override hàm này để gom toàn bộ GetComponent về một chỗ
    }

    protected virtual void ResetValue()
    {
        // Các lớp con override hàm này để đặt giá trị mặc định cho biến
    }
}
