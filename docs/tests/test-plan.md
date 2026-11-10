# KẾ HOẠCH KIỂM THỬ CƠ SỞ DỮ LIỆU
## Hệ thống quản lý cửa hàng tiện lợi — Convenience Store Management System

| Thông tin | Giá trị |
|---|---|
| Mã kế hoạch | DB-TP-001 |
| Dự án | Convenience Store Management System |
| Ngày cập nhật | 30/10/2026 |
| Nhánh kiểm thử | `feature/db-codefirst-remaining-schema` |
| Commit kiểm thử | `12d2f3e` |
| Công nghệ | ASP.NET Core MVC, .NET 10, xUnit 2.9.3, EF Core 10.0.13 |
| Hệ quản trị CSDL | Aiven MySQL 8.4 |
| Database kiểm thử cấu trúc | `cuahang_codefirst_dev` |
| Trạng thái Unit Test | PASS — 8/8 |
| Trạng thái kiểm thử cấu trúc | PASS — 12/12 |
| Trạng thái kiểm thử hành vi | CHƯA TRIỂN KHAI |
| Tài liệu kết quả | `docs/tests/test-report.md` |
| Tài liệu test case | `docs/tests/database-test-cases.md` |

---

## 1. Mục tiêu kiểm thử

### 1.1. Mục tiêu tổng quát

Kiểm tra và xác minh cấu trúc cơ sở dữ liệu được xây dựng bằng phương pháp Entity Framework Core Code First, bảo đảm những đối tượng được lựa chọn phù hợp với phiên bản thiết kế và Migration đã triển khai.

Đồng thời, kế hoạch xác định các hoạt động kiểm thử cần bổ sung để đánh giá hành vi thực tế của VIEW, TRIGGER, các ràng buộc dữ liệu và tính nhất quán của giao dịch.

### 1.2. Mục tiêu cụ thể

1. Kiểm tra solution có thể biên dịch thành công trên nền tảng .NET 10.
2. Đảm bảo các Unit Test hiện có không phát sinh lỗi.
3. Kiểm tra số lượng và danh sách các bảng trong cơ sở dữ liệu.
4. Kiểm tra sự tồn tại và khả năng truy vấn của các VIEW.
5. Kiểm tra sự tồn tại, sự kiện và thời điểm kích hoạt của các TRIGGER.
6. Kiểm tra lịch sử Migration và phiên bản Migration mới nhất.
7. Kiểm tra sự tồn tại của khóa chính trên các bảng được lựa chọn.
8. Lưu kết quả kiểm thử tự động và bằng chứng phục vụ đánh giá, báo cáo.
9. Xác định những trường hợp kiểm thử hành vi cần được triển khai ở giai đoạn tiếp theo.

**Nguyên tắc:** Việc một bảng, VIEW hoặc TRIGGER tồn tại không có nghĩa là toàn bộ hành vi nghiệp vụ của đối tượng đó đã đúng.

## 2. Phạm vi kiểm thử

### 2.1. Phạm vi đã triển khai

**a. Unit Test**

Kiểm thử các quy tắc và phép tính thuộc phạm vi Dashboard thông qua dữ liệu giả lập và thời gian được cố định.

Các Unit Test không yêu cầu kết nối MySQL.

**b. Database Schema Integration Test**

Kiểm thử tự động các thành phần sau:

| Thành phần | Nội dung kiểm tra | Test case |
|---|---|---|
| Bảng | Tổng số 29 bảng vật lý | DB01 |
| Bảng nghiệp vụ | Danh sách 28 bảng nghiệp vụ | DB02 |
| VIEW | Sự tồn tại của 3 VIEW | DB03 |
| VIEW | Khả năng truy vấn từng VIEW mà không lấy dữ liệu | DB04-A/B/C |
| TRIGGER | Sự tồn tại và cấu hình TRIGGER UPDATE | DB05 |
| TRIGGER | Sự tồn tại và cấu hình TRIGGER DELETE | DB06 |
| Migration | Tổng số 23 Migration đã áp dụng | DB07 |
| Migration | Migration cuối cùng khớp phiên bản chuẩn | DB08 |
| Khóa chính | Kiểm tra khóa chính bảng `the_kho` | DB09 |
| Khóa chính | Kiểm tra khóa chính bảng `san_pham` | DB10 |

