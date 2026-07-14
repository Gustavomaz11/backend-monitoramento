using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SafeNavigation.Application.Abstractions;
using SafeNavigation.Application.Errors;
using SafeNavigation.Application.Models;
using SafeNavigation.Domain.Entities;

namespace SafeNavigation.Application.Services;

public sealed class AuthService(
    ISafeNavigationDbContext db,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IClock clock,
    IOptions<AuthOptions> options)
{
    public async Task<AuthResponse> RegisterAsync(RegisterGuardianRequest request, CancellationToken cancellationToken)
    {
        if (!request.AcceptedTerms) throw new ValidationFailedException("Terms must be accepted.");

        var email = NormalizeEmail(request.Email);
        var exists = await db.Guardians.AnyAsync(x => x.Email == email, cancellationToken);
        if (exists) throw new ConflictException("Email already registered.");

        var guardian = new Guardian
        {
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            PasswordHash = passwordHasher.Hash(request.Password),
            CreatedAt = clock.UtcNow,
            UpdatedAt = clock.UtcNow
        };

        db.Guardians.Add(guardian);
        var response = await IssueGuardianTokensAsync(guardian, cancellationToken);
        await AddAuditAsync("guardian", guardian.Id, "guardian.registered", "guardian", guardian.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return response;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        var guardian = await db.Guardians.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (guardian is null || !passwordHasher.Verify(request.Password, guardian.PasswordHash))
        {
            throw new UnauthorizedOperationException("Invalid credentials.");
        }

        var response = await IssueGuardianTokensAsync(guardian, cancellationToken);
        await AddAuditAsync("guardian", guardian.Id, "guardian.login", "guardian", guardian.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return response;
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var tokenHash = tokenService.HashOpaqueToken(request.RefreshToken);
        var storedToken = await db.RefreshTokens
            .Include(x => x.Guardian)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null || storedToken.RevokedAt is not null || storedToken.ExpiresAt <= clock.UtcNow)
        {
            throw new UnauthorizedOperationException("Invalid refresh token.");
        }

        storedToken.RevokedAt = clock.UtcNow;
        var response = await IssueGuardianTokensAsync(storedToken.Guardian!, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return response;
    }

    public async Task LogoutAsync(string refreshToken, Guid guardianId, CancellationToken cancellationToken)
    {
        var tokenHash = tokenService.HashOpaqueToken(refreshToken);
        var storedToken = await db.RefreshTokens
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash && x.GuardianId == guardianId, cancellationToken);

        if (storedToken is null) return;

        storedToken.RevokedAt = clock.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    private Task<AuthResponse> IssueGuardianTokensAsync(Guardian guardian, CancellationToken cancellationToken)
    {
        var tokenPair = tokenService.CreateGuardianTokens(guardian);
        db.RefreshTokens.Add(new RefreshToken
        {
            Guardian = guardian,
            TokenHash = tokenService.HashOpaqueToken(tokenPair.RefreshToken),
            ExpiresAt = clock.UtcNow.AddDays(options.Value.RefreshTokenDays),
            CreatedAt = clock.UtcNow
        });

        return Task.FromResult(new AuthResponse(tokenPair.AccessToken, tokenPair.RefreshToken, tokenPair.ExpiresIn));
    }

    private async Task AddAuditAsync(string actorType, Guid actorId, string action, string entityType, Guid entityId, CancellationToken cancellationToken)
    {
        db.AuditLogs.Add(new AuditLog
        {
            ActorType = actorType,
            ActorId = actorId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            CreatedAt = clock.UtcNow
        });

        await Task.CompletedTask;
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}

public sealed class AuthOptions
{
    public int RefreshTokenDays { get; set; } = 30;
}
