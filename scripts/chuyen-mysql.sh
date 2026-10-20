#!/usr/bin/env bash
# =====================================================================
# Chuyển khung máy chủ từ PostgreSQL sang MySQL 9.7.2 theo HUONG_DAN_MIGRATION.md (ADR-001).
# Làm các bước cần NuGet và CSDL thật: đổi gói, sinh entity (scaffold), tạo migration
# InitialCreate dùng SchemaV1, build, kiểm tra model không lệch CSDL.
#
# Chạy bằng Git Bash ở thư mục gốc repo:
#   export MYSQL_PWD='mat-khau-root'        # bắt buộc
#   bash scripts/chuyen-mysql.sh
#
# Trước khi chạy: trong MySQL Workbench tạo CSDL cuahang_scaffold
# (utf8mb4, utf8mb4_0900_ai_ci) rồi chạy docs/db/schema_v1.sql vào đó. Không chạy dữ liệu mẫu.
# Tùy chọn: MYSQL_HOST (mặc định localhost), MYSQL_PORT (3306), MYSQL_USER (root),
#           CSDL_SCAFFOLD (cuahang_scaffold), CSDL_DEV (để trống = không chạy database update).
# =====================================================================
set -euo pipefail

loi() { echo "LỖI: $*" >&2; exit 1; }
buoc() { echo; echo "==> $*"; }

GOC=$(git rev-parse --show-toplevel 2>/dev/null) || loi "Hãy chạy trong thư mục repo."
WEB="$GOC/src/ConvenienceStore.Web"
[ -f "$WEB/ConvenienceStore.Web.csproj" ] || loi "Không thấy $WEB/ConvenienceStore.Web.csproj"
[ -f "$WEB/Data/Migrations/SchemaV1.cs" ] || loi "Thiếu Data/Migrations/SchemaV1.cs (có sẵn trong nhánh feature/t3-11-chuyen-mysql)."

: "${MYSQL_PWD:?Chưa đặt MYSQL_PWD. Chạy: export MYSQL_PWD='mat-khau-root'}"
MYSQL_HOST=${MYSQL_HOST:-localhost}
MYSQL_PORT=${MYSQL_PORT:-3306}
MYSQL_USER=${MYSQL_USER:-root}
CSDL_SCAFFOLD=${CSDL_SCAFFOLD:-cuahang_scaffold}
CSDL_DEV=${CSDL_DEV:-}
case "$MYSQL_PWD" in *";"*) loi "Mật khẩu có dấu ; sẽ làm hỏng chuỗi kết nối, hãy đổi mật khẩu hoặc tạo tài khoản MySQL khác.";; esac
KET_NOI() { echo "server=$MYSQL_HOST;port=$MYSQL_PORT;database=$1;user=$MYSQL_USER;password=$MYSQL_PWD"; }

buoc "1/7 Kiểm tra công cụ"
command -v dotnet >/dev/null || loi "Chưa có .NET SDK 10 (https://dotnet.microsoft.com/download)."
dotnet --version
if ! dotnet ef --version >/dev/null 2>&1; then
  echo "Chưa có dotnet-ef, đang cài..."
  dotnet tool install --global dotnet-ef
  export PATH="$PATH:$HOME/.dotnet/tools"
fi
dotnet ef --version | tail -1

cd "$WEB"

buoc "2/7 Đổi gói: bỏ Npgsql, thêm MySql.EntityFrameworkCore"
if grep -q "Npgsql.EntityFrameworkCore.PostgreSQL" ConvenienceStore.Web.csproj; then
  dotnet remove package Npgsql.EntityFrameworkCore.PostgreSQL
fi
if ! grep -q '"MySql.EntityFrameworkCore"' ConvenienceStore.Web.csproj; then
  dotnet add package MySql.EntityFrameworkCore
fi
grep -E 'MySql.EntityFrameworkCore|EntityFrameworkCore.Design' ConvenienceStore.Web.csproj
echo "Ghi phiên bản MySql.EntityFrameworkCore ở trên vào ADR-001 mục 2.2."