Tổng cộng có **10 phương thức kiểm thử và 12 test case tích hợp**.

### 2.2. Phạm vi chưa triển khai

Các nội dung sau chưa được kiểm thử hành vi:

- Xác minh giá bán hiện hành được tính đúng theo lịch sử giá.
- Xác minh trạng thái chương trình khuyến mãi theo thời gian.
- Xác minh lượt sử dụng khuyến mãi theo trạng thái hóa đơn.
- Xác minh TRIGGER từ chối UPDATE và DELETE trên bảng `the_kho`.
- Xác minh dữ liệu thẻ kho không thay đổi sau thao tác bị từ chối.
- Xác minh khả năng INSERT hợp lệ vào thẻ kho.
- Kiểm thử ràng buộc khóa ngoại (FK), UNIQUE và CHECK.
- Kiểm thử giao dịch cập nhật tồn kho đồng thời.
- Kiểm thử rollback Migration và các thay đổi cấu trúc khác.

Các trường hợp này được quản lý trong tài liệu `database-test-cases.md` với trạng thái **CHƯA TRIỂN KHAI**.

## 3. Cơ sở và phiên bản kiểm thử

Cấu trúc cơ sở dữ liệu được đối chiếu theo baseline đã ghi nhận:

| Thành phần | Giá trị chuẩn |
|---|---|
| Bảng vật lý | 29 |
| Bảng nghiệp vụ | 28 |
| VIEW | 3 |
| TRIGGER | 2 |
| Migration đã áp dụng | 23 |
| Migration mới nhất | `20261027004309_DatabaseViewsAndStockCardTriggers` |

Baseline được sử dụng với nhánh `feature/db-codefirst-remaining-schema`, commit `12d2f3e`, đã được xác minh bằng lệnh Git.

Khi hệ thống bổ sung Migration hoặc thay đổi cấu trúc hợp lệ, bộ kiểm thử và baseline phải được rà soát, cập nhật thông qua quy trình review của nhóm.

Không được tùy ý thay giá trị mong đợi chỉ để làm cho test PASS.

## 4. Môi trường và công cụ kiểm thử

### 4.1. Môi trường phát triển

| Thành phần | Cấu hình |
|---|---|
| Hệ điều hành | Windows |
| Công cụ phát triển | Visual Studio / PowerShell |
| Nền tảng | .NET 10 |
| Ngôn ngữ | C# |
| Unit Test framework | xUnit 2.9.3 |
| Test SDK | Microsoft.NET.Test.Sdk 17.14.1 |
| ORM | Entity Framework Core 10.0.13 |
| Database provider | MySql.EntityFrameworkCore 10.0.9 |
| Database | Aiven MySQL 8.4 |
| Test project | `tests/ConvenienceStore.Web.Tests` |

### 4.2. Môi trường kiểm thử cấu trúc

Database: `cuahang_codefirst_dev`.

Môi trường này chỉ được sử dụng cho các bài kiểm thử cấu trúc bằng truy vấn đọc.

Các thao tác được cho phép trong phạm vi bộ test hiện tại bao gồm:

- Đọc metadata từ `information_schema`.
- Đọc lịch sử Migration.
- Thực hiện truy vấn VIEW không trả về bản ghi nhằm kiểm tra khả năng thực thi.

Không thực hiện INSERT, UPDATE, DELETE, DROP, ALTER hoặc CREATE trong bộ kiểm thử cấu trúc này.

Ưu tiên sử dụng tài khoản dịch vụ chỉ có quyền đọc phù hợp thay vì tài khoản quản trị.

