# Kết nối MySQL local và đưa code lên Git

Thông tin kết nối thật được đặt trong User Secrets hoặc biến môi trường. Không điền mật khẩu thật vào appsettings.json, appsettings.Development.json, launchSettings.json hoặc tài liệu PR.

## Máy của Hoài

Ứng dụng hiện dùng MySQL local 127.0.0.1:3306, database cuahang. Các khóa CuaHang, PosMySql và DefaultConnection trong User Secrets cùng trỏ về local. Cấu hình Aiven giữ riêng để dự phòng, không được tự động dùng nếu local lỗi.

Visual Studio: chuột phải project ConvenienceStore.Web → Manage User Secrets. File này nằm ngoài repository tại %APPDATA%/Microsoft/UserSecrets/<UserSecretsId>/secrets.json, không đi theo commit/push.

Program.cs đọc ConnectionStrings:CuaHang và báo rõ nếu thiếu. MySqlPosCatalog hỗ trợ CuaHang khi không cấu hình riêng PosMySql. Nếu dùng cả hai khóa, chúng phải trỏ cùng database để AUTH và POS nhìn cùng dữ liệu.

## Thành viên khác chạy dự án

1. Checkout code và cài .NET/MySQL theo README.
2. Chạy migration chung trên database local theo hướng dẫn nhóm; nạp dữ liệu demo bằng script được duyệt.
3. Mở Manage User Secrets và đặt ConnectionStrings:CuaHang bằng kết nối riêng của máy mình. Khi cần PosMySql, dùng cùng giá trị.
4. Khởi động ứng dụng trong Development. Đăng nhập tài khoản ứng dụng của database đó; tài khoản MySQL không phải tài khoản website.

Ví dụ cấu trúc (thay các placeholder trực tiếp trong User Secrets):

```json
{
  "ConnectionStrings": {
    "CuaHang": "Server=127.0.0.1;Port=3306;Database=YOUR_DATABASE;User ID=YOUR_USER;Password=YOUR_PASSWORD;"
  }
}
```

Có thể cấu hình bằng biến môi trường ConnectionStrings__CuaHang. .NET không tự đọc file .env nếu không có thành phần nạp riêng; mẫu .env.example dành để tham khảo hoặc cho môi trường triển khai sử dụng.

## Kiểm tra trước khi push

```powershell
git status --short
git diff --cached --stat
```

Chỉ stage các file code/tài liệu cần nộp. Không đưa file User Secrets, tệp chứa tài khoản kiểm thử hoặc bản ZIP tự đóng gói từ máy mình vào PR. .gitignore đã bỏ qua secrets.json, appsettings.Local.json, appsettings.*.local.json và .env.*; vẫn giữ .env.example.

Dữ liệu local và User Secrets không tự chia sẻ qua Git. Thành viên khác dùng cùng migration và tự cấu hình kết nối; muốn cùng dữ liệu thì dùng database chung hoặc cùng bộ dữ liệu demo đã được nhóm thống nhất.
