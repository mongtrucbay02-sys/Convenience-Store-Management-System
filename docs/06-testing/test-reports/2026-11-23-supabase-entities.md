# Kiểm chứng ánh xạ Supabase — 23/11/2026

Trạng thái: hoàn thành lớp entity/mapping, chưa nghiệm thu toàn bộ ứng dụng PostgreSQL. Nhánh cục bộ `feature/supabase-entities-20261123`; chưa commit/push.

## Kết quả thực chạy

| Kiểm tra | Kết quả | Giới hạn |
|---|---|---|
| Build ứng dụng + project test (.NET 10) | 0 lỗi, 0 cảnh báo | Cục bộ, chưa CI nhánh này |
| Đối chiếu catalog Supabase thật | 31 bảng, 307 cột, 31 PK, 62 FK, 89 CHECK, 17 trigger, 3 view | Chỉ đọc metadata, không duyệt thiết kế thay nhóm CSDL |
| SELECT qua EF trên Supabase | 34/34 bảng/view truy vấn thành công | Take(1), tổng 2 dòng được đọc; nhiều bảng rỗng, chưa kiểm chứng ghi/cập nhật hoặc đủ dữ liệu nghiệp vụ |
| Hồi quy đầy đủ MySQL tạm 8.4.13 | 740/740 đạt, 0 bỏ qua, 2 phút 36 giây | Server localhost:33592 và CSDL test riêng, không dùng CSDL nhóm |
| Sau thay đổi cuối giữ giới hạn lọc giá + hai ca biên mới | 41/41 test liên quan đạt, 0 bỏ qua | Gồm Sp01ProductUnitTests, SupabaseModelMappingTests, CustomerRulesTests, GeneratedColumnsMetadataTests; không chạy lại toàn bộ 742 ca sau thay đổi nhỏ cuối |
| So model MySQL với snapshot cũ | 0 migration operation | Không thay schema MySQL hiện có |
| Whitespace và cấu hình | git diff --check đạt; không có mật khẩu trong tệp thay đổi | .env thật bị Git ignore; ví dụ dùng placeholder |

Bộ mới có 742 ca sau khi thêm hai ca biên cuối. Không ghi kết quả 742/742 toàn bộ vì chưa chạy đủ bộ trên revision cuối. Kết quả đầy đủ 740/740 và đợt 41/41 được ghi riêng ở trên.

## Những lỗi tương thích đã xử lý

- Khóa shared entity khuyến mãi dùng cùng kiểu long với khóa chính.
- Giữ kiểu lưu unsigned/nullable/identity của MySQL; khóa tạm EF nằm trong phạm vi unsigned thay vì số âm.
- DateOnly chuyển qua DateTime tại provider MySQL cũ; PostgreSQL giữ kiểu date gốc.
- Không ghi năm cột sinh trong INSERT/UPDATE.
- Razor Products/Prices, Purchasing/Create và Inventory/Stock dùng long thay ép kiểu uint.
- Giữ giới hạn giá API hiện có; kiểm tra số dư điểm bigint không âm/không tràn số.

## Chạy lại test ánh xạ không cần CSDL

Tại thư mục repository:

```powershell
dotnet test .\tests\ConvenienceStore.Web.Tests\ConvenienceStore.Web.Tests.csproj --filter "FullyQualifiedName~SupabaseModelMappingTests"
```

Fixture `tests/ConvenienceStore.Web.Tests/PostgreSql/supabase-schema-contract.json` lấy từ catalog thật, không chứa mật khẩu hay dữ liệu người dùng. Các test kiểm tra đủ cột/kiểu/nullability, tên và các cột của FK, CHECK/trigger, cột sinh, view keyless, kiểu gốc, concurrency, migration guard và khóa tạm MySQL.

## Phần tiếp theo

Chuyển SQL/cách xử lý exception MySQL sang PostgreSQL trong các service; sửa xử lý thời gian UTC và fixture/CI PostgreSQL; duyệt baseline PostgreSQL; kiểm tra quyền/RLS với role ứng dụng; chạy E2E đăng nhập, bán hàng, điểm, nhập/xuất kho, đổi trả, báo cáo và staging trên CSDL test riêng trước khi triển khai.

Trong đợt này không chạy migration, EnsureCreated hay INSERT/UPDATE/DELETE trên Supabase/Aiven dùng chung. Metadata trigger không triển khai lại hàm trigger, view hay RLS.

Thiết kế và danh sách entity–bảng: [ánh xạ Supabase](../../04-system-design/supabase-entity-mapping-20261123.md).
