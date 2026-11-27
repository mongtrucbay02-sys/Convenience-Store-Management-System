using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ConvenienceStore.Web.Data.Migrations;

[DbContext(typeof(CuaHangDbContext))]
[Migration("20261127030000_ExchangePlanSnapshots")]
public sealed class ExchangePlanSnapshots : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<string>(name: "phuong_an_doi", schema: "smartmart",
            table: "phieu_doi_tra", type: "jsonb", nullable: true);

    // A rollback must not silently discard approval history.
    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("Restore a reviewed backup; exchange approval snapshots must be preserved.");
}
