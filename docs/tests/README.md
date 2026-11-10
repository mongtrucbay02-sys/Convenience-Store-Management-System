# HƯỚNG DẪN KIỂM THỬ CƠ SỞ DỮ LIỆU

**Dự án:** Convenience Store Management System — Hệ thống quản lý cửa hàng tiện lợi
**Ngày cập nhật:** 30/10/2026
**Branch kiểm thử:** `feature/db-codefirst-remaining-schema`
**Commit đã kiểm thử:** `12d2f3e`
**Công nghệ:** ASP.NET Core MVC, .NET 10, xUnit 2.9.3, EF Core 10.0.13, MySQL 8.4

## 1. Giới thiệu

Thư mục `docs/tests/` quản lý các tài liệu phục vụ kiểm thử tự động và đánh giá chất lượng cơ sở dữ liệu của hệ thống Convenience Store Management System.

Bộ kiểm thử hiện tại tập trung vào hai nhóm:

- **Unit Test:** Kiểm tra logic nghiệp vụ Dashboard bằng dữ liệu giả lập, không kết nối cơ sở dữ liệu.
- **Database Integration Test:** Kiểm tra cấu trúc cơ sở dữ liệu MySQL thực tế bằng các truy vấn chỉ đọc.

Các bài kiểm thử hành vi của VIEW, TRIGGER, ràng buộc dữ liệu và xử lý đồng thời đã được thiết kế nhưng chưa triển khai.

## 2. Cấu trúc thư mục

```text
Convenience-Store-Management-System/
├── docs/
│   └── tests/
│       ├── README.md
│       ├── test-plan.md
│       ├── database-test-cases.md
│       └── test-report.md
│
└── tests/
    └── ConvenienceStore.Web.Tests/
        ├── ConvenienceStore.Web.Tests.csproj
        ├── DashboardServiceTests.cs
        ├── Integration/
        │   └── Database/
        │       └── Schema/
        │           └── SchemaStructureTests.cs
        └── TestResults/
            ├── unit-tests.trx
            ├── database-integration-tests.trx
            └── database-integration-passed.trx
```

**Lưu ý:** `TestResults/` chứa các bằng chứng được tạo trên máy thực thi, không nhất thiết có trong Git repository.

## 3. Danh sách tài liệu

| Tài liệu | Nội dung |
|---|---|
| `README.md` | Hướng dẫn sử dụng bộ kiểm thử |
| `test-plan.md` | Kế hoạch, phạm vi, môi trường và tiêu chí nghiệm thu |
| `database-test-cases.md` | Các kịch bản kiểm thử, điều kiện đầu vào và kết quả mong đợi |
| `test-report.md` | Báo cáo kết quả thực tế, bằng chứng và quyết định nghiệm thu |

Mã nguồn kiểm thử tự động được đặt tại:

`tests/ConvenienceStore.Web.Tests/Integration/Database/Schema/SchemaStructureTests.cs`

Không đặt mã kiểm thử trong thư mục `src/` của ứng dụng và không thay thế file `DashboardServiceTests.cs` hiện có.

## 4. Yêu cầu môi trường

| Thành phần | Yêu cầu |
|---|---|
| Hệ điều hành | Windows |
| IDE | Visual Studio hoặc IDE hỗ trợ .NET |
| .NET SDK | .NET 10 |
| Framework kiểm thử | xUnit 2.9.3 |
| EF Core | 10.0.13 |
| MySQL provider | MySql.EntityFrameworkCore 10.0.9 |
| MySQL client | `MySql.Data` — phiên bản 26.7.0 đã được resolve trong project test |
| Database | MySQL 8.4 trên Aiven |
| Database kiểm thử cấu trúc | `cuahang_codefirst_dev` |

Project test đã có các thư viện cần thiết thông qua NuGet và tham chiếu đến project ứng dụng.

Không cần tạo project xUnit mới.

## 5. Hướng dẫn chạy kiểm thử

### 5.1. Kiểm tra Git

Mở PowerShell tại thư mục gốc repository.

```powershell
git branch --show-current
git rev-parse --short HEAD
```

Baseline đã kiểm thử:

```text
feature/db-codefirst-remaining-schema
12d2f3e
```

Khi chuyển sang nhánh hoặc commit khác, cần kiểm tra lại tính tương thích của schema và bộ test.

### 5.2. Biên dịch ứng dụng

```powershell
dotnet build ".\ConvenienceStoreManagement.slnx" --no-restore
```

Kết quả mong đợi: `Build succeeded`.

### 5.3. Chạy Unit Test

```powershell
dotnet test ".\ConvenienceStoreManagement.slnx" --no-restore --filter "Category!=DatabaseIntegration"
```

Kết quả đã ghi nhận ngày 30/10/2026:

```text
total: 8
failed: 0
succeeded: 8
skipped: 0
```

**Trạng thái:** PASS.

Unit Test không yêu cầu kết nối MySQL.

### 5.4. Chuẩn bị Database Integration Test

Các Database Integration Test được thiết kế ở chế độ **không tự động kết nối database**.

Để chạy, cần thiết lập hai biến môi trường trong cùng phiên PowerShell:

```powershell
$env:RUN_MYSQL_INTEGRATION_TESTS = "1"
```

Nạp chuỗi kết nối vào `TEST_MYSQL_CONNECTION` bằng User Secrets hoặc nguồn cấu hình bảo mật được phê duyệt.

Ví dụ, với cấu hình User Secrets hiện tại:

```powershell
$secretId = "44c44e3a-cfa7-4b10-ac16-97a862795a61"
$path = Join-Path $env:APPDATA "Microsoft\UserSecrets\$secretId\secrets.json"
$j = Get-Content $path -Raw | ConvertFrom-Json
$env:TEST_MYSQL_CONNECTION = if ($j.'ConnectionStrings:DefaultConnection') { $j.'ConnectionStrings:DefaultConnection' } else { $j.ConnectionStrings.DefaultConnection }
```

