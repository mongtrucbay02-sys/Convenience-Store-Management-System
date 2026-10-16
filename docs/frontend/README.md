[README.md](https://github.com/user-attachments/files/33535000/README.md)
# Quy ước code phía giao diện – Convenience Store Management System

**Mã công việc:** T3-16 – Tuần 3  
**Phụ trách:** Developer #3 – Nguyễn Thị Thu Hoài (Bán hàng)  
**Trạng thái:** Đề xuất để nhóm review, chưa phải quy ước đã phê duyệt.  
**Công nghệ dự kiến:** ASP.NET Core MVC, Razor Views, CSS, JavaScript. Cần đối chiếu với cấu trúc hiện có trong `src/ConvenienceStore.Web` trước khi áp dụng.

## 1. Mục tiêu và phạm vi

- Các thành viên sử dụng cùng quy tắc đặt tên, bố cục và thành phần giao diện.
- Tránh viết trùng layout, thông báo lỗi, bảng dữ liệu và mã gọi API.
- Không tự ý đổi tên đường dẫn API, DTO, trường dữ liệu hoặc quyền truy cập đã được nhóm chốt.
- Tài liệu này chỉ định nghĩa quy ước frontend; không thay thế OpenAPI hay thiết kế ERD.

## 2. Cấu trúc thư mục đề xuất

> Không tạo lại project hoặc di chuyển file đang có khi chưa thống nhất với nhóm. Chỉ bổ sung phần phù hợp vào project hiện tại.

```text
src/ConvenienceStore.Web/
├── Controllers/
│   ├── PosController.cs
│   └── ReturnsController.cs
├── Models/
│   └── ViewModels/
│       ├── Pos/
│       └── Returns/
├── Views/
│   ├── Shared/
│   │   ├── _Layout.cshtml
│   │   ├── _Header.cshtml
│   │   ├── _Sidebar.cshtml
│   │   ├── _Alert.cshtml
│   │   └── _ValidationScriptsPartial.cshtml
│   ├── Pos/
│   │   └── Index.cshtml
│   └── Returns/
│       └── Index.cshtml
└── wwwroot/
    ├── css/
    │   ├── site.css
    │   └── pages/
    │       ├── pos.css
    │       └── returns.css
    └── js/
        ├── shared/
        │   ├── api-client.js
        │   └── ui-feedback.js
        └── pages/
            ├── pos.js
            └── returns.js
```

## 3. Quy tắc đặt tên

| Thành phần | Quy tắc | Ví dụ |
|---|---|---|
| Controller C# | PascalCase + `Controller` | `PosController.cs` |
| Class/ViewModel C# | PascalCase | `CartItemViewModel.cs` |
| Action C# | PascalCase | `Index`, `Details`, `Checkout` |
| Thư mục View | Trùng tên Controller, bỏ `Controller` | `Views/Pos/` |
| Razor View | PascalCase | `Index.cshtml`, `Details.cshtml` |
| Partial View | Bắt đầu bằng `_` | `_Sidebar.cshtml` |
| JavaScript/CSS | kebab-case, chữ thường | `api-client.js`, `pos.css` |
| Hàm/biến JavaScript | camelCase | `loadProducts`, `cartItems` |
| Hằng số JavaScript | UPPER_SNAKE_CASE | `DEFAULT_PAGE_SIZE` |
| CSS class | kebab-case; ưu tiên tiền tố module | `.pos-cart-item`, `.returns-table` |
| ID HTML | kebab-case, duy nhất trong trang | `pos-search-input` |
| URL route | Theo routing đã thống nhất | `/Pos/Index` (minh họa) |

**Không dùng:** tên file có dấu tiếng Việt, dấu cách, tên chung chung như `new.js`, `test2.css`; không sửa tên endpoint chỉ để hợp với giao diện.

## 4. Razor, layout và component dùng chung

1. Trang Razor sử dụng layout chung `Views/Shared/_Layout.cshtml` nếu nhóm đã chọn layout này.
2. Header, sidebar, breadcrumb, alert và modal dùng partial/view component chung; không sao chép mã HTML giữa các module.
3. Các partial nhận dữ liệu qua model rõ ràng; tránh phụ thuộc tùy tiện vào `ViewBag`.
4. Component dùng chung không chứa logic nghiệp vụ POS/DT. Logic riêng nằm ở ViewModel, Controller, service hoặc file JS của module.
5. Không tự tạo component chung trùng với component mà thành viên khác đang xây dựng. Thống nhất người sở hữu, interface và tên trước khi sửa `Views/Shared`.
6. Giao diện phải có trạng thái đang tải, rỗng, thành công, lỗi; có nhãn form, thông báo validation và hỗ trợ thao tác bàn phím cơ bản.

**Điểm cần phối hợp với #6:** xác nhận các trường dữ liệu/DTO dùng chung theo ERD hợp nhất (ví dụ mã sản phẩm, số lượng tồn, mã hóa đơn, trạng thái đổi trả). Nếu #6 không phụ trách UI shared components, cần xác định lại đúng thành viên sở hữu layout/component trước khi tích hợp.

## 5. Quy ước gọi API

- Ưu tiên sử dụng một lớp/hàm gọi API dùng chung, không lặp lại `fetch` và logic xử lý lỗi ở từng màn hình.
- Đường dẫn, phương thức, body và response phải khớp hợp đồng OpenAPI đã được nhóm duyệt. Không tự suy đoán endpoint.
- Dùng `Content-Type: application/json` khi gửi JSON; xử lý `204 No Content` không gọi `response.json()`.
- Không lưu access token hoặc dữ liệu nhạy cảm trong mã nguồn. Cách xác thực (cookie hay bearer) phải theo module AUTH #5.
- Với ứng dụng dùng cookie, các request thay đổi dữ liệu phải tuân theo cơ chế chống CSRF/antiforgery của backend.
- Giao diện không được tự tính giá cuối cùng hay xác nhận tồn kho như nguồn dữ liệu tin cậy; backend phải kiểm tra lại.
- Tránh gửi trùng yêu cầu thanh toán/hoàn tất đơn: khóa nút trong lúc xử lý và phối hợp backend về idempotency nếu cần.

### Ví dụ hàm gọi API dùng chung (minh họa, chưa tích hợp)

```javascript
async function apiRequest(url, options = {}) {
  const headers = new Headers(options.headers || {});
  if (options.body && !(options.body instanceof FormData)) {
    headers.set('Content-Type', 'application/json');
  }

  let response;
  try {
    response = await fetch(url, {
      ...options,
      headers,
      credentials: 'same-origin'
    });
  } catch {
    throw new Error('Không thể kết nối đến máy chủ. Vui lòng thử lại.');
  }

  const contentType = response.headers.get('content-type') || '';
  const data = response.status === 204
    ? null
    : contentType.includes('application/json')
      ? await response.json()
      : await response.text();

  if (!response.ok) {
    const message = data && typeof data === 'object'
      ? (data.detail || data.message || data.title)
      : null;
    const error = new Error(message || `Yêu cầu thất bại (${response.status}).`);
    error.status = response.status;
    error.details = data;
    throw error;
  }
  return data;
}
```

> Ví dụ dùng `fetch` trực tiếp cho minh họa. Nếu nhóm quyết định giao diện MVC gọi service nội bộ thay vì REST API, cần cập nhật phần này theo kiến trúc đã chốt.

## 6. Quy ước xử lý lỗi giao diện

| Trường hợp | Cách hiển thị đề xuất |
|---|---|
| 400 – Request sai | Hiển thị thông báo và hướng dẫn sửa |
| 401 – Chưa đăng nhập/hết phiên | Chuyển đến luồng đăng nhập do AUTH quy định; không tự đoán URL |
| 403 – Không đủ quyền | Hiển thị “Bạn không có quyền thực hiện thao tác này” |
| 404 – Không tìm thấy | Hiển thị trạng thái không có dữ liệu hoặc thông báo phù hợp |
| 409 – Xung đột | Tải lại dữ liệu và thông báo có thay đổi từ người dùng khác |
| 422 – Validation | Hiển thị lỗi cạnh trường nhập tương ứng nếu API cung cấp chi tiết |
| 500/503 – Lỗi hệ thống | Thông báo chung, cho phép thử lại thao tác an toàn |
| Lỗi mạng | Thông báo mất kết nối; không giả định giao dịch đã thất bại hay thành công |

**Đặc biệt với POS-05/POS-07 và DT-04:** nếu mất kết nối sau khi gửi yêu cầu, phải tra cứu trạng thái giao dịch trước khi thử lại để tránh thanh toán hoặc hoàn tiền hai lần.

Không hiển thị stack trace, SQL error hoặc dữ liệu nhạy cảm cho người dùng. Log kỹ thuật theo cơ chế logging của nhóm.

## 7. Quy ước theo module

**POS:** các màn hình bán hàng chỉ quản lý trạng thái giỏ tạm thời; tính giá, khuyến mãi, kiểm tra tồn và ghi hóa đơn phải do backend xác nhận. POS-03 và POS-04 phải dùng hợp đồng thống nhất với thành viên #5.

**DT:** bắt buộc tham chiếu hóa đơn gốc, kiểm tra số lượng đã trả và chính sách đổi trả ở backend. Giao diện chỉ hiển thị kết quả xác thực; không tự duyệt ngoại lệ DT-06.

## 8. Phối hợp giữa các thành viên

- **#3 (POS/DT):** cung cấp yêu cầu màn hình, cách gọi API, trạng thái lỗi và các component cần dùng.
- **#5 (AUTH):** xác nhận quyền truy cập, thông tin phiên đăng nhập, cấu trúc dữ liệu khách hàng/khuyến mãi nếu đúng phạm vi module #5, và hợp đồng POS-03/POS-04.
- **#6 (ERD hợp nhất):** xác nhận tên trường và quan hệ dữ liệu liên quan sản phẩm, hóa đơn, đổi trả. Không tự suy ra #6 sở hữu toàn bộ component giao diện.
- **Người phụ trách khung UI:** xác nhận layout, sidebar, điều hướng, component dùng chung và cách phân quyền menu.

Các điểm cần chốt trước khi merge: cấu trúc thư mục thực tế, tên layout, kiểu xác thực, định dạng lỗi API, vị trí API client, quy ước route, chủ sở hữu shared components.

## 9. Checklist trước khi tạo Pull Request

- [ ] Tạo nhánh riêng, không commit trực tiếp vào `main`/`develop`.
- [ ] Không sửa file ngoài phạm vi nếu chưa trao đổi với người phụ trách.
- [ ] Tên file, class, biến và CSS tuân thủ quy ước.
- [ ] Dùng layout/component chung, không tạo trùng.
- [ ] Request/response khớp OpenAPI đã thống nhất.
- [ ] Có trạng thái loading/empty/error và validation.
- [ ] Không đưa mật khẩu, token, connection string vào Git.
- [ ] Kiểm tra giao diện trên màn hình nhỏ và màn hình máy tính.
- [ ] Chạy build/test phù hợp trước khi yêu cầu review.
- [ ] Liên kết Pull Request với Issue T3-16.

## 10. Cách cập nhật tài liệu

Khi nhóm thống nhất một quy ước mới, sửa README trong Pull Request, mô tả lý do và thông báo cho các module liên quan. Nếu quy ước xung đột với mã nguồn đang có, ưu tiên thảo luận trước khi đổi hàng loạt.
