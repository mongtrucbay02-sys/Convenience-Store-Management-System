using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ConvenienceStore.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class PostgreSqlBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            using var stream=typeof(CuaHangDbContext).Assembly.GetManifestResourceStream("SmartMart.PostgreSqlBaseline.sql")
                ?? throw new InvalidOperationException("Missing PostgreSQL baseline resource.");
            using var reader=new StreamReader(stream);
            migrationBuilder.Sql(reader.ReadToEnd());
        }
        protected override void Down(MigrationBuilder migrationBuilder) =>
            throw new NotSupportedException("Baseline rollback is intentionally disabled. Restore a reviewed backup instead.");
    }
}
