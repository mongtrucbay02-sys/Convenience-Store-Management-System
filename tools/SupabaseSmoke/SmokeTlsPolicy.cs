using System.Net;
using Npgsql;

namespace SmartMart.SupabaseSmoke;

internal static class SmokeTlsPolicy
{
    internal static NpgsqlConnectionStringBuilder Configure(string input, SmokeReport report)
    {
        // Parsing errors are handled by the runner without exposing their messages.
        var settings = new NpgsqlConnectionStringBuilder(input);
        report.TlsMode = settings.SslMode.ToString();
        report.CertificateConfigured = !string.IsNullOrWhiteSpace(settings.RootCertificate);
        report.CertificateFileExists = report.CertificateConfigured && File.Exists(settings.RootCertificate);
        if (string.IsNullOrWhiteSpace(settings.Host))
            throw new SmokeValidationException("MissingHost");
        if (settings.TryGetValue("Trust Server Certificate", out var trust) && trust is true)
            throw new SmokeValidationException("CertificateBypassNotAllowed");

        if (!IsLocal(settings.Host))
        {
            if (settings.SslMode != SslMode.VerifyFull)
                throw new SmokeValidationException("RemoteTlsRequiresVerifyFull");
            if (!report.CertificateConfigured)
                throw new SmokeValidationException("MissingRootCertificate");
            if (!report.CertificateFileExists)
                throw new SmokeValidationException("RootCertificateNotFound");
            // Npgsql 10 otherwise prefers GSS encryption. This probe requires TLS.
            settings.GssEncryptionMode = GssEncryptionMode.Disable;
        }

        // Preserve the CA path and SSL mode. Npgsql validates the certificate and hostname.
        settings.Pooling = false;
        settings.Multiplexing = false;
        settings.SearchPath = "smartmart";
        settings.Timezone = "Asia/Ho_Chi_Minh";
        settings.Timeout = 25;
        settings.CommandTimeout = 20;
        settings.Options = "-c default_transaction_read_only=on";
        settings.IncludeErrorDetail = false;
        settings.LogParameters = false;
        settings.PersistSecurityInfo = false;
        settings.ApplicationName = "SmartMart-Supabase-ReadOnly-Smoke";
        return settings;
    }

    private static bool IsLocal(string host) =>
        host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
        || (IPAddress.TryParse(host, out var address) && IPAddress.IsLoopback(address));
}

internal sealed class SmokeValidationException(string code) : Exception(code)
{
    internal string Code { get; } = code;
}
