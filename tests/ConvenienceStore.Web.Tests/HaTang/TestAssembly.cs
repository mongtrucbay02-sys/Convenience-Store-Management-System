using Xunit;

// Các fixture tạo/xóa database tạm trên cùng một server PostgreSQL 17 cục bộ.
// Chạy tuần tự để DDL/cleanup giữa các collection không tranh chấp nhau.
// Test đồng thời nghiệp vụ vẫn tạo request song song bằng Task.WhenAll bên trong từng test.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
