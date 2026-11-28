# Đề xuất cập nhật SRS theo migration runtime — 23/11/2026

> **Trạng thái:** Đề xuất / chờ nhóm duyệt. Không thay thế SRS được duyệt hoặc bộ 81 use case gốc.
>
> **Phụ trách:** #5 Nguyễn Tiến Trung · **Người duyệt:** Chưa xác nhận · **Có dùng AI:** AI-TRUNG-004.

## Nguồn và giới hạn

Đối chiếu mã `CuaHangDbContext` và ba migration hiện hành với [schema v1](../db/schema_v1.sql), [change list SRS v1.2](../srs/SRS_v1.2_danh-sach-thay-doi.md), [SRS ứng viên](srs.md), [ERD/từ điển đang có](../04-system-design/database/) và [bản đối chiếu vật lý](../04-system-design/database/reconciliation-20261123/README.md). SRS Word gốc nằm ngoài repo theo tài liệu hiện có; chưa thể xác nhận mọi điều khoản của bản gốc đã được đối chiếu.

## Nội dung đề xuất để nhóm đưa vào SRS chính thức

| Mã | Mục liên quan | Nội dung đề xuất | Căn cứ / việc cần chốt |
|---|---|---|---|
| SRS-DB01 | Chương CSDL/ERD | Ghi baseline 28 bảng + ba bảng mở rộng, ba view, 12 trigger; tách tài liệu lịch sử và schema runtime | Migration Pos12CancelInvoice/CashLedger; duyệt DB-R06 |
| SRS-DB02 | POS-12 | Hủy hóa đơn đã thanh toán chỉ dành cho Admin; giữ hóa đơn, trả tồn/thu hồi điểm, ghi lý do/người làm/thời điểm và bản ghi hoàn tiền duy nhất theo hóa đơn | `hoan_tien_hoa_don`, `PosInvoiceCancellationService`; đối chiếu với đặc tả gốc trước khi ký duyệt |
| SRS-DB03 | POS-07, DT-04 | Thu bán tiền mặt và chi hoàn phiếu trả nằm trong transaction nghiệp vụ; khóa quỹ, không âm; sổ lưu số tiền, số dư trước/sau và liên kết chứng từ | `CashLedgerService`, `MySqlPosCheckoutStore`, ReturnOrderService |
| SRS-DB04 | POS-12/đối soát quỹ | Chốt cách ghi khoản chi do hủy hóa đơn vào sổ quỹ, đảm bảo không ghi trùng và rollback đồng bộ | **Khoảng thiếu DB-R01**, chưa triển khai đường ghi quỹ do hủy; có thể cần migration mới |
| SRS-DB05 | Triển khai dữ liệu hiện có | Chốt số dư mở quỹ và thời điểm bắt đầu sổ khi database đã có hóa đơn trước CashLedger | Seed hiện bằng 0; **DB-R02**, không tự backfill theo suy luận |
| SRS-DB06 | Audit/phi chức năng | Phân biệt audit POS-12 bất biến ở DB với sổ quỹ mới chưa có trigger bất biến; xác định retention và quyền chỉnh sửa | DB-R03/04; chưa có quyết định đồng nghĩa mọi sổ đều append-only |
| SRS-DB07 | ORM/migration | Một context vận hành; baseline ánh xạ EF, ba bảng mới hiện do SQL migration quản lý. Chọn cơ chế theo dõi về sau, giữ lịch sử migration | Không tạo `ApplicationDbContext` thứ hai; không sửa migration đã phát hành |
| SRS-DB08 | Nền tảng/kiểm thử | CI .NET 10 và MySQL 8.4 tạm cho từng job; fixture tự tạo/xóa DB, không dùng database phát triển chung | Workflow mới chưa chạy GitHub tại thời điểm soạn; ADR cũ ghi 9.7.2 cần nhóm chốt phiên bản |
| SRS-DB09 | BC-06, đơn vị tiền | Giữ số thập phân của giá vốn bình quân khi tính giá trị tồn; làm tròn tổng khi hiển thị VND | Regression và E2E 22/11; không làm tròn đơn giá trước khi nhân tồn |
| SRS-DB10 | FR-07 ứng viên/NH-04 | Chốt có thực hiện số theo chứng từ, số thực nhận và chênh lệch theo dòng hay giữ `so_luong` hiện có | FR-07 mới là đề xuất; schema chưa có ba trường riêng, không đánh dấu đã hoàn thành |
| SRS-DB11 | KM-02/03 | Chốt cách lưu ảnh KM: hiện có input URL nhưng chưa có persistence | Chưa hoàn thành; không coi nhập được URL là đã lưu được ảnh |

## Cách thông qua

1. Nhóm chọn phiên bản SRS/ERD nguồn và người duyệt theo module, xác nhận phạm vi use case gốc.
2. Ghi quyết định cho từng dòng cùng người/ngày/PR. Nội dung chưa xác nhận giữ trạng thái Đề xuất.
3. Bổ sung migration **mới** nếu thay đổi DDL, cập nhật bản schema/ERD được chọn và test nghiệp vụ tương ứng.
4. Chạy CI thật, review độc lập và staging trước khi đánh dấu Đã triển khai/Đã nghiệm thu.

Tài liệu này chỉ chuẩn bị nội dung để nhóm duyệt; chưa sửa tệp Word SRS bên ngoài và chưa xác nhận các quyết định đang mở.
