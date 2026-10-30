# TÀI LIỆU KỊCH BẢN VÀ TEST CASE KIỂM THỬ CƠ SỞ DỮ LIỆU

**Mã tài liệu:** DB-TC-001
**Dự án:** Convenience Store Management System
**Ngày thực hiện kiểm thử:** 30/10/2026
**Nhánh Git:** `feature/db-codefirst-remaining-schema`
**Commit:** `12d2f3e`
**Cơ sở dữ liệu:** Aiven MySQL 8.4 — `cuahang_codefirst_dev`
**Migration chuẩn:** `20261027004309_DatabaseViewsAndStockCardTriggers`
**Tổng số Migration:** 23
**Công cụ kiểm thử:** .NET 10, xUnit 2.9.3, EF Core 10.0.13

## 1. Mục đích và phạm vi kiểm thử

Tài liệu xác định các kịch bản, điều kiện đầu vào, các bước thực hiện, kết quả mong đợi và trạng thái thực tế của các test case liên quan đến cơ sở dữ liệu của hệ thống quản lý cửa hàng tiện lợi.

Hoạt động kiểm thử được chia thành hai nhóm:

**Nhóm A — Kiểm thử cấu trúc cơ sở dữ liệu (đã thực hiện)**

Kiểm tra số lượng bảng, danh sách bảng nghiệp vụ, VIEW, TRIGGER, Migration và một số khóa chính bằng các truy vấn chỉ đọc.

**Nhóm B — Kiểm thử hành vi cơ sở dữ liệu (chưa triển khai)**

Kiểm tra kết quả nghiệp vụ của VIEW, khả năng ngăn chặn thay đổi dữ liệu của TRIGGER, các ràng buộc FK, UNIQUE, CHECK và xử lý giao dịch đồng thời.

Các test có thao tác INSERT, UPDATE, DELETE hoặc thay đổi cấu trúc dữ liệu chỉ được thực hiện trên cơ sở dữ liệu thử nghiệm độc lập.

## 2. Môi trường và điều kiện thực hiện

| Thành phần | Yêu cầu |
|---|---|
| Ngôn ngữ | C# |
| Framework | .NET 10 |
| Test framework | xUnit 2.9.3 |
| ORM | Entity Framework Core 10.0.13 |
| DBMS | MySQL 8.4 trên Aiven |
| Database | `cuahang_codefirst_dev` |
| Project kiểm thử | `tests/ConvenienceStore.Web.Tests` |
| File kiểm thử | `Integration/Database/Schema/SchemaStructureTests.cs` |
| Phân loại | `DatabaseIntegration` |
| Hình thức | Tự động, truy vấn chỉ đọc |
| Kết quả chạy | 12/12 PASS |

### 2.1. Điều kiện tiên quyết

1. Mã nguồn được checkout tại commit `12d2f3e`.
2. Solution .NET 10 build thành công.
3. Database đích có quyền truy vấn metadata và lịch sử Migration.
4. Chuỗi kết nối được cung cấp an toàn qua User Secrets hoặc biến môi trường.
5. Biến `RUN_MYSQL_INTEGRATION_TESTS` được thiết lập bằng `1`.
6. Biến `TEST_MYSQL_CONNECTION` chứa thông tin kết nối hợp lệ.
7. Không ghi mật khẩu hoặc thông tin xác thực vào mã nguồn, báo cáo và log chia sẻ.

## 3. Nhóm A — Test case kiểm thử cấu trúc cơ sở dữ liệu

### 3.1. Bảng tổng hợp test case

