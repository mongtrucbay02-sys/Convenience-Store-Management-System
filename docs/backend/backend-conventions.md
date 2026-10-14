# Quy ước phát triển Backend

> Tài liệu phục vụ Issue #52 (T3-12). Cập nhật ngày 15/10/2026.
>
> **Trạng thái:** tài liệu dùng để review. Các mục được ghi **PENDING** hoặc **Đề xuất** chưa phải quyết định kiến trúc chính thức và cần Lead/Team phê duyệt trước khi áp dụng bắt buộc.

## 1. Mục đích và phạm vi

Tài liệu này thống nhất cách tổ chức và viết code phía máy chủ cho project ASP.NET Core MVC hiện có tại `src/ConvenienceStore.Web`. Đối tượng sử dụng là toàn bộ thành viên phát triển, reviewer và người duy trì backend.

Mục tiêu:

- giúp code của các thành viên có cấu trúc, cách đặt tên và cách xử lý lỗi nhất quán;
- làm tiêu chí kiểm tra trước khi mở Pull Request (PR) và khi code review;
- giữ ranh giới trách nhiệm giữa Controller, Service, DTO, Entity và tầng truy cập dữ liệu;
- hạn chế lộ bí mật, dữ liệu nhạy cảm và chi tiết lỗi nội bộ;
- phân biệt rõ hiện trạng đã có trên `main` với kiến trúc đang được đề xuất.

### 1.1. Trạng thái đã kiểm chứng

| Phạm vi | Trạng thái ngày 15/10/2026 |
| --- | --- |
| `main` | Có một project ASP.NET Core MVC nhắm `net10.0`, `HomeController`, Razor Views mặc định và route MVC mặc định. Chưa có EF Core, Npgsql, `ApplicationDbContext`, module nghiệp vụ hoặc migration. |
| PR #74 / nhánh `feature/t3-11-backend-foundation` | Đang đề xuất nền tảng module-based MVC, EF Core/Npgsql, một `ApplicationDbContext`, Products Area, Service qua DI và cấu hình vị trí View. Nội dung này chưa có trên `main`, vì vậy không được mô tả là đã triển khai chính thức. |
| PR #73 / nhánh `feature/t3-09-product-purchase-erd` | Đang đề xuất ERD/từ điển dữ liệu Products/Purchasing. Tài liệu nhánh này dùng thuật ngữ và kiểu dữ liệu MySQL; chưa phải schema PostgreSQL chính thức. |
| Issue #4 | Chưa kiểm chứng được nội dung chi tiết từ workspace hoặc truy cập GitHub công khai. Mọi quy ước endpoint và HTTP response trong tài liệu này được đánh dấu `PENDING – Issue #4`. |

Khi một PR liên quan được merge hoặc Lead chốt quyết định mới, tài liệu này phải được cập nhật trong một thay đổi riêng. Không suy diễn một đề xuất đang review thành tiêu chuẩn đã duyệt.

## 2. Công nghệ và kiến trúc

### 2.1. Nền tảng

- ASP.NET Core MVC trên .NET 10.
- Razor Views cho giao diện render phía máy chủ.
- EF Core 10 Code First với provider Npgsql cho PostgreSQL/Supabase PostgreSQL là hướng tích hợp dữ liệu của backend foundation.
- Modular Monolith theo module nghiệp vụ là hướng kiến trúc đang đề xuất.

Issue #52 chỉ tạo tài liệu quy ước. Không tạo project, cài package, sửa `Program.cs`, sửa `.csproj`, tạo Entity/DbContext, sinh migration hoặc thay đổi database.

### 2.2. Nguyên tắc kiến trúc đề xuất

Mỗi module gom các thành phần của cùng một năng lực nghiệp vụ, ví dụ Products hoặc Purchasing. Các module vẫn chạy trong cùng ứng dụng và cùng process; “module” không đồng nghĩa với microservice.

Trong giai đoạn hiện tại, PR #74 đề xuất:

- một web project `ConvenienceStore.Web`;
- một `ApplicationDbContext` dùng chung;
- Controller và View phân vùng bằng MVC Area;
- Service đăng ký qua Dependency Injection (DI);
- dữ liệu và migration quản lý tập trung.

**PENDING – cần Lead phê duyệt kiến trúc module-based.** Cho đến khi PR #74 được duyệt và merge, cấu trúc trên chỉ là định hướng. Code mới không được tự tạo thêm project hoặc tự mở rộng nền tảng ngoài issue đang làm.

