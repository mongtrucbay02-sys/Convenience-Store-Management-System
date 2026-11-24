using ConvenienceStore.Web.Data.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ConvenienceStore.Web.Data;

public partial class CuaHangDbContext
{
    // The frozen MySQL snapshot retains the approved physical types during the
    // transition to signed PostgreSQL CLR types. This does not run migrations.
    private static void ApplyLegacyMySqlStorageTypes(ModelBuilder modelBuilder)
    {
        var legacy = new CuaHangDbContextModelSnapshot().Model;
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        foreach (var property in entity.GetProperties())
        {
            var previous = legacy.FindEntityType(entity.Name)?.FindProperty(property.Name);
            if (previous is null) continue;
            property.SetColumnType(previous.GetColumnType());
            property.IsNullable = previous.IsNullable;
            var currentType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
            var previousType = Nullable.GetUnderlyingType(previous.ClrType) ?? previous.ClrType;
            if (currentType == typeof(short) && previousType == typeof(byte))
            {
                property.SetValueConverter(new ValueConverter<short, byte>(value => checked((byte)value), value => value));
                if (property.IsPrimaryKey()) property.ValueGenerated = previous.ValueGenerated;
            }
            else if (currentType == typeof(short) && previousType == typeof(ushort))
                property.SetValueConverter(new ValueConverter<short, ushort>(value => checked((ushort)value), value => checked((short)value)));
            else if (currentType == typeof(long) && previousType == typeof(uint))
                property.SetValueConverter(new ValueConverter<long, uint>(value => checked((uint)value), value => value));
            else if (currentType == typeof(long) && previousType == typeof(ulong))
                property.SetValueConverter(new ValueConverter<long, ulong>(value => checked((ulong)value), value => checked((long)value)));
            else if (currentType == typeof(DateOnly) && previousType == typeof(DateTime))
                property.SetValueConverter(new ValueConverter<DateOnly, DateTime>(
                    value => value.ToDateTime(TimeOnly.MinValue), value => DateOnly.FromDateTime(value)));
            else continue;
            // EF's signed default generator uses negative temporary keys, which
            // cannot pass a checked unsigned converter. Match the old unsigned
            // generator's high positive temporary range; never persist these IDs.
            if (currentType == typeof(long) && property.ValueGenerated == ValueGenerated.OnAdd && property.IsPrimaryKey())
                property.SetValueGeneratorFactory((_, _) => new LegacyUnsignedTemporaryKeyGenerator());
        }
    }

    private sealed class LegacyUnsignedTemporaryKeyGenerator : ValueGenerator<long>
    {
        private static long _next = uint.MaxValue;
        public override bool GeneratesTemporaryValues => true;
        public override long Next(EntityEntry entry) => Interlocked.Decrement(ref _next);
    }
}