| Mã test | Phương thức kiểm thử | Điều kiện / dữ liệu đầu vào | Kết quả mong đợi | Kết quả thực tế |
|---|---|---|---|---|
| DB01 | `DB01_HasExactly29BaseTables` | Truy vấn `information_schema.TABLES` | Có chính xác 29 bảng vật lý, bao gồm bảng lịch sử Migration | PASS |
| DB02 | `DB02_HasAll28ExpectedBusinessTables` | Danh sách 28 bảng nghiệp vụ đã thiết kế | Tồn tại đầy đủ 28 bảng nghiệp vụ theo danh sách chuẩn | PASS |
| DB03 | `DB03_HasExactlyThreeExpectedViews` | Truy vấn `information_schema.VIEWS` | Có đúng 3 VIEW theo thiết kế | PASS |
| DB04-A | `DB04_ViewCanBePreparedAndQueriedWithoutReadingRows` | VIEW `v_chuong_trinh_khuyen_mai` | Truy vấn `SELECT * ... LIMIT 0` thành công | PASS |
| DB04-B | `DB04_ViewCanBePreparedAndQueriedWithoutReadingRows` | VIEW `v_gia_ban_hien_hanh` | Truy vấn `SELECT * ... LIMIT 0` thành công | PASS |
| DB04-C | `DB04_ViewCanBePreparedAndQueriedWithoutReadingRows` | VIEW `v_luot_dung_khuyen_mai` | Truy vấn `SELECT * ... LIMIT 0` thành công | PASS |
| DB05 | `DB05_UpdateProtectionTriggerExists` | Metadata TRIGGER | Tồn tại `trg_the_kho_khong_sua` với sự kiện BEFORE UPDATE trên `the_kho` | PASS |
| DB06 | `DB06_DeleteProtectionTriggerExists` | Metadata TRIGGER | Tồn tại `trg_the_kho_khong_xoa` với sự kiện BEFORE DELETE trên `the_kho` | PASS |
| DB07 | `DB07_Has23AppliedMigrations` | Bảng `__EFMigrationsHistory` | Có chính xác 23 Migration đã áp dụng | PASS |
| DB08 | `DB08_LatestMigrationMatchesBaseline` | Danh sách Migration theo thời gian | Migration cuối là `20261027004309_DatabaseViewsAndStockCardTriggers` | PASS |
| DB09 | `DB09_StockCardHasPrimaryKey` | Metadata bảng `the_kho` | Bảng có khóa chính | PASS |
| DB10 | `DB10_ProductHasPrimaryKey` | Metadata bảng `san_pham` | Bảng có khóa chính | PASS |

**Tổng số:** 10 phương thức kiểm thử, 12 trường hợp thực thi, 12 PASS.

### 3.2. Kịch bản kiểm thử DB01 — Số lượng bảng

**Mục tiêu:** Kiểm tra tổng số bảng vật lý trong cơ sở dữ liệu.

**Tiền điều kiện:** Kết nối thành công đến `cuahang_codefirst_dev`.

**Các bước thực hiện:**

1. Kết nối cơ sở dữ liệu bằng tài khoản kiểm thử.
2. Truy vấn `information_schema.TABLES`.
3. Lọc theo database hiện tại và loại `BASE TABLE`.
4. Đếm tổng số bảng.
5. So sánh kết quả với giá trị mong đợi là 29.

**Kết quả mong đợi:** 29 bảng vật lý.

**Kết quả thực tế:** PASS.

### 3.3. Kịch bản kiểm thử DB02 — Danh sách bảng nghiệp vụ

**Mục tiêu:** Xác minh tất cả 28 bảng nghiệp vụ trong thiết kế đã được tạo.

**Các bước thực hiện:**

1. Lấy danh sách bảng nghiệp vụ mong đợi từ bộ kiểm thử.
2. Đọc danh sách bảng hiện có trong MySQL.
3. Loại trừ bảng hệ thống `__EFMigrationsHistory`.
4. Đối chiếu tên bảng với danh sách chuẩn.
5. Xác định bảng thiếu hoặc không khớp.

**Kết quả mong đợi:** Tồn tại đầy đủ 28 bảng nghiệp vụ.

**Kết quả thực tế:** PASS.

### 3.4. Kịch bản kiểm thử DB03 và DB04 — VIEW

**Mục tiêu:** Xác minh ba VIEW tồn tại và có khả năng thực hiện truy vấn.

**Đối tượng:**

- `v_chuong_trinh_khuyen_mai`
- `v_gia_ban_hien_hanh`
- `v_luot_dung_khuyen_mai`

**Các bước thực hiện:**

1. Truy vấn `information_schema.VIEWS`.
2. Kiểm tra danh sách ba VIEW bắt buộc.
3. Thực hiện truy vấn không lấy bản ghi với từng VIEW.
4. Kiểm tra truy vấn có thực thi thành công hay không.

**Kết quả mong đợi:** Ba VIEW tồn tại và đều có thể truy vấn.

**Kết quả thực tế:** DB03 PASS; DB04-A/B/C PASS.

