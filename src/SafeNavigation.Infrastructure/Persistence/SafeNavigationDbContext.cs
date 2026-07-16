using Microsoft.EntityFrameworkCore;
using SafeNavigation.Application.Abstractions;
using SafeNavigation.Domain.Entities;

namespace SafeNavigation.Infrastructure.Persistence;

public sealed class SafeNavigationDbContext(DbContextOptions<SafeNavigationDbContext> options)
    : DbContext(options), ISafeNavigationDbContext
{
    public DbSet<Guardian> Guardians => Set<Guardian>();
    public DbSet<Child> Children => Set<Child>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<DeviceConfig> DeviceConfigs => Set<DeviceConfig>();
    public DbSet<PairingCode> PairingCodes => Set<PairingCode>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<DeviceRefreshToken> DeviceRefreshTokens => Set<DeviceRefreshToken>();
    public DbSet<AppUsage> AppUsages => Set<AppUsage>();
    public DbSet<DomainCategory> DomainCategories => Set<DomainCategory>();
    public DbSet<DomainAccess> DomainAccesses => Set<DomainAccess>();
    public DbSet<BlockingRule> BlockingRules => Set<BlockingRule>();
    public DbSet<BlockAttempt> BlockAttempts => Set<BlockAttempt>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<UnblockRequest> UnblockRequests => Set<UnblockRequest>();
    public DbSet<PrivacyRequest> PrivacyRequests => Set<PrivacyRequest>();
    public DbSet<SyncBatch> SyncBatches => Set<SyncBatch>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("citext");

        modelBuilder.Entity<Guardian>(entity =>
        {
            entity.ToTable("guardians");
            entity.Property(x => x.Email).HasColumnType("citext");
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.Status).HasMaxLength(40);
        });

        modelBuilder.Entity<Child>(entity =>
        {
            entity.ToTable("children");
            entity.HasOne(x => x.Guardian).WithMany(x => x.Children).HasForeignKey(x => x.GuardianId);
            entity.HasIndex(x => x.GuardianId);
        });

        modelBuilder.Entity<Device>(entity =>
        {
            entity.ToTable("devices");
            entity.HasOne(x => x.Child).WithMany(x => x.Devices).HasForeignKey(x => x.ChildId);
            entity.HasIndex(x => x.ChildId);
            entity.HasIndex(x => x.DevicePublicId).IsUnique();
            entity.HasIndex(x => x.LastSyncAt);
            entity.Property(x => x.Platform).HasMaxLength(40);
            entity.Property(x => x.Status).HasMaxLength(40);
        });

        modelBuilder.Entity<DeviceConfig>(entity =>
        {
            entity.ToTable("device_configs");
            entity.HasOne(x => x.Device).WithOne(x => x.Config).HasForeignKey<DeviceConfig>(x => x.DeviceId);
            entity.HasIndex(x => x.DeviceId).IsUnique();
            entity.Property(x => x.UsageScheduleJson).HasColumnType("jsonb");
        });

        modelBuilder.Entity<PairingCode>(entity =>
        {
            entity.ToTable("pairing_codes");
            entity.HasIndex(x => x.CodeHash).IsUnique();
            entity.HasIndex(x => new { x.GuardianId, x.ExpiresAt });
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasOne(x => x.Guardian).WithMany(x => x.RefreshTokens).HasForeignKey(x => x.GuardianId);
            entity.HasIndex(x => x.TokenHash).IsUnique();
        });

        modelBuilder.Entity<DeviceRefreshToken>(entity =>
        {
            entity.ToTable("device_refresh_tokens");
            entity.HasOne(x => x.Device).WithMany(x => x.RefreshTokens).HasForeignKey(x => x.DeviceId);
            entity.HasIndex(x => x.TokenHash).IsUnique();
        });

        modelBuilder.Entity<AppUsage>(entity =>
        {
            entity.ToTable("app_usage");
            entity.HasIndex(x => new { x.DeviceId, x.ClientRecordId }).IsUnique();
            entity.HasIndex(x => new { x.DeviceId, x.PackageName, x.UsageDate }).IsUnique();
            entity.HasIndex(x => new { x.DeviceId, x.UsageDate });
        });

        modelBuilder.Entity<DomainCategory>(entity =>
        {
            entity.ToTable("domain_categories");
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasData(
                new DomainCategory { Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"), Name = "social", DisplayName = "Redes sociais", RiskLevel = 1 },
                new DomainCategory { Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002"), Name = "video", DisplayName = "Videos", RiskLevel = 1 },
                new DomainCategory { Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003"), Name = "games", DisplayName = "Jogos", RiskLevel = 1 },
                new DomainCategory { Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000004"), Name = "education", DisplayName = "Educacao", RiskLevel = 0 },
                new DomainCategory { Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000005"), Name = "news", DisplayName = "Noticias", RiskLevel = 1 },
                new DomainCategory { Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000006"), Name = "adult", DisplayName = "Conteudo adulto", RiskLevel = 5 },
                new DomainCategory { Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000007"), Name = "gambling", DisplayName = "Apostas", RiskLevel = 5 },
                new DomainCategory { Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000008"), Name = "unknown", DisplayName = "Sites desconhecidos", RiskLevel = 2 },
                new DomainCategory { Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000009"), Name = "malicious", DisplayName = "Malicioso", RiskLevel = 5 },
                new DomainCategory { Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000010"), Name = "violence", DisplayName = "Violencia explicita", RiskLevel = 5 });
        });

        modelBuilder.Entity<DomainAccess>(entity =>
        {
            entity.ToTable("domain_accesses");
            entity.HasIndex(x => new { x.DeviceId, x.ClientRecordId }).IsUnique();
            entity.HasIndex(x => new { x.DeviceId, x.Domain, x.LastAccessAt });
            entity.HasIndex(x => new { x.DeviceId, x.IpAddress, x.LastAccessAt });
        });

        modelBuilder.Entity<BlockingRule>(entity =>
        {
            entity.ToTable("blocking_rules");
            entity.HasIndex(x => x.GuardianId);
            entity.HasIndex(x => new { x.ChildId, x.Enabled });
            entity.HasIndex(x => new { x.DeviceId, x.Enabled });
        });

        modelBuilder.Entity<BlockAttempt>(entity =>
        {
            entity.ToTable("block_attempts");
            entity.HasIndex(x => new { x.DeviceId, x.ClientRecordId }).IsUnique();
            entity.HasIndex(x => new { x.DeviceId, x.AttemptedAt });
        });

        modelBuilder.Entity<Alert>(entity =>
        {
            entity.ToTable("alerts");
            entity.HasIndex(x => new { x.GuardianId, x.Status, x.CreatedAt });
        });

        modelBuilder.Entity<UnblockRequest>(entity =>
        {
            entity.ToTable("unblock_requests");
            entity.HasOne(x => x.Device).WithMany().HasForeignKey(x => x.DeviceId);
            entity.HasIndex(x => new { x.DeviceId, x.Status, x.CreatedAt });
            entity.HasIndex(x => x.Domain);
        });

        modelBuilder.Entity<PrivacyRequest>(entity =>
        {
            entity.ToTable("privacy_requests");
            entity.HasIndex(x => new { x.GuardianId, x.RequestType, x.CreatedAt });
        });

        modelBuilder.Entity<SyncBatch>(entity =>
        {
            entity.ToTable("sync_batches");
            entity.HasIndex(x => new { x.DeviceId, x.ClientBatchId }).IsUnique();
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.HasIndex(x => x.CreatedAt);
            entity.Property(x => x.MetadataJson).HasColumnType("jsonb");
        });
    }
}
