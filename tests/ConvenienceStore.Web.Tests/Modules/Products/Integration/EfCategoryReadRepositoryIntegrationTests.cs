
using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Identity.Entities;
using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Modules.Products.Entities;
using ConvenienceStore.Web.Modules.Products.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ConvenienceStore.Web.Tests.Modules.Products.Integration;

public sealed class EfCategoryReadRepositoryIntegrationTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var connectionString =
            Environment.GetEnvironmentVariable("SP07_TEST_CONNECTION")
            ?? throw new InvalidOperationException(
                "SP07_TEST_CONNECTION is required.");

        var builder = new System.Data.Common.DbConnectionStringBuilder
        {
            ConnectionString = connectionString
        };

        var server = builder["Server"]?.ToString();
        var port = builder["Port"]?.ToString();
        var database = builder["Database"]?.ToString();
        var user = builder["User"]?.ToString();

        if (server != "127.0.0.1" ||
            port != "3307" ||
            database != "sp07_repository_tests" ||
            user != "sp07_test")
        {
            throw new InvalidOperationException(
                "Unsafe test database target.");
        }

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseMySQL(connectionString)
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task InTransactionAsync(
        Func<ApplicationDbContext, Task> test)
    {
        await using var db = CreateDbContext();

        // Every test uses the same DbContext and transaction.
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        try
        {
            await test(db);
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    private static CategoryListQuery Query(
        int page = 1,
        int size = 20) =>
        new() { Page = page, Size = size };

    private static async Task<DanhMuc> AddCategoryAsync(
        ApplicationDbContext db,
        string name,
        string status = "HOAT_DONG")
    {
        var category = new DanhMuc
        {
            Ten = name,
            TrangThai = status,
            KhongDoiTra = false
        };

        db.DanhMucs.Add(category);
        await db.SaveChangesAsync();
        return category;
    }

    private static async Task<TaiKhoan> AddTestUserAsync(
        ApplicationDbContext db)
    {
        var suffix = Guid.NewGuid().ToString("N")[..10];

        var role = new VaiTro
        {
            Id = 3,
            Ma = "NK",
            Ten = "Nhan kho test"
        };

        db.VaiTros.Add(role);
        await db.SaveChangesAsync();

        var user = new TaiKhoan
        {
            TenDangNhap = "sp07_" + suffix,
            MatKhauBam = "integration-test-only",
            HoTen = "SP07 Test User",
            VaiTroId = role.Id
        };

        db.TaiKhoans.Add(user);
        await db.SaveChangesAsync();

        return user;
    }

    private static async Task AddProductAsync(
        ApplicationDbContext db,
        DanhMuc category,
        TaiKhoan user,
        string status = "DANG_KINH_DOANH")
    {
        var suffix = Guid.NewGuid().ToString("N")[..12];

        db.SanPhams.Add(new SanPham
        {
            Ma = "T" + suffix,
            TenSanPham = "SP07 test product " + suffix,
            DanhMucId = category.Id,
            DonViTinh = "Cai",
            NguoiTaoId = user.Id,
            TrangThai = status
        });

        await db.SaveChangesAsync();
    }

    // SP07-DB-01: Empty category list.
    [Fact]
    public async Task DB01_EmptyDatabase_ReturnsEmptyPage()
    {
        await InTransactionAsync(async db =>
        {
            Assert.Equal(0, await db.DanhMucs.CountAsync());

            var repository = new EfCategoryReadRepository(db);
            var result =
                await repository.GetCategoriesAsync(Query());

            Assert.Empty(result.Items);
            Assert.Equal(0, result.Total);
            Assert.Equal(1, result.Page);
            Assert.Equal(20, result.Size);
        });
    }

    // SP07-DB-02: Three categories.
    [Fact]
    public async Task DB02_ThreeCategories_ReturnsTotalThree()
    {
        await InTransactionAsync(async db =>
        {
            await AddCategoryAsync(db, "SP07 DB02 A");
            await AddCategoryAsync(db, "SP07 DB02 B");
            await AddCategoryAsync(db, "SP07 DB02 C");

            var repository = new EfCategoryReadRepository(db);
            var result =
                await repository.GetCategoriesAsync(Query());

            Assert.Equal(3, result.Total);
            Assert.Equal(3, result.Items.Count);
        });
    }

    // SP07-DB-03: Second page contains one category.
    [Fact]
    public async Task DB03_PageTwo_ReturnsLastCategory()
    {
        await InTransactionAsync(async db =>
        {
            await AddCategoryAsync(db, "SP07 DB03 A");
            await AddCategoryAsync(db, "SP07 DB03 B");
            var last =
                await AddCategoryAsync(db, "SP07 DB03 C");

            var repository = new EfCategoryReadRepository(db);
            var result =
                await repository.GetCategoriesAsync(
                    Query(page: 2, size: 2));

            Assert.Equal(3, result.Total);
            Assert.Single(result.Items);
            Assert.Equal(last.Id, result.Items[0].Id);
            Assert.Equal(2, result.Page);
            Assert.Equal(2, result.Size);
        });
    }

    // SP07-DB-04: Count two products.
    [Fact]
    public async Task DB04_CategoryWithTwoProducts_CountsTwo()
    {
        await InTransactionAsync(async db =>
        {
            var category =
                await AddCategoryAsync(db, "SP07 DB04");
            var user = await AddTestUserAsync(db);

            await AddProductAsync(db, category, user);
            await AddProductAsync(db, category, user);

            var repository = new EfCategoryReadRepository(db);
            var result =
                await repository.GetCategoriesAsync(Query());

            var item = Assert.Single(result.Items);
            Assert.Equal(category.Id, item.Id);
            Assert.Equal(2, item.SoSanPham);
        });
    }

    // SP07-DB-05: Category without products.
    [Fact]
    public async Task DB05_CategoryWithoutProducts_CountsZero()
    {
        await InTransactionAsync(async db =>
        {
            await AddCategoryAsync(db, "SP07 DB05");

            var repository = new EfCategoryReadRepository(db);
            var result =
                await repository.GetCategoriesAsync(Query());

            var item = Assert.Single(result.Items);
            Assert.Equal(0, item.SoSanPham);
        });
    }

    // SP07-DB-06: Page size capped at 100.
    [Fact]
    public async Task DB06_Size150_IsNormalizedTo100()
    {
        await InTransactionAsync(async db =>
        {
            var repository = new EfCategoryReadRepository(db);
            var result =
                await repository.GetCategoriesAsync(
                    Query(size: 150));

            Assert.Equal(100, result.Size);
        });
    }

    // SP07-DB-07: Verify current hidden-category behavior.
    [Fact]
    public async Task DB07_HiddenCategory_IsCurrentlyIncluded()
    {
        await InTransactionAsync(async db =>
        {
            var category = await AddCategoryAsync(
                db, "SP07 DB07", "DA_AN");

            var repository = new EfCategoryReadRepository(db);
            var result =
                await repository.GetCategoriesAsync(Query());

            var item = Assert.Single(result.Items);
            Assert.Equal(category.Id, item.Id);
            Assert.Equal("DA_AN", item.TrangThai);
        });
    }

    // SP07-DB-08: Verify current stopped-product behavior.
    [Fact]
    public async Task DB08_StoppedProduct_IsCurrentlyCounted()
    {
        await InTransactionAsync(async db =>
        {
            var category =
                await AddCategoryAsync(db, "SP07 DB08");
            var user = await AddTestUserAsync(db);

            await AddProductAsync(
                db, category, user, "NGUNG_KINH_DOANH");

            var repository = new EfCategoryReadRepository(db);
            var result =
                await repository.GetCategoriesAsync(Query());

            var item = Assert.Single(result.Items);
            Assert.Equal(1, item.SoSanPham);
        });
    }
}