## 3. Quy tắc đặt tên

Mã C# tuân theo quy ước .NET: tên kiểu/thành viên public dùng `PascalCase`; parameter, biến cục bộ dùng `camelCase`; private field dùng `_camelCase`. Tên phải diễn đạt ý nghĩa nghiệp vụ, tránh viết tắt khó hiểu.

| Thành phần | Quy ước | Ví dụ |
| --- | --- | --- |
| Namespace | `PascalCase`, phản ánh project và đường dẫn module | `ConvenienceStore.Web.Modules.Products.Services` |
| Class | Danh từ, `PascalCase` | `ProductService` |
| Interface | Tiền tố `I`, `PascalCase` | `IProductService` |
| Entity | Danh từ số ít, không thêm hậu tố `Entity` nếu không cần phân biệt | `Product`, `PurchaseReceipt` |
| MVC Controller | Tên nghiệp vụ + `Controller` | `ProductsController` |
| API Controller | Tên tài nguyên số nhiều + `Controller`; route chờ Issue #4 | `ProductsController` |
| Service | Tên năng lực + `Service`; interface tương ứng khi có hợp đồng cần thay thế/test | `ProductService`, `IProductService` |
| Method | Động từ hoặc cụm động từ, `PascalCase` | `GetById`, `CreateProduct` |
| Async method | Hậu tố `Async` | `GetByIdAsync`, `SaveChangesAsync` |
| Parameter | `camelCase`, không thêm tiền tố kiểu dữ liệu | `productId`, `cancellationToken` |
| Local variable | `camelCase` | `existingProduct` |
| Private field | `_camelCase`; field DI là `readonly` khi có thể | `_productService` |
| DTO | Mục đích + `Dto`; ưu tiên tên cụ thể | `ProductSummaryDto` |
| Request model | Hành động + `Request` | `CreateProductRequest` |
| Response model | Tài nguyên/mục đích + `Response` | `ProductDetailsResponse` |
| Database table | `snake_case`, danh từ số nhiều | `products`, `purchase_receipts` |
| Database column | `snake_case` | `created_at`, `category_id` |
| Primary key | Cột `id`; constraint `pk_<table>` | `id`, `pk_products` |
| Foreign key | `<entity>_id`; constraint `fk_<table>_<referenced_table>_<column>` | `category_id`, `fk_products_categories_category_id` |
| Unique constraint | `uq_<table>_<column(s)>` | `uq_products_sku` |
| Index | `ix_<table>_<column(s)>` | `ix_products_category_id_status` |
| API endpoint | Danh từ tài nguyên; không đưa động từ CRUD vào URL | `/api/products/{id}` |

Tên bảng, cột và constraint ở trên là quy ước đích cho PostgreSQL; mapping phải được khai báo nhất quán bằng Fluent API. Schema cuối cùng vẫn phải đối chiếu ERD đã duyệt.

**PENDING – Issue #4:** route prefix, versioning, số ít/số nhiều, casing của URL, envelope response, pagination và mã lỗi API chưa được Lead chốt. Ví dụ endpoint trong bảng chỉ minh họa cách đặt tên tài nguyên, không phải hợp đồng API chính thức.

Quy tắc bổ sung:

- dùng một thuật ngữ nhất quán từ UI, Use Case, code đến database;
- không dùng tên chung chung như `Data`, `Info`, `Manager`, `Helper` nếu có thể gọi đúng trách nhiệm;
- không thêm hậu tố `Async` cho method không trả `Task`, `Task<T>` hoặc kiểu awaitable;
- acronym trong tên C# dùng casing dễ đọc, ví dụ `ProductId`, `HttpClient`, `Sku` thay vì `ProductID`, `HTTPClient`, `SKU`;
- không dùng tên khác nhau chỉ để che xung đột giữa module; cần làm rõ ranh giới nghiệp vụ.

## 4. Cấu trúc thư mục module-based

Cấu trúc sau là **đề xuất đang chờ duyệt**, không phải cây thư mục hiện đã tồn tại đầy đủ trên `main`:

