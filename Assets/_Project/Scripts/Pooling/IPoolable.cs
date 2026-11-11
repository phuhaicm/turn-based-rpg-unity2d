public interface IPoolable
{
    // Được gọi mỗi khi lấy Object ra khỏi hàng đợi Pool để reset lại trạng thái ban đầu
    void OnSpawnFromPool();

    // Được gọi ngay trước khi trả Object về lại Pool để dừng Coroutine hoặc hiệu ứng
    void OnReturnToPool();
}

