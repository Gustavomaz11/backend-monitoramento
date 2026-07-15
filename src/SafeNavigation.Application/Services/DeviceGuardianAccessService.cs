using Microsoft.EntityFrameworkCore;
using SafeNavigation.Application.Abstractions;
using SafeNavigation.Application.Errors;
using SafeNavigation.Application.Models;

namespace SafeNavigation.Application.Services;

public sealed class DeviceGuardianAccessService(
    ISafeNavigationDbContext db,
    IPasswordHasher passwordHasher)
{
    public async Task VerifyAsync(
        Guid deviceId,
        GuardianCredentialVerificationRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var guardian = await db.Devices
            .Where(device => device.Id == deviceId && device.Status == "active")
            .Select(device => device.Child!.Guardian)
            .SingleOrDefaultAsync(cancellationToken);

        var passwordIsValid = guardian is not null &&
                              passwordHasher.Verify(request.Password, guardian.PasswordHash);
        var valid = guardian is not null &&
                    guardian.Status == "active" &&
                    guardian.Email == normalizedEmail &&
                    passwordIsValid;
        if (!valid) throw new ForbiddenOperationException("Guardian credentials were not accepted.");
    }
}
