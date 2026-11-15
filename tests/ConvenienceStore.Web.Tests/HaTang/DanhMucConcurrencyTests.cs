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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExplicitBusinessModified_IncrementsVersionEvenWhenLocalValuesEqual(bool asynchronous)
    {
        await using var db = csdl.TaoDbContext();
        var category = await db.DanhMucs.SingleAsync(x => x.Id == _danhMucId);
        var original = category.PhienBan;
        // Preserve explicit write intent: detached updates also have equal local originals.
        db.Entry(category).Property(x => x.MoTa).IsModified = true;
        db.Entry(category).Property(x => x.PhienBan).IsModified = true;
        if (asynchronous) await db.SaveChangesAsync();
        else db.SaveChanges();

        Assert.Equal(original + 1, category.PhienBan);
        await using var verify = csdl.TaoDbContext();
        Assert.Equal(original + 1, (await verify.DanhMucs.AsNoTracking()
            .SingleAsync(x => x.Id == _danhMucId)).PhienBan);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DetachedUpdate_IncrementsOnceAndRejectsStaleWriter(bool asynchronous)
    {
        await using var read = csdl.TaoDbContext();
        var first = await read.DanhMucs.AsNoTracking().SingleAsync(x => x.Id == _danhMucId);
        var stale = await read.DanhMucs.AsNoTracking().SingleAsync(x => x.Id == _danhMucId);
        var original = first.PhienBan;
        first.MoTa = "detached winner";
        await using var write = csdl.TaoDbContext();
        write.DanhMucs.Update(first);
        if (asynchronous) await write.SaveChangesAsync();
        else write.SaveChanges();
        Assert.Equal(original + 1, first.PhienBan);

        stale.MoTa = "detached stale";
        await using var conflicting = csdl.TaoDbContext();
        conflicting.DanhMucs.Update(stale);
        if (asynchronous)
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => conflicting.SaveChangesAsync());
        else
            Assert.Throws<DbUpdateConcurrencyException>(() => conflicting.SaveChanges());
        Assert.Equal(original + 1, stale.PhienBan);
        await using var verify = csdl.TaoDbContext();
        var saved = await verify.DanhMucs.AsNoTracking().SingleAsync(x => x.Id == _danhMucId);
        Assert.Equal("detached winner", saved.MoTa);
        Assert.Equal(original + 1, saved.PhienBan);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TrackedValuesRestoredBeforeSave_DoNotIncrementVersion(bool asynchronous)
    {
        await using var db = csdl.TaoDbContext();
        var category = await db.DanhMucs.SingleAsync(x => x.Id == _danhMucId);
        var original = category.PhienBan;
        var description = category.MoTa;
        category.MoTa = "temporary";
        category.MoTa = description;
        if (asynchronous) await db.SaveChangesAsync();
        else db.SaveChanges();
        Assert.Equal(original, category.PhienBan);
        await using var verify = csdl.TaoDbContext();
        Assert.Equal(original, (await verify.DanhMucs.AsNoTracking()
            .SingleAsync(x => x.Id == _danhMucId)).PhienBan);
    }

    [Fact]
    public void TwoContexts_SynchronousConflictAndRetry_DoNotIncrementTwice()
    {
        using var first = csdl.TaoDbContext();
        using var second = csdl.TaoDbContext();
        var a = first.DanhMucs.Single(x => x.Id == _danhMucId);
        var b = second.DanhMucs.Single(x => x.Id == _danhMucId);
        a.MoTa = "sync winner";
        first.SaveChanges();
        b.MoTa = "sync stale";
        Assert.Throws<DbUpdateConcurrencyException>(() => second.SaveChanges());
        var attemptedVersion = b.PhienBan;
        Assert.Throws<DbUpdateConcurrencyException>(() => second.SaveChanges());
        Assert.Equal(attemptedVersion, b.PhienBan);
        using var verify = csdl.TaoDbContext();
        Assert.Equal("sync winner", verify.DanhMucs.Single(x => x.Id == _danhMucId).MoTa);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task VersionOnlyChange_DoesNotPersistClientSuppliedVersion(bool asynchronous)
    {
        await using var db = csdl.TaoDbContext();
        var category = await db.DanhMucs.SingleAsync(x => x.Id == _danhMucId);
        var original = category.PhienBan;
        category.PhienBan = original + 50;
        if (asynchronous) await db.SaveChangesAsync();
        else db.SaveChanges();
        Assert.Equal(original, category.PhienBan);
        await using var verify = csdl.TaoDbContext();
        Assert.Equal(original, (await verify.DanhMucs.AsNoTracking()
            .SingleAsync(x => x.Id == _danhMucId)).PhienBan);
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