Đường dẫn User Secrets có thể khác trên máy của từng thành viên. Không sao chép mật khẩu hoặc connection string sang tài liệu hoặc repository.

Chỉ chạy test khi đã xác minh đúng database đích và quyền truy cập phù hợp.

### 5.5. Chạy Database Integration Test

```powershell
dotnet test ".\ConvenienceStoreManagement.slnx" --no-restore --filter "Category=DatabaseIntegration" --logger "trx;LogFileName=database-integration-passed.trx"
```

Kết quả đã ghi nhận ngày 30/10/2026:

```text
total: 12
failed: 0
succeeded: 12
skipped: 0
duration: 12.0s
```

**Trạng thái:** PASS.

Bộ kiểm thử có 10 phương thức test DB01–DB10, tương ứng 12 trường hợp thực thi, trong đó DB04 gồm ba bộ dữ liệu đầu vào.

### 5.6. Xóa biến môi trường sau kiểm thử

```powershell
Remove-Item Env:TEST_MYSQL_CONNECTION -ErrorAction SilentlyContinue
Remove-Item Env:RUN_MYSQL_INTEGRATION_TESTS -ErrorAction SilentlyContinue
```

Không giữ thông tin xác thực trong script được đưa lên Git.

## 6. Kiểm tra kết quả TRX

File TRX lưu chi tiết kết quả thực thi kiểm thử.

Để xem kết quả của Database Integration Test:

```powershell
$trxPath = ".\tests\ConvenienceStore.Web.Tests\TestResults\database-integration-passed.trx"
[xml]$trx = Get-Content $trxPath -Raw
$trx.TestRun.ResultSummary.Counters
```

Kết quả đã xác minh:

```text
total       : 12
executed    : 12
passed      : 12
failed      : 0
notExecuted : 0
```

Để xem số test theo trạng thái:

```powershell
$trx.TestRun.Results.UnitTestResult | Group-Object outcome | Select-Object Name,Count
```

Kết quả:

```text
Name    Count
Passed  12
```

Các lần chạy bị SKIP không được xem là PASS.

## 7. Phạm vi kiểm thử đã hoàn thành

| Nhóm | Nội dung | Kết quả |
|---|---|---|
| Unit Test | Logic Dashboard | 8/8 PASS |
| Bảng | Kiểm tra 29 bảng vật lý và 28 bảng nghiệp vụ | PASS |
| VIEW | Kiểm tra 3 VIEW tồn tại và truy vấn được | PASS |
| TRIGGER | Kiểm tra 2 TRIGGER tồn tại và đúng sự kiện | PASS |
| Migration | Kiểm tra 23 Migration và Migration mới nhất | PASS |
| Khóa chính | Kiểm tra `san_pham` và `the_kho` | PASS |

**Tổng kết:** 20 test case đã thực thi thành công thuộc hai nhóm Unit Test và Database Integration Test.

## 8. Các kiểm thử chưa triển khai

Các kịch bản tiếp theo bao gồm:

- VIEW-01 đến VIEW-03: Kiểm tra kết quả nghiệp vụ của ba VIEW.
- TRG-01 đến TRG-04: Kiểm tra hành vi INSERT, UPDATE, DELETE và tính bất biến dữ liệu thẻ kho.
- CON-01 đến CON-04: Kiểm tra FK, UNIQUE, CHECK và giao dịch tồn kho đồng thời.

**Trạng thái:** CHƯA TRIỂN KHAI.

Các trường hợp trên phải chạy trong database kiểm thử độc lập có dữ liệu mẫu được kiểm soát.

Không chạy những bài test có thao tác thay đổi dữ liệu trên `cuahang_codefirst_dev` dùng chung.

## 9. Quy định an toàn và quản lý Git

1. Không commit connection string, mật khẩu Aiven hoặc thông tin xác thực.
2. Không tự động chạy Integration Test trên database dùng chung.
3. Không sử dụng tài khoản quản trị MySQL cho các bài test đọc thông thường khi có thể cấp quyền tối thiểu.
4. Không thực hiện DML/DDL ngoài phạm vi kiểm thử đã phê duyệt.
5. Không sửa số lượng bảng hoặc Migration kỳ vọng chỉ để làm test PASS.
6. Không đưa thư mục `tmp/` hoặc dữ liệu tạm lên Git khi chưa được rà soát.
7. Kiểm tra file TRX trước khi chia sẻ vì log có thể chứa thông tin môi trường hoặc lỗi nhạy cảm.
8. Không ghi nhận một test là PASS nếu test chưa được thực thi.

## 10. Kết quả và công việc tiếp theo

Tính đến ngày 30/10/2026:

| Hạng mục | Trạng thái |
|---|---|
| Build solution | PASS |
| Unit Test | 8/8 PASS |
| Database Schema Integration Test | 12/12 PASS |
| Xuất bằng chứng TRX | Đã hoàn thành |
| Tài liệu kế hoạch | Đã cập nhật |
| Tài liệu test case | Đã cập nhật |
| Báo cáo kết quả | Đã cập nhật |
| Kiểm thử hành vi database | Chưa triển khai |
| Review và commit Git | Chưa hoàn thành |

**Kết luận:** Bộ kiểm thử tự động hiện tại đã đạt nghiệm thu trong phạm vi Unit Test và kiểm thử cấu trúc cơ sở dữ liệu chỉ đọc.

Việc nghiệm thu toàn bộ hành vi cơ sở dữ liệu vẫn cần các bài kiểm thử bổ sung trên môi trường độc lập.