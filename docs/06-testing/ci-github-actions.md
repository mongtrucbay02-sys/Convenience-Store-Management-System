# CI GitHub Actions — .NET 10 và MySQL riêng

> **Trạng thái:** Đã viết workflow, chờ chạy trên GitHub và review độc lập.
>
> **Phụ trách:** #5 Nguyễn Tiến Trung · **Ngày:** 2026-11-23.
>
> **Có dùng AI:** OpenAI Codex, xem AI-TRUNG-004 trong [nhật ký](../09-ai-usage/ai-usage-log.md).

## Cách hoạt động

[`.github/workflows/ci.yml`](../../.github/workflows/ci.yml) chạy khi mở/cập nhật PR vào bất kỳ nhánh nào, khi push vào `main`/`develop`, hoặc qua `workflow_dispatch` khi workflow đã xuất hiện trên nhánh mặc định. Một commit mới trên cùng PR hủy lượt CI cũ còn chạy.

Job **Build and test** dùng Ubuntu 24.04, .NET SDK `10.0.x`, service chính thức `mysql:8.4` và giới hạn 25 phút:

1. Checkout mã; action checkout/setup-dotnet/upload-artifact được ghim bằng commit SHA từ repository chính thức.
2. `dotnet restore ConvenienceStoreManagement.slnx`.
3. `dotnet build ConvenienceStoreManagement.slnx --configuration Release --no-restore`.
4. Chạy toàn bộ project test ở Release, không restore/build lại.
5. [verify-trx.ps1](../../scripts/ci/verify-trx.ps1) từ chối kết quả không có test, có lỗi, bị bỏ qua hoặc chưa thực thi đủ.
6. Upload TRX kể cả khi test lỗi; giữ artifact 7 ngày.

## MySQL và cấu hình

- Mỗi job có service MySQL dùng một lần, health check và cổng host được GitHub cấp động. Container bị dọn khi job kết thúc.
- `ConnectionStrings__CuaHangTest` chỉ trỏ tới service này. Fixture tạo `cuahang_test_*`, áp migration thật, chạy test rồi xóa database.
- Root được dùng **chỉ trong service CI** vì fixture cần tạo/xóa database và tạo trigger. Password `ci-only-password` là giá trị công khai dành cho service tạm, không phải thông tin tài khoản của nhóm.
- Không cần GitHub secret Aiven, `.env` cá nhân hoặc `ConnectionStrings__CuaHang` của ứng dụng. Không đặt chúng vào workflow.
- Không dùng `pull_request_target` để chạy mã PR; token chỉ có `contents: read`, checkout không giữ credential.
- Chưa ghim patch/digest của image `mysql:8.4`; đây là nhánh tương thích Aiven. Bằng chứng cục bộ hiện dùng 8.4.13, không phải khẳng định đã chạy MySQL 9.7.2 như tài liệu cũ. Nhóm cần chốt lại ma trận phiên bản trong ADR/SRS.

Tài liệu tham khảo: [GitHub service containers](https://docs.github.com/en/actions/tutorials/use-containerized-services/use-docker-service-containers), [setup-dotnet](https://github.com/actions/setup-dotnet), [checkout](https://github.com/actions/checkout), [upload-artifact](https://github.com/actions/upload-artifact).

## Người dùng cần làm sau khi push

1. Mở PR, vào **Checks**, chờ **SmartMart CI / Build and test** kết thúc.
2. Nếu lỗi, mở step tương ứng và tải artifact `test-results-<run-id>-<attempt>`; không đánh dấu CI đạt từ kết quả chạy ở máy.
3. Chỉ sau khi check xuất hiện và chạy thành công, quản trị repo mới chọn **Build and test** làm required status check trong ruleset của `main`/`develop` nếu nhóm đồng ý. Thêm workflow không tự bật bảo vệ nhánh.
4. PR từ người đóng góp bên ngoài có thể cần người có quyền duyệt chạy workflow theo cài đặt GitHub hiện tại.

## Kiểm tra tại máy

- Dùng `actionlint` kiểm tra cú pháp workflow và GitHub expressions. Không cần Docker Desktop để viết/kiểm tra YAML.
- Khi chạy test tại máy, dùng MySQL riêng có quyền tạo/xóa database. Không dùng Aiven chung. Không thừa hưởng `ASPNETCORE_URLS` cố định hoặc chế độ Development từ script mở web; các test host có cấu hình riêng.
- Các lệnh restore/build/test trong workflow có thể chạy trên máy với `ConnectionStrings__CuaHangTest` riêng. Kết quả cục bộ không xác minh đầy đủ Ubuntu/service container/GitHub runner.
- Máy Windows hiện chưa có Docker daemon hoạt động, nên chưa chạy service container Linux tại máy. Trạng thái GitHub CI chỉ được xác nhận sau lượt run thật.
