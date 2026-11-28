using System.Text;
using System.Text.RegularExpressions;

namespace ConvenienceStore.Web.Tests.HaTang;

// Only parses the checked-in schema_v1.sql format, not arbitrary SQL.
internal static class SchemaReference
{
    internal static readonly string Sql = File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "SchemaReference", "schema_v1.sql"));

    internal static string[] Names(string pattern) => Regex.Matches(Sql, pattern,
        RegexOptions.IgnoreCase).Select(m => m.Groups[1].Value).Order().ToArray();

    internal static string[] Tables => Names(@"CREATE TABLE\s+(\w+)");
    internal static string[] Views => Names(@"CREATE VIEW\s+(\w+)");
    internal static string[] Checks => Names(@"CONSTRAINT\s+(\w+)\s+CHECK\s*\(");
    internal static string[] Triggers => Regex.Matches(Sql,
        @"CREATE TRIGGER\s+(\w+)\s+(BEFORE|AFTER)\s+(UPDATE|DELETE|INSERT)\s+ON\s+(\w+)",
        RegexOptions.IgnoreCase).Select(m =>
            $"{m.Groups[1]}|{m.Groups[2]}|{m.Groups[3]}|{m.Groups[4]}").Order().ToArray();

    internal static IEnumerable<string> CreateStatements()
    {
        var delimiter = ";";
        var statement = new StringBuilder();
        foreach (var line in Sql.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith("--")) continue;
            if (trimmed.StartsWith("DELIMITER "))
            {
                delimiter = trimmed[10..].Trim();
                continue;
            }
            statement.AppendLine(line);
            if (!trimmed.EndsWith(delimiter, StringComparison.Ordinal)) continue;
            var command = statement.ToString().Trim();
            command = command[..^delimiter.Length];
            if (command.StartsWith("CREATE ", StringComparison.OrdinalIgnoreCase))
                yield return command;
            statement.Clear();
        }
    }

    internal static string Normalize(string sql) => Regex.Replace(
        Regex.Replace(sql, @"(?m)^\s*--[^\r\n]*", ""), @"\s+", " ").Trim();
}