**Giới hạn:** Truy vấn `LIMIT 0` không xác minh tính đúng đắn của dữ liệu trả về hoặc logic tính toán của VIEW.

### 3.5. Kịch bản kiểm thử DB05 và DB06 — TRIGGER

**Mục tiêu:** Xác minh TRIGGER bảo vệ tính bất biến của dữ liệu thẻ kho đã được tạo.

**Đối tượng:**

- `trg_the_kho_khong_sua`
- `trg_the_kho_khong_xoa`

**Các bước thực hiện:**

1. Truy vấn `information_schema.TRIGGERS`.
2. Kiểm tra tên TRIGGER.
3. Kiểm tra bảng áp dụng là `the_kho`.
4. Kiểm tra thời điểm kích hoạt là `BEFORE`.
5. Kiểm tra sự kiện tương ứng là `UPDATE` hoặc `DELETE`.

**Kết quả mong đợi:** Hai TRIGGER tồn tại với các thuộc tính đúng thiết kế.

**Kết quả thực tế:** DB05 và DB06 PASS.

**Giới hạn:** Chưa thực hiện UPDATE hoặc DELETE để kiểm chứng hành vi từ chối thực tế.

### 3.6. Kịch bản kiểm thử DB07 và DB08 — Migration

**Mục tiêu:** Kiểm tra lịch sử cập nhật cấu trúc database bằng Entity Framework Core.

**Các bước thực hiện:**

1. Đọc bảng `__EFMigrationsHistory`.
2. Đếm số Migration đã được áp dụng.
3. Xác định Migration mới nhất.
4. So sánh số lượng và tên Migration với baseline.

**Kết quả mong đợi:** Có 23 Migration; Migration cuối là `20261027004309_DatabaseViewsAndStockCardTriggers`.

**Kết quả thực tế:** DB07 và DB08 PASS.

### 3.7. Kịch bản kiểm thử DB09 và DB10 — Khóa chính

**Mục tiêu:** Xác minh các bảng được lựa chọn có ràng buộc khóa chính.

**Đối tượng:**

- `the_kho`
- `san_pham`

**Các bước thực hiện:**

1. Truy vấn metadata của bảng cần kiểm tra.
2. Xác định ràng buộc `PRIMARY KEY`.
3. Đối chiếu sự tồn tại của khóa chính.

**Kết quả mong đợi:** Cả hai bảng đều có khóa chính.

**Kết quả thực tế:** DB09 và DB10 PASS.

**Giới hạn:** Chưa kiểm thử toàn bộ khóa chính, khóa ngoại hoặc ràng buộc của tất cả các bảng trong hệ thống.

## 4. Quy trình thực thi kiểm thử tự động

### 4.1. Kiểm tra nhánh và phiên bản mã nguồn

```powershell
git branch --show-current
git rev-parse --short HEAD
```

Phiên bản đã ghi nhận:

```text
feature/db-codefirst-remaining-schema
12d2f3e
```

### 4.2. Biên dịch solution

```powershell
dotnet build ".\ConvenienceStoreManagement.slnx" --no-restore
```

**Kết quả:** PASS.

### 4.3. Chạy Unit Test

```powershell
dotnet test ".\ConvenienceStoreManagement.slnx" --no-restore --filter "Category!=DatabaseIntegration"
```

**Kết quả:** 8 PASS, 0 FAIL, 0 SKIP.

### 4.4. Chuẩn bị Database Integration Test

Thiết lập `RUN_MYSQL_INTEGRATION_TESTS=1` và nạp `TEST_MYSQL_CONNECTION` từ nguồn bí mật được quản lý trên máy phát triển.

Không hiển thị chuỗi kết nối có mật khẩu trong Terminal, log hoặc tài liệu.

### 4.5. Chạy kiểm thử tích hợp và xuất bằng chứng

```powershell
dotnet test ".\ConvenienceStoreManagement.slnx" --no-restore --filter "Category=DatabaseIntegration" --logger "trx;LogFileName=database-integration-passed.trx"
```

**Kết quả thực tế:**

```text
Test summary:
total: 12
failed: 0
succeeded: 12
skipped: 0
duration: 12.0s
```

### 4.6. Xóa thông tin kết nối tạm thời

