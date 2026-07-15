using Npgsql;
using SafeNavigation.Infrastructure;

namespace SafeNavigation.UnitTests;

public sealed class DatabaseConnectionStringFactoryTests
{
    [Fact]
    public void Create_UsesVerifyFullByDefault()
    {
        var connectionString = DatabaseConnectionStringFactory.Create(
            "postgresql://user:password@database.example.com:5432/navigation",
            null);

        var parsed = new NpgsqlConnectionStringBuilder(connectionString);

        Assert.Equal("database.example.com", parsed.Host);
        Assert.Equal("navigation", parsed.Database);
        Assert.Equal(SslMode.VerifyFull, parsed.SslMode);
    }

    [Fact]
    public void Create_UsesRequiredTlsForPrivateRenderEndpoint()
    {
        var connectionString = DatabaseConnectionStringFactory.Create(
            "postgresql://user:p%40ss%3Bword@dpg-private-a/navigation",
            "Require");

        var parsed = new NpgsqlConnectionStringBuilder(connectionString);

        Assert.Equal("p@ss;word", parsed.Password);
        Assert.Equal(SslMode.Require, parsed.SslMode);
    }

    [Fact]
    public void Create_RejectsInsecureSslMode()
    {
        var action = () => DatabaseConnectionStringFactory.Create(
            "postgresql://user:password@database.example.com/navigation",
            "Disable");

        var exception = Assert.Throws<InvalidOperationException>(action);
        Assert.Contains("Database:SslMode", exception.Message);
    }
}
