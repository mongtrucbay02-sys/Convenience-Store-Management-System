# BÁO CÁO KẾT QUẢ THỰC HIỆN KIỂM THỬ CƠ SỞ DỮ LIỆU

**Mã báo cáo:** DB-TR-001
**Ngày thực hiện:** 30/10/2026
**Nhánh Git được kiểm thử:** `feature/db-codefirst-remaining-schema`
**Commit được kiểm thử:** `12d2f3e`
**Cơ sở dữ liệu:** Aiven MySQL 8.4 — `cuahang_codefirst_dev`
**Công cụ kiểm thử:** xUnit 2.9.3, .NET 10, EF Core 10.0.13
**Người thực hiện:** Thành viên nhóm phát triển (chưa ghi nhận cụ thể người chạy kiểm thử)
**Trạng thái kiểm thử cấu trúc CSDL:** PASS
**Trạng thái kiểm thử hành vi CSDL:** CHƯA TRIỂN KHAI

---

## 1. Mục tiêu kiểm thử

Báo cáo này ghi nhận quá trình thực hiện và kết quả kiểm thử tự động đối với cơ sở dữ liệu của hệ thống **Convenience Store Management System — Quản lý cửa hàng tiện lợi**.

Các mục tiêu kiểm thử bao gồm:

1. Xác minh các Unit Test hiện có được thực thi thành công và không phát sinh lỗi.
2. Kiểm tra cấu trúc cơ sở dữ liệu so với phiên bản chuẩn được xây dựng bằng phương pháp Entity Framework Core Code First.
3. Xác minh sự tồn tại và một số thuộc tính cấu trúc của các đối tượng cơ sở dữ liệu, bao gồm bảng, VIEW, TRIGGER, Migration và khóa chính.
4. Thu thập, lưu trữ bằng chứng kiểm thử để có thể đối chiếu và tái thực hiện.
5. Xác định các chức năng và quy tắc nghiệp vụ chưa được kiểm thử đầy đủ.

**Giới hạn phạm vi:** Đợt kiểm thử này chưa chứng minh toàn bộ tính đúng đắn của logic nghiệp vụ trong VIEW, hành vi thực tế của TRIGGER hoặc các ràng buộc dữ liệu.

## 2. Môi trường kiểm thử

| Thành phần | Cấu hình |
|---|---|
| Tên hệ thống | Convenience Store Management System |
| Nền tảng ứng dụng | ASP.NET Core MVC — .NET 10 |
| Framework kiểm thử | xUnit 2.9.3 |
| Bộ công cụ kiểm thử | Microsoft.NET.Test.Sdk 17.14.1 |
| Entity Framework Core | 10.0.13 |
| Nhà cung cấp CSDL cho EF Core | MySql.EntityFrameworkCore 10.0.9 |
| Hệ quản trị cơ sở dữ liệu | Aiven MySQL 8.4 |
| Tên cơ sở dữ liệu | `cuahang_codefirst_dev` |
| Project kiểm thử | `tests/ConvenienceStore.Web.Tests` |
| Môi trường thực thi | Windows PowerShell |
| Ngày thực hiện | 30/10/2026 |

Thông tin xác thực kết nối cơ sở dữ liệu được cung cấp thông qua **User Secrets** trên máy phát triển và các biến môi trường tạm thời.

Báo cáo không chứa mật khẩu, chuỗi kết nối hoặc thông tin xác thực bí mật.

## 3. Tổng hợp kết quả thực hiện kiểm thử

| Nhóm kiểm thử | Số test case | PASS | FAIL | SKIP | Trạng thái |
|---|---:|---:|---:|---:|---|
| Unit Test hiện có | 8 | 8 | 0 | 0 | PASS |
| Database Schema Integration Test | 12 | 12 | 0 | 0 | PASS |
| Kiểm thử hành vi VIEW/TRIGGER/Constraint | 11 dự kiến | — | — | — | CHƯA TRIỂN KHAI |

**Kết quả:** Cả 20 test case đã thực thi trong hai lần chạy thành công đều đạt yêu cầu, không có trường hợp thất bại hoặc bị bỏ qua.

Trong đó, 12 trường hợp kiểm thử tích hợp cơ sở dữ liệu được tạo từ 10 phương thức test DB01–DB10. Riêng DB04 được thực hiện với ba VIEW khác nhau nên tương ứng với ba test case.

### 3.1. Kiểm thử đơn vị — Unit Test

**Lệnh thực thi:**

```powershell
dotnet test ".\ConvenienceStoreManagement.slnx" --no-restore --filter "Category!=DatabaseIntegration"
```

**Kết quả thực tế:**

```text
Test summary:
total: 8
failed: 0
succeeded: 8
skipped: 0
```

**Đánh giá:** PASS.

Tất cả 8 Unit Test được thực thi thành công. Không ghi nhận test thất bại hoặc bị bỏ qua.

### 3.2. Kiểm thử tích hợp cơ sở dữ liệu — Lần chạy đầu tiên

