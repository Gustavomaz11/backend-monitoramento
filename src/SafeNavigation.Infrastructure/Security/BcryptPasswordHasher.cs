using SafeNavigation.Application.Abstractions;

namespace SafeNavigation.Infrastructure.Security;

public sealed class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string value) => BCrypt.Net.BCrypt.HashPassword(value, workFactor: 12);

    public bool Verify(string value, string hash) => BCrypt.Net.BCrypt.Verify(value, hash);
}
