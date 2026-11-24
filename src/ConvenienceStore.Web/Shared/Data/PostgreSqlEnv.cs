namespace ConvenienceStore.Web.Shared.Data;

internal static class PostgreSqlEnv
{
    public static string? GetConnectionString(string contentRoot, IConfiguration configuration)
        => GetValue(contentRoot, configuration, "ConnectionStrings__CuaHang", "ConnectionStrings:CuaHang");

    public static string GetProvider(string contentRoot, IConfiguration configuration)
    {
        var value = GetValue(contentRoot, configuration, "Database__Provider", "Database:Provider") ?? "PostgreSql";
        if (value.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase)) return "PostgreSql";
        throw new InvalidOperationException("Database:Provider must be PostgreSql; no fallback provider is supported.");
    }

    private static string? GetValue(string contentRoot, IConfiguration configuration, string key, string configurationKey)
    {
        var environmentValue = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrWhiteSpace(environmentValue))
            return environmentValue;

        var path = Path.Combine(contentRoot, ".env");
        if (File.Exists(path))
        {
            foreach (var line in File.ReadLines(path))
            {
                var entry = line.Trim();
                if (entry.Length == 0 || entry.StartsWith('#'))
                    continue;

                int separator = entry.IndexOf('=');
                if (separator < 0 || entry[..separator].Trim() != key)
                    continue;

                var value = entry[(separator + 1)..].Trim();
                if (value.Length >= 2 && value[0] is ('"' or '\'') && value[^1] == value[0])
                    value = value[1..^1];

                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }
        }

        return configuration[configurationKey];
    }
}