### 4.3. Môi trường kiểm thử hành vi

Sử dụng một cơ sở dữ liệu MySQL 8.4 độc lập, được tạo dành riêng cho kiểm thử.

Yêu cầu:

1. Có cấu trúc tương thích với baseline được phê duyệt.
2. Có dữ liệu mẫu độc lập và xác định được kết quả mong đợi.
3. Không sử dụng dữ liệu nghiệp vụ thật của nhóm.
4. Các test có thể chạy lặp lại.
5. Có cơ chế khôi phục trạng thái hoặc tái tạo database.
6. Không tác động đến `cuahang_codefirst_dev`.

Cần chú ý rằng một số thao tác DDL trên MySQL có thể tự động commit. Vì vậy, không sử dụng rollback làm cơ chế an toàn duy nhất cho các bài kiểm thử thay đổi cấu trúc.

## 5. Phương pháp và chiến lược kiểm thử

### 5.1. Unit Test

Sử dụng xUnit để kiểm tra logic C# với các phụ thuộc được thay thế bằng Fake hoặc dữ liệu giả lập.

**Mục tiêu:** Phát hiện sai lệch trong các quy tắc nghiệp vụ thuộc phạm vi test mà không phụ thuộc vào database hoặc dịch vụ bên ngoài.

**Kết quả đã ghi nhận:** 8/8 PASS.

### 5.2. Kiểm thử tích hợp cấu trúc cơ sở dữ liệu

Sử dụng xUnit và MySQL để kiểm tra metadata thực tế.

Các bài test được đánh dấu:

`Category=DatabaseIntegration`

Đặc điểm:

- Chỉ đọc dữ liệu và metadata.
- Chỉ chạy khi được bật thủ công.
- Không ghi thông tin xác thực vào log.
- Xác minh database đích trước khi thực thi.
- Đánh giá theo assertion đã định nghĩa.

**Kết quả đã ghi nhận:** 12/12 PASS.

### 5.3. Kiểm thử hành vi cơ sở dữ liệu

Sử dụng Integration Test với bộ dữ liệu thử nghiệm độc lập.

Các kỹ thuật dự kiến bao gồm:

- Kiểm thử trường hợp hợp lệ.
- Kiểm thử trường hợp không hợp lệ.
- Kiểm thử giá trị biên.
- Kiểm thử ràng buộc dữ liệu.
- Kiểm thử giao dịch đồng thời.
- Kiểm thử tính nhất quán sau khi thao tác bị từ chối.

**Trạng thái:** CHƯA TRIỂN KHAI.

## 6. Vai trò và trách nhiệm

| Vai trò | Thành viên | Trách nhiệm |
|---|---|---|
| QA / Testing | Nguyễn Thị Thu Hoài? Không — cần xác nhận theo phân công thực tế | Không sử dụng hàng này để giao việc |
| QA / Testing | Tạ Quang Huy | Điều phối kiểm thử, rà soát test case và tổng hợp báo cáo |
| Developer / Database | Phan Hải Yến | Xây dựng và kiểm tra test liên quan đến schema, sản phẩm, nhập hàng và tồn kho |
| Team Lead | Phạm Gia Thăng | Rà soát thay đổi database, xác nhận phạm vi và phê duyệt kiểm thử tác động dữ liệu |

**Lưu ý:** Bảng này thể hiện phân công trách nhiệm dự kiến theo vai trò trong nhóm, không phải bằng chứng rằng từng thành viên đã thực hiện hoặc phê duyệt các công việc tương ứng.

Người thực thi từng lần kiểm thử cần được ghi nhận riêng trong báo cáo.

## 7. Quy trình thực hiện kiểm thử

### 7.1. Kiểm tra phiên bản mã nguồn

```powershell
git branch --show-current
git rev-parse --short HEAD
```

Kết quả baseline:

```text
feature/db-codefirst-remaining-schema
12d2f3e
```

### 7.2. Biên dịch solution