```text
src/ConvenienceStore.Web/
├── Controllers/
│   └── HomeController.cs              # MVC hiện có, giữ tương thích
├── Views/                              # View hiện có ngoài Area
│   ├── Home/
│   └── Shared/
├── Data/
│   └── ApplicationDbContext.cs         # Đề xuất trong PR #74
├── Modules/
│   └── <Module>/
│       ├── Controllers/
│       ├── Services/
│       ├── DTOs/
│       ├── Entities/
│       ├── Configurations/
│       └── Views/
│           ├── <Controller>/
│           └── Shared/
└── Shared/
    ├── Exceptions/
    └── Middleware/
```

| Thành phần | Trách nhiệm |
| --- | --- |
| `Modules/<Module>/Controllers` | Nhận HTTP request của module, gọi Service, chọn View hoặc tạo HTTP response. |
| `Modules/<Module>/Services` | Điều phối use case và quy tắc nghiệp vụ; chứa interface/implementation khi cần. |
| `Modules/<Module>/DTOs` | Kiểu dữ liệu đi qua biên HTTP hoặc biên hợp đồng giữa các tầng; không chứa persistence behavior. |
| `Modules/<Module>/Entities` | Mô hình persistence và bất biến cốt lõi của module; không dùng trực tiếp làm request DTO. |
| `Modules/<Module>/Configurations` | Fluent mapping bằng `IEntityTypeConfiguration<T>` cho Entity của module. |
| `Modules/<Module>/Views` | Razor Views của Area/module, sắp theo tên Controller. |
| `Data/ApplicationDbContext.cs` | DbContext dùng chung và điểm tập hợp EF Core configuration trong giai đoạn hiện tại. |
| `Shared/Exceptions` | Exception dùng chung thật sự giữa nhiều module; không biến thành kho chứa mọi exception. |
| `Shared/Middleware` | Middleware xuyên suốt ứng dụng đã được duyệt, ví dụ xử lý lỗi tập trung. |
| `Controllers` và `Views` ở root | Giữ luồng MVC hiện có như `HomeController`; không di chuyển chỉ để khớp cây đề xuất. |

Không tạo thư mục rỗng “để dành”. Chỉ thêm thành phần khi issue có nhu cầu thực tế. Module sở hữu Entity, configuration và logic của mình; `Shared` chỉ chứa phần thật sự dùng chung, ổn định và không thuộc một module cụ thể.

## 5. Controllers, Services và DTOs

### 5.1. Controller

Controller chịu trách nhiệm ở biên HTTP:

- nhận và bind input;
- kiểm tra trạng thái model/validation;
- chuyển dữ liệu sang Service;
- ánh xạ kết quả nghiệp vụ thành View hoặc HTTP result;
- truyền `CancellationToken` xuống các thao tác async phù hợp.

Controller không chứa truy vấn EF Core phức tạp, transaction, tính toán nghiệp vụ dài, hoặc logic có thể tái sử dụng. Action nên ngắn và thể hiện rõ luồng xử lý.

### 5.2. Service

Service triển khai use case, bảo vệ quy tắc nghiệp vụ và điều phối persistence. Dùng interface khi cần tạo hợp đồng giữa các thành phần, thay implementation hoặc cô lập dependency trong test; không tạo interface máy móc cho mọi class không có nhu cầu.

Đăng ký lifetime DI theo trách nhiệm:

- `Scoped`: mặc định cho service sử dụng `DbContext` hoặc dữ liệu theo request;
- `Transient`: service nhẹ, stateless và không cần giữ instance trong request;
- `Singleton`: chỉ khi dependency và state thread-safe, không phụ thuộc service scoped.

Không dùng service locator (`HttpContext.RequestServices.GetService`) thay cho constructor injection.

### 5.3. DTO và Entity

- Request DTO chỉ chứa trường client được phép gửi.
- Response DTO chỉ lộ dữ liệu client được phép xem.
- Không bind HTTP request trực tiếp vào Entity; cách này dễ over-posting và làm rò rỉ cấu trúc database.
- Không trả Entity trực tiếp từ API.
- Validation cú pháp/định dạng đặt gần request model; validation nghiệp vụ cần dữ liệu hoặc trạng thái đặt trong Service.
- Mapping phải rõ ràng. Chỉ thêm thư viện mapping khi team thống nhất và issue cho phép.

### 5.4. Ví dụ C#

Ví dụ dưới đây minh họa ranh giới Controller/Service/DTO; route và định dạng response vẫn là `PENDING – Issue #4`. Ba khối code tương ứng với **ba file riêng**, không phải nội dung để ghép nguyên vào một file. Project hiện bật `ImplicitUsings`; các `using` liên quan đến module vẫn được viết tường minh để làm rõ dependency.

