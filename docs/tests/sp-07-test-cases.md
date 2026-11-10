# SP-07 — Tài liệu test case: Danh sách danh mục

**Mã tài liệu:** SP07-TC-001
**Dự án:** Convenience Store Management System
**Ngày kiểm thử:** 30/10/2026
**Branch:** `integration/sp-07-codefirst`
**Phạm vi:** `GET /api/v1/categories`, `CategoriesController`, `CategoryService`, `EfCategoryReadRepository`
**Công nghệ:** ASP.NET Core MVC, .NET 10, xUnit, EF Core, MySQL 8.4
**Môi trường integration:** Docker MySQL `127.0.0.1:3307`, database riêng `sp07_repository_tests`; không sử dụng Aiven.

## 1. Mục tiêu và nguyên tắc

Xác minh hợp đồng danh sách danh mục, phân trang, kiểm tra tham số, ánh xạ dữ liệu, số lượng sản phẩm theo danh mục và tính an toàn của fixture kiểm thử. Unit Test dùng phụ thuộc giả lập và không kết nối database. Repository Integration Test dùng EF Core với MySQL Docker và transaction rollback. HTTP Test hiện là kiểm thử thủ công, không tính là test tự động.

**Tiền điều kiện:** ứng dụng build thành công; schema test có đủ 23 migration; dữ liệu nghiệp vụ trong bốn bảng `danh_muc`, `san_pham`, `tai_khoan`, `vai_tro` ban đầu rỗng; biến `SP07_TEST_CONNECTION` chỉ trỏ vào database test riêng. Test không chạy migration hoặc DDL, không truy cập database Aiven.

## 2. Unit Test — 10 trường hợp

| ID | Phương thức xUnit | Dữ liệu / thao tác | Kết quả mong đợi | Thực tế |
|---|---|---|---|---|
| SP07-UT-01 | `CategoryServiceTests.GetCategories_ForwardsCancellationToken` | Truyền cancellation token | Repository nhận đúng token | PASS |
| SP07-UT-02 | `CategoryServiceTests.GetCategories_ForwardsQueryToRepository` | Truyền query vào service | Repository nhận đúng query | PASS |
| SP07-UT-03 | `CategoryServiceTests.GetCategories_ReturnsRepositoryResult` | Repository trả về trang dữ liệu | Service trả đúng kết quả | PASS |
| SP07-UT-04 | `CategoriesControllerTests.ValidQuery_ReturnsOkWithCategories` | Query hợp lệ, dữ liệu có danh mục | HTTP 200 và dữ liệu hợp lệ | PASS |
| SP07-UT-05 | `CategoriesControllerTests.ValidQuery_ForwardsQueryAndCancellationToken` | Query hợp lệ và token | Controller chuyển đúng tham số và token | PASS |
| SP07-UT-06 | `CategoriesControllerTests.EmptyCategories_ReturnsEmptyPagedResult` | Kết quả rỗng | Trang rỗng theo hợp đồng DTO | PASS |
| SP07-UT-07 | `CategoriesControllerTests.PageZero_ReturnsBadRequestWithoutCallingService` | `page=0` | HTTP 400; không gọi service | PASS |
| SP07-UT-08 | `CategoriesControllerTests.SizeZero_ReturnsBadRequestWithoutCallingService` | `size=0` | HTTP 400; không gọi service | PASS |
| SP07-UT-09 | `CategoriesControllerTests.SizeOver100_ForwardsNormalizedSizeToService` | `size>100` | Service nhận size được chuẩn hóa | PASS |
| SP07-UT-10 | `CategoriesControllerTests.SizeOver100_NormalizedSizeIs100` | `size>100` | `size=100` | PASS |

## 3. Repository Integration Test — 8 trường hợp

**Dữ liệu kiểm thử** được tạo và truy vấn trong cùng một transaction EF Core; rollback trong `finally`. Những kiểm thử này yêu cầu database test riêng ban đầu rỗng và không chạy song song với tác vụ ghi dữ liệu khác.

