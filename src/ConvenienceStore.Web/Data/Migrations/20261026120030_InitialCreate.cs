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