buoc "3/7 Sinh entity từ CSDL $CSDL_SCAFFOLD"
dotnet ef dbcontext scaffold "$(KET_NOI "$CSDL_SCAFFOLD")" MySql.EntityFrameworkCore \
  --output-dir Data/Entities --context-dir Data --context CuaHangDbContext \
  --no-onconfiguring --force
SO_ENTITY=$(ls Data/Entities/*.cs | wc -l)
echo "Số lớp entity: $SO_ENTITY (28 bảng; nếu có thêm 3 lớp cho view là bình thường)"
[ "$SO_ENTITY" -ge 28 ] || loi "Thiếu entity. Kiểm tra CSDL $CSDL_SCAFFOLD đã chạy đủ schema_v1.sql chưa."
for v in v_gia_ban_hien_hanh v_chuong_trinh_khuyen_mai v_luot_dung_khuyen_mai; do
  if grep -q "\"$v\"" Data/CuaHangDbContext.cs && ! grep -q "ToView(\"$v\")" Data/CuaHangDbContext.cs; then
    echo "CẢNH BÁO: view $v được sinh nhưng thiếu .ToView(\"$v\") trong CuaHangDbContext.cs. Sửa tay trước khi commit."
  fi
done

buoc "4/7 Tạo migration InitialCreate và thay phần thân bằng SchemaV1"
if ls Data/Migrations/*_InitialCreate.cs >/dev/null 2>&1; then
  loi "Đã có migration InitialCreate. Xóa các file *_InitialCreate*.cs và CuaHangDbContextModelSnapshot.cs nếu muốn tạo lại."
fi
dotnet ef migrations add InitialCreate --output-dir Data/Migrations
MIG=$(ls Data/Migrations/*_InitialCreate.cs | grep -v Designer)
cat > "$MIG" <<'EOF'
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConvenienceStore.Web.Data.Migrations
{
    /// <summary>
    /// Tạo CSDL giống hệt docs/db/schema_v1.sql: 28 bảng, 3 view, 10 trigger, CHECK và dữ liệu khởi tạo.
    /// Phần thân lấy từ SchemaV1 (HUONG_DAN_MIGRATION.md). Không sửa sau khi đã gộp vào main.
    /// </summary>
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in SchemaV1.Up)
                migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in SchemaV1.Down)
                migrationBuilder.Sql(sql);
        }
    }
}
EOF
echo "Đã thay phần thân: $MIG"

buoc "5/7 Build"
dotnet build "$GOC/ConvenienceStoreManagement.slnx" -nologo -v q

buoc "6/7 Kiểm tra model không lệch CSDL (migration thử phải trống)"
SNAP=Data/Migrations/CuaHangDbContextModelSnapshot.cs
cp "$SNAP" "$SNAP.bak"
dotnet ef migrations add KiemTraKhongLech --output-dir Data/Migrations >/dev/null
THU=$(ls Data/Migrations/*_KiemTraKhongLech.cs | grep -v Designer)
SO_LENH=$(grep -c "migrationBuilder\." "$THU" || true)
rm -f Data/Migrations/*_KiemTraKhongLech.cs Data/Migrations/*_KiemTraKhongLech.Designer.cs
mv "$SNAP.bak" "$SNAP"
if [ "$SO_LENH" -ne 0 ]; then
  loi "Migration thử có $SO_LENH lệnh: model và CSDL đang lệch. Báo #1 trước khi commit."
fi
echo "Đạt: model khớp CSDL."

buoc "7/7 Dựng CSDL phát triển bằng migration"
if [ -n "$CSDL_DEV" ]; then
  dotnet ef database update --connection "$(KET_NOI "$CSDL_DEV")"
  echo "Đã dựng $CSDL_DEV. Chạy docs/db/kiem_thu_v1.sql rồi kiem_tra_rang_buoc.sql trong Workbench: phải đạt 29/29."
else
  echo "Bỏ qua (chưa đặt CSDL_DEV). Muốn dựng thử: tạo CSDL trống cuahang_dev rồi chạy lại với CSDL_DEV=cuahang_dev."
fi

echo
echo "Xong. Kiểm tra rồi commit:"
echo "  git add src/ConvenienceStore.Web && git commit -m \"feat(db): scaffold entity MySQL va migration InitialCreate (SchemaV1)\""