| ID | Phương thức xUnit | Dữ liệu / bước thực hiện | Kết quả mong đợi | Thực tế |
|---|---|---|---|---|
| SP07-DB-01 | `DB01_EmptyDatabase_ReturnsEmptyPage` | Không có danh mục; truy vấn trang 1 size 20 | `Total=0`, `Items` rỗng, `Page=1`, `Size=20` | PASS |
| SP07-DB-02 | `DB02_ThreeCategories_ReturnsTotalThree` | Tạo 3 danh mục; truy vấn trang 1 | `Total=3`, 3 items | PASS |
| SP07-DB-03 | `DB03_PageTwo_ReturnsLastCategory` | Tạo 3 danh mục; query `page=2&size=2` | `Total=3`, 1 item cuối | PASS |
| SP07-DB-04 | `DB04_CategoryWithTwoProducts_CountsTwo` | Tạo vai trò, tài khoản, một danh mục, 2 sản phẩm | `SoSanPham=2` | PASS |
| SP07-DB-05 | `DB05_CategoryWithoutProducts_CountsZero` | Danh mục không có sản phẩm | `SoSanPham=0` | PASS |
| SP07-DB-06 | `DB06_Size150_IsNormalizedTo100` | Query `size=150` | `Size=100` | PASS |
| SP07-DB-07 | `DB07_HiddenCategory_IsCurrentlyIncluded` | Danh mục trạng thái `DA_AN` | Theo implementation hiện tại vẫn trả danh mục | PASS* |
| SP07-DB-08 | `DB08_StoppedProduct_IsCurrentlyCounted` | Một sản phẩm `NGUNG_KINH_DOANH` | Theo implementation hiện tại vẫn tính `SoSanPham=1` | PASS* |

*Hai trường hợp DB-07 và DB-08 kiểm chứng hành vi hiện tại, **không chứng minh quy tắc nghiệp vụ đã được phê duyệt**. Cần xác nhận với nhóm trước nghiệm thu chức năng.

## 4. HTTP Test thủ công — 4 trường hợp

| ID | Request | Kết quả mong đợi | Kết quả ghi nhận | Hình thức |
|---|---|---|---|---|
| SP07-HTTP-01 | `GET /api/v1/categories?page=1&size=20` trên database test rỗng | 200; `items=[]`, `total=0`, `page=1`, `size=20` | PASS | Thủ công |
| SP07-HTTP-02 | `GET /api/v1/categories?page=0&size=20` | 400 | PASS | Thủ công |
| SP07-HTTP-03 | `GET /api/v1/categories?page=1&size=0` | 400 | PASS | Thủ công |
| SP07-HTTP-04 | `GET /api/v1/categories?page=1&size=150` | 200; size chuẩn hóa thành 100 | PASS | Thủ công |

**Giới hạn:** các HTTP test trên mới được kiểm tra thủ công; không có bản ghi tự động/trx trong bộ evidence hiện tại. Chưa kiểm chứng đầy đủ xác thực, phân quyền, phân trang trên endpoint có dữ liệu thực hoặc concurrency của HTTP API.

## 5. Thực thi và bằng chứng

Chạy Unit Test (không cần database):

```powershell
dotnet test ".\tests\ConvenienceStore.Web.Tests\ConvenienceStore.Web.Tests.csproj" --filter "FullyQualifiedName~CategoriesControllerTests|FullyQualifiedName~CategoryServiceTests" --verbosity normal
```

Chạy Integration Test chỉ sau khi thiết lập an toàn `SP07_TEST_CONNECTION` trỏ tới MySQL Docker `127.0.0.1:3307`, database `sp07_repository_tests`, tài khoản `sp07_test` (không lưu mật khẩu trong tài liệu):

```powershell
dotnet test ".\tests\ConvenienceStore.Web.Tests\ConvenienceStore.Web.Tests.csproj" --filter "FullyQualifiedName~EfCategoryReadRepositoryIntegrationTests" --verbosity normal
Remove-Item Env:SP07_TEST_CONNECTION -ErrorAction SilentlyContinue
```

Bằng chứng kiểm thử tại `docs/tests/evidence/sp-07/`: `unit-tests.txt`, `integration-tests.txt`, `database-rollback.txt` (chỉ commit bản đã loại bỏ dòng lệnh/mật khẩu và stderr không cần thiết). Kết quả ghi nhận ngày 30/10/2026: **10/10 Unit PASS; 8/8 Integration PASS; 4/4 HTTP thủ công PASS**.

## 6. Hạn chế và tiêu chí nghiệm thu

- **PASS trong phạm vi đã kiểm thử:** 18 test tự động, 4 trường hợp HTTP thủ công, rollback để bốn bảng trở lại trạng thái rỗng.
- **Chờ quyết định nghiệp vụ:** lọc danh mục `DA_AN`; đếm sản phẩm `NGUNG_KINH_DOANH`.
- **Chưa chứng minh:** authentication/authorization của API, HTTP E2E tự động, xử lý đồng thời và khả năng tái lập trên môi trường khác.
- Không dùng kết quả schema integration cũ (`DB01`–`DB10` trên Aiven) làm bằng chứng cho SP-07. Hai bộ mã test thuộc phạm vi khác nhau.
