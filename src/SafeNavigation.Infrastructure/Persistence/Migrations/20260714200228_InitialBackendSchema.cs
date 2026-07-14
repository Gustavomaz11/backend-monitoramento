using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SafeNavigation.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialBackendSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateTable(
                name: "alerts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    guardian_id = table.Column<Guid>(type: "uuid", nullable: false),
                    child_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    alert_type = table.Column<string>(type: "text", nullable: false),
                    severity = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    summary = table.Column<string>(type: "text", nullable: false),
                    related_entity_type = table.Column<string>(type: "text", nullable: true),
                    related_entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_alerts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_type = table.Column<string>(type: "text", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "text", nullable: false),
                    entity_type = table.Column<string>(type: "text", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: true),
                    ip_hash = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "blocking_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    guardian_id = table.Column<Guid>(type: "uuid", nullable: false),
                    child_id = table.Column<Guid>(type: "uuid", nullable: true),
                    device_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rule_type = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: false),
                    action = table.Column<string>(type: "text", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_blocking_rules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "domain_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    risk_level = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_domain_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "guardians",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "citext", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_guardians", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "children",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    guardian_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    birth_year = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_children", x => x.id);
                    table.ForeignKey(
                        name: "fk_children_guardians_guardian_id",
                        column: x => x.guardian_id,
                        principalTable: "guardians",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pairing_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    guardian_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code_hash = table.Column<string>(type: "text", nullable: false),
                    child_display_name = table.Column<string>(type: "text", nullable: false),
                    device_name = table.Column<string>(type: "text", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pairing_codes", x => x.id);
                    table.ForeignKey(
                        name: "fk_pairing_codes_guardians_guardian_id",
                        column: x => x.guardian_id,
                        principalTable: "guardians",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    guardian_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_guardians_guardian_id",
                        column: x => x.guardian_id,
                        principalTable: "guardians",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "devices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    child_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_public_id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    platform = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    app_version = table.Column<string>(type: "text", nullable: true),
                    android_version = table.Column<string>(type: "text", nullable: true),
                    manufacturer = table.Column<string>(type: "text", nullable: true),
                    model = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    last_sync_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_devices", x => x.id);
                    table.ForeignKey(
                        name: "fk_devices_children_child_id",
                        column: x => x.child_id,
                        principalTable: "children",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "app_usage",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    package_name = table.Column<string>(type: "text", nullable: false),
                    app_name = table.Column<string>(type: "text", nullable: true),
                    usage_date = table.Column<DateOnly>(type: "date", nullable: false),
                    total_foreground_ms = table.Column<long>(type: "bigint", nullable: false),
                    first_used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    open_count_estimate = table.Column<int>(type: "integer", nullable: false),
                    source = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_usage", x => x.id);
                    table.ForeignKey(
                        name: "fk_app_usage_devices_device_id",
                        column: x => x.device_id,
                        principalTable: "devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "block_attempts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    blocking_rule_id = table.Column<Guid>(type: "uuid", nullable: true),
                    domain = table.Column<string>(type: "text", nullable: true),
                    ip_address = table.Column<string>(type: "text", nullable: true),
                    protocol = table.Column<string>(type: "text", nullable: false),
                    port = table.Column<int>(type: "integer", nullable: true),
                    attempted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    foreground_package_name = table.Column<string>(type: "text", nullable: true),
                    correlation_type = table.Column<string>(type: "text", nullable: false),
                    child_request_status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_block_attempts", x => x.id);
                    table.ForeignKey(
                        name: "fk_block_attempts_devices_device_id",
                        column: x => x.device_id,
                        principalTable: "devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "device_configs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    retention_days = table.Column<int>(type: "integer", nullable: false),
                    vpn_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    usage_stats_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    sync_interval_minutes = table.Column<int>(type: "integer", nullable: false),
                    timezone = table.Column<string>(type: "text", nullable: false),
                    config_version = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_device_configs", x => x.id);
                    table.ForeignKey(
                        name: "fk_device_configs_devices_device_id",
                        column: x => x.device_id,
                        principalTable: "devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "device_refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_device_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_device_refresh_tokens_devices_device_id",
                        column: x => x.device_id,
                        principalTable: "devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "domain_accesses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    domain = table.Column<string>(type: "text", nullable: true),
                    ip_address = table.Column<string>(type: "text", nullable: true),
                    protocol = table.Column<string>(type: "text", nullable: false),
                    port = table.Column<int>(type: "integer", nullable: true),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    first_access_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_access_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    access_count = table.Column<int>(type: "integer", nullable: false),
                    foreground_package_name = table.Column<string>(type: "text", nullable: true),
                    correlation_type = table.Column<string>(type: "text", nullable: false),
                    source = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_domain_accesses", x => x.id);
                    table.ForeignKey(
                        name: "fk_domain_accesses_devices_device_id",
                        column: x => x.device_id,
                        principalTable: "devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_domain_accesses_domain_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "domain_categories",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "sync_batches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    records_received = table.Column<int>(type: "integer", nullable: false),
                    records_accepted = table.Column<int>(type: "integer", nullable: false),
                    error_summary = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sync_batches", x => x.id);
                    table.ForeignKey(
                        name: "fk_sync_batches_devices_device_id",
                        column: x => x.device_id,
                        principalTable: "devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "domain_categories",
                columns: new[] { "id", "display_name", "name", "risk_level" },
                values: new object[,]
                {
                    { new Guid("aaaaaaaa-0000-0000-0000-000000000001"), "Redes sociais", "social", 1 },
                    { new Guid("aaaaaaaa-0000-0000-0000-000000000002"), "Videos", "video", 1 },
                    { new Guid("aaaaaaaa-0000-0000-0000-000000000003"), "Jogos", "games", 1 },
                    { new Guid("aaaaaaaa-0000-0000-0000-000000000004"), "Educacao", "education", 0 },
                    { new Guid("aaaaaaaa-0000-0000-0000-000000000005"), "Noticias", "news", 1 },
                    { new Guid("aaaaaaaa-0000-0000-0000-000000000006"), "Conteudo adulto", "adult", 5 },
                    { new Guid("aaaaaaaa-0000-0000-0000-000000000007"), "Apostas", "gambling", 5 },
                    { new Guid("aaaaaaaa-0000-0000-0000-000000000008"), "Sites desconhecidos", "unknown", 2 },
                    { new Guid("aaaaaaaa-0000-0000-0000-000000000009"), "Malicioso", "malicious", 5 },
                    { new Guid("aaaaaaaa-0000-0000-0000-000000000010"), "Violencia explicita", "violence", 5 }
                });

            migrationBuilder.CreateIndex(
                name: "ix_alerts_guardian_id_status_created_at",
                table: "alerts",
                columns: new[] { "guardian_id", "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_app_usage_device_id_package_name_usage_date",
                table: "app_usage",
                columns: new[] { "device_id", "package_name", "usage_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_app_usage_device_id_usage_date",
                table: "app_usage",
                columns: new[] { "device_id", "usage_date" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_created_at",
                table: "audit_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_block_attempts_device_id_attempted_at",
                table: "block_attempts",
                columns: new[] { "device_id", "attempted_at" });

            migrationBuilder.CreateIndex(
                name: "ix_blocking_rules_child_id_enabled",
                table: "blocking_rules",
                columns: new[] { "child_id", "enabled" });

            migrationBuilder.CreateIndex(
                name: "ix_blocking_rules_device_id_enabled",
                table: "blocking_rules",
                columns: new[] { "device_id", "enabled" });

            migrationBuilder.CreateIndex(
                name: "ix_blocking_rules_guardian_id",
                table: "blocking_rules",
                column: "guardian_id");

            migrationBuilder.CreateIndex(
                name: "ix_children_guardian_id",
                table: "children",
                column: "guardian_id");

            migrationBuilder.CreateIndex(
                name: "ix_device_configs_device_id",
                table: "device_configs",
                column: "device_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_device_refresh_tokens_device_id",
                table: "device_refresh_tokens",
                column: "device_id");

            migrationBuilder.CreateIndex(
                name: "ix_device_refresh_tokens_token_hash",
                table: "device_refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_devices_child_id",
                table: "devices",
                column: "child_id");

            migrationBuilder.CreateIndex(
                name: "ix_devices_device_public_id",
                table: "devices",
                column: "device_public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_devices_last_sync_at",
                table: "devices",
                column: "last_sync_at");

            migrationBuilder.CreateIndex(
                name: "ix_domain_accesses_category_id",
                table: "domain_accesses",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_domain_accesses_device_id_domain_last_access_at",
                table: "domain_accesses",
                columns: new[] { "device_id", "domain", "last_access_at" });

            migrationBuilder.CreateIndex(
                name: "ix_domain_accesses_device_id_ip_address_last_access_at",
                table: "domain_accesses",
                columns: new[] { "device_id", "ip_address", "last_access_at" });

            migrationBuilder.CreateIndex(
                name: "ix_domain_categories_name",
                table: "domain_categories",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_guardians_email",
                table: "guardians",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pairing_codes_code_hash",
                table: "pairing_codes",
                column: "code_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pairing_codes_guardian_id_expires_at",
                table: "pairing_codes",
                columns: new[] { "guardian_id", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_guardian_id",
                table: "refresh_tokens",
                column: "guardian_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token_hash",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sync_batches_device_id_client_batch_id",
                table: "sync_batches",
                columns: new[] { "device_id", "client_batch_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alerts");

            migrationBuilder.DropTable(
                name: "app_usage");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "block_attempts");

            migrationBuilder.DropTable(
                name: "blocking_rules");

            migrationBuilder.DropTable(
                name: "device_configs");

            migrationBuilder.DropTable(
                name: "device_refresh_tokens");

            migrationBuilder.DropTable(
                name: "domain_accesses");

            migrationBuilder.DropTable(
                name: "pairing_codes");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "sync_batches");

            migrationBuilder.DropTable(
                name: "domain_categories");

            migrationBuilder.DropTable(
                name: "devices");

            migrationBuilder.DropTable(
                name: "children");

            migrationBuilder.DropTable(
                name: "guardians");
        }
    }
}
