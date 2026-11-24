# CI GitHub Actions — .NET 10 và PostgreSQL 17 riêng

> Cập nhật ngày 24/11/2026 cho nhánh `integration/smartmart-postgresql-only`. Workflow hiện chỉ dùng PostgreSQL. Kết quả GitHub của bộ commit tích hợp này: **NOT RUN**.

## Cách hoạt động

[Workflow](../../.github/workflows/ci.yml) chạy khi mở/cập nhật PR, khi push vào `main`/`develop`, hoặc qua `workflow_dispatch` khi workflow có trên nhánh phù hợp. Một commit mới trên cùng PR hủy lượt CI cũ còn chạy. Chuẩn bị commit cục bộ không kích hoạt workflow.

Job **Build and test** dùng Ubuntu 24.04, .NET SDK `10.0.x`, service `postgres:17-alpine` và giới hạn 25 phút:

1. Checkout mã; các action checkout/setup-dotnet/upload-artifact được ghim bằng commit SHA.
2. Restore solution và build Release.
3. Publish ứng dụng MVC/Razor rồi chạy [verify-postgresql-runtime.ps1](../../scripts/ci/verify-postgresql-runtime.ps1): bắt buộc Npgsql, EF Core 10 cùng phiên bản; từ chối MySQL/Pomelo và cấu hình development trong publish output.
4. Chạy toàn bộ project test ở Release với PostgreSQL 17 riêng, không restore/build lại.
5. [verify-trx.ps1](../../scripts/ci/verify-trx.ps1) từ chối kết quả thiếu test, có lỗi, bị bỏ qua hoặc chưa thực thi đủ.
6. Upload TRX kể cả khi test lỗi; giữ artifact 7 ngày.

## PostgreSQL và cấu hình

- Mỗi job có service PostgreSQL dùng một lần, health check và cổng host do GitHub cấp động. Container được dọn khi job kết thúc.
- `ConnectionStrings__CuaHangPostgreSqlTest` trỏ tới service loopback này. Fixture kiểm tra PostgreSQL major 17, từ chối host ngoài loopback và tạo/xóa database tên ngẫu nhiên của chính fixture.
- Tài khoản có quyền tạo/xóa database chỉ dùng trong service CI riêng. Mật khẩu service được khai báo công khai trong workflow để khởi tạo container tạm, không phải credential Supabase hoặc tài khoản của nhóm.
- Không dùng `.env` cá nhân, `ConnectionStrings__CuaHang` phát hành hoặc secret Supabase chung cho job test. Không giữ provider/service MySQL dự phòng.
- Token chỉ có `contents: read`, checkout không giữ credential; không dùng `pull_request_target` để thực thi mã PR.
- SDK `10.0.x` và image `postgres:17-alpine` chưa ghim đầy đủ patch/digest; SM-15 vẫn mở cho release tái lập. Bằng chứng cục bộ trước đó dùng SDK 10.0.401 và PostgreSQL 17.11, không chứng minh runner Linux đã chạy thành công.

## Sau khi người dùng cho phép push

1. Mở/cập nhật PR, chờ **SmartMart CI / Build and test** hoàn tất trên đúng commit chứa thay đổi.
2. Nếu lỗi, xem step tương ứng và tải artifact `test-results-<run-id>-<attempt>`; không đánh dấu CI đạt từ kết quả máy cá nhân.
3. Chỉ sau khi check xuất hiện và chạy thành công, owner mới quyết định required status check/ruleset. Commit workflow không tự bật bảo vệ nhánh.
4. PR bên ngoài có thể cần người có quyền cho phép chạy theo cài đặt repository.

## Kiểm tra tại máy và giới hạn bằng chứng

Các lệnh restore/build/publish/test hiện hành nằm trong [hướng dẫn migration](../db/POSTGRESQL-MIGRATIONS.md). Mọi test ghi chỉ dùng PostgreSQL 17 riêng; không chạy migration, seed, reset hoặc test ghi trên Supabase chung. Kết quả cục bộ không xác minh GitHub runner/service container.

[Báo cáo 763/763 test](test-reports/2026-11-24-postgresql-integration.md) là bằng chứng của lần chạy trước khi commit. [Báo cáo chuẩn bị commit](test-reports/2026-11-24-commit-preparation.md) ghi rõ build chạy lại và kiểm tra hash source; không có lần test database mới trong lượt chuẩn bị này. Bằng chứng CI/MySQL lịch sử được giữ trong [báo cáo 23/11](test-reports/2026-11-23-ci-schema.md).