`Modules/Products/DTOs/CreateProductRequest.cs`:

```csharp
namespace ConvenienceStore.Web.Modules.Products.DTOs;

public sealed record CreateProductRequest(string Sku, string Name);
```

`Modules/Products/Services/IProductService.cs`:

```csharp
using ConvenienceStore.Web.Modules.Products.DTOs;

namespace ConvenienceStore.Web.Modules.Products.Services;

public interface IProductService
{
    Task CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken);
}
```

`Modules/Products/Controllers/ProductsController.cs`:

```csharp
using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Modules.Products.Services;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Products.Controllers;

[Area("Products")]
public sealed class ProductsController : Controller
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        await _productService.CreateAsync(request, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Index() => View();
}
```

Ví dụ cố ý không cung cấp `ProductService` implementation, validation attribute, persistence hoặc DI registration vì các chi tiết đó phụ thuộc use case và quyết định nền tảng được phê duyệt. Đây là mẫu phân tách trách nhiệm và namespace, không phải tính năng hoàn chỉnh để sao chép rồi đưa vào production.

`CancellationToken` nên được truyền đến EF Core, HTTP client và thao tác I/O hỗ trợ hủy. Không dùng token như cơ chế rollback nghiệp vụ sau khi transaction đã commit.

### 5.5. Ranh giới module

Một module không truy cập trực tiếp Entity, bảng riêng, repository nội bộ hoặc implementation Service của module khác nếu chưa có hợp đồng được thống nhất. Khi cần tích hợp:

1. xác định module sở hữu dữ liệu và quy tắc;
2. định nghĩa interface/DTO nội bộ tối thiểu ở vị trí được Lead duyệt;
3. gọi qua hợp đồng đó;
4. không trả `IQueryable` qua ranh giới module;
5. làm rõ transaction, concurrency và lỗi có thể xảy ra.

Ranh giới Inventory/Purchasing hiện là **PENDING**; không giả định Purchasing được tự cập nhật bảng tồn kho.

## 6. MVC Areas và Razor Views

### 6.1. Area cho module

Controller trong module dùng `[Area("<Module>")]`. Giá trị Area phải khớp tên module và thư mục View:

```csharp
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Products.Controllers;

[Area("Products")]
public sealed class ProductsController : Controller
{
    public IActionResult Index() => View();
}
```

Với cấu trúc đề xuất, View của action trên đặt tại:

```text
Modules/Products/Views/Products/Index.cshtml
```

View dùng chung riêng cho module đặt tại `Modules/Products/Views/Shared`. View dùng chung toàn ứng dụng tiếp tục đặt tại `Views/Shared`.

### 6.2. Cấu hình vị trí View

PR #74 đang đề xuất thêm hai mẫu vào `AreaViewLocationFormats`:

```csharp
builder.Services.Configure<Microsoft.AspNetCore.Mvc.Razor.RazorViewEngineOptions>(
    options =>
    {
        options.AreaViewLocationFormats.Insert(
            0,
            "/Modules/{2}/Views/{1}/{0}.cshtml");

        options.AreaViewLocationFormats.Insert(
            1,
            "/Modules/{2}/Views/Shared/{0}.cshtml");
    });
```

Trong đó `{2}` là Area, `{1}` là Controller và `{0}` là View. Cấu hình này chưa có trên `main`; chỉ áp dụng sau khi nền tảng được duyệt/merge. Route Area cũng phải được map trước route mặc định.

### 6.3. MVC Controller và API Controller

| MVC Controller | API Controller |
| --- | --- |
| Kế thừa `Controller`. | Thường kế thừa `ControllerBase`. |
| Trả Razor View, redirect hoặc kết quả phục vụ UI MVC. | Trả dữ liệu/HTTP status, không trả Razor View. |
| Dùng model state để render lại form và validation message. | Thường dùng `[ApiController]` và response lỗi theo hợp đồng API. |
| Route có thể theo conventional routing/Area. | Route, version và response contract là `PENDING – Issue #4`. |

Không trộn action render View và action API trong cùng Controller chỉ vì cùng tài nguyên.

`HomeController` và `Views/Home`, `Views/Shared` hiện có trên `main` phải tiếp tục hoạt động với route mặc định. Không di chuyển hoặc đổi route của chúng trong một task module nếu không có yêu cầu riêng và kiểm thử hồi quy.

