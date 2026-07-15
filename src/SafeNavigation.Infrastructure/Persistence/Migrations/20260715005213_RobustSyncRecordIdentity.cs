using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeNavigation.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RobustSyncRecordIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "client_record_id",
                table: "domain_accesses",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "client_record_id",
                table: "block_attempts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "client_record_id",
                table: "app_usage",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql("UPDATE domain_accesses SET client_record_id = id;");
            migrationBuilder.Sql("UPDATE block_attempts SET client_record_id = id;");
            migrationBuilder.Sql("UPDATE app_usage SET client_record_id = id;");
            migrationBuilder.Sql("ALTER TABLE domain_accesses ALTER COLUMN client_record_id DROP DEFAULT;");
            migrationBuilder.Sql("ALTER TABLE block_attempts ALTER COLUMN client_record_id DROP DEFAULT;");
            migrationBuilder.Sql("ALTER TABLE app_usage ALTER COLUMN client_record_id DROP DEFAULT;");

            migrationBuilder.CreateIndex(
                name: "ix_domain_accesses_device_id_client_record_id",
                table: "domain_accesses",
                columns: new[] { "device_id", "client_record_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_block_attempts_device_id_client_record_id",
                table: "block_attempts",
                columns: new[] { "device_id", "client_record_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_app_usage_device_id_client_record_id",
                table: "app_usage",
                columns: new[] { "device_id", "client_record_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_domain_accesses_device_id_client_record_id",
                table: "domain_accesses");

            migrationBuilder.DropIndex(
                name: "ix_block_attempts_device_id_client_record_id",
                table: "block_attempts");

            migrationBuilder.DropIndex(
                name: "ix_app_usage_device_id_client_record_id",
                table: "app_usage");

            migrationBuilder.DropColumn(
                name: "client_record_id",
                table: "domain_accesses");

            migrationBuilder.DropColumn(
                name: "client_record_id",
                table: "block_attempts");

            migrationBuilder.DropColumn(
                name: "client_record_id",
                table: "app_usage");
        }
    }
}
