# SP-07 — Báo cáo kết quả thực thi kiểm thử

**Mã báo cáo:** SP07-TR-001
**Dự án:** Convenience Store Management System
**Ngày thực thi:** 30/10/2026
**Branch:** `integration/sp-07-codefirst`
**Commit:** chưa chốt commit cho bộ thay đổi SP-07; xác minh `git rev-parse --short HEAD` trước khi tạo PR.
**Phạm vi:** chức năng SP-07 — danh sách danh mục sản phẩm.

## 1. Tổng hợp

| Nhóm kiểm thử | Số trường hợp | PASS | FAIL | SKIP | Đánh giá |
|---|---:|---:|---:|---:|---|
| Unit Test (Controller + Service) | 10 | 10 | 0 | 0 | PASS |
| Repository Integration Test (EF Core + MySQL Docker) | 8 | 8 | 0 | 0 | PASS |
| HTTP Test thủ công | 4 | 4 | 0 | 0 | PASS (thủ công) |
| **Tổng** | **22** | **22** | **0** | **0** | **PASS trong phạm vi** |

**Tổng test tự động:** 18/18 PASS. **HTTP kiểm tra thủ công:** 4/4 PASS. Không đồng nhất 22 trường hợp trên thành 22 test tự động.

## 2. Môi trường và tính an toàn

- Windows PowerShell, ASP.NET Core MVC, .NET 10, xUnit, EF Core/MySQL provider.
- Integration Test: MySQL Docker 8.4.13 qua `127.0.0.1:3307`; database `sp07_repository_tests`; đã áp dụng 23 migrations.
- `CreateDbContext()` kiểm tra chính xác server, port, database và tài khoản để không kết nối nhầm database chia sẻ.
- Dữ liệu kiểm thử được tạo bằng EF Core và rollback trong `finally`.
- Sau khi chạy 8 test, bốn bảng `danh_muc`, `san_pham`, `tai_khoan`, `vai_tro` đều có 0 bản ghi; dữ liệu test không còn tồn tại.
- Không thực hiện DML/DDL hoặc migration lên Aiven trong đợt kiểm thử SP-07.

## 3. Kết quả và traceability

| Nhóm | Test IDs | Mục tiêu xác minh | Kết quả |
|---|---|---|---|
| Controller | SP07-UT-04 đến UT-10 | Status code, truyền tham số, chuẩn hóa kích thước trang | PASS |
| Service | SP07-UT-01 đến UT-03 | Chuyển tiếp query, token, dữ liệu | PASS |
| Repository | SP07-DB-01 đến DB-08 | Trang rỗng, ba danh mục, phân trang, đếm sản phẩm, trạng thái | PASS |
| HTTP thủ công | SP07-HTTP-01 đến HTTP-04 | 200/400 và JSON theo yêu cầu | PASS thủ công |
| Fixture | kiểm tra sau test | `categories=0`, `products=0`, `users=0`, `roles=0` | PASS |

Chi tiết dữ liệu, bước thực hiện và kỳ vọng từng trường hợp được ghi ở [`sp-07-test-cases.md`](sp-07-test-cases.md).

## 4. Bằng chứng

| Mã | Đường dẫn tương đối | Nội dung được xác nhận |
|---|---|---|
| SP07-EVD-UT | `evidence/sp-07/unit-tests.txt` | `Total tests: 10`, `Passed: 10`, `Build succeeded` |
| SP07-EVD-DB | `evidence/sp-07/integration-tests.txt` | `Total tests: 8`, `Passed: 8`, `Build succeeded` |
| SP07-EVD-RB | `evidence/sp-07/database-rollback.txt` | Bốn bảng nghiệp vụ test có 0 bản ghi sau kiểm thử |
| SP07-EVD-HTTP | Chưa có artifact riêng trong đợt evidence này | Kết quả thủ công từ lần kiểm tra API trước đó; chưa có file log chuyên biệt |

**Bảo mật trước commit:** log rollback thô từng chứa mật khẩu test trên dòng lệnh do PowerShell lưu stderr. Chỉ commit bản đã làm sạch; rà soát mọi file evidence để không đưa mật khẩu, token, connection string hoặc User Secrets vào Git.

## 5. Vấn đề, giới hạn và rủi ro

| ID | Nhận xét | Trạng thái | Hành động tiếp theo |
|---|---|---|---|
| SP07-OBS-01 | Danh mục `DA_AN` vẫn được trả về | Hành vi hiện tại đã test PASS; nghiệp vụ chưa phê duyệt | Xác nhận yêu cầu lọc/hiển thị |
| SP07-OBS-02 | Sản phẩm `NGUNG_KINH_DOANH` vẫn được tính vào `SoSanPham` | Hành vi hiện tại đã test PASS; nghiệp vụ chưa phê duyệt | Xác nhận cách tính |
| SP07-OBS-03 | Chưa có kiểm thử HTTP tự động và xác thực/phân quyền đầy đủ | PENDING | Thiết kế HTTP E2E với fixture độc lập |
| SP07-OBS-04 | Fixture Repository Test giả định DB rỗng và chạy không đồng thời với tác vụ ghi khác | Rủi ro tái lập | Giữ database độc lập; cải thiện cô lập khi mở rộng CI |
| SP07-OBS-05 | File rollback từng chứa dòng lệnh có mật khẩu test | CẦN XỬ LÝ trước Git | Làm sạch evidence và kiểm tra secrets |

## 6. Quyết định đánh giá

**PASS trong phạm vi triển khai SP-07 đã kiểm thử**: 10/10 Unit Test, 8/8 Repository Integration Test, 4/4 HTTP Test thủ công; rollback được xác minh. Chưa kết luận đã nghiệm thu hoàn toàn chức năng do còn quyết định nghiệp vụ về trạng thái danh mục/sản phẩm và chưa có bằng chứng phân quyền API.

**Chưa commit:** cần làm sạch evidence, xác minh `git diff --check`, kiểm tra danh sách file được stage có đúng phạm vi SP-07 và chốt Git HEAD. Không thay đổi tài liệu baseline kiểm thử schema Aiven (`test-plan.md`, `database-test-cases.md`, `test-report.md`) vì đó là phạm vi khác.