```powershell
Remove-Item Env:TEST_MYSQL_CONNECTION -ErrorAction SilentlyContinue
Remove-Item Env:RUN_MYSQL_INTEGRATION_TESTS -ErrorAction SilentlyContinue
```

### 4.7. Kiểm chứng từ file TRX

Kết quả thống kê đã xác minh:

```text
total       : 12
executed    : 12
passed      : 12
failed      : 0
notExecuted : 0
```

**Kết luận nhóm A:** PASS.

## 5. Nhóm B — Kịch bản kiểm thử hành vi chưa triển khai

Các trường hợp này cần MySQL thử nghiệm độc lập, có dữ liệu mẫu được kiểm soát.

| Mã test | Kịch bản | Bước kiểm thử chính | Kết quả mong đợi | Loại kiểm thử | Trạng thái |
|---|---|---|---|---|---|
| VIEW-01 | Giá bán theo thời gian | Tạo lịch sử giá quá khứ, hiện tại, tương lai và giá đã hủy; truy vấn VIEW giá hiện hành | Trả về giá mới nhất đủ điều kiện còn hiệu lực | Integration | CHƯA TRIỂN KHAI |
| VIEW-02 | Trạng thái chương trình khuyến mãi | Tạo chương trình trước, trong và sau khoảng thời gian áp dụng | Trạng thái được xác định đúng theo quy tắc thiết kế | Integration | CHƯA TRIỂN KHAI |
| VIEW-03 | Thống kê lượt dùng khuyến mãi | Tạo hóa đơn đã thanh toán, chưa thanh toán và đã hủy | Chỉ tính các lượt sử dụng đủ điều kiện | Integration | CHƯA TRIỂN KHAI |
| TRG-01 | Chặn sửa thẻ kho | UPDATE bản ghi `the_kho` hợp lệ | MySQL từ chối với SQLSTATE 45000 theo thiết kế | Negative Integration | CHƯA TRIỂN KHAI |
| TRG-02 | Chặn xóa thẻ kho | DELETE bản ghi `the_kho` hợp lệ | MySQL từ chối với SQLSTATE 45000 theo thiết kế | Negative Integration | CHƯA TRIỂN KHAI |
| TRG-03 | Dữ liệu không đổi sau thao tác bị từ chối | Đọc bản ghi sau UPDATE/DELETE không thành công | Dữ liệu ban đầu được giữ nguyên | Integration | CHƯA TRIỂN KHAI |
| TRG-04 | Cho phép tạo bản ghi thẻ kho hợp lệ | INSERT bằng quy trình nghiệp vụ hợp lệ | Tạo bản ghi đúng thiết kế; TRIGGER không cấm INSERT | Integration | CHƯA TRIỂN KHAI |
| CON-01 | Vi phạm khóa ngoại danh mục sản phẩm | Tạo sản phẩm tham chiếu danh mục không tồn tại | Database từ chối, không phát sinh dữ liệu mồ côi | Negative Integration | CHƯA TRIỂN KHAI |
| CON-02 | Vi phạm ràng buộc UNIQUE | Tạo hai bản ghi trùng khóa nghiệp vụ có ràng buộc duy nhất | Database từ chối bản ghi vi phạm | Negative Integration | CHƯA TRIỂN KHAI |
| CON-03 | Vi phạm ràng buộc CHECK | Thử số lượng hoặc giá nhập âm tại cột bị ràng buộc | Database hoặc cơ chế kiểm tra theo thiết kế từ chối giá trị | Negative Integration | CHƯA TRIỂN KHAI |
| CON-04 | Xử lý biến động tồn kho đồng thời | Hai giao dịch cùng thay đổi tồn kho được kiểm soát | Không phát sinh tồn kho âm; tồn kho và thẻ kho nhất quán | Concurrency Integration | CHƯA TRIỂN KHAI |

### 5.1. Yêu cầu đối với dữ liệu kiểm thử

Trước khi triển khai Nhóm B cần:

1. Kiểm tra Entity Configuration và Migration thực tế để xác định đúng tên cột, kiểu dữ liệu, khóa chính và khóa ngoại.
2. Chuẩn bị bộ dữ liệu thử nghiệm độc lập.
3. Xác định trạng thái database trước và sau kiểm thử.
4. Bảo đảm mỗi test độc lập và có thể chạy lặp lại.
5. Có cơ chế tái tạo hoặc dọn dẹp database thử nghiệm.
6. Không sử dụng database `cuahang_codefirst_dev` dùng chung để kiểm thử thay đổi dữ liệu.

