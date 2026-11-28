# Chương 3. Phân tích và thiết kế hệ thống (bản nháp)

Bản nháp cho báo cáo cuối kỳ (T7-01, issue #170). Người viết: #1 Phạm Gia Thăng, 02/11/2026. Các thành viên bổ sung phần của mình ở chỗ ghi **[Bổ sung – tên]**. Số liệu lấy từ SRS v1.0.3, Đặc tả Use Case 18/10, Bảng MoSCoW 13/10, ERD v1 và repo ngày 02/11/2026.

## 3.1. Phân tích yêu cầu

### 3.1.1. Tác nhân

| Tác nhân | Viết tắt | Việc chính |
| --- | --- | --- |
| Thu ngân | TN | Bán hàng, thu tiền mặt, in hóa đơn, đổi trả, tra cứu khách |
| Nhân viên kho | NK | Sản phẩm, nhập hàng, kiểm kê, xuất hủy |
| Admin/Quản lý | AD | Tài khoản và phân quyền, duyệt phiếu, khuyến mãi, báo cáo, cấu hình |

Mỗi tài khoản có đúng một vai trò; quyền được cài sẵn theo ma trận 27 mã quyền (PA-AUTH-04.A).

### 3.1.2. Use case theo module và độ ưu tiên

Hệ thống có 81 use case chia 9 module. Độ ưu tiên xếp theo MoSCoW ngày 13/10/2026.

| Module | Số use case | Must | Should | Could |
| --- | --- | --- | --- | --- |
| Xác thực và phân quyền (AUTH) | 10 | 6 | 3 | 1 |
| Sản phẩm và danh mục (SP) | 9 | 6 | 2 | 1 |
| Nhập hàng và nhà cung cấp (NH) | 9 | 5 | 2 | 2 |
| Kho và tồn kho (KHO) | 8 | 1 | 5 | 2 |
| Bán hàng (POS) | 11 | 6 | 4 | 1 |
| Báo cáo và thống kê (BC) | 12 | 3 | 2 | 7 |
| Khách hàng và tích điểm (KH) | 8 | 0 | 4 | 4 |
| Khuyến mãi (KM) | 6 | 0 | 4 | 2 |
| Đổi trả hàng (DT) | 8 | 0 | 5 | 3 |
| **Tổng** | **81** | **27** | **31** | **23** |

Sáu module đầu là module chủ chốt; ba module sau là module bổ sung. Thứ tự làm: Must trước, rồi Should, Could cuối tuần; khi chậm thì cắt theo thứ tự Could của module bổ sung, Could của module chủ chốt, Should của Đổi trả (Kế hoạch v3 mục 1.4).

### 3.1.3. Cách đặc tả

Mỗi use case đặc tả theo một mẫu: actor, mô tả, tiền điều kiện, hậu điều kiện, quan hệ «include»/«extend», luồng chính, luồng thay thế, ngoại lệ và quy tắc nghiệp vụ (BR). Chỗ có nhiều cách làm hợp lý được ghi thành "phương án vận hành" (PA) với các lựa chọn, ưu và nhược điểm, và lựa chọn đã chọn kèm lý do. Từ đặc tả, nhóm rút ra user story có tiêu chí chấp nhận dạng Given–When–Then cho 27 use case Must.

**[Bổ sung – #6]** Tóm tắt kết quả khảo sát hiện trạng và bài toán (chương 2 của báo cáo) để nối sang phần này.

### 3.1.4. Yêu cầu phi chức năng

Chín yêu cầu phi chức năng đã chốt thuộc 5 nhóm: hiệu năng (NFR-HN), bảo mật (NFR-BM), dễ dùng (NFR-SD), tương thích (NFR-TC), bảo trì (NFR-BT). Ví dụ: hoàn tất một đơn không quá 2 giây (NFR-HN-02); bán một đơn nằm trọn trên một màn hình (NFR-SD-04); sao lưu CSDL trước mỗi buổi demo (NFR-BT-06).

**[Bổ sung – #4]** Kết quả đo theo từng mã NFR (T7-08).

## 3.2. Mô hình use case

Sơ đồ use case tổng quan và sơ đồ của từng module nằm trong SRS mục 3.3 và 3.4–3.12. **[Bổ sung – #1]** chèn sơ đồ tổng quan và sơ đồ module Bán hàng làm ví dụ.

Năm luồng chính được vẽ thêm sơ đồ hoạt động (tài liệu "Activity Diagram 5 luồng chính"): đăng nhập, nhập hàng, bán hàng tại quầy, kiểm kê và điều chỉnh tồn, trả hàng. Luồng hoàn tất đơn POS-07 có sơ đồ tuần tự trong `docs/diagrams/pos/POS-07-sequence.md`.

## 3.3. Thiết kế kiến trúc

### 3.3.1. Lựa chọn kiến trúc

Hệ thống là một ứng dụng web nguyên khối chia module (modular monolith) trên ASP.NET Core (.NET 10). Lý do (ADR-001): nhóm 6 người trong 9 tuần, một cửa hàng đơn lẻ, ít người dùng đồng thời; tách dịch vụ chỉ thêm việc triển khai và giao tiếp mạng mà không có lợi ích tương xứng. Ranh giới module vẫn được giữ trong mã nguồn để có thể tách sau này.

| Tầng | Thành phần | Ghi chú |
| --- | --- | --- |
| Giao diện | Razor view (MVC), Bootstrap, jQuery; trang đăng nhập HTML gọi REST | SRS v1.0.3 ghi React; đổi sang Razor MVC, ghi trong ADR-002 (SRS v1.2, thay đổi C1) |
| Ứng dụng | Controller MVC và controller REST `/api/v1`, chia theo thư mục `Modules/<Module>` | Mỗi module có Controllers, Services, Dtos/Models |
| Nghiệp vụ dùng chung | Dịch vụ "Ghi nhận biến động kho", xử lý lỗi API chung, truy vấn CSDL dùng chung | `Modules/Inventory`, `Shared/` |
| Dữ liệu | EF Core + MySql.EntityFrameworkCore, MySQL 9.7.2 | Entity sinh từ CSDL (scaffold); migration `InitialCreate` dựng đúng `schema_v1.sql` |

### 3.3.2. Dịch vụ "Ghi nhận biến động kho"

Mọi thay đổi tồn kho (nhập hàng, bán hàng, hủy hóa đơn, trả hàng, kiểm kê, xuất hủy) đi qua một dịch vụ duy nhất. Dịch vụ chạy trong giao tác do bên gọi mở, khóa các dòng tồn bằng `SELECT … FOR UPDATE` theo thứ tự mã sản phẩm tăng dần để tránh khóa chéo, kiểm tra tồn không âm, tính giá vốn bình quân gia quyền, cập nhật `ton_kho` và ghi một dòng `the_kho` cho mỗi sản phẩm. Bên gọi tự commit hoặc rollback, nên hóa đơn và tồn kho luôn thay đổi cùng nhau.

Thiết kế này xử lý được trường hợp hai thu ngân cùng bán món hàng cuối cùng: thử ngày 01/11 với hai lệnh thanh toán gửi cùng lúc, chỉ một đơn thành công, tồn không âm.

**[Bổ sung – #2, #3]** Một đoạn về cách module NH và POS gọi dịch vụ này.

### 3.3.3. Xác thực và phân quyền

Phiên đăng nhập lưu ở máy chủ: cookie `HttpOnly` chứa token ngẫu nhiên, CSDL chỉ lưu giá trị băm SHA-256 của token. Mật khẩu băm bằng `PasswordHasher` của ASP.NET Core. Chống CSRF bằng header `X-XSRF-TOKEN`. Quyền kiểm tra ở máy chủ theo 27 mã quyền, không dựa vào việc ẩn nút trên giao diện. Tài khoản có mật khẩu tạm phải đổi mật khẩu trước khi dùng các chức năng khác.

## 3.4. Thiết kế cơ sở dữ liệu

ERD v1 (18/10/2026) gồm 28 bảng, 3 view và 10 trigger; tệp `docs/db/schema_v1.sql`. Các quyết định thiết kế chính:

| Quyết định | Cách làm | Lý do |
| --- | --- | --- |
| Giữ lịch sử, không xóa | Trigger chặn sửa/xóa `the_kho`, `lich_su_diem`, dòng hóa đơn, dòng phiếu đổi trả; hóa đơn chỉ được chuyển sang "Đã hủy" | Truy vết được mọi biến động (BR-POS-07, BR-POS-12) |
| Ràng buộc tại CSDL | 53 CHECK: tồn không âm, hướng biến động đúng loại, tổng tiền khớp, giảm giá là bội số 100 đ… | Lỗi lập trình bị chặn ngay ở CSDL; bộ kiểm tra 29 thao tác sai đều bị chặn |
| Lưu giá tại thời điểm bán | `ct_hoa_don` lưu tên, đơn giá, mức giảm, giá vốn | Báo cáo và đổi trả không bị ảnh hưởng khi giá thay đổi về sau |
| Trạng thái hiển thị suy ra | View `v_chuong_trinh_khuyen_mai` tính "Đã lên lịch", "Đang chạy" từ thời gian | Không cần tác vụ nền (PA-KM-05.A) |
| Giờ Việt Nam | Mỗi kết nối đặt `time_zone = '+07:00'` | Báo cáo theo ngày đúng giờ cửa hàng (BR-BC-03) |
| Khóa lạc quan | Cột `phien_ban` trên các bảng hay bị sửa (sản phẩm, khách hàng, khuyến mãi, phiếu nhập, phiếu kiểm kê…) | Hai người sửa cùng lúc không ghi đè nhau |

Đã kiểm tra trên MySQL 9.7.2 ngày 02/11/2026: schema, bộ kiểm tra ràng buộc và migration chạy sạch.

**[Bổ sung – #1]** Chèn ERD toàn hệ thống (`erd_toan_he_thong.png`).

## 3.5. Thiết kế giao tiếp (API)

REST JSON dưới `/api/v1`, cùng domain với giao diện. Quy ước chung trong `docs/api/API_CONVENTIONS.md`: tên tài nguyên, phân trang, định dạng lỗi thống nhất (mã lỗi, thông báo, chi tiết theo trường), mã HTTP 401/403/409/422. Hợp đồng từng module viết bằng OpenAPI 3: `openapi-auth.yaml`, `pos-dt.openapi.yaml`, `openapi-report-customer-promotion.yaml`.

## 3.6. Một số phương án vận hành đã chọn

| Phương án | Lựa chọn | Lý do ngắn |
| --- | --- | --- |
| PA-KHO-01 Bán khi tồn không đủ | Chặn trong giao tác hoàn tất đơn | Không bán âm, xử lý được nhiều quầy |
| PA-KHO-04 Xuất hủy | Admin duyệt mọi phiếu | Xuất hủy là chỗ dễ gian lận nhất |
| PA-POS-03 Hủy hóa đơn đã thanh toán | Chỉ Admin | Thao tác hiếm và nhạy cảm |
| PA-KH-01 Nhận diện khách | Số điện thoại | Khách không phải nhớ mã |
| PA-KH-02 Tích điểm | Admin cấu hình tỷ lệ, mặc định 10.000 đ = 1 điểm | Linh hoạt với một màn hình |
| PA-KM-01 Phạm vi khuyến mãi | Theo sản phẩm, không có khuyến mãi cấp hóa đơn | Giá thực trả có sẵn trên từng dòng, đổi trả đơn giản |
| PA-DT-02 Thẩm quyền đổi trả | Thu ngân tự xử lý; ngoại lệ hoặc trên 500.000 đ cần Admin | Cân bằng trải nghiệm và chống gian lận |
| PA-DT-04 Hàng trả về | Thu ngân chọn nhập lại kho hoặc hủy từng dòng | Tồn chính xác ngay |

## 3.7. Thiết kế giao diện

**[Bổ sung – #6]** Design system, khung 20 màn hình MH-01 → MH-20, ảnh màn hình bán hàng và hóa đơn K80 thật, kết quả thử ở 360 px và 1440 px.

## 3.8. Nhận xét và hạn chế của thiết kế

- Giao diện đổi từ React sang Razor MVC khi đang làm; SRS và ADR cần cập nhật theo (đang làm ở SRS v1.2).
- Một số module (khuyến mãi, sản phẩm) từng được viết trên nền dữ liệu khác với ERD v1; nhóm đang đưa về một DbContext duy nhất.
- Báo cáo truy vấn trực tiếp dữ liệu giao dịch (PA-BC-03.A): đủ cho một cửa hàng, nhưng cần chỉ mục và sẽ chậm nếu dữ liệu lớn (T7-07 của #2).
