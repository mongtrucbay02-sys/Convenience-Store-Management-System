Implementation update 2026-11-24: the integration branch now uses PostgreSQL/Npgsql only; the dual-provider transition described below is historical. PostgreSQL baseline/snapshot, role-specific RLS and invoker views are tracked in the repository. The existing price ceiling remains a business rule; no broader price policy or SRS approval is claimed. See [current migration contract](../db/POSTGRESQL-MIGRATIONS.md) and [integration evidence](../06-testing/test-reports/2026-11-24-postgresql-integration.md).

---

# Bổ sung kỹ thuật SRS — PostgreSQL / Supabase, 23/11/2026

Trạng thái: đã hiện thực/kiểm thử cục bộ; chờ nhóm duyệt cập nhật SRS/ADR. Căn cứ: nhóm yêu cầu chuyển từ MySQL/Aiven sang PostgreSQL 17/Supabase và bản catalog 31 bảng do nhóm cung cấp. Không tự gắn trạng thái Approved cho ADR-003 hay thay thế bộ đặc tả nghiệp vụ gốc.

- Backend giữ ASP.NET Core MVC và một CuaHangDbContext; bổ sung Npgsql cho PostgreSQL 17, schema smartmart. MySQL vẫn có nhánh tương thích trong giai đoạn chuyển tiếp.
- Chuyển bigint sang long, date sang DateOnly, timestamptz lưu UTC; ngày giao dịch/bộ lọc/hiển thị theo UTC+7. Giữ sao chép giá/giá vốn trên hóa đơn.
- Giữ các actor AD/TN/NK, quyền máy chủ, CSRF và bắt buộc đổi mật khẩu. Phiên PostgreSQL ghi/gia hạn het_han_luc rõ ràng; đặt lại mật khẩu vẫn gỡ khóa tạm và hủy phiên cũ.
- Cấp mã chứng từ bằng upsert có khóa; lấy khóa mới bằng INSERT RETURNING, không dựa vào lastval có thể bị trigger đổi sequence.
- Thanh toán/đổi trả/hủy vẫn gom hóa đơn, tồn kho, điểm và sổ quỹ trong cùng giao dịch. Giữ cột sinh, trigger bất biến và chống xử lý lặp.
- Giữ tìm kiếm tiếng Việt không phân biệt hoa/thường/dấu cho sản phẩm/POS/khách hàng; PostgreSQL dùng lower/translate, không cần cài extension hoặc thêm function lên CSDL nhóm.
- CI dùng database PostgreSQL 17 và MySQL 8.4 độc lập; không lấy dữ liệu cloud của nhóm làm dữ liệu test.

Không bổ sung use case mới, không tự thêm bảng vào Supabase. Script schema/seed là sản phẩm tham chiếu chờ nhóm CSDL duyệt; baseline EF PostgreSQL, DB role/RLS và nghiệm thu staging còn cần chốt riêng.

Đối chiếu: [ánh xạ](../04-system-design/supabase-entity-mapping-20261123.md), [cấu hình](../05-implementation/setup/postgresql-runtime-setup.md), [kết quả kiểm thử](../06-testing/test-reports/2026-11-23-postgresql-runtime.md).
