using System.Net;
using Npgsql;

namespace SafeNavigation.Infrastructure;

public static class DatabaseConnectionStringFactory
{
    public static string? Create(string? databaseUrl, string? sslMode)
    {
        if (string.IsNullOrWhiteSpace(databaseUrl)) return null;

        var uri = new Uri(databaseUrl);
        if (uri.Scheme is not ("postgres" or "postgresql")) return databaseUrl;

        var credentials = uri.UserInfo.Split(':', 2);
        var mode = ParseSslMode(sslMode);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = WebUtility.UrlDecode(credentials[0]),
            Password = credentials.Length > 1 ? WebUtility.UrlDecode(credentials[1]) : string.Empty,
            SslMode = mode
        };
        return builder.ConnectionString;
    }

    private static SslMode ParseSslMode(string? sslMode)
    {
        var configuredMode = string.IsNullOrWhiteSpace(sslMode) ? nameof(SslMode.VerifyFull) : sslMode;
        if (Enum.TryParse<SslMode>(configuredMode, true, out var parsedMode) &&
            parsedMode is SslMode.Require or SslMode.VerifyCA or SslMode.VerifyFull)
        {
            return parsedMode;
        }

        throw new InvalidOperationException(
            "Database:SslMode must be Require, VerifyCA, or VerifyFull.");
    }
}
