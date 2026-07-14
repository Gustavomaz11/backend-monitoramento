using Microsoft.EntityFrameworkCore;
using SafeNavigation.Domain.Entities;

namespace SafeNavigation.Application.Abstractions;

public interface ISafeNavigationDbContext
{
    DbSet<Guardian> Guardians { get; }
    DbSet<Child> Children { get; }
    DbSet<Device> Devices { get; }
    DbSet<DeviceConfig> DeviceConfigs { get; }
    DbSet<PairingCode> PairingCodes { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<DeviceRefreshToken> DeviceRefreshTokens { get; }
    DbSet<AppUsage> AppUsages { get; }
    DbSet<DomainAccess> DomainAccesses { get; }
    DbSet<BlockAttempt> BlockAttempts { get; }
    DbSet<BlockingRule> BlockingRules { get; }
    DbSet<Alert> Alerts { get; }
    DbSet<UnblockRequest> UnblockRequests { get; }
    DbSet<PrivacyRequest> PrivacyRequests { get; }
    DbSet<SyncBatch> SyncBatches { get; }
    DbSet<AuditLog> AuditLogs { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
