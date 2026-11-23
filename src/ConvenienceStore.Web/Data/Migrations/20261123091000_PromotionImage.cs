using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ConvenienceStore.Web.Data.Migrations;

[DbContext(typeof(CuaHangDbContext))]
[Migration("20261123091000_PromotionImage")]
public sealed class PromotionImage : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<string>(name: "anh_url", table: "chuong_trinh_khuyen_mai", type: "varchar(2048)", maxLength: 2048, nullable: true);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn(name: "anh_url", table: "chuong_trinh_khuyen_mai");
}
