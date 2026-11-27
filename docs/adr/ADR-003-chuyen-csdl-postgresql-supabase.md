# ADR-003: Chuyển CSDL từ MySQL sang PostgreSQL trên Supabase

| | |
|---|---|
| **Dự án** | Hệ thống quản lý cửa hàng tiện lợi – Nhóm 7, học phần INFO2005 |
| **Trạng thái** | **Đã chấp nhận** – nhóm chốt ngày 23/11/2026. **Đã chuyển ứng dụng** ngày 24/11/2026 (PR #286); kết quả ở mục 7 |
| **Ngày** | 23/11/2026 |
| **Người ghi** | #1 Phạm Gia Thăng |
| **Người xác nhận** | Các thành viên ghi "Chốt" trong PR đưa ADR này vào repo |
| **Thay thế** | ADR-001 mục 2 (dòng CSDL, triển khai), 3.3 và phần Aiven của mục 3.10; đề xuất X5 "chốt MySQL 8.4" ở issue #257 |
| **Liên quan** | Báo cáo kiểm kê CSDL Supabase 31 bảng của #2 Phan Hải Yến (23/11/2026); biên bản duyệt CSDL tuần 8 `docs/quan-ly/T8-02_duyet-csdl-tuan-8.md` |

## 1. Bối cảnh

- Từ tuần 3, CSDL là MySQL: ADR-001 chốt 9.7.2; CI và bằng chứng test chạy 8.4; bản dùng chung đặt trên Aiven (chỉ có 8.4).
- Ngày 23/11/2026, #2 Yến dựng xong schema `smartmart` trên Supabase (PostgreSQL 17.12), chuyển từ `schema_v1.sql` cùng 3 bảng quỹ và hoàn tiền:

| Hạng mục | Số lượng |
|---|---|
| Bảng | 31 |
| Khóa ngoại | 62 (có FK ghép của `ct_phieu_doi_tra`) |
| CHECK | 89 |
| Trigger | 17 |
| View | 3 |
| Bảng bật RLS | 31/31; vai trò `anon`/`authenticated` không có quyền đọc |

- Báo cáo của #2 kèm kết quả thử SQL: 15/15 ca trigger bất biến, 4/4 ca giao tác, 1 ca bán đồng thời trên bảng thử. Báo cáo tự ghi rõ là **chưa** chạy ứng dụng .NET trên CSDL này.
- Nhóm chốt dùng PostgreSQL/Supabase làm CSDL của sản phẩm.

## 2. Quyết định

| Hạng mục | Chốt |
|---|---|
| CSDL | **PostgreSQL 17** trên **Supabase**, schema `smartmart` |
| Truy cập CSDL | EF Core + **Npgsql.EntityFrameworkCore.PostgreSQL** 10.0.x (đã có trong csproj); bỏ `MySql.EntityFrameworkCore` khi chuyển xong |
| Kết nối | Chỉ backend kết nối phía máy chủ bằng vai trò PostgreSQL riêng, quyền tối thiểu. Không dùng Data API, không đưa khóa `service_role` hay chuỗi kết nối vào repo hoặc trình duyệt |
| Xác thực | Giữ `tai_khoan`, `phien_dang_nhap` và mã AUTH hiện có (AUTH-01 → AUTH-10, có test). **Không** chuyển sang Supabase Auth (đề xuất của #1, phụ trách AUTH; xác nhận cùng ADR này) |
| RLS | Giữ bật, không tạo policy mở (`USING (true)`) |
| Thời gian | Lưu `timestamptz`; hiển thị theo `Asia/Ho_Chi_Minh` ở ứng dụng (thay `time_zone '+07:00'` của MySQL) |
| CI | Dịch vụ `postgres:17` tạm trong GitHub Actions; không trỏ CI vào project Supabase thật |

## 3. Hiện trạng tại thời điểm chốt (23/11/2026, nhánh `develop`; xem mục 7 cho hiện trạng sau khi chuyển)

Ứng dụng **vẫn chạy MySQL**:

- `Program.cs` dùng `UseMySQL`.
- CI dựng `mysql:8.4`; bộ test hiện có là 709 test.

Khối lượng phải chuyển:

| Hạng mục | Số lượng |
|---|---|
| Tệp mã có SQL riêng của MySQL (`FOR UPDATE … `, `LAST_INSERT_ID`, `SIGNAL SQLSTATE`, `time_zone`…) | 31 |
| Tệp mã gọi trực tiếp kiểu `MySql…` | 8 |
| Tệp test dựng CSDL MySQL tạm | 37 |

## 4. Cách chuyển và điều kiện hoàn tất

1. **Phương án lùi:** gắn tag cho bản MySQL đang chạy được, ví dụ `v0.9-mysql`, trước khi gộp thay đổi CSDL.
2. **Một nguồn schema duy nhất:** lấy DDL `smartmart` của #2 làm gốc và ánh xạ EF theo đó, hoặc sinh lại migration bằng Npgsql rồi so khớp với DDL của #2. Không duy trì song song hai bản. DDL được xuất từ Supabase và đưa vào repo qua PR (đề xuất `docs/db/postgresql/`).
3. **Ánh xạ EF:**
   - Đổi `uint` sang `long` cho Id và khóa ngoại, vì Npgsql không ánh xạ `uint` thành `bigint`.
   - Thay `IF(...)` trong `HasComputedColumnSql` bằng `CASE WHEN`.
   - Đặt schema `smartmart`.
   - Không ghi vào cột GENERATED.
4. **SQL thô:** mỗi module chuyển phần SQL của mình (người phụ trách module); giữ khóa dòng `FOR UPDATE` và cập nhật có điều kiện như hiện tại.
5. **Kết nối Supabase:** đi qua pooler; nếu dùng chế độ transaction (cổng 6543) thì tắt prepared statement trong chuỗi kết nối Npgsql.
6. **Điều kiện hoàn tất (gộp vào `develop`):**
   - CI trên PostgreSQL đạt toàn bộ test.
   - Các luồng demo (AUTH, SP, NH, KHO, POS, DT, KM, BC) chạy được trên Supabase.
   - Biên bản duyệt CSDL ghi đạt 5 tiêu chí (tiêu chí 5 đổi thành "đạt trên PostgreSQL 17").
7. **Trước buổi demo và bảo vệ:**
   - Mở project Supabase. Gói Free tự tạm dừng khi không có truy cập khoảng một tuần.
   - Admin chốt số dư mở quỹ (quy tắc X4).

Nếu đến hạn đóng băng tính năng (27/11) chưa đạt điều kiện ở bước 6, trưởng nhóm quyết định nộp bằng tag bản lùi hay lùi hạn chuyển. Báo cáo cuối kỳ phải ghi đúng bản nào đã được nộp.

## 5. Phương án đã cân nhắc

| Phương án | Lý do không chọn |
|---|---|
| Giữ MySQL 8.4 (đề xuất X5) | Nhóm chốt PostgreSQL/Supabase ngày 23/11; schema `smartmart` đã dựng và kiểm chứng ở mức SQL. Ưu điểm của MySQL (ứng dụng và 709 test đã chạy) được giữ qua phương án lùi |
| Chạy song song: một phần MySQL, một phần Supabase | Hai CSDL cho một ứng dụng làm mất tính nhất quán của giao tác kho–hóa đơn–quỹ; không chọn |
| SQL Server | Phải viết lại toàn bộ SQL; không có lợi thế so với PostgreSQL cho nhóm |

## 6. Hệ quả

- Các tài liệu đang ghi MySQL 8.4 hoặc 9.7.2 được ghi chú "thay bằng ADR-003". Nhật ký và biên bản đã xảy ra giữ nguyên, vì đó là lịch sử.
- Các migration MySQL đã duyệt (T8-02) vẫn có giá trị cho bản lùi. Nội dung của chúng (bảng quỹ, hoàn tiền, `anh_url`) đã có trong schema `smartmart`, và sẽ được duyệt lại khi có migration/ánh xạ Npgsql.
- Gỡ gói `MySql.EntityFrameworkCore` và `docker-compose` MySQL sau khi chuyển xong, không gỡ trước.
- Rủi ro: thời gian chuyển ngắn trước đóng băng 27/11; bộ test phải viết lại phần hạ tầng. Phương án lùi ở bước 1 mục 4 giảm rủi ro này.

## 7. Kết quả sau khi chuyển (cập nhật 27/11/2026)

| Điều kiện ở mục 4 | Kết quả |
|---|---|
| 1. Tag bản lùi MySQL trước khi gộp | **Không thực hiện.** Repo chưa có tag nào. Bản MySQL cuối cùng vẫn truy lại được trong lịch sử `develop` (trước PR #282, #286) |
| 2. Một nguồn schema | Migration `PostgreSqlBaseline` (#286) là nguồn duy nhất; kiểm tra khớp với schema Supabase bằng `supabase-schema-contract.json` và smoke test |
| 3. Ánh xạ EF | Đã chuyển sang Npgsql; trong `src/`, `tests/` không còn tham chiếu MySQL |
| 4. SQL thô | Đã chuyển (#286, 220 tệp) |
| 5. Kết nối Supabase | Vai trò `smartmart_backend` (RLS, không BYPASSRLS), TLS `VerifyFull` (#298, #299) |
| 6. Điều kiện hoàn tất | CI trên PostgreSQL 17: **877/877** (lượt `cc20336`, 26/11); #1 chạy lại cục bộ 27/11 cũng 877/877. Smoke **đọc** Supabase đạt. **Ghi** dữ liệu trên Supabase và luồng demo chưa có bằng chứng (#293). Migration `PostgreSqlBaseline` chưa có biên bản duyệt CSDL theo 5 tiêu chí |
| 7. Trước demo, bảo vệ (04/12) | Còn phải làm: mở project Supabase, Admin chốt số dư mở quỹ |