Trong lần chạy đầu tiên, các Database Integration Test chưa được thực thi vì chưa cấu hình đầy đủ biến môi trường cho phép kết nối tới cơ sở dữ liệu.

**Kết quả thực tế:**

```text
Test summary:
total: 10
failed: 0
succeeded: 0
skipped: 10
```

**Đánh giá:** SKIPPED — CHƯA BẬT MÔI TRƯỜNG KIỂM THỬ.

Đây là cơ chế bảo vệ được thiết kế nhằm ngăn các bài kiểm thử tích hợp tự động kết nối đến cơ sở dữ liệu dùng chung khi chưa được cho phép.

Kết quả này được lưu lại để phục vụ truy vết, nhưng không được tính là một lần kiểm thử cơ sở dữ liệu thành công.

### 3.3. Kiểm thử tích hợp cơ sở dữ liệu — Lần chạy thành công

Sau khi thiết lập đầy đủ biến môi trường, bộ kiểm thử được thực thi lại với kết nối MySQL.

**Lệnh thực thi:**

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

Build succeeded in 13.8s
```

**Kết quả xác minh từ file TRX:**

```text
Name    Count
Passed  12
```

**Đánh giá:** PASS.

Toàn bộ 12 test case được thực thi thành công và không có test bị bỏ qua.

Việc chạy Database Integration Test yêu cầu thiết lập hai biến môi trường:

- `RUN_MYSQL_INTEGRATION_TESTS=1`: cho phép thực hiện kiểm thử tích hợp.
- `TEST_MYSQL_CONNECTION`: cung cấp chuỗi kết nối cơ sở dữ liệu.

Hai biến môi trường đã được xóa khỏi phiên PowerShell sau khi hoàn thành kiểm thử.

## 4. Phạm vi và kết quả kiểm thử cấu trúc cơ sở dữ liệu

| Mã test | Kịch bản kiểm thử | Kết quả |
|---|---|---|
| DB01 | Kiểm tra cơ sở dữ liệu có chính xác 29 bảng vật lý | PASS |
| DB02 | Kiểm tra sự tồn tại đầy đủ của 28 bảng nghiệp vụ theo danh sách thiết kế | PASS |
| DB03 | Kiểm tra sự tồn tại của 3 VIEW bắt buộc | PASS |
| DB04 | Kiểm tra khả năng thực hiện truy vấn đối với 3 VIEW mà không cần lấy dữ liệu thực tế | PASS — 3 test case |
| DB05 | Kiểm tra sự tồn tại và định nghĩa sự kiện của TRIGGER bảo vệ thao tác UPDATE | PASS |
| DB06 | Kiểm tra sự tồn tại và định nghĩa sự kiện của TRIGGER bảo vệ thao tác DELETE | PASS |
| DB07 | Kiểm tra cơ sở dữ liệu đã áp dụng 23 Migration | PASS |
| DB08 | Kiểm tra Migration mới nhất khớp với phiên bản chuẩn | PASS |
| DB09 | Kiểm tra khóa chính của bảng `the_kho` | PASS |
| DB10 | Kiểm tra khóa chính của bảng `san_pham` | PASS |

**Tổng hợp độ bao phủ:** 10 phương thức kiểm thử, 12 test case được thực thi, 12 PASS.

Các kết quả này chứng minh những thuộc tính cấu trúc cơ sở dữ liệu đã được kiểm tra phù hợp với kỳ vọng của bộ test.

Tuy nhiên, kết quả PASS ở mức cấu trúc không đồng nghĩa với việc toàn bộ Entity, quan hệ dữ liệu hoặc logic SQL đã được xác minh đầy đủ.

## 5. Bằng chứng kiểm thử

| Mã bằng chứng | Đường dẫn file | Kết quả |
|---|---|---|
| EVD-UNIT-001 | `tests/ConvenienceStore.Web.Tests/TestResults/unit-tests.trx` | 8 PASS |
| EVD-DB-001 | `tests/ConvenienceStore.Web.Tests/TestResults/database-integration-tests.trx` | Lần chạy đầu tiên bị bỏ qua |
| EVD-DB-002 | `tests/ConvenienceStore.Web.Tests/TestResults/database-integration-passed.trx` | 12 PASS |

Các file `.trx` được tạo tự động khi chạy lệnh `dotnet test` với tham số `--logger`.

Những file này hiện được lưu trên máy phát triển. Việc tồn tại trên máy cục bộ không đồng nghĩa với việc chúng đã được đưa lên GitHub.

Các câu lệnh và kết quả kiểm thử được ghi trong báo cáo nhằm bảo đảm các thành viên khác có thể tái thực hiện quy trình kiểm thử.

**Lưu ý bảo mật:** Cần kiểm tra và loại bỏ thông tin nhạy cảm trước khi chia sẻ bằng chứng. Không đưa mật khẩu, token hoặc chuỗi kết nối có thông tin xác thực vào repository.

## 6. Các vấn đề và nhận xét sau kiểm thử

| Mã vấn đề | Mô tả | Mức độ | Trạng thái |
|---|---|---|---|
| OBS-001 | Database Integration Test bị bỏ qua trong lần chạy đầu tiên do thiếu cấu hình cho phép thực thi | Thông tin | ĐÃ XỬ LÝ |
| OBS-002 | Lỗi nhập lệnh PowerShell khiến tham số xuất báo cáo TRX không được thực thi; đã chạy lại thành công | Thông tin | ĐÃ XỬ LÝ |
| OBS-003 | Chưa kiểm tra kết quả nghiệp vụ của VIEW bằng các bộ dữ liệu đầu vào được kiểm soát | Trung bình | CHƯA THỰC HIỆN |
| OBS-004 | Chưa kiểm tra hành vi thực tế của TRIGGER khi thực hiện UPDATE/DELETE trên cơ sở dữ liệu thử nghiệm độc lập | Trung bình | CHƯA THỰC HIỆN |
| OBS-005 | Chưa kiểm thử đầy đủ các ràng buộc FK, UNIQUE, CHECK và các tình huống truy cập đồng thời | Trung bình | CHƯA THỰC HIỆN |

Trong các lần chạy thành công, không ghi nhận lỗi assertion từ Unit Test hoặc Database Schema Integration Test.

Những vấn đề chưa được kiểm thử được ghi nhận để bổ sung ở các giai đoạn tiếp theo, không được xem là những lỗi chức năng đã được xác nhận.

## 7. Đánh giá và quyết định nghiệm thu

### 7.1. Nghiệm thu Unit Test

**Kết quả: PASS**

Toàn bộ 8 Unit Test hiện có đã được thực thi thành công.

Không ghi nhận trường hợp FAIL hoặc SKIP.

**Quyết định:** Chấp nhận kết quả kiểm thử đơn vị trong phạm vi 8 test hiện có.

### 7.2. Nghiệm thu kiểm thử tích hợp cấu trúc cơ sở dữ liệu

**Kết quả: PASS**

Toàn bộ 12 trường hợp kiểm thử tích hợp cấu trúc cơ sở dữ liệu đã được thực thi thành công trên môi trường MySQL được cấu hình.

Phạm vi được xác minh bao gồm:

- Số lượng và danh sách các bảng nghiệp vụ.
- Sự tồn tại của ba VIEW và khả năng thực hiện truy vấn đọc.
- Sự tồn tại và thuộc tính sự kiện của hai TRIGGER.
- Số lượng và phiên bản Migration đã áp dụng.
- Khóa chính của một số bảng được lựa chọn.

**Quyết định:** Chấp nhận kết quả kiểm thử tích hợp cấu trúc cơ sở dữ liệu ở phạm vi chỉ đọc.

### 7.3. Nghiệm thu kiểm thử hành vi cơ sở dữ liệu

**Kết quả: PENDING — CHƯA ĐỦ ĐIỀU KIỆN NGHIỆM THU**

Các công việc còn phải thực hiện:

1. Kiểm thử dữ liệu đầu ra của VIEW bằng bộ dữ liệu mẫu có kết quả kỳ vọng xác định trước.
2. Kiểm thử khả năng từ chối UPDATE và DELETE của TRIGGER.
3. Kiểm thử ràng buộc khóa ngoại (FK), duy nhất (UNIQUE) và điều kiện kiểm tra (CHECK).
4. Kiểm thử các trường hợp truy cập và cập nhật dữ liệu đồng thời.
5. Thực hiện các test có thao tác thay đổi dữ liệu trên một cơ sở dữ liệu MySQL thử nghiệm độc lập.

**Quyết định:** Chưa nghiệm thu hành vi cơ sở dữ liệu cho đến khi các bài kiểm thử tương ứng được triển khai và thực thi thành công.

## 8. Kết luận chung

**Quyết định: PASS đối với các Unit Test và Database Schema Integration Test đã triển khai.**

Ngày 30/10/2026, hệ thống đã thực thi thành công:

- 8/8 Unit Test.
- 12/12 Database Schema Integration Test.
- Tổng cộng 20 test case PASS trong hai lần chạy thành công được ghi nhận.

Kết quả cho thấy các chức năng thuộc phạm vi kiểm thử đơn vị hiện có hoạt động đúng theo assertion được thiết lập, đồng thời các thành phần cấu trúc cơ sở dữ liệu được lựa chọn phù hợp với phiên bản chuẩn Code First.

**Giới hạn kết luận:** Báo cáo này chưa xác nhận tính đúng đắn đầy đủ của toàn bộ hành vi nghiệp vụ cơ sở dữ liệu. Các bài kiểm thử VIEW, TRIGGER, ràng buộc dữ liệu và xử lý đồng thời vẫn cần được bổ sung.

**Công việc tiếp theo:** Xác minh Git HEAD chính xác, lưu giữ bằng chứng thực thi, đồng bộ bảng test case với kết quả kiểm thử, hoàn thiện tài liệu và tạo commit chứa mã kiểm thử cùng tài liệu đã được rà soát.