# Ma trận truy vết 27 use case Must và ngưỡng NFR – theo SRS chính thức

Soạn 28/11/2026 · #1 Phạm Gia Thăng (phụ trách SRS, use case). Trả lời QA-02 và QA-04 trong review PR #304.

**Nguồn:**
- **SRS chính thức** v1.0.3, mục 3.2 (bảng MoSCoW) và mục 5 (NFR). Bản Word ở PR #301: `docs/srs/SRS_ban-cuoi_du-thao_28-11-2026.docx`.
- Test lấy từ lượt chạy toàn bộ 877 test trên PostgreSQL 17.12 cô lập (#1, 27/11, commit `cc20336`, 877/877 đạt).

> Các tệp `requirements-traceability.md`, `acceptance-criteria.md`, `non-functional-requirements.md`, `moscow-priority.md`, `srs.md` trong thư mục này mang nhãn **SIMULATED**. Đó là bản giả lập giai đoạn 1–3, **không phải SRS chính thức**: mã NFR-02, NFR-03… và nhãn POS-08, NH-03 ở đó khác nghĩa với SRS. Kiểm thử và báo cáo cuối dùng mã ở tệp này.

## 1. 27 use case Must (SRS mục 3.2)

Cột "Test tự động" ghi tên lớp test (số ca) chứng minh trực tiếp use case. "Chưa có test riêng" nghĩa là chưa tìm thấy test tự động gắn trực tiếp với use case, không có nghĩa là chức năng lỗi.

| # | Use case | Issue | Test tự động (lớp, số ca) | Ghi chú |
| --- | --- | --- | --- | --- |
| 1 | AUTH-01 Đăng nhập | #23 | `Auth.DichVuXacThucTests` (26); `PostgreSqlRuntimeTests.Auth_CreateLoginChangePasswordLogout_AndLockAfterFiveFailures` | Khóa tạm kiểm trước mật khẩu (chốt 23/11) |
| 2 | AUTH-02 Đăng xuất | #24 | `Auth.DichVuXacThucTests`; `PostgreSqlRuntimeTests.Auth_…Logout…` | Đăng xuất hủy giỏ đang bán (POS-09) |
| 3 | AUTH-03 Đổi mật khẩu | #25 | `Auth.DichVuTaiKhoanTests` (18); `PostgreSqlRuntimeTests.SM01_ForcedPassword_…` | |
| 4 | AUTH-05 Xem danh sách tài khoản | #26 | `Auth.DichVuTaiKhoanQuanTriTests` (16) | |
| 5 | AUTH-06 Tạo tài khoản | #27 | `Auth.DichVuTaiKhoanQuanTriTests`; `Auth.KhoiTaoAdminDauTienTests` (15) | SĐT không bắt buộc (chốt 23/11) |
| 6 | AUTH-10 Gán vai trò và phân quyền | #28 | `Auth.QuyenVaTokenTests` (20); `Auth.DichVuTaiKhoanTests.GanVaiTro_…` | 27 mã quyền |
| 7 | SP-01 Xem, tìm, lọc sản phẩm | #56 | `Sp01ProductHttpTests` (22), `Sp01ProductUnitTests` (16), `Sp01ProductPostgreSqlTests` (1) | |
| 8 | SP-03 Thêm sản phẩm | #58 | `Sp03ProductHttpTests` (16), `Sp03ProductUnitTests` (6), `Sp03Image*` (9), `Sp03ProductPostgreSqlTests` (2) | |
| 9 | SP-04 Sửa thông tin sản phẩm | #59 | `Sp04ProductHttpTests` (12), `Sp04ProductUnitTests` (5), `Sp04ProductPostgreSqlTests` (3) | |
| 10 | SP-05 Cập nhật giá bán | #60 | `Sp05PricingHttpTests` (16), `Sp05PricingUnitTests` (10), `Sp05PricingPostgreSqlTests` (5); `PostgreSqlRuntimeTests.SM02_…` | |
| 11 | SP-07 Xem danh sách danh mục | #62 | `CategoryHttpFeatureTests` (28), `CategoryServiceTests` (9), `CategoriesControllerTests` (7) | |
| 12 | SP-08 Thêm/sửa danh mục | #63 | `Sp08CategoryHttpTests` (13), `Sp08CategoryUnitTests` (5); `HaTang.DanhMucConcurrencyTests` (15) | |
| 13 | KHO-01 Xem tồn kho hiện tại | #89 | `Inventory.DichVuKhoTests.LayTonKho_KhongKhoa_TraDungThuTuVaCoDuoiNguong` | Chỉ có test tầng dịch vụ; **chưa có test HTTP/trang tồn kho** |
| 14 | NH-01 Xem danh sách NCC | #65 | `PostgreSql.PurchasingParityTests` (SM08/SM13, 14 ca) | |
| 15 | NH-02 Thêm/sửa NCC | #66 | **Chưa có test riêng** | Cần test tạo, sửa, trùng SĐT/mã NCC |
| 16 | NH-04 Tạo phiếu nhập (nháp) | #92 | `Inventory.PurchaseReceiptFlowTests` (2) | Chỉ qua luồng xác nhận; nên thêm test tạo nháp |
| 17 | NH-06 Xác nhận nhập kho | #94 | `TichHopDichVuKhoTests.Nh06_…`; `PurchaseReceiptFlowTests.ConfirmingDraft…`; `PostgreSqlRuntimeTests.Purchasing_ConfirmTwice…` | |
| 18 | NH-07 Tra cứu phiếu nhập | #95 | `PostgreSql.PurchasingParityTests` (lọc NCC, ngày, trạng thái) | |
| 19 | POS-01 Tạo đơn, tìm, thêm vào giỏ | #33 | `PostgreSqlRuntimeTests.Cart_ConcurrentFirstRequests…`, `…Search_PreservesVietnameseAccent…`; `PosPromotionDatabaseTests` | |
| 20 | POS-02 Chỉnh sửa giỏ hàng | #34 | **Chưa có test riêng** (gián tiếp: `PosHeldOrderTests`, `PosPromotionDatabaseTests.Cart_…`) | Cần test sửa số lượng, xóa dòng, vượt tồn |
| 21 | POS-05 Thanh toán tiền mặt, tiền thừa | #35 | `PostgreSqlRuntimeTests.Checkout_CommitsInvoiceStockPointsAndCash…` | Nên thêm ca biên: tiền khách đưa < tổng |
| 22 | POS-07 Hoàn tất đơn, in hóa đơn | #97 | `TichHopDichVuKhoTests.Pos07_*` (3); `…Checkout_…PreventsDuplicateSale`; `…ConcurrentCheckout_LastItemSoldOnce…`; `PosInvoiceReceiptRenderTests` (3) | Bán đồng thời món cuối |
| 23 | POS-09 Hủy đơn đang bán | #36 | `Auth.DichVuXacThucTests.Pos09_Logout_CancelsOnlyNonemptyCart…`; `…HeldOrder_HoldRecallAndCancel…` | |
| 24 | POS-10 Tra cứu hóa đơn | #98 | `PosInvoiceDetailsTests` (21), `PosInvoiceStoreTests` (2) | |
| 25 | BC-01 Dashboard tổng quan | #45 | `DashboardServiceTests` (8) | |
| 26 | BC-02 Doanh thu theo thời gian | #105 | `OperationalReportsServiceTests` (18); `…Reports_UseVietnamMidnightForUtcPaidAt` | |
| 27 | BC-03 Doanh thu theo sản phẩm, bán chạy | #106 | `OperationalReportsServiceTests`; `ReportSnapshotPostgreSqlTests` (6) | |

**Tổng hợp:**
- 25/27 use case Must có test tự động liên quan.
- NH-02 và POS-02 chưa có test riêng.
- KHO-01 và NH-04 mới chỉ có test tầng dịch vụ.
- Cả 27 issue đều đã đóng.
- Đề nghị #4 bổ sung 4 nhóm ca kiểm thử thủ công hoặc tự động cho NH-02, POS-02, KHO-01, NH-04 trước ngày 04/12.

## 2. Ngưỡng hiệu năng chính thức (SRS mục 5, "Đã chốt")

Đo trên bản chạy thử với dữ liệu mẫu **1.000 sản phẩm, 10.000 hóa đơn trong 6 tháng**.

| Mã | Nội dung | Ngưỡng | Use case |
| --- | --- | --- | --- |
| NFR-HN-01 | Gợi ý sản phẩm khi thu ngân gõ tìm | ≤ 1 giây | POS-01 |
| NFR-HN-02 | Hoàn tất đơn (lưu hóa đơn, trừ tồn, tích điểm) | ≤ 2 giây | POS-07 |
| NFR-HN-03 | Dashboard và báo cáo trong 1 tháng | ≤ 3 giây | BC-01 → BC-03 |
| NFR-HN-04 | Trang danh sách có phân trang | ≤ 2 giây mỗi trang | AUTH-05, SP-01, NH-07, POS-10 |
| NFR-HN-05 | Người dùng đồng thời | ≥ 5 (3 thu ngân, 1 kho, 1 Admin) vẫn đạt HN-01 → HN-04 | – |

Các nhóm NFR khác trong SRS mục 5: bảo mật NFR-BM-01 → 09, toàn vẹn NFR-TV-01 → 08, tính toán NFR-TT-01 → 04, dễ dùng NFR-SD, tương thích NFR-TC, bảo trì NFR-BT, giới hạn NFR-GH, pháp lý NFR-PL. #4 lấy đúng các mã này thay cho NFR-02 → NFR-07 trong `docs/06-testing/nfr-verification.md` (PR #304).

**Lưu ý NFR-BM-09 (HTTPS trực tuyến):** không có bản triển khai trực tuyến (#242 "not planned"); SRS bản cuối ghi theo hiện trạng (PR #301, mục C2).