```powershell
dotnet build ".\ConvenienceStoreManagement.slnx" --no-restore
```

**Kết quả đã ghi nhận:** PASS.

### 7.3. Chạy Unit Test

```powershell
dotnet test ".\ConvenienceStoreManagement.slnx" --no-restore --filter "Category!=DatabaseIntegration"
```

**Kết quả đã ghi nhận:** 8 PASS, 0 FAIL, 0 SKIP.

### 7.4. Bật Database Integration Test

Thiết lập:

```powershell
$env:RUN_MYSQL_INTEGRATION_TESTS = "1"
```

Nạp `TEST_MYSQL_CONNECTION` từ User Secrets hoặc nguồn cấu hình bảo mật đã được phê duyệt.

Không in mật khẩu hoặc chuỗi kết nối ra Terminal.

### 7.5. Thực thi Integration Test và xuất TRX

```powershell
dotnet test ".\ConvenienceStoreManagement.slnx" --no-restore --filter "Category=DatabaseIntegration" --logger "trx;LogFileName=database-integration-passed.trx"
```

**Kết quả đã ghi nhận:**

```text
Test summary:
total: 12
failed: 0
succeeded: 12
skipped: 0
duration: 12.0s
```

### 7.6. Xóa biến môi trường

```powershell
Remove-Item Env:TEST_MYSQL_CONNECTION -ErrorAction SilentlyContinue
Remove-Item Env:RUN_MYSQL_INTEGRATION_TESTS -ErrorAction SilentlyContinue
```

### 7.7. Kiểm tra bằng chứng TRX

Kết quả đã xác minh:

```text
total       : 12
executed    : 12
passed      : 12
failed      : 0
notExecuted : 0
```

## 8. Tiêu chí bắt đầu và kết thúc kiểm thử

### 8.1. Tiêu chí bắt đầu

Hoạt động kiểm thử chỉ được thực hiện khi:

1. Solution biên dịch thành công.
2. Xác định đúng nhánh và commit cần kiểm thử.
3. Database đích phù hợp với baseline.
4. Có thông tin xác thực hợp lệ và được quản lý an toàn.
5. Tài khoản kiểm thử có đủ quyền cần thiết.
6. Bộ test không chứa các thao tác ngoài phạm vi cho phép.
7. Các biến cho phép chạy Integration Test được thiết lập có chủ đích.

### 8.2. Tiêu chí hoàn thành kiểm thử cấu trúc

Nhóm kiểm thử cấu trúc được nghiệm thu khi:

- Tất cả 12 test case được thực thi.
- Không có test FAIL.
- Không có test SKIP.
- Có bằng chứng kết quả thực thi.
- Database không bị thay đổi bởi bộ kiểm thử.
- Kết quả phù hợp với baseline được phê duyệt.

**Trạng thái hiện tại: PASS.**

### 8.3. Tiêu chí nghiệm thu toàn bộ cơ sở dữ liệu

Chỉ được nghiệm thu toàn bộ hành vi cơ sở dữ liệu khi:

1. Hoàn thành kiểm thử logic của ba VIEW.
2. Xác minh hành vi UPDATE/DELETE của hai TRIGGER.
3. Kiểm thử FK, UNIQUE và CHECK theo cấu trúc triển khai thực tế.
4. Xác minh tính nhất quán của giao dịch tồn kho.
5. Có đầy đủ kết quả và bằng chứng từ môi trường kiểm thử độc lập.
6. Các lỗi nghiêm trọng được xử lý và kiểm thử lại.

**Trạng thái hiện tại: PENDING — CHƯA ĐỦ ĐIỀU KIỆN NGHIỆM THU.**

## 9. Quản lý bằng chứng và báo cáo

| Mã bằng chứng | File | Kết quả |
|---|---|---|
| EVD-UNIT-001 | `tests/ConvenienceStore.Web.Tests/TestResults/unit-tests.trx` | 8 PASS |
| EVD-DB-001 | `tests/ConvenienceStore.Web.Tests/TestResults/database-integration-tests.trx` | Lần chạy đầu tiên bị SKIP |
| EVD-DB-002 | `tests/ConvenienceStore.Web.Tests/TestResults/database-integration-passed.trx` | 12 PASS |