## 7. Error Handling và Validation

### 7.1. Phân loại lỗi

| Loại | Cách xử lý | HTTP dự kiến cho API |
| --- | --- | --- |
| Input validation | Từ chối sớm; MVC render lại form kèm lỗi; Service không được tin dữ liệu chỉ vì client đã validate. | `400 Bad Request` hoặc validation response — `PENDING – Issue #4`. |
| Lỗi nghiệp vụ | Service trả kết quả có kiểu rõ ràng hoặc ném exception nghiệp vụ đã thống nhất; thông báo cho người dùng không lộ nội bộ. | Phụ thuộc hợp đồng — `PENDING – Issue #4`. |
| Không tìm thấy tài nguyên | Không trả object giả/null mơ hồ; chuyển thành kết quả not-found phù hợp ở biên HTTP. | `404 Not Found` — cấu trúc body `PENDING – Issue #4`. |
| Xung đột trạng thái | Phát hiện concurrency, mã duy nhất trùng hoặc transition không hợp lệ; không ghi đè im lặng. | Thường là `409 Conflict` — `PENDING – Issue #4`. |
| Lỗi hệ thống | Log exception với context an toàn; trả thông báo chung và correlation/request id nếu quy ước cho phép. | `500 Internal Server Error`; body `PENDING – Issue #4`. |

### 7.2. Quy tắc bắt buộc

- Không trả stack trace, inner exception, câu SQL, connection string, đường dẫn máy chủ hoặc thông tin nhạy cảm cho client.
- Không `catch (Exception)` rồi bỏ qua. Chỉ bắt khi có thể xử lý, bổ sung context an toàn, chuyển đổi sang lỗi có nghĩa hoặc đảm bảo cleanup; giữ nguyên stack trace bằng `throw;` khi ném lại.
- Không dùng exception cho luồng bình thường có thể biểu diễn rõ bằng result/validation.
- Không tự chuyển mọi exception thành `200 OK` với cờ thất bại.
- Lỗi validation phải chỉ rõ field khi phù hợp, nhưng không tiết lộ quy tắc bảo mật nội bộ.
- Thông báo hiển thị cho người dùng và log kỹ thuật là hai nội dung khác nhau.
- Một middleware/exception handler dùng chung chỉ được triển khai trong task riêng sau khi Lead chốt error contract. Issue #52 không triển khai middleware.

Định dạng HTTP error response, error code, correlation id và mapping exception-to-status phải đồng bộ với **Issue #4** trước khi trở thành chuẩn bắt buộc.

## 8. Logging

Dùng `ILogger<T>` qua constructor injection và structured logging. Message template phải ổn định; dữ liệu thay đổi truyền bằng placeholder, không nội suy chuỗi.

### 8.1. Mức log

| Mức | Khi sử dụng |
| --- | --- |
| `Debug` | Chi tiết phục vụ chẩn đoán khi phát triển; không phụ thuộc mức này cho audit nghiệp vụ. |
| `Information` | Sự kiện vận hành bình thường có giá trị, ví dụ hoàn tất một use case quan trọng. Tránh log mọi dòng/mọi request trùng với access log. |
| `Warning` | Tình huống bất thường nhưng request/hệ thống còn xử lý được, ví dụ xung đột dự kiến hoặc retry. |
| `Error` | Thao tác thất bại do exception/lỗi cần điều tra nhưng ứng dụng vẫn có thể tiếp tục phục vụ. |
| `Critical` | Sự cố nghiêm trọng đe dọa khả năng hoạt động hoặc tính toàn vẹn toàn hệ thống. |

Không ghi vào log:

- password;
- access token hoặc refresh token;
- API key/secret;
- connection string đầy đủ;
- cookie/session id có thể sử dụng để chiếm phiên;
- dữ liệu cá nhân nhạy cảm không cần thiết.

Chỉ log định danh tối thiểu cần điều tra. Với exception, truyền exception vào overload của logger để giữ stack trace phía server:

```csharp
public sealed class ProductService
{
    private readonly ILogger<ProductService> _logger;

    public ProductService(ILogger<ProductService> logger)
    {
        _logger = logger;
    }

    public void LogProductCreated(long productId, string sku)
    {
        _logger.LogInformation(
            "Created product {ProductId} with SKU {Sku}",
            productId,
            sku);
    }
}
```

Không viết `LogInformation($"Created product {productId}")` vì làm mất lợi ích structured logging. Event name, correlation id, retention, masking và mức log chung vẫn cần Team chốt.

