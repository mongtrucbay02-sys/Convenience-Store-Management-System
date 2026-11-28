# SRS bản cuối – tổng hợp thay đổi so với SRS v1.0.3 (T9-02, issue #257)

Soạn 23/11/2026, cập nhật 28/11/2026 · #1 Phạm Gia Thăng. Gộp mọi thay đổi đã ghi trong repo để sửa **một lần** tệp Word SRS v1.0.3 (thư mục tài liệu chung của nhóm, tuần 3) thành SRS bản cuối.

Nguồn:
- `docs/srs/SRS_v1.1_danh-sach-thay-doi.md` (PR #231) và `docs/srs/SRS_v1.2_danh-sach-thay-doi.md`.
- `docs/03-requirements/cash-promotion-change-proposal-20261123.md` (PR #232).
- ADR-002 (PR #231) và ADR-003 (PR #281).
- Kết quả X1 → X10 đăng ở issue #257 ngày 27/11.

Đối chiếu code tại `develop` `cc20336`; CI 877/877.

Cột "Trạng thái":
- **Đã chốt**: có quyết định. Gồm các mục X1 → X10 không bị phản đối trước 20:00 ngày 25/11.
- **Đã cài đặt**: có code trên `develop`.
- **Chờ quyết định**: đã hỏi nhóm ở #257, chưa có trả lời. SRS chưa sửa mục này.
- **Đề xuất**: #1 đề xuất theo hiện trạng code, **chưa được #5 duyệt**. Không dùng làm bằng chứng nghiệm thu.

Sửa ngày 28/11 theo review của #2 ở PR #301 (SRS-R01 → R10). Mục nào còn "chờ quyết định" hoặc "đề xuất" thì chờ #5 chốt ở #257.

## 1. Thay đổi bắt buộc (SRS v1.0.3 đang ghi khác sản phẩm)

| Mã | Mục SRS | Sửa thành | Nguồn | Trạng thái |
| --- | --- | --- | --- | --- |
| C1 | 2.4, 2.5, 7.2 vấn đề 3 | Giao diện ASP.NET Core MVC (Razor) + Bootstrap, jQuery; trang đăng nhập HTML gọi REST. Tham chiếu ADR-002 | v1.2 | Đã cài đặt; **Đã chốt** (X1) |
| C2 | 2.4 (triển khai) | Ghi đúng hiện trạng triển khai tại thời điểm nộp. Đến 28/11: **chưa có bản web trực tuyến HTTPS**; ứng dụng chạy bằng `dotnet run`, kết nối CSDL qua biến môi trường. #242 (deploy HTTPS) đóng "not planned" 24/11 rồi **mở lại 27/11**, chưa có quyết định | v1.2; #242 | **Chờ quyết định** (#5, #4) |
| C3 / S5 | 3.4 AUTH-07 | AUTH-07 chỉ sửa họ tên, SĐT, email; đổi vai trò qua AUTH-10 | v1.1, v1.2 | Đã cài đặt |
| C4 | KH-01 | Tìm theo SĐT hoặc tên; mã khách là id nội bộ dạng `KH000012` | v1.2 | **Đã chốt** (X2) |
| C5 | 6.x, POS-09 | Đơn hủy lưu trong `gio_hang` (`DA_HUY`), không bảng riêng | v1.2 | Đã cài đặt |
| C6 | 4.x, 7.1 | Endpoint tra cứu hóa đơn cho DT-01 dùng quyền `DOI_TRA` | v1.2 | **Đã chốt** (X2) |
| S1 | 1.1, bảng module | KHO-01, 06, 07 thuộc #2; dự phòng #1 ↔ #2 (thực tế phần NH/KHO trên `develop` do #5 làm, PR #222) | v1.1 | Cập nhật theo thực tế |
| S4 | 2.4, 2.5, 6 (môi trường, ràng buộc, CSDL) | **PostgreSQL 17**, schema `smartmart`, truy cập bằng EF Core 10 + Npgsql; không còn MySQL. CI dựng PostgreSQL 17 tạm cho mỗi PR. CSDL dùng chung là Supabase. **Yêu cầu kiến trúc:** backend chạy bằng vai trò riêng `smartmart_backend` (RLS, không BYPASSRLS), không dùng `postgres`, không dùng Supabase Auth | ADR-003; #286; #293; PR #300 | CSDL: **Đã chốt** (ADR-003), chuyển xong 24/11 (#286). Vai trò `smartmart_backend`: **chưa nghiệm thu quyền ghi** – smoke chỉ đọc; #293 còn mở; PR #300 chỉ ra 5 chỗ lỗi quyền (WR-06 → 10) |

## 2. Quy tắc bổ sung đã chốt khi lập trình

| Mã | Mục SRS | Nội dung | Trạng thái |
| --- | --- | --- | --- |
| S2 | 2.4, 6 | Lược đồ: migration `PostgreSqlBaseline` (#286) trên PostgreSQL 17: 31 bảng, 3 view. `schema_v1.sql` (MySQL, 18/10) giữ làm lịch sử | Đã cài đặt; duyệt CSDL bổ sung 28/11 (PR #302). Smoke **đọc** trên Supabase đạt; **ghi trên Supabase chưa nghiệm thu (#293)** |
| S3 | AUTH, 6 | Admin đầu tiên tạo khi khởi động từ biến môi trường, bắt buộc đổi mật khẩu | Đã cài đặt |
| S6–S9 | AUTH-01 → 03, 06, 10, NFR-BM | Phiên `phien_dang_nhap`, cookie `cs_session` 30 phút; CSRF `X-XSRF-TOKEN`; 27 mã quyền; `403 PHAI_DOI_MAT_KHAU` | Đã cài đặt |
| S10–S11 | AUTH-08, 09 | Khóa hủy mọi phiên, không khóa Admin cuối; mở khóa gỡ cả khóa tạm; đặt lại mật khẩu trả một lần, bắt buộc đổi | Đã cài đặt |
| B1 | KH-01 | Tổng chi tiêu = hóa đơn đã thanh toán − tiền hoàn phiếu trả hoàn tất | **Đã chốt** (X2) |
| B2 | BR-KH-01 | Thu ngân thấy SĐT đã che; Admin thấy đủ | **Đã chốt** (X2) |
| B3 → B6 | KM-02, POS-04, POS-07 | Giá trị % nguyên 1–100; giảm tối đa theo đơn vị; hòa thì chọn chương trình kết thúc sớm hơn; tính lại khuyến mãi khi hoàn tất | **Đã chốt** (X2) |
| B7, B8 | DT-01, DT-02 | Hạn đổi trả theo chính sách hiệu lực, giờ Việt Nam; ngưỡng 500.000 đ trên tổng tiền hoàn | **Đã chốt** (X2) |
| B9 | Dịch vụ kho | `TRA_HANG`, `HUY_HOA_DON`; giá vốn bình quân làm tròn 2 chữ số | Đã cài đặt |
| CP-01 | POS-12 | Hủy hóa đơn tiền mặt ghi chi quỹ = tiền khách đưa − tiền thừa, cùng giao tác với hoàn kho, thu hồi điểm, lịch sử hoàn | Đã cài đặt; **Đã chốt** (X3) |
| CP-02 | POS-12 | Mỗi hóa đơn chỉ hủy/hoàn một lần; lịch sử chi không sửa/xóa | Đã cài đặt; **Đã chốt** (X3) |
| CP-03 | (mới) Quỹ tiền mặt | Admin chốt số dư mở quỹ một lần bằng số thực đếm; hệ thống lưu trước/sau/chênh lệch | Đã cài đặt; **Đã chốt** (X3) |
| CP-04 | KM-02, KM-03 | Ảnh khuyến mãi: URL HTTP/HTTPS ≤ 2048 ký tự, lưu xuống CSDL | Đã cài đặt; **Đã chốt** (X3) |
| R3 | POS-12 | Hủy hóa đơn bị từ chối (`QUY_KHONG_DU`) khi quỹ không đủ, kể cả khi chưa chốt số dư mở. Admin chốt số dư mở trước demo | Đã cài đặt; **Đã chốt** (X4) |

**Quy tắc nghiệp vụ giữ theo SRS (SRS-R08):** SP-06 (xóa hẳn sản phẩm chưa phát sinh giao dịch), NH-03 (xóa NCC chưa có phiếu nhập), NH-05 (sửa dòng phiếu nháp), KH-04 (xóa khách chưa có hóa đơn, có rồi thì ẩn và xóa thông tin cá nhân). Lỗi quyền ghi trên Supabase (PR #300) phải sửa ở code và cấu hình quyền, **không sửa SRS để hợp với lỗi**.

**Chờ duyệt, chưa đưa vào SRS (SRS-R07):** đề xuất REC-D01 → D07 ngày 28/11 của #2 ở PR #300 (cách tính dashboard theo giá trị hàng trả/đổi – BR-BC-02; mở quỹ – CP-03/R3; kiểm kê dữ liệu đếm cũ – KHO-04). Đây là baseline kỹ thuật đề xuất, chưa được #5 duyệt. Điểm nào làm đổi yêu cầu thì phải qua quy trình thay đổi yêu cầu (`CONTRIBUTING.md`); điểm nào chỉ sửa cách cài đặt thì không đổi SRS.

## 3. Phạm vi use case

Use case cắt được SRS ghi "Không thuộc bản nộp" kèm lý do (thiếu thời gian, mức Could).

| Use case | Issue | MoSCoW | Code trên `develop` | Quyết định |
| --- | --- | --- | --- | --- |
| BC-09, BC-10, BC-11 | #248 → #250 | Could | Có (#275, #289) | **Giữ** (#5 rút khỏi danh sách cắt, 23/11) |
| KM-05, KM-06 | #247, #246 | Could | Có (#275, #289) | **Giữ** (#5, 23/11) |
| DT-07 Danh sách phiếu đổi trả | #237 | Should | Có (#287) | **Giữ** (X8) |
| NH-09 Lịch sử nhập theo NCC | #146 | Could | Có (#294, 26/11; 31 test `SupplierPurchaseHistoryTests`) | **Đề xuất giữ** (đã có code và test; X7 ngày 23/11 ghi cắt khi chưa có code) – **chờ #5 xác nhận** |
| DT-03 Đổi hàng | #238 | Could | Chưa; PR #291 đang bị yêu cầu sửa (người hoàn tiền, API, snapshot đổi hàng, test liên module) | **Chờ quyết định.** Chỉ giữ nếu trước khi #5 gắn tag: #291 xử lý xong review, gộp vào `develop` với CI xanh, có kiểm thử liên module và qua nghiệm thu; không đạt thì ghi "Không thuộc bản nộp" |
| DT-06 Duyệt đổi trả ngoại lệ | #239 | Could | Chưa; PR #291 | **Chờ quyết định**, cùng điều kiện như DT-03 |
| AUTH-04 Xem/cập nhật hồ sơ cá nhân | #110 | Could | **Không** (issue đóng 04/11 chỉ kèm tệp giao diện; `GET/PUT /api/v1/me` không có trong code) | **Đề xuất** ghi không thuộc bản nộp (mức Could) – **chờ #5 quyết định**. Phát hiện khi đối chiếu API ngày 28/11 |
| DT-08 Cấu hình chính sách đổi trả | #253 | Could | Không (đổi trả vẫn dùng bảng `chinh_sach_doi_tra`, chỉ thiếu màn hình chỉnh) | **Cắt** (X7; #253 đóng "not planned" 26/11) |
| KH-07 Cấu hình quy tắc tích điểm | #189 | Could | Chưa có màn hình cấu hình; POS đã đọc quy tắc mới nhất từ `quy_tac_tich_diem` (mặc định 10.000 đ = 1 điểm) | **Chờ quyết định.** X7 ngày 23/11 ghi cắt, nhưng #189 được **mở lại ngày 26/11** kèm phạm vi làm màn hình cấu hình (lịch 28/11 giao #6); quá hạn chưa gộp thì đóng "not planned" và dùng mặc định PA-KH-02.B. **Không** thu hồi quyền ghi `quy_tac_tich_diem` (D5, PR #300) trước khi chốt |
| AI-01 Gợi ý số lượng nhập | – | – | Không | **Cắt**, giữ thiết kế ở ADR-001 mục 3.11 (X9) |

Tham khảo: [báo cáo module tuần 8](../05-implementation/week8-bc-kh-km-report.md), [NH-09](../05-implementation/NH09.md), [ma trận nghiệm thu cuối](../06-testing/test-reports/final-acceptance-matrix.md).

## 4. Yêu cầu phi chức năng cần ghi đúng hiện trạng

| Mã | Nội dung | Ghi vào SRS cuối |
| --- | --- | --- |
| NFR-BM-09 | Bản triển khai trực tuyến dùng HTTPS | **Chưa kiểm chứng**: chưa có bản web trực tuyến (#242 đang mở). Lưu ý: đây là HTTPS giữa trình duyệt và web; khác với TLS `VerifyFull` giữa backend và Supabase (#286, #298), phần này đã có |
| NFR-HN-01 → 05 | Thời gian phản hồi ≤ 1–3 giây, ≥ 5 người dùng đồng thời (dữ liệu mẫu 1.000 sản phẩm, 10.000 hóa đơn) | **CHƯA KIỂM CHỨNG.** Chưa có kết quả đo. #4 đo trên PostgreSQL 17 cô lập đúng quy mô dữ liệu, **không nạp dữ liệu hiệu năng vào Supabase dùng chung**; #176 tối ưu truy vấn còn mở |
| NFR kiểm thử | Test tự động | 877 test trên PostgreSQL 17 cô lập, chạy trong CI cho mỗi PR (lượt `cc20336`: 877/877); chạy lại cục bộ ngày 27/11 cũng 877/877. CI dùng quyền rộng hơn Supabase, nên **không chứng minh quyền ghi trên Supabase** |

## 5. Ba điểm AUTH từ issue #5 (đã chốt 23/11, `docs/quan-ly/quyet-dinh-AUTH-01-06-09.md`, PR #271)

| Vấn đề | Trạng thái |
| --- | --- |
| AUTH-06: SĐT tài khoản bắt buộc hay không | **Đã chốt 23/11:** không bắt buộc; sửa đặc tả AUTH-06 bước 2 |
| AUTH-09: đặt lại mật khẩu khi đang khóa tạm có gỡ khóa tạm không | **Đã chốt 23/11:** gỡ khóa tạm, bộ đếm về 0; khóa do Admin vẫn phải mở trước; sửa E1 đặc tả AUTH-09 |
| AUTH-01: thứ tự kiểm tra khóa tạm và mật khẩu | **Đã chốt 23/11:** khóa tạm kiểm trước mật khẩu; sửa bước 4–5 đặc tả AUTH-01, đóng vấn đề mở số 8 |

## 6. Việc còn lại để ra SRS bản cuối

1. ~~Đọc phản hồi X1 → X10 sau 20:00 25/11~~: không có phản đối; kết quả đã đăng ở #257 ngày 27/11.
2. Chờ #5 chốt ở #257: NH-09 (đề xuất giữ), DT-03/DT-06 (theo điều kiện ở mục 3), KH-07 (#189 mở lại), AUTH-04 (đề xuất cắt), C2 triển khai (#242 mở lại). Sau khi #5 gắn tag, chốt lại theo code tại tag.
2a. Ma trận 27 use case Must, issue, test và ngưỡng NFR chính thức: `docs/03-requirements/ma-tran-truy-vet-27-must_SRS-chinh-thuc.md`.
3. #1 sửa tệp Word theo mục 1–5 (bật theo dõi thay đổi), kèm bản PDF hiển thị các thay đổi để review: `docs/srs/SRS_ban-cuoi_du-thao_28-11-2026.docx` và `.pdf`. Sau khi #5 chốt: chấp nhận thay đổi, xuất PDF bản cuối.
