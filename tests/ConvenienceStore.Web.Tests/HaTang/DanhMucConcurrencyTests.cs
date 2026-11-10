using ConvenienceStore.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ConvenienceStore.Web.Tests.HaTang;

[CollectionDefinition(Ten)]
public sealed class DanhMucConcurrencyCollection : ICollectionFixture<CsdlMySqlTam>
{
    public const string Ten = "DanhMucConcurrency";
}

[Collection(DanhMucConcurrencyCollection.Ten)]
public sealed class DanhMucConcurrencyTests(CsdlMySqlTam csdl) : IAsyncLifetime
{
    private uint _danhMucId;

    public async Task InitializeAsync()
    {
        await using var db = csdl.TaoDbContext();
        var danhMuc = new DanhMuc
        {
            Ten = $"Concurrency-{Guid.NewGuid():N}",
            TrangThai = "HOAT_DONG"
        };
        db.DanhMucs.Add(danhMuc);
        await db.SaveChangesAsync();
        _danhMucId = danhMuc.Id;
    }

    public async Task DisposeAsync()
    {
        await using var db = csdl.TaoDbContext();
        var danhMuc = await db.DanhMucs.FindAsync(_danhMucId);
        if (danhMuc is not null)
        {
            db.DanhMucs.Remove(danhMuc);
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Added_UsesInitialDatabaseVersion()
    {
        await using var db = csdl.TaoDbContext();
        var danhMuc = await db.DanhMucs.SingleAsync(x => x.Id == _danhMucId);
        Assert.Equal(1u, danhMuc.PhienBan);
    }

    [Fact]
    public void UpdateSync_IncrementsVersionExactlyOnce()
    {
        using var db = csdl.TaoDbContext();
        var danhMuc = db.DanhMucs.Single(x => x.Id == _danhMucId);
        var initialVersion = danhMuc.PhienBan;

        danhMuc.MoTa = "updated synchronously";
        db.SaveChanges();

        Assert.Equal(initialVersion + 1, danhMuc.PhienBan);
        using var verificationDb = csdl.TaoDbContext();
        Assert.Equal(initialVersion + 1,
            verificationDb.DanhMucs.AsNoTracking()
                .Single(x => x.Id == _danhMucId).PhienBan);
    }

    [Fact]
    public async Task Update_IncrementsVersionExactlyOnce()
    {
        await using var db = csdl.TaoDbContext();
        var danhMuc = await db.DanhMucs.SingleAsync(x => x.Id == _danhMucId);
        var initialVersion = danhMuc.PhienBan;

        danhMuc.MoTa = "updated";
        await db.SaveChangesAsync();

        Assert.Equal(initialVersion + 1, danhMuc.PhienBan);

        await using var verificationDb = csdl.TaoDbContext();
        var saved = await verificationDb.DanhMucs
            .AsNoTracking()
            .SingleAsync(x => x.Id == _danhMucId);
        Assert.Equal(initialVersion + 1, saved.PhienBan);
    }

    [Fact]
    public async Task SaveWithoutChanges_DoesNotIncrementVersion()
    {
        await using var db = csdl.TaoDbContext();
        var danhMuc = await db.DanhMucs.SingleAsync(x => x.Id == _danhMucId);
        var initialVersion = danhMuc.PhienBan;

        await db.SaveChangesAsync();

        Assert.Equal(initialVersion, danhMuc.PhienBan);
        await using var verificationDb = csdl.TaoDbContext();
        Assert.Equal(initialVersion,
            (await verificationDb.DanhMucs.AsNoTracking()
                .SingleAsync(x => x.Id == _danhMucId)).PhienBan);
    }

    [Fact]
    public async Task TwoContexts_StaleUpdateThrowsConcurrencyException()
    {
        await using var contextA = csdl.TaoDbContext();
        await using var contextB = csdl.TaoDbContext();
        var categoryA = await contextA.DanhMucs.SingleAsync(x => x.Id == _danhMucId);
        var categoryB = await contextB.DanhMucs.SingleAsync(x => x.Id == _danhMucId);

        categoryA.MoTa = "changed by A";
        await contextA.SaveChangesAsync();

        categoryB.MoTa = "changed by B";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => contextB.SaveChangesAsync());

        var versionAfterConflict = categoryB.PhienBan;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => contextB.SaveChangesAsync());
        Assert.Equal(versionAfterConflict, categoryB.PhienBan);

        await using var verificationDb = csdl.TaoDbContext();
        var saved = await verificationDb.DanhMucs.AsNoTracking()
            .SingleAsync(x => x.Id == _danhMucId);
        Assert.Equal("changed by A", saved.MoTa);
        Assert.Equal(categoryA.PhienBan, saved.PhienBan);
    }

    [Fact]
    public async Task TwoContexts_ReloadThenRetry_SucceedsWithoutOverwritingPriorChange()
    {
        await using var contextA = csdl.TaoDbContext();
        await using var contextB = csdl.TaoDbContext();
        var categoryA = await contextA.DanhMucs.SingleAsync(x => x.Id == _danhMucId);
        var categoryB = await contextB.DanhMucs.SingleAsync(x => x.Id == _danhMucId);
        var initialVersion = categoryA.PhienBan;

        categoryA.MoTa = "changed by A";
        await contextA.SaveChangesAsync();

        categoryB.KhongDoiTra = true;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => contextB.SaveChangesAsync());
        Assert.Equal(initialVersion + 1, categoryB.PhienBan);

        var entry = contextB.Entry(categoryB);
        var databaseValues = await entry.GetDatabaseValuesAsync();
        Assert.NotNull(databaseValues);
        entry.CurrentValues.SetValues(databaseValues);
        entry.OriginalValues.SetValues(databaseValues);
        categoryB.KhongDoiTra = true;

        await contextB.SaveChangesAsync();

        await using var verificationDb = csdl.TaoDbContext();
        var saved = await verificationDb.DanhMucs.AsNoTracking()
            .SingleAsync(x => x.Id == _danhMucId);
        Assert.Equal("changed by A", saved.MoTa);
        Assert.True(saved.KhongDoiTra);
        Assert.Equal(initialVersion + 2, saved.PhienBan);
        Assert.Equal(saved.PhienBan, categoryB.PhienBan);
    }
}