Cần lưu ý rằng nhiều thao tác DDL trên MySQL có thể tự động commit; không được xem rollback là cơ chế bảo vệ duy nhất khi kiểm thử thay đổi schema.

## 6. Bảng truy vết giữa thiết kế và kiểm thử

| Thành phần thiết kế / mã nguồn | Test case liên quan | Trạng thái |
|---|---|---|
| EF Core Migration và `ApplicationDbContext` | DB01, DB02, DB07, DB08 | PASS |
| Metadata MySQL `information_schema` | DB03, DB05, DB06, DB09, DB10 | PASS |
| Định nghĩa ba VIEW | DB04-A/B/C | PASS ở mức truy vấn |
| Logic nghiệp vụ của VIEW | VIEW-01, VIEW-02, VIEW-03 | CHƯA TRIỂN KHAI |
| Định nghĩa TRIGGER bảo vệ `the_kho` | DB05, DB06 | PASS ở mức cấu trúc |
| Hành vi TRIGGER | TRG-01, TRG-02, TRG-03, TRG-04 | CHƯA TRIỂN KHAI |
| Ràng buộc Entity và cơ sở dữ liệu | CON-01, CON-02, CON-03 | CHƯA TRIỂN KHAI |
| Logic cập nhật tồn kho đồng thời | CON-04 | CHƯA TRIỂN KHAI |

## 7. Bằng chứng kiểm thử

| Mã bằng chứng | File | Kết quả |
|---|---|---|
| EVD-UNIT-001 | `tests/ConvenienceStore.Web.Tests/TestResults/unit-tests.trx` | 8 PASS |
| EVD-DB-001 | `tests/ConvenienceStore.Web.Tests/TestResults/database-integration-tests.trx` | Lần chạy ban đầu bị SKIP |
| EVD-DB-002 | `tests/ConvenienceStore.Web.Tests/TestResults/database-integration-passed.trx` | 12 PASS |

Các file TRX được lưu trên máy phát triển và có thể không được đưa vào Git.

Khi tái kiểm thử trên máy khác, cần sử dụng cùng phiên bản mã nguồn, môi trường tương thích và database có baseline tương ứng.

## 8. Tiêu chí nghiệm thu

### 8.1. Kiểm thử cấu trúc

**PASS** khi:

- Tất cả test case thuộc DB01–DB10 được thực thi.
- Không có FAIL.
- Không có SKIP trong lần chạy nghiệm thu.
- Kết quả được lưu trong TRX.
- Database đích được xác minh đúng.
- Không thực hiện các thao tác thay đổi dữ liệu.

**Kết quả thực tế:** 12/12 PASS — ĐẠT.

### 8.2. Kiểm thử hành vi

**PENDING** cho đến khi:

- Có database thử nghiệm độc lập.
- Hoàn thành mã kiểm thử cho 11 trường hợp VIEW/TRG/CON.
- Có dữ liệu kiểm thử và kỳ vọng rõ ràng.
- Các bài test được chạy và có bằng chứng tương ứng.
- Không còn lỗi ảnh hưởng đến các quy tắc nghiệp vụ bắt buộc.

**Kết quả hiện tại:** CHƯA ĐỦ ĐIỀU KIỆN NGHIỆM THU.

## 9. Kết luận

Đến ngày 30/10/2026, bộ kiểm thử tự động của hệ thống đã xác minh thành công các thành phần cấu trúc cơ sở dữ liệu được lựa chọn.

**Kết quả chính:**

- 10 phương thức kiểm thử cấu trúc đã triển khai.
- 12 test case tích hợp cơ sở dữ liệu đã thực thi.
- 12 PASS, 0 FAIL, 0 SKIP.
- 11 kịch bản kiểm thử hành vi đã được xác định nhưng chưa triển khai.

**Quyết định:** Chấp nhận kết quả kiểm thử cấu trúc cơ sở dữ liệu. Chưa nghiệm thu toàn bộ hành vi nghiệp vụ database.

Công việc tiếp theo là triển khai kiểm thử hành vi VIEW, TRIGGER và Constraint trên môi trường MySQL thử nghiệm độc lập, sau đó cập nhật báo cáo thực thi theo bằng chứng mới.