## 9. EF Core Code First và Database

### 9.1. Trạng thái và tổ chức

`main` chưa có EF Core hoặc DbContext. PR #74 đang đề xuất EF Core 10, Npgsql và một `Data/ApplicationDbContext.cs`. Nếu đề xuất được duyệt, giai đoạn hiện tại dùng một `ApplicationDbContext` chung; Entity và mapping vẫn nằm theo module để giữ quyền sở hữu rõ ràng.

Ví dụ mapping đề xuất:

```csharp
using ConvenienceStore.Web.Modules.Products.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConvenienceStore.Web.Modules.Products.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(product => product.Id)
            .HasName("pk_products");

        builder.Property(product => product.Sku)
            .HasColumnName("sku")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(product => product.Sku)
            .IsUnique()
            .HasDatabaseName("uq_products_sku");
    }
}
```

`Product` trong ví dụ là Entity minh họa dự kiến thuộc namespace `ConvenienceStore.Web.Modules.Products.Entities`. Entity này chưa tồn tại trên `main`; đoạn mapping sẽ chỉ biên dịch khi task triển khai đã tạo `Product` với tối thiểu các property `Id` và `Sku`, đồng thời schema được phê duyệt. Ví dụ không xác nhận cấu trúc Entity hoặc schema Products là quyết định chính thức.

Quy tắc:

- ưu tiên Fluent API thay vì dàn trải mapping bằng data annotation;
- mỗi Entity có `IEntityTypeConfiguration<T>` riêng khi mapping không còn tầm thường;
- `ApplicationDbContext.OnModelCreating` áp dụng configuration theo cơ chế được Team thống nhất;
- không đặt logic HTTP hoặc gọi Service trong Entity configuration;
- cấu hình độ dài, nullability, precision, relationship, delete behavior, index và constraint một cách tường minh theo ERD đã duyệt;
- thao tác async dùng API EF Core async và truyền `CancellationToken` khi phù hợp;
- không dùng lazy loading nếu chưa có quyết định và lý do rõ ràng;
- tránh N+1; chỉ tải cột/quan hệ cần thiết cho use case.

### 9.2. Migration và thay đổi schema

- Migration được quản lý tập trung cho `ApplicationDbContext`, trong vị trí/quy trình Team phê duyệt.
- Mỗi migration phải nhỏ, có tên diễn đạt thay đổi và đi cùng thay đổi Entity/mapping tương ứng.
- Reviewer phải đọc cả migration code và SQL dự kiến cho PostgreSQL.
- Không tự chạy migration lên Supabase hoặc database dùng chung. Chỉ người/quy trình được phân quyền mới được áp dụng sau khi review và có phương án backup/rollback phù hợp.
- Không sửa hoặc xóa migration đã được áp dụng trên môi trường dùng chung; tạo migration tiếp theo để điều chỉnh.
- Không trộn thay đổi schema ngoài scope issue.
- Trước khi đổi schema phải đối chiếu ERD đã duyệt và thống nhất với người quản lý schema/module liên quan.

PR #73 mô tả ERD nháp bằng kiểu MySQL. Không sao chép trực tiếp `BIGINT UNSIGNED`, `DATETIME(6)`, `AUTO_INCREMENT`, generated-column syntax, collation hoặc delete/update behavior của MySQL sang PostgreSQL. Phải chọn kiểu và constraint tương đương theo semantics PostgreSQL/Npgsql, đồng thời chờ ERD được phê duyệt.

**PENDING:** quy trình tạo, đặt tên, review, áp dụng và rollback migration; quyền áp dụng lên từng môi trường; schema PostgreSQL chính thức cho Products/Purchasing.

## 10. Configuration và Secrets

Phân tách cấu hình theo môi trường và không lưu bí mật trong Git.

- Development: dùng .NET User Secrets cho giá trị nhạy cảm cục bộ.
- Deployment: dùng environment variables hoặc secret manager của nền tảng triển khai.
- Chỉ commit cấu hình mẫu/không nhạy cảm và tên key cần thiết.
- `UserSecretsId` là metadata định danh, có thể commit; file/value secrets không được commit.
- Không đặt password thật, access token, refresh token, API key hoặc connection string thật trong README, `appsettings*.json`, source code, test fixture, ảnh chụp hoặc comment.
- Không in secret ra terminal, log, CI output hoặc báo cáo review.
- Nếu nghi ngờ secret đã bị commit, dừng sử dụng, báo Lead và rotate/revoke; chỉ xóa khỏi commit mới là chưa đủ.

