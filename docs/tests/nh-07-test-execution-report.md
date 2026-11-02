# NH-07 — Test Execution Report (Draft)

- Issue: #95; branch: `feature/nh-07-receipt-list`; code SHA tested: `aeba11c781281b9af8bef568036fc1b5cb0ed49e`.
- Date: 2026-11-02; Windows PowerShell, .NET SDK 10.0.401, .NET 10, EF Core/MySQL provider. Không kết nối database trong phiên NH-07.
- Phạm vi: DTO, bộ lọc/validation service, EF Core query shape ở mức code review, DI. Không có HTTP endpoint, migration hoặc ghi dữ liệu.

| Command | Exit | PASS | FAIL | SKIPPED | Ghi chú |
|---|---:|---:|---:|---:|---|
| `dotnet restore .\ConvenienceStoreManagement.slnx` | 0 | — | — | — | Restore thành công |
| `dotnet test ... --no-restore --filter FullyQualifiedName~ReceiptListServiceTests` trước implementation | 1 | 0 | — | — | CS0234/CS0246: DTO/service chưa tồn tại, red phase có chủ đích |
| `dotnet build .\ConvenienceStoreManagement.slnx --no-restore` sau implementation | 0 | — | — | — | 0 warning, 0 error |
| `dotnet test ... --no-build --filter FullyQualifiedName~ReceiptListServiceTests` trên code SHA trên | 0 | 4 | 0 | 0 | 4 test case xUnit thực thi (3 phương thức, 1 Theory có 2 dữ liệu) |
| `dotnet test ... --no-build --filter FullyQualifiedName!~Integration` | 0 | 12 | 0 | 0 | Regression trên cùng source tree, trước docs commit |

## Coverage và giới hạn

NH07-TC-01..04 VERIFIED ở service. Repository dùng `AsNoTracking`, lọc mã phiếu/NCC ID/ngày nhập/trạng thái, chiếu tên NCC/người lập, sắp ngày/ID giảm dần; mới qua build và self-review, **chưa được xác minh với MySQL thật**. NH07-TC-05 BLOCKED vì không có target test riêng NH-07 được duyệt. NH07-TC-06 BLOCKED vì stack schema thiếu AUTH từ `develop`; không mở endpoint không được bảo vệ. CI, staging, review độc lập NOT RUN. Không đánh dấu feature ACCEPTED.

Lỗi setup trước khi test-first: lần ghi test đầu gặp `DirectoryNotFoundException` vì nhánh schema chưa có thư mục `Modules/Purchasing` trong test project; tạo thư mục mới và ghi lại thành công. CS0234/CS0246 biến mất sau khi triển khai. Không sửa hoặc xóa assertion. Không chạy full suite vì các integration khác cần kết nối riêng.