Bằng chứng kiểm thử được quản lý theo các nguyên tắc:

- Ghi nhận ngày thực hiện, nhánh và commit.
- Lưu lệnh chạy và số lượng PASS/FAIL/SKIP.
- Phân biệt kiểm thử đã thực thi và chưa triển khai.
- Rà soát log trước khi chia sẻ.
- Không đưa thông tin xác thực vào Git.
- Không tự động xem file TRX cục bộ là bằng chứng đã lưu trên repository.

Báo cáo thực thi chi tiết được lưu tại:

`docs/tests/test-report.md`

Danh sách kịch bản và test case được quản lý tại:

`docs/tests/database-test-cases.md`

## 10. Rủi ro và biện pháp kiểm soát

| Rủi ro | Ảnh hưởng | Biện pháp |
|---|---|---|
| Chạy test trên nhầm database | Ảnh hưởng dữ liệu hoặc kết quả kiểm thử | Xác minh tên database trước khi chạy |
| Lộ connection string | Rủi ro bảo mật | Sử dụng User Secrets và biến môi trường tạm thời |
| Test bị SKIP | Không có bằng chứng kiểm thử thực tế | Kiểm tra số PASS/FAIL/SKIP trong log và TRX |
| Baseline thay đổi | Test không còn phản ánh schema mới | Rà soát và cập nhật theo Migration đã duyệt |
| Test hành vi trên database dùng chung | Có thể thay đổi dữ liệu nghiệp vụ | Sử dụng database test độc lập |
| Test cấu trúc PASS nhưng logic sai | Đánh giá thiếu chính xác | Triển khai thêm kiểm thử hành vi và giá trị biên |
| Lỗi xử lý đồng thời | Tồn kho hoặc thẻ kho không nhất quán | Kiểm thử giao dịch đồng thời bằng dữ liệu kiểm soát |

## 11. Kết quả và kế hoạch tiếp theo

### 11.1. Kết quả đã hoàn thành

Đến ngày 30/10/2026:

- Solution build thành công.
- 8/8 Unit Test PASS.
- 12/12 Database Schema Integration Test PASS.
- Bằng chứng kiểm thử đã được xuất dưới định dạng TRX.
- Nhánh và commit `12d2f3e` đã được xác minh.

### 11.2. Công việc tiếp theo

1. Hoàn thiện việc rà soát và thống nhất tài liệu kiểm thử.
2. Chuẩn bị database MySQL thử nghiệm độc lập.
3. Triển khai kiểm thử hành vi VIEW.
4. Triển khai kiểm thử hành vi TRIGGER.
5. Bổ sung kiểm thử FK, UNIQUE, CHECK.
6. Bổ sung kiểm thử các thao tác tồn kho đồng thời.
7. Cập nhật báo cáo bằng kết quả và bằng chứng mới.
8. Thực hiện review và commit mã kiểm thử cùng tài liệu.

## 12. Kết luận

**Kết quả nghiệm thu hiện tại: PASS đối với Unit Test và Database Schema Integration Test đã triển khai.**

Bộ kiểm thử đã xác nhận thành công 20 test case thuộc hai nhóm kiểm thử hiện có.

Tuy nhiên, việc kiểm thử cấu trúc không thay thế cho kiểm thử hành vi cơ sở dữ liệu. Những yêu cầu liên quan đến logic VIEW, TRIGGER, ràng buộc dữ liệu và xử lý đồng thời vẫn đang chờ triển khai.

**Quyết định:** Chấp nhận giai đoạn kiểm thử cấu trúc. Tiếp tục giai đoạn kiểm thử hành vi trên môi trường MySQL độc lập trước khi nghiệm thu đầy đủ cơ sở dữ liệu.