Ví dụ chỉ mô tả tên key, không chứa giá trị thật:

```text
ConnectionStrings__DefaultConnection
```

Thành viên tự cấu hình secret trên máy của mình theo kênh nội bộ đã được phê duyệt. Không gửi secret qua PR, issue hoặc chat công khai.

## 11. Code Review Checklist

Trước khi mở PR, tác giả tự kiểm tra:

- [ ] `dotnet build` thành công, không bỏ qua warning mới có liên quan đến thay đổi.
- [ ] Đang làm đúng branch của issue và base branch theo quy trình nhóm.
- [ ] PR chỉ chứa scope của issue; không trộn refactor, format hoặc tính năng không liên quan.
- [ ] Không chứa password, token, API key, connection string thật hoặc dữ liệu cá nhân nhạy cảm.
- [ ] Tên namespace, class, interface, method, biến, DTO và database object tuân theo quy ước.
- [ ] Controller chỉ xử lý biên HTTP; nghiệp vụ nằm ở Service; request/response dùng DTO phù hợp.
- [ ] Entity không được bind/trả trực tiếp qua HTTP.
- [ ] Dependency được cung cấp qua DI với lifetime phù hợp.
- [ ] Async method có hậu tố `Async`; `CancellationToken` được truyền đến I/O phù hợp.
- [ ] Validation input và validation nghiệp vụ được đặt đúng tầng.
- [ ] Các trường hợp not found, conflict, business error và system error được xử lý rõ ràng.
- [ ] Logging dùng `ILogger<T>`, placeholder có cấu trúc, đúng log level và không lộ dữ liệu nhạy cảm.
- [ ] Module không truy cập dữ liệu riêng của module khác khi chưa có hợp đồng.
- [ ] Query EF Core tránh N+1 và chỉ lấy dữ liệu cần thiết.
- [ ] Không thêm/sửa migration hoặc schema ngoài scope; không tự áp dụng migration lên database dùng chung.
- [ ] Nếu có thay đổi schema, đã đối chiếu ERD và người quản lý schema, đồng thời review SQL PostgreSQL dự kiến.
- [ ] Razor View nằm đúng vị trí và route Area không làm hỏng `HomeController`/Views hiện có.
- [ ] API endpoint/response không tự đặt thành chuẩn khi Issue #4 chưa chốt.
- [ ] Có test phù hợp hoặc mô tả rõ đã kiểm thử gì, dữ liệu/điều kiện nào và kết quả ra sao.
- [ ] Tài liệu liên quan được cập nhật khi thay đổi quyết định đã duyệt.

Reviewer kiểm tra dựa trên code và bằng chứng test, không chỉ dựa vào checklist đã đánh dấu.

## 12. Pending Decisions

Các nội dung sau phải được Lead/Team chốt và ghi lại trước khi coi là quy ước bắt buộc:

1. **Kiến trúc module-based:** duyệt cấu trúc trong PR #74; quy tắc tạo module mới; vị trí interface dùng giữa module; có tiếp tục một web project và một DbContext hay không.
2. **Endpoint và response:** route prefix, API versioning, resource naming, pagination, success/error envelope, error code và correlation id theo Issue #4.
3. **Ranh giới Inventory/Purchasing:** module sở hữu tồn kho, hợp đồng ghi nhận nhập hàng, transaction boundary, idempotency, concurrency và quyền đọc dữ liệu giữa module.
4. **Quy trình migration:** người tạo/review/áp dụng, nơi lưu migration, naming, môi trường được phép chạy, backup, rollback và xử lý migration đã deploy.
5. **Error handling chung:** kiểu business result/exception, exception mapping, validation response và middleware/handler dùng chung.
6. **Logging chung:** event naming, correlation, log level theo sự kiện, masking dữ liệu, audit log, retention và quyền truy cập log.
7. **Schema Products/Purchasing:** chuyển ERD đề xuất trong PR #73 từ semantics MySQL sang PostgreSQL/Npgsql và phê duyệt datatype, constraint, index, relationship.

Cho đến khi có quyết định, PR liên quan phải ghi rõ giả định, tránh khóa cứng hợp đồng chưa duyệt và gắn tham chiếu issue/decision tương ứng.